﻿using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Renci.SshNet;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using System.Xml.Serialization;
using Tolk.BusinessLogic.Data;
using Tolk.BusinessLogic.Entities;
using Tolk.BusinessLogic.Helpers;
using Tolk.BusinessLogic.Models.Peppol;

namespace Tolk.BusinessLogic.Services
{
    public class PeppolService
    {
        private readonly ILogger<PeppolService> _logger;
        private readonly TolkOptions _options;
        private readonly ISwedishClock _clock;
        private const int NumberOfTries = 5;        

        public PeppolService(
            ILogger<PeppolService> logger,
            IOptions<TolkOptions> options,
            ISwedishClock clock)
        {
            _logger = logger;
            _options = options?.Value;
            _clock = clock;            
        }

        public async Task SendOrderAgreements()
        {
            using TolkDbContext context = _options.GetContext();

            await CreatePeppolEnvelopedMessagesToSend(context);            
            if (_options.Peppol.UsePeppol)
            {
                //then get all waiting to be sent(to also get previously failed)
                var peppolMessageIds = await context.OutboundPeppolMessages
                .Where(e => e.DeliveredAt == null && e.FailedTries < NumberOfTries && !e.IsHandling)
                .Select(e => e.OutboundPeppolMessageId)
                .ToListAsync();

                _logger.LogInformation("Found {count} peppol messages to send: {peppolMessageIds}",
                    peppolMessageIds.Count, string.Join(", ", peppolMessageIds));

                if (peppolMessageIds.Any())
                {
                    try
                    {
                        //Connect to the sftp
                        var sftpSettings = _options.Peppol.SftpSettings;
                        using SftpClient sftpClient = new SftpClient(sftpSettings.Host, sftpSettings.Port, sftpSettings.UserName, sftpSettings.Password);
                        sftpClient.Connect();
                        var peppolMessages = context.OutboundPeppolMessages
                            .Where(e => peppolMessageIds.Contains(e.OutboundPeppolMessageId) && e.IsHandling == false)
                            .Select(c => c);
                        await peppolMessages.ForEachAsync(c => c.IsHandling = true);
                        await context.SaveChangesAsync();
                        bool success = false;
                        string errorMessage = string.Empty;
                        foreach (var messageId in peppolMessageIds)
                        {
                            var message = await context.OutboundPeppolMessages.Include(opm => opm.PeppolMessagePayload)
                                .SingleOrDefaultAsync(e => e.OutboundPeppolMessageId == messageId && e.DeliveredAt == null);
                            try
                            {
                                if (message == null)
                                {
                                    _logger.LogInformation("Peppol message {messageId} was in list to be sent, but now appears to have been sent.", messageId);
                                }
                                else
                                {
                                    _logger.LogInformation("Sending peppol message {messageId}", messageId);
                                    try
                                    {
                                        //send file to peppol sftp
                                        var payloadToSend = _options.Peppol.UseEnvelope ? message.Payload : message.PeppolMessagePayload.Payload;
                                        using (MemoryStream ms = new MemoryStream(payloadToSend))                                        
                                        sftpClient.UploadFile(ms, $"{sftpSettings.UploadFolder}/{message.Identifier}.xml");                                                                                    
                                        success = true;
                                    }
                                    catch (Exception e)
                                    {
                                        success = false;
                                        _logger.LogWarning(e, "Peppol message {messageId} failed with the following status code: {statusCode}, try number {tries}", message.OutboundPeppolMessageId, 200/*response.StatusCode*/, message.FailedTries + 1);
                                        errorMessage = $"Call {message.OutboundPeppolMessageId} failed with the following status code: {200}, try number {message.FailedTries + 1}";
                                    }
                                }
                            }
                            catch (Exception ex)
                            {
                                _logger.LogError(ex, "Failure sending peppol message {messageId}", messageId);
                            }
                            finally
                            {
                                if (success)
                                {
                                    message.DeliveredAt = _clock.SwedenNow;
                                }
                                else
                                {
                                    message.FailedTries++;
                                    FailedPeppolMessage failedMessage = new FailedPeppolMessage
                                    {
                                        OutboundPeppolMessageId = message.OutboundPeppolMessageId,
                                        ErrorMessage = errorMessage,
                                        FailedAt = _clock.SwedenNow
                                    };
                                    context.FailedPeppolMessages.Add(failedMessage);
                                    if (message.FailedTries == NumberOfTries && message.NotificationType != Enums.NotificationType.ErrorNotification)
                                    {
                                        message.HasNotifiedFailure = false;
                                    }
                                }
                                message.IsHandling = false;
                                await context.SaveChangesAsync();
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Something went wrong when sending peppol messages");
                    }
                    finally
                    {
                        //Making sure no peppol messages are left hanging
                        var messages = context.OutboundPeppolMessages
                            .Where(e => peppolMessageIds.Contains(e.OutboundPeppolMessageId) && e.IsHandling == true)
                            .Select(c => c);
                        await messages.ForEachAsync(c => c.IsHandling = false);
                        await context.SaveChangesAsync();
                    }
                }
            }
        }

        private async Task CreatePeppolEnvelopedMessagesToSend(TolkDbContext context)
        {
            try
            {
                var peppolPayloads = await context.PeppolPayloads
                .Where(p => p.OutboundPeppolMessageId == null && p.ReplacedById == null)
                .Select(p => new
                {
                    p.PeppolPayloadId,
                    Reciever = p.Request.Order.CustomerOrganisation.PeppolId,
                    p.Payload,
                    p.PeppolMessageType
                })
                .ToListAsync();               
                foreach (var x in peppolPayloads)
                {
                    using var memoryStream = new MemoryStream();
                    using var writer = new StreamWriter(memoryStream, Encoding.UTF8);
                    var identfier = Guid.NewGuid().ToString();
                    var model = new StandardBusinessDocumentModel
                    {
                        StandardBusinessDocumentHeader = new StandardBusinessDocumentHeaderModel
                        {
                            Receiver = new PartnerModel(x.Reciever),
                            Sender = new PartnerModel(_options.Peppol.SenderIdentifier),
                            DocumentIdentification = new DocumentIdentificationModel
                            {
                                CreatedAt = DateTimeOffset.UtcNow,
                                InstanceIdentifier = identfier,
                                Type = "OrderResponse",
                                Standard = Constants.defaultNamespace
                            },
                            //BusinessScope = new BusinessScopeModel
                            //{
                            //    Scopes = ScopeModel.GetScopeModelForType(x.PeppolMessageType)
                            //}
                        },
                    };                          
                    SerializeEnvelopeAndAddPayload(model,
                        writer,x.Payload);
                    memoryStream.Position = 0;
                    byte[] byteArray = new byte[memoryStream.Length];
                    memoryStream.Read(byteArray, 0, (int)memoryStream.Length);
                    memoryStream.Close();
                    context.PeppolPayloads
                        .Where(p => p.PeppolPayloadId == x.PeppolPayloadId)
                        .Single().OutboundPeppolMessage = new OutboundPeppolMessage(identfier,
                           x.Reciever,
                           byteArray,
                           DateTimeOffset.UtcNow,
                           x.PeppolMessageType == Enums.PeppolMessageType.OrderAgreement ? Enums.NotificationType.OrderAgreementCreated : Enums.NotificationType.OrderResponseCreated
                        );
                }

                await context.SaveChangesAsync();
                _logger.LogInformation("Created {count} peppol messages to send.", peppolPayloads.Count);
            }
            catch (Exception ex)
            {
                //TOTOTOTOTOTOOTODO: Is this enough? Shouldn't this send som kind of email too?
                _logger.LogError(ex, "Something went wrong when creating peppol messages to send.");
            }
        }

        private void CreateAndSerializeEnvolpedPayload(List<object> peppolPayloads)
        {
            throw new NotImplementedException();
        }

        private static void SerializeEnvelopeAndAddPayload(StandardBusinessDocumentModel model, StreamWriter writer, byte[] payload)
        {
            XElement orderRoot;
            using (MemoryStream stream = new MemoryStream(payload))
            {
                var orderXML = XDocument.Load(stream);
                orderRoot = orderXML.Root;
            }

            var doc = new XDocument();
            using (var tempWriter = doc.CreateWriter())
            {
                XmlSerializerNamespaces ns = new XmlSerializerNamespaces();
                ns.Add(nameof(Constants.sh), Constants.sh);
                var xser = new XmlSerializer(typeof(StandardBusinessDocumentModel));
                xser.Serialize(tempWriter,model,ns);
            } 
            
            var docRoot = doc.Root;
            docRoot.Add(orderRoot);            
            docRoot.Save(writer);            
        }
    }
}
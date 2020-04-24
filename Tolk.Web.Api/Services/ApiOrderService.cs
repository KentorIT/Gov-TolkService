﻿using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Tolk.Api.Payloads.ApiPayloads;
using Tolk.Api.Payloads.Enums;
using Tolk.Api.Payloads.Responses;
using Tolk.Api.Payloads.WebHookPayloads;
using Tolk.BusinessLogic.Data;
using Tolk.BusinessLogic.Entities;
using Tolk.BusinessLogic.Enums;
using Tolk.BusinessLogic.Utilities;
using Tolk.Web.Api.Exceptions;
using Tolk.Web.Api.Helpers;

namespace Tolk.Web.Api.Services
{
    public class ApiOrderService
    {
        private readonly TolkDbContext _dbContext;
        private readonly ILogger _logger;

        public ApiOrderService(TolkDbContext dbContext, ILogger<ApiOrderService> logger)
        {
            _logger = logger;
            _dbContext = dbContext;
        }

        public async Task<Order> GetOrderAsync(string orderNumber, int brokerId)
        {
            var order = await _dbContext.Orders.GetOrderWithBrokerAndOrderNumber(orderNumber, brokerId);

            if (order == null)
            {
                _logger.LogWarning($"Broker with broker id {brokerId}, tried to get order {orderNumber}, but it could not be returned. This could happen if the order number is wrong, or that the broker has no request connected.");
                throw new InvalidApiCallException(ErrorCodes.OrderNotFound);
            }
            return order;
        }

        public async Task<OrderGroup> GetOrderGroupAsync(string orderGroupNumber, int brokerId)
        {
#warning include-fest
            var orderGroup = await _dbContext.OrderGroups
                .Include(o => o.RequestGroups).ThenInclude(r => r.Ranking)
                .SingleOrDefaultAsync(o => o.OrderGroupNumber == orderGroupNumber &&
                //Must have a request connected to the order for the broker, any status...
                o.RequestGroups.Any(r => r.Ranking.BrokerId == brokerId));
            if (orderGroup == null)
            {
                _logger.LogWarning($"Broker with broker id {brokerId}, tried to get order group {orderGroupNumber}, but it could not be returned. This could happen if the order group number is wrong, or that the broker has no request connected.");
                throw new InvalidApiCallException(ErrorCodes.OrderGroupNotFound);
            }
            return orderGroup;
        }

        public RequestDetailsResponse GetResponseFromRequest(Request request, bool getAttachments = true)
        {
            if (request == null)
            {
                return null;
            }
#warning Det small här för mig!!!!!
            request.RequirementAnswers = _dbContext.OrderRequirementRequestAnswer.GetRequirementAnswersForRequest(request.RequestId).ToList();
            request.PriceRows = _dbContext.RequestPriceRows.GetPriceRowsForRequest(request.RequestId).ToList();
            request.Order.Requirements = _dbContext.OrderRequirements.GetRequirementsForOrder(request.OrderId).ToList();
            request.Order.InterpreterLocations = _dbContext.OrderInterpreterLocation.GetOrderedInterpreterLocationsForOrder(request.OrderId).ToList();
            request.Order.CompetenceRequirements = _dbContext.OrderCompetenceRequirements.GetOrderedCompetenceRequirementsForOrder(request.OrderId).ToList();
            request.Order.PriceRows = _dbContext.OrderPriceRows.GetPriceRowsForOrder(request.OrderId).ToList();

            var attachments = getAttachments ? GetAttachments(request) : Enumerable.Empty<AttachmentInformationModel>();
            
            return new RequestDetailsResponse
            {
                Status = request.Status.GetCustomName(),
                StatusMessage = request.DenyMessage ?? request.CancelMessage,
                CreatedAt = request.CreatedAt,
                OrderNumber = request.Order.OrderNumber,
                CustomerInformation = new CustomerInformationModel
                {
                    Name = request.Order.CustomerOrganisation.Name,
                    Key = request.Order.CustomerOrganisation.OrganisationPrefix,
                    OrganisationNumber = request.Order.CustomerOrganisation.OrganisationNumber,
                    ContactName = request.Order.CreatedByUser.FullName,
                    ContactPhone = request.Order.ContactPhone,
                    ContactEmail = request.Order.ContactEmail,
                    InvoiceReference = request.Order.InvoiceReference,
                    PriceListType = request.Order.CustomerOrganisation.PriceListType.GetCustomName(),
                    TravelCostAgreementType = request.Order.CustomerOrganisation.TravelCostAgreementType.GetCustomName(),
                    ReferenceNumber = request.Order.CustomerReferenceNumber,
                    UnitName = request.Order.CustomerUnit?.Name,
                    DepartmentName = request.Order.UnitName,
                    UseSelfInvoicingInterpreter = request.Order.CustomerOrganisation.UseSelfInvoicingInterpreter
                },
                Region = request.Order.Region.Name,
                ExpiresAt = request.ExpiresAt,
                Language = new LanguageModel
                {
                    Key = request.Order.Language?.ISO_639_Code,
                    Description = request.Order.OtherLanguage ?? request.Order.Language.Name,
                },
                StartAt = request.Order.StartAt,
                EndAt = request.Order.EndAt,
                Locations = request.Order.InterpreterLocations.Select(l => new LocationModel
                {
                    OffsiteContactInformation = l.OffSiteContactInformation,
                    Street = l.Street,
                    City = l.City,
                    Rank = l.Rank,
                    Key = l.InterpreterLocation.GetCustomName()
                }),
                CompetenceLevels = request.Order.CompetenceRequirements.Select(c => new CompetenceModel
                {
                    Key = c.CompetenceLevel.GetCustomName(),
                    Rank = c.Rank ?? 0
                }),
                CompetenceLevelsAreRequired = request.Order.SpecificCompetenceLevelRequired,
                AllowMoreThanTwoHoursTravelTime = request.Order.AllowExceedingTravelCost == AllowExceedingTravelCost.YesShouldBeApproved || request.Order.AllowExceedingTravelCost == AllowExceedingTravelCost.YesShouldNotBeApproved,
                CreatorIsInterpreterUser = request.Order.CreatorIsInterpreterUser,
                MealBreakIncluded = request.Order.MealBreakIncluded,
                Description = request.Order.Description,
                AssignmentType = request.Order.AssignmentType.GetCustomName(),
                Attachments = attachments,
                Requirements = request.Order.Requirements.Select(r => new RequirementModel
                {
                    Description = r.Description,
                    IsRequired = r.IsRequired,
                    RequirementId = r.OrderRequirementId,
                    RequirementType = r.RequirementType.GetCustomName()
                }),
                CalculatedPriceInformationFromRequest = request.Order.PriceRows.GetPriceInformationModel(request.Order.PriceCalculatedFromCompetenceLevel.GetCustomName(), request.Ranking.BrokerFee),
                CalculatedPriceInformationFromAnswer = request.PriceRows.Any() ?
                    request.PriceRows.GetPriceInformationModel(((CompetenceAndSpecialistLevel)request.CompetenceLevel).GetCustomName(), request.Ranking.BrokerFee)
                    : null,
                Interpreter = request.Interpreter != null ? new InterpreterModel
                {
                    InterpreterId = request.Interpreter.InterpreterBrokerId,
                    Email = request.Interpreter.Email,
                    FirstName = request.Interpreter.FirstName,
                    LastName = request.Interpreter.LastName,
                    OfficialInterpreterId = request.Interpreter.OfficialInterpreterId,
                    PhoneNumber = request.Interpreter.PhoneNumber,
                    InterpreterInformationType = EnumHelper.GetCustomName(InterpreterInformationType.ExistingInterpreter)
                } : null,
                InterpreterLocation = request.InterpreterLocation.HasValue ? EnumHelper.GetCustomName((InterpreterLocation)request.InterpreterLocation) : null,
                InterpreterCompetenceLevel = request.CompetenceLevel.HasValue ? EnumHelper.GetCustomName((CompetenceAndSpecialistLevel)request.CompetenceLevel) : null,
                ExpectedTravelCosts = request.PriceRows.FirstOrDefault(pr => pr.PriceRowType == PriceRowType.TravelCost)?.Price ?? 0,
                ExpectedTravelCostInfo = request.ExpectedTravelCostInfo,
                RequirementAnswers = request.RequirementAnswers
                    .Select(r => new RequirementAnswerModel
                    {
                        Answer = r.Answer,
                        CanMeetRequirement = r.CanSatisfyRequirement,
                        RequirementId = r.OrderRequirementId
                    }),
            };
        }

        public RequestGroupDetailsResponse GetResponseFromRequestGroup(RequestGroup requestGroup)
        {
            if (requestGroup == null)
            {
                return null;
            }
            OrderGroup orderGroup = requestGroup.OrderGroup;
#warning include-fest
            var occasions = _dbContext.Requests
                .Include(r => r.Ranking).ThenInclude(r => r.Broker)
                .Include(r => r.RequirementAnswers)
                .Include(r => r.PriceRows).ThenInclude(p => p.PriceListRow)
                .Include(r => r.Interpreter)
                .Include(r => r.Order).ThenInclude(o => o.CreatedByUser)
                .Include(r => r.Order).ThenInclude(o => o.CustomerUnit)
                .Include(r => r.Order).ThenInclude(o => o.CustomerOrganisation)
                .Include(r => r.Order).ThenInclude(o => o.Region)
                .Include(r => r.Order).ThenInclude(o => o.Language)
                .Include(r => r.Order).ThenInclude(o => o.Requirements)
                .Include(r => r.Order).ThenInclude(o => o.InterpreterLocations)
                .Include(r => r.Order).ThenInclude(o => o.CompetenceRequirements)
                .Include(r => r.Order).ThenInclude(o => o.PriceRows).ThenInclude(p => p.PriceListRow)
                .Where(r => r.RequestGroupId == requestGroup.RequestGroupId && r.ReplacedByRequest == null)
                .Select(r => GetResponseFromRequest(r, false)).ToList();
            return new RequestGroupDetailsResponse
            {
                Status = requestGroup.Status.GetCustomName(),
                StatusMessage = requestGroup.DenyMessage ?? requestGroup.CancelMessage,
                CreatedAt = requestGroup.CreatedAt,
                OrderGroupNumber = orderGroup.OrderGroupNumber,
                Description = orderGroup.Orders.First().Description,
                CustomerInformation = new CustomerInformationModel
                {
                    Name = orderGroup.CustomerOrganisation.Name,
                    Key = orderGroup.CustomerOrganisation.OrganisationPrefix,
                    OrganisationNumber = orderGroup.CustomerOrganisation.OrganisationNumber,
                    ContactName = orderGroup.CreatedByUser.FullName,
                    ContactPhone = orderGroup.ContactPhone,
                    ContactEmail = orderGroup.ContactEmail,
                    PriceListType = orderGroup.CustomerOrganisation.PriceListType.GetCustomName(),
                    TravelCostAgreementType = orderGroup.CustomerOrganisation.TravelCostAgreementType.GetCustomName(),
                    UnitName = orderGroup.CustomerUnit?.Name,
                    UseSelfInvoicingInterpreter = orderGroup.CustomerOrganisation.UseSelfInvoicingInterpreter
                },
                Region = orderGroup.Region.Name,
                ExpiresAt = requestGroup.ExpiresAt,
                Language = new LanguageModel
                {
                    Key = orderGroup.Language?.ISO_639_Code,
                    Description = orderGroup.OtherLanguage ?? orderGroup.Language.Name,
                },
                Locations = orderGroup.InterpreterLocations.Select(l => new LocationModel
                {
                    Rank = l.Rank,
                    Key = l.InterpreterLocation.GetCustomName()
                }),
                CompetenceLevels = orderGroup.CompetenceRequirements.Select(c => new CompetenceModel
                {
                    Key = c.CompetenceLevel.GetCustomName(),
                    Rank = c.Rank ?? 0
                }),
                CompetenceLevelsAreRequired = orderGroup.SpecificCompetenceLevelRequired,
                AllowMoreThanTwoHoursTravelTime = orderGroup.AllowExceedingTravelCost == AllowExceedingTravelCost.YesShouldBeApproved || orderGroup.AllowExceedingTravelCost == AllowExceedingTravelCost.YesShouldNotBeApproved,
                CreatorIsInterpreterUser = orderGroup.CreatorIsInterpreterUser,
                AssignmentType = orderGroup.AssignmentType.GetCustomName(),
                Attachments = orderGroup.Attachments.Select(a => new AttachmentInformationModel
                {
                    AttachmentId = a.AttachmentId,
                    FileName = a.Attachment.FileName
                }).Union(requestGroup.Attachments.Select(a => new AttachmentInformationModel
                {
                    AttachmentId = a.Attachment.AttachmentId,
                    FileName = a.Attachment.FileName,
                })),
                Requirements = orderGroup.Requirements.Select(r => new RequirementModel
                {
                    Description = r.Description,
                    IsRequired = r.IsRequired,
                    RequirementId = r.OrderGroupRequirementId,
                    RequirementType = r.RequirementType.GetCustomName()
                }),
                Occasions = occasions
            };
        }

        private IEnumerable<AttachmentInformationModel> GetAttachments(Request request)
        {
            var attachments = _dbContext.Attachments.GetAttachmentsForOrderAndGroup(request.OrderId, request.Order.OrderGroupId).ToList();
            attachments.AddRange(_dbContext.Attachments.GetAttachmentsForRequest(request.RequestId, request.RequestGroupId).ToList());
            return attachments
                .Select(a => new AttachmentInformationModel
                {
                    AttachmentId = a.AttachmentId,
                    FileName = a.FileName,
                });
        }
    }
}

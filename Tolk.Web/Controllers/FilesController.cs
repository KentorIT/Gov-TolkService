using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Tolk.BusinessLogic.Data;
using Tolk.BusinessLogic.Entities;
using Tolk.BusinessLogic.Helpers;
using Tolk.BusinessLogic.Services;
using Tolk.BusinessLogic.Utilities;
using Tolk.Web.Authorization;
using Tolk.Web.Helpers;
using Tolk.Web.Models;

namespace Tolk.Web.Controllers
{
    public class FilesController : Controller
    {
        private readonly TolkDbContext _dbContext;
        private readonly ISwedishClock _clock;
        private readonly TolkOptions _options;
        private readonly IAuthorizationService _authorizationService;

        public FilesController(TolkDbContext dbContext, ISwedishClock clock, IOptions<TolkOptions> options, IAuthorizationService authorizationService)
        {
            _dbContext = dbContext;
            _clock = clock;
            _options = options.Value;
            _authorizationService = authorizationService;
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public JsonResult Upload(List<IFormFile> files, Guid? groupKey = null)
        {
            if (files == null)
            {
                return Json(new
                {
                    success = false,
                    ErrorMessage = $"Inga filer hittades för uppladdning."
                });
            }
            var list = new List<FileModel>();
            var uploadedLength = files.Sum(f => f.Length);
            if (uploadedLength < _options.CombinedMaxSizeAttachments && groupKey.HasValue)
            {
                uploadedLength += _dbContext.TemporaryAttachmentGroups.Where(t => t.TemporaryAttachmentGroupKey == groupKey).Sum(t => t.Attachment.Blob.Length);
            }
            if (uploadedLength > _options.CombinedMaxSizeAttachments)
            {
                //log here as well
                return Json(new
                {
                    success = false,
                    ErrorMessage = $"Den totala storleken på alla bifogade filer överstiger den tillåtna gränsen {_options.CombinedMaxSizeAttachments / 1024 / 1024} MB"
                });
            }
            using (var trn = _dbContext.Database.BeginTransaction())
            {
                //Create group
                groupKey ??= Guid.NewGuid();

                foreach (var file in files)
                {
                    var extension = Path.GetExtension(file.FileName);
                    if (!_options.AllowedFileExtensions.Split(",").Any(e => e.ToSwedishUpper() == extension.ToSwedishUpper()))
                    {
                        trn.Rollback();
                        //log here as well
                        return Json(new
                        {
                            success = false,
                            ErrorMessage = $"Filer av typen {extension} är inte tillåtna. Inga filer laddades upp."
                        });
                    }

                    using Stream stream = file.OpenReadStream();
                    byte[] byteArray = new byte[file.Length];
                    stream.Read(byteArray, 0, (int)file.Length);
                    stream.Close();
                    var fileName = Path.GetFileName(file.FileName);
                    var attachment = new Attachment
                    {

                        Blob = byteArray,
                        FileName = fileName,
                        CreatedBy = User.GetUserId(),
                        ImpersonatingCreator = User.TryGetImpersonatorId(),
                        TemporaryAttachmentGroup = new TemporaryAttachmentGroup { TemporaryAttachmentGroupKey = groupKey.Value, CreatedAt = _clock.SwedenNow, }
                    };
                    _dbContext.Attachments.Add(attachment);
                    _dbContext.SaveChanges();
                    list.Add(new FileModel { Id = attachment.AttachmentId, FileName = fileName, Size = file.Length });
                }
                trn.Commit();
            }
            return Json(new
            {
                success = true,
                fileInfo = list.ToArray(),
                groupKey
            });
        }

        [HttpGet]
        public async Task<ActionResult> Download(int id)
        {
            var orderAttachment = await _dbContext.OrderAttachments.GetOrderAttachmentByAttachmentId(id);
            Attachment attachment = orderAttachment?.Attachment;
            if (attachment == null)
            {
                var requestAttachment = await _dbContext.RequestAttachments.GetRequestAttachmentByAttachmentId(id);
                attachment = requestAttachment?.Attachment;
                if (attachment == null)
                {
                    var orderGroupAttachment = await _dbContext.OrderGroupAttachments.GetOrderGroupAttachmentByAttachmentId(id);
                    attachment = orderGroupAttachment?.Attachment;
                    if (attachment == null)
                    {
                        var requestGroupAttachment = await _dbContext.RequestGroupAttachments.GetRequestGroupAttachmentByAttachmentId(id);
                        attachment = requestGroupAttachment?.Attachment;
                        if (attachment == null)
                        {
                            var requisitionAttachment = await _dbContext.RequisitionAttachments.GetRequisitionAttachmentByAttachmentId(id);
                            attachment = requisitionAttachment?.Attachment;
                            if (attachment != null && (await _authorizationService.AuthorizeAsync(User, requisitionAttachment, Policies.View)).Succeeded)
                            {
                                return File(attachment.Blob, System.Net.Mime.MediaTypeNames.Application.Octet, attachment.FileName);
                            }
                        }
                        else if ((await _authorizationService.AuthorizeAsync(User, requestGroupAttachment, Policies.View)).Succeeded)
                        {
                            return File(attachment.Blob, System.Net.Mime.MediaTypeNames.Application.Octet, attachment.FileName);
                        }
                    }
                    else
                    {
                        orderGroupAttachment.OrderGroup.RequestGroups = await _dbContext.RequestGroups.GetRequestGroupsForOrderGroup(orderGroupAttachment.OrderGroup.OrderGroupId).ToListAsync();
                        if ((await _authorizationService.AuthorizeAsync(User, orderGroupAttachment, Policies.View)).Succeeded)
                        {
                            return File(attachment.Blob, System.Net.Mime.MediaTypeNames.Application.Octet, attachment.FileName);
                        }
                    }
                }
                else if ((await _authorizationService.AuthorizeAsync(User, requestAttachment, Policies.View)).Succeeded)
                {
                    return File(attachment.Blob, System.Net.Mime.MediaTypeNames.Application.Octet, attachment.FileName);
                }
            }
            else
            {
                orderAttachment.Order.Requests = await _dbContext.Requests.GetRequestsForOrder(orderAttachment.Order.OrderId).ToListAsync();
                if ((await _authorizationService.AuthorizeAsync(User, orderAttachment, Policies.View)).Succeeded)
                {
                    return File(attachment.Blob, System.Net.Mime.MediaTypeNames.Application.Octet, attachment.FileName);
                }
            }
            //if attachment still is null it's not connected yet (it's just created in UI)
            if (attachment == null)
            {
                attachment = await _dbContext.Attachments.GetNonConnectedAttachmentById(id);
                if (attachment != null && (await _authorizationService.AuthorizeAsync(User, attachment, Policies.View)).Succeeded)
                {
                    return File(attachment.Blob, System.Net.Mime.MediaTypeNames.Application.Octet, attachment.FileName);
                }
            }
            return Forbid();
        }

        [HttpDelete]
        [ValidateAntiForgeryToken]
        public async Task<JsonResult> Delete(int id, Guid groupKey)
        {
            //this delete is just for deleting attachments added in UI before connected to orders, requests and requisitions 
            var attachment = await _dbContext.Attachments.GetNonConnectedAttachmentById(id);
            if (attachment != null && attachment.TemporaryAttachmentGroup.TemporaryAttachmentGroupKey == groupKey && (await _authorizationService.AuthorizeAsync(User, attachment, Policies.Delete)).Succeeded)
            {
                _dbContext.TemporaryAttachmentGroups.Remove(attachment.TemporaryAttachmentGroup);
                _dbContext.Attachments.Remove(attachment);
                await _dbContext.SaveChangesAsync();
            }
            return Json(new { success = true });
        }

    }
}

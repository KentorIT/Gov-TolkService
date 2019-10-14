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
                if (!groupKey.HasValue)
                {
                    //Create group
                    groupKey = Guid.NewGuid();
                }
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


                    using (Stream stream = file.OpenReadStream())
                    {
                        byte[] byteArray;
                        byteArray = new byte[file.Length];
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
            var attachment = _dbContext.Attachments
                .Include(a => a.Requests).ThenInclude(r => r.Request).ThenInclude(r => r.Ranking)
                .Include(a => a.Requests).ThenInclude(r => r.Request).ThenInclude(r => r.Order)
                .Include(a => a.Requisitions).ThenInclude(r => r.Requisition).ThenInclude(r => r.Request).ThenInclude(r => r.Ranking)
                .Include(a => a.Requisitions).ThenInclude(r => r.Requisition).ThenInclude(r => r.Request).ThenInclude(r => r.Order)
                .Include(a => a.Orders).ThenInclude(o => o.Order).ThenInclude(o => o.Requests).ThenInclude(r => r.Ranking)
                .SingleOrDefault(a => a.AttachmentId == id);
            //Add validation...
            if (attachment == null)
            {
                throw new FileNotFoundException();
            }
            if ((await _authorizationService.AuthorizeAsync(User, attachment, Policies.View)).Succeeded)
            {
                return File(attachment.Blob, System.Net.Mime.MediaTypeNames.Application.Octet, attachment.FileName);
            }
            return Forbid();
        }

        [HttpDelete]
        [ValidateAntiForgeryToken]
        public JsonResult Delete(int id, Guid groupKey)
        {
            var attachment = _dbContext.Attachments
                .Include(a => a.TemporaryAttachmentGroup)
                .Include(a => a.Requisitions)
                .Include(a => a.Requests)
                .SingleOrDefault(a => a.AttachmentId == id && a.TemporaryAttachmentGroup.TemporaryAttachmentGroupKey == groupKey);
            //Add check for if the user is allowed to remove the attachment
            // Check if the file is not connected to any requisitions or requests. If it is, just remove the temp-connection.
            if (attachment != null)
            {
                if (attachment.Requisitions.Any() || attachment.Requests.Any())
                {
                    _dbContext.TemporaryAttachmentGroups.Remove(attachment.TemporaryAttachmentGroup);
                }
                else
                {
                    _dbContext.Attachments.Remove(attachment);
                }
                _dbContext.SaveChanges();
            }
            return Json(new { success = true });
        }
    }
}

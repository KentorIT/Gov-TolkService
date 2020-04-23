using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Threading.Tasks;
using Tolk.BusinessLogic.Entities;

namespace Tolk.Web.Models
{
    public class AttachmentListModel
    {
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "CA2227:Collection properties should be read only", Justification = "Used in razor view")]
        public List<FileModel> Files { get; set; } = new List<FileModel>();

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "CA2227:Collection properties should be read only", Justification = "Used in razor view")]
        public List<FileModel> DisplayFiles { get; set; } = new List<FileModel>();

        public bool AllowUpload { get; set; } = false;

        public bool AllowDelete { get; set; } = false;

        public bool AllowDownload { get; set; } = true;

        public string Title { get; set; } = "Bifogade filer";

        public string Description { get; set; } = "Möjlighet att bifoga filer som kan vara relevanta";

        internal static async Task<AttachmentListModel> GetReadOnlyModelFromList(IQueryable<Attachment> attachments, string title)
            => new AttachmentListModel
            {
                AllowDelete = false,
                AllowDownload = true,
                AllowUpload = false,
                Title = title,
                DisplayFiles = await attachments
                    .Select(a => new FileModel
                    {
                        Id = a.AttachmentId,
                        FileName = a.FileName,
                        Size = a.Blob.Length
                    }).ToListAsync()
            };
        internal static async Task<AttachmentListModel> GetEditableModelFromList(IQueryable<Attachment> attachments, string title, string description)
            => new AttachmentListModel
            {
                AllowDelete = true,
                AllowDownload = true,
                AllowUpload = true,
                Title = title,
                Description = description,
                Files = await attachments
                    .Select(a => new FileModel
                    {
                        Id = a.AttachmentId,
                        FileName = a.FileName,
                        Size = a.Blob.Length
                    }).ToListAsync()
            };
    }
}

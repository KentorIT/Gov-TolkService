using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace Tolk.Web.Models
{
    public class AttachmentListModel
    {
        public List<FileModel> Files { get; set; }

        public bool AllowUpload { get; set; } = false;

        public bool AllowDelete { get; set; } = false;

        public bool AllowDownload { get; set; } = true;
    }
}

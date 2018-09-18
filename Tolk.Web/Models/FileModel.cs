using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace Tolk.Web.Models
{
    public class FileModel
    {
        public int Id { get; set; }

        public string FileName { get; set; }

        public long? Size { get; set; }
    }
}

﻿using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Tolk.BusinessLogic.Entities
{
    public class Attachment
    {
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        [Key]
        public int AttachmentId { get; set; }

        [Required]
        [MaxLength(255)]
        public string FileName { get; set; }

        [Required]
        public byte[] Blob { get; set; }

        public int CreatedBy { get; set; }

        [ForeignKey(nameof(CreatedBy))]
        public AspNetUser CreatedByUser { get; set; }

        public int? ImpersonatingCreator { get; set; }

        [ForeignKey(nameof(ImpersonatingCreator))]
        public AspNetUser CreatedByImpersonator { get; set; }

        public TemporaryAttachmentGroup TemporaryAttachmentGroup { get; set; }

        public List<RequisitionAttachment> Requisitions { get; set; }

        public List<RequestAttachment> Requests { get; set; }

        public List<RequestGroupAttachment> RequestGroups { get; set; }

        public List<OrderAttachment> Orders { get; set; }

        public List<OrderGroupAttachment> OrderGroups { get; set; }

        public List<OrderAttachmentHistoryEntry> OrderAttachmentHistoryEntries { get; set; }


    }
}


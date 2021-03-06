﻿using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Tolk.BusinessLogic.Enums;

namespace Tolk.BusinessLogic.Entities
{
    public class SystemMessage
    {

        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int SystemMessageId { get; set; }

        public DateTimeOffset CreatedAt { get; set; }

        public int CreatedBy { get; set; }

        [ForeignKey(nameof(CreatedBy))]
        public AspNetUser CreatedByUser { get; set; }

        public int? ImpersonatingCreator { get; set; }

        [ForeignKey(nameof(ImpersonatingCreator))]
        public AspNetUser CreatedByImpersonator { get; set; }

        [MaxLength(255)]
        public string SystemMessageHeader { get; set; }

        [MaxLength(2000)]
        public string SystemMessageText { get; set; }

        public DateTimeOffset ActiveFrom { get; set; }

        public DateTimeOffset ActiveTo { get; set; }

        public SystemMessageType SystemMessageType { get; set; }

        public SystemMessageUserTypeGroup SystemMessageUserTypeGroup { get; set; }

        public int? LastUpdatedBy { get; set; }

        [ForeignKey(nameof(LastUpdatedBy))]
        public AspNetUser LastUpdatedByUser { get; set; }

        public int? ImpersonatingLastUpdated { get; set; }

        [ForeignKey(nameof(ImpersonatingLastUpdated))]
        public AspNetUser LastUpdatedImpersonator { get; set; }

        public DateTimeOffset? LastUpdatedAt { get; set; }

        public DateTimeOffset LastUpdatedCreatedAt { get => LastUpdatedAt ?? CreatedAt; }

        public string CssClass { get => SystemMessageType == SystemMessageType.Information ? "system-message-info" : "system-message-warning"; }

        public void Create(DateTimeOffset swedenNow, int userId, int? impersonatorId, DateTimeOffset activeFrom, DateTimeOffset activeTo, string systemMessageHeader, string systemMessageText, SystemMessageType systemMessageType, SystemMessageUserTypeGroup displayedForUserTypeGroup)
        {
            CreatedAt = swedenNow;
            ImpersonatingCreator = impersonatorId;
            CreatedBy = userId;
            ActiveFrom = activeFrom;
            ActiveTo = activeTo;
            SystemMessageHeader = systemMessageHeader;
            SystemMessageText = systemMessageText;
            SystemMessageType = systemMessageType;
            SystemMessageUserTypeGroup = displayedForUserTypeGroup;
        }

        public void Update(DateTimeOffset swedenNow, int userId, int? impersonatorId, DateTimeOffset activeFrom, DateTimeOffset activeTo, string systemMessageHeader, string systemMessageText, SystemMessageType systemMessageType, SystemMessageUserTypeGroup displayedForUserTypeGroup)
        {
            LastUpdatedAt = swedenNow;
            LastUpdatedBy = userId;
            ImpersonatingLastUpdated = impersonatorId;
            ActiveFrom = activeFrom;
            ActiveTo = activeTo;
            SystemMessageHeader = systemMessageHeader;
            SystemMessageText = systemMessageText;
            SystemMessageType = systemMessageType;
            SystemMessageUserTypeGroup = displayedForUserTypeGroup;
        }
    }
}

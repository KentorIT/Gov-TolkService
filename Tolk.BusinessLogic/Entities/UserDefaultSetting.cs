﻿using System.ComponentModel.DataAnnotations.Schema;
using Tolk.BusinessLogic.Enums;

namespace Tolk.BusinessLogic.Entities
{
    public class UserDefaultSetting
    {
        public int UserId { get; set; }

        public DefaultSettingsType DefaultSettingType { get; set; }

        public string Value { get; set; }

        #region foreign keys

        [ForeignKey(nameof(UserId))]
        public AspNetUser User { get; set; }

        #endregion
    }
}

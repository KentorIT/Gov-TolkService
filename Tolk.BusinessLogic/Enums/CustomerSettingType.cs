using System;
using System.ComponentModel;

namespace Tolk.BusinessLogic.Enums
{
    [Serializable]
    public enum CustomerSettingType
    {
        [Description("Använder sammanhållen bokning")]
        UseOrderGroups = 1,
        [Description("Tolken fakturerar själv tolkarvode")]
        UseSelfInvoicingInterpreter = 2,
        [Description("Dölj fält Bifoga filer")]
        HideAttachmentField = 3
    }
}

using System.ComponentModel;
using Tolk.BusinessLogic.Utilities;

namespace Tolk.BusinessLogic.Enums
{
    public enum OrderChangeLogType
    {
        [CustomName("attachments")]
        [Description("Bifogade filer")]
        Attachment = 1,

        [CustomName("not_used", false)]
        [Description("Person med rätt att granska rekvisition")]
        ContactPerson = 2,

        [CustomName("information_fields")]
        [Description("Informationsfält för bokning")]
        OrderInformationFields = 3,

        [CustomName("attachments_and_information_fields")]
        [Description("Informationsfält och bifogade filer")]
        AttachmentAndOrderInformationFields = 4
    }
}

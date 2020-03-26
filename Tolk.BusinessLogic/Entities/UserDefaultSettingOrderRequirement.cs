using System.ComponentModel.DataAnnotations.Schema;
using Tolk.BusinessLogic.Enums;


namespace Tolk.BusinessLogic.Entities
{
    public class UserDefaultSettingOrderRequirement
    {

        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int UserDefaultSettingOrderRequirementId { get; set; }

        public int UserId { get; set; }

        public RequirementType RequirementType { get; set; }

        public string Description { get; set; }

        public bool IsRequired { get; set; }

        #region foreign keys

        [ForeignKey(nameof(UserId))]
        public AspNetUser User { get; set; }

        #endregion
    }
}

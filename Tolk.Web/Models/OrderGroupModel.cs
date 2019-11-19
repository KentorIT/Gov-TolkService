using System.ComponentModel.DataAnnotations;
using System.Linq;
using Tolk.BusinessLogic.Entities;
using Tolk.BusinessLogic.Enums;
using Tolk.Web.Attributes;
using Tolk.Web.Helpers;

namespace Tolk.Web.Models
{
    public class OrderGroupModel
    {
        public int? OrderGroupId { get; set; }


        [Display(Name = "Sammanhållet bokningsID")]
        public string OrderGroupNumber { get; set; }

        public int? RequestGroupId { get; set; }

        [Display(Name = "Anledning till att svaret inte godtas")]
        [DataType(DataType.MultilineText)]
        [ClientRequired]
        [StringLength(1000)]
        [Placeholder("Beskriv anledning till varför du inte godtar svaret.")]
        public string DenyMessage { get; set; }

        [Display(Name = "Anledning till att bokningen avbokas")]
        [DataType(DataType.MultilineText)]
        [ClientRequired]
        [StringLength(1000)]
        [Placeholder("Beskriv anledning till avbokning.")]
        public string CancelMessage { get; set; }

        public bool AllowNoAnswerConfirmation => true;
        public bool ActiveRequestIsAnswered => true;
        public bool HasOtherCompetenceLevel => true;
        public bool AllowProcessing => true;
        public decimal ExpectedTravelCosts => 500;
        public string ExpectedTravelCostInfo => "Här skall det stå bra saker om vad kostnaderna kommer sig av.";
        public bool AllowOrderGroupCancellation => true;
        public bool AllowPrint => true;
        public bool AllowDenial => true;

        #region methods

        internal static OrderGroupModel GetModelFromOrderGroup(OrderGroup orderGroup)
        {
            return new OrderGroupModel
            {
                OrderGroupId = orderGroup.OrderGroupId,
                OrderGroupNumber = orderGroup.OrderGroupNumber,
                //NOTE: This does not work if partial answers are allowed!!
                RequestGroupId = orderGroup.RequestGroups.SingleOrDefault(g => g.Status == RequestStatus.Accepted)?.RequestGroupId
            };
        }

        #endregion
    }
}

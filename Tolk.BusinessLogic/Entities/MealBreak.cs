using System;
using System.ComponentModel.DataAnnotations.Schema;
using Tolk.BusinessLogic.Validation;

namespace Tolk.BusinessLogic.Entities
{
    public class MealBreak
    {

        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int MealBreakId { get; set; }

        public int RequisitionId { get; set; }

        [ForeignKey(nameof(RequisitionId))]
        public Requisition Requisition { get; set; }

        public DateTimeOffset StartAt { get; set; }

        private DateTimeOffset _endAt;

        public DateTimeOffset EndAt
        {
            get { return _endAt; }
            set
            {
                Validate.Ensure(value > StartAt, $"{nameof(EndAt)} cannot occur before {nameof(StartAt)}");
                _endAt = value;
            }
        }

        public int Minutes { get => (int)(EndAt - StartAt).TotalMinutes; }

        [NotMapped]
        public DateTime StartAtTemp { get; set; }

        [NotMapped]
        public DateTime EndAtTemp { get; set; }

    }
}

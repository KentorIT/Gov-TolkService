using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace Tolk.BusinessLogic.Entities
{
    public class UpdateBase
    {
        public DateTimeOffset UpdatedAt { get; set; }

        public int UpdatedBy { get; set; }

        [ForeignKey(nameof(UpdatedBy))]
        public AspNetUser UpdatedByUser { get; set; }

        public int? ImpersonatorUpdatedBy { get; set; }

        [ForeignKey(nameof(ImpersonatorUpdatedBy))]
        public AspNetUser UpdatedByImpersonator { get; set; }

    }
}

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace Tolk.Web.Models
{
    public class OccasionListModel
    {
        public IEnumerable<OrderOccasionDisplayModel> Occasions { get; set; }

        [DataType(DataType.MultilineText)]
        [Display(Name = "Tillfällen för tolk")]
        public string InterpreterOccasionsCompactDisplay =>
            string.Join("\n", Occasions
                .Where(o => !o.ExtraInterpreter)
                .OrderBy(o => o.Information)
                .Select(o => o.Information));

        [DataType(DataType.MultilineText)]
        [Display(Name = "Tillfällen för extra tolk")]
        public string ExtraInterpreterOccasionsCompactDisplay =>
            string.Join("\n", Occasions
                .Where(o => o.ExtraInterpreter)
                .OrderBy(o => o.Information)
                .Select(o => o.Information));

        public bool HasSeveralOccasions { get; set; }

        public bool ShowInformation { get; set; }

        public decimal TotalPrice => Occasions?.Sum(o => o.PriceInformationModel.TotalPriceToDisplay) ?? 0;
    }
}

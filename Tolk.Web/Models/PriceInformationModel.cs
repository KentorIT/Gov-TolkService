using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;
using Tolk.BusinessLogic.Utilities;
using Tolk.Web.Helpers;


namespace Tolk.Web.Models
{
    public class PriceInformationModel
    {

        [NoDisplayName]
        public DisplayPriceInformation PriceInformationToDisplay { get; set; }

        [NoDisplayName]
        public string Header { get; set; }

        [DataType(DataType.Currency)]
        public decimal TotalPriceToDisplay { get { return PriceInformationToDisplay.TotalPrice; } }

    }
}

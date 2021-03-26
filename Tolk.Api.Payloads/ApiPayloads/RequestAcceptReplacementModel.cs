using NJsonSchema.Annotations;
using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace Tolk.Api.Payloads.ApiPayloads
{
    [Description("Schema för att besvara ett ersättningsuppdrag")]
    [JsonSchemaFlatten]
    public class RequestAcceptReplacementModel : ApiPayloadBaseModel
    {
        [Description("Id på det avrop som skall besvaras")]
        [Required]
        public string OrderNumber { get; set; }

        [Description("Det inställelsesätt som tolken kommer inställa sig med. Måste vara ett av värdena i [/List/LocationTypes]")]
        [Required]
        public string Location { get; set; }

        [Description("Förväntad reskostnad, om någon.")]
        public decimal? ExpectedTravelCosts { get; set; }

        [Description("Eventuell beskrivning av förväntad reskostnad")]
        public string ExpectedTravelCostInfo { get; set; }

        [Description("Beskriver en sista svarstid för beställaren att godkänna svaret.")]
        public DateTimeOffset? LatestAnswerTimeForCustomer { get; set; }

        [Description("Förmedlingens eget bokningsnummer att koppla till bokningen.")]
        public string BrokerReferenceNumber { get; set; }
    }
}

using NJsonSchema.Annotations;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace Tolk.Api.Payloads.ApiPayloads
{
    [Description("Schema för att besvara ett avrop")]
    [JsonSchemaFlatten]
    public class RequestAnswerModel : RequestAcceptModel
    {
        [Description("Den tillsatta tolken")]
        [Required]
        public InterpreterModel Interpreter { get; set; }

        [Description("Förväntad reskostnad, om någon.")]
        public decimal? ExpectedTravelCosts { get; set; }

        [Description("Eventuell beskrivning av förväntad reskostnad")]
        public string ExpectedTravelCostInfo { get; set; }

        [Description("Beskriver en sista svarstid för beställaren att godkänna svaret.")]
        public DateTimeOffset? LatestAnswerTimeForCustomer { get; set; }
    }
}

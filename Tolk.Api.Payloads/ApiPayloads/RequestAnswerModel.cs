using NJsonSchema.Annotations;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace Tolk.Api.Payloads.ApiPayloads
{
    [Description("Schema för att besvara ett avrop")]
    [JsonSchemaFlatten]
    public class RequestAnswerModel : ApiPayloadBaseModel
    {
        [Description("Id på det avrop som skall besvaras")]
        [Required]
        public string OrderNumber { get; set; }

        [Description("Den tillsatta tolken")]
        [Required]
        public InterpreterModel Interpreter { get; set; }

        [Description("Det inställelsesätt som tolken kommer inställa sig med. Måste vara ett av värdena i [/List/LocationTypes] och även finnas med i förfrågan")]
        [Required]
        public string Location { get; set; }

        [Description("Den kompetensnivå som tolken har inom det språk som skall tolkas. Måste vara ett av värdena i [/List/CompetenceLevels] och även finnas med i förfrågan")]
        [Required]
        public string CompetenceLevel { get; set; }

        [Description("Förväntad reskostnad, om någon.")]
        public decimal? ExpectedTravelCosts { get; set; }

        [Description("Eventuell beskrivning av förväntad reskostnad")]
        public string ExpectedTravelCostInfo { get; set; }

        [Description("Om avropet innehåller specifika krav så skall dessa besvaras här.")]
        public IEnumerable<RequirementAnswerModel> RequirementAnswers { get; set; }

        [Description("Beskriver en sista svarstid för beställaren att godkänna svaret.")]
        public DateTimeOffset? LatestAnswerTimeForCustomer { get; set; }

        [Description ("Förmedlingens eget bokningsnummer att koppla till bokningen.")]
        public string BrokerReferenceNumber { get; set; }
    }
}

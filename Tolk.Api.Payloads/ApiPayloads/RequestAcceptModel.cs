using NJsonSchema.Annotations;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace Tolk.Api.Payloads.ApiPayloads
{
    [Description("Schema för att bekräfta ett avrop")]
    [JsonSchemaFlatten]
    public class RequestAcceptModel : ApiPayloadBaseModel
    {
        [Description("Id på det avrop som skall behandlas")]
        [Required]
        public string OrderNumber { get; set; }

        [Description("Det inställelsesätt som tolken kommer inställa sig med. Måste vara ett av värdena i [/List/LocationTypes] och även finnas med i förfrågan")]
        [Required]
        public string Location { get; set; }

        [Description("Den kompetensnivå som tolken har inom det språk som skall tolkas. Behöver sättas om kompetensnivå är ett krav. Måste vara ett av värdena i [/List/CompetenceLevels]  och även finnas med i förfrågan")]
        public string CompetenceLevel { get; set; }

        [Description("Om avropet innehåller specifika krav så skall dessa besvaras här.")]
        public IEnumerable<RequirementAnswerModel> RequirementAnswers { get; set; }

        [Description ("Förmedlingens eget bokningsnummer att koppla till bokningen.")]
        public string BrokerReferenceNumber { get; set; }

        [Description("Beskriver besvarad starttid, om avropet har flexibel tid.")]
        public DateTimeOffset? RespondedStartAt { get; set; }
    }
}

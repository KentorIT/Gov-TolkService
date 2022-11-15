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
        [Description("Id på det avrop som skall bekräftas")]
        [Required]
        public string OrderNumber { get; set; }

        [Description("Den kompetensnivå som tolken har inom det språk som skall tolkas. Behöver sättas om kompetensnivå är ett krav. Måste vara ett av värdena i [/List/CompetenceLevels]")]
        public string CompetenceLevel { get; set; }

        [Description("Om avropet innehåller specifika krav så skall dessa besvaras här.")]
        public IEnumerable<RequirementAnswerModel> RequirementAnswers { get; set; }

        [Description ("Förmedlingens eget bokningsnummer att koppla till bokningen.")]
        public string BrokerReferenceNumber { get; set; }
    }
}

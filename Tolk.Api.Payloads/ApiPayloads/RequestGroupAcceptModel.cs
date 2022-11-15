using NJsonSchema.Annotations;
using System.Collections.Generic;
using System.ComponentModel;

namespace Tolk.Api.Payloads.ApiPayloads
{
    [Description("Schema för att bekräfta en sammanhållen förfrågan")]
    [JsonSchemaFlatten]
    public class RequestGroupAcceptModel : ApiPayloadBaseModel
    {
        [Description("Id på det avrop som skall bekräftas")]
        public string OrderGroupNumber { get; set; }
        [Description("Den kompetensnivå som tolken har inom det språk som skall tolkas. Behöver sättas om kompetensnivå är ett krav. Måste vara ett av värdena i [/List/CompetenceLevels]")]
        public string CompetenceLevel { get; set; }
        [Description("Om avropet innehåller specifika krav så skall dessa besvaras här.")]
        public IEnumerable<RequirementAnswerModel> RequirementAnswers { get; set; }
        [Description("Förmedlingens eget bokningsnummer att koppla till bokningen.")]
        public string BrokerReferenceNumber { get; set; }
    }
}

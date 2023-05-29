using NJsonSchema.Annotations;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace Tolk.Api.Payloads.ApiPayloads
{
    [Description("Schema för att bekräfta en sammanhållen förfrågan")]
    [JsonSchemaFlatten]
    public class RequestGroupAcceptModel : ApiPayloadBaseModel
    {
        [Description("Id på det avrop som skall bekräftas")]
        public string OrderGroupNumber { get; set; }


        [Description("Det inställelsesätt som tolken kommer inställa sig med. Måste vara ett av värdena i [/List/LocationTypes] och även finnas med i förfrågan")]
        [Required]
        public string Location { get; set; }

        public InterpreterGroupAcceptModel InterpreterAccept { get; set; }

        public InterpreterGroupAcceptModel ExtraInterpreterAccept { get; set; }

        [Description("Förmedlingens eget bokningsnummer att koppla till bokningen.")]
        public string BrokerReferenceNumber { get; set; }
    }
}

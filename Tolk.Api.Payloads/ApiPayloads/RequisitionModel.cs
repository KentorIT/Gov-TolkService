using NJsonSchema.Annotations;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace Tolk.Api.Payloads.ApiPayloads
{
    [JsonSchemaFlatten]
    [Description("Modell för att skapa en ny rekvisition kopplat till ett visst uppdrag")]
    public class RequisitionModel : ApiPayloadBaseModel
    {
        [Description("Det avrop som rekvisitionen skall kopplas till")]
        [Required]
        public string OrderNumber { get; set; }

        [Required]
        [Description("Tolkens skattsedel")]
        public string TaxCard { get; set; }

        [Required]
        [Description("Den faktiska starttiden för uppdraget")]
        public DateTimeOffset AcctualStartedAt { get; set; }

        [Required]
        [Description("Den faktiska sluttiden för uppdraget")]
        public DateTimeOffset AcctualEndedAt { get; set; }

        [Description("Avser tid i minuter för total tidsspillan som restid, väntetider mm som överstiger 30 minuter och som inträffat utanför förväntad start- och sluttid")]
        public int? WasteTime { get; set; }

        [Description("Avser tid i minuter av den totala tidsspillan som angetts och som inträffat utanför vardagar 07:00-18:00")]
        public int? WasteTimeInconvenientHour { get; set; }

        [Description("Måltidspauser")]
        public IEnumerable<MealBreakModel> MealBreaks { get; set; }

        [Description("Utlägg utöver kostnader för eventuell bilersättning och traktamente")]
        public decimal? Outlay { get; set; }

        [Description("Uppgift om bilersättning ska utgå. Ange i antal hela kilometer.")]
        public int? CarCompensation { get; set; }

        [Description("Uppgift om traktamente (flerdygnsförrättning inkl. ev. måltidsavdrag) ska utgå. Ange i antal dagar eller i belopp i SEK")]
        public string PerDiem { get; set; }

        [Required]
        [Description("Information om angivna tider och kostnader.")]
        public string Message { get; set; }
    }
}

using System;

namespace Tolk.BusinessLogic.Utilities
{
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false)]
    public class ContractDefinitionAttribute : Attribute
    {
        public string Usage { get; set; } = "Ramavtalen för tolkförmedlingstjänster kan nyttjas av statliga myndigheter, samt offentligt styrda organ som lämnat bekräftelse (fullmakt).";
        public string Includes { get; set; } = "Ramavtalen omfattar förmedling och genomförande av tolktjänster.";
        public string ExcludedServices { get; set; } = "Konferenstolkning, teckenspråkstolkning, skrivtolkning samt skriftlig översättning och språkgranskning ingår inte i detta ramavtalsområde.";
        public string RankingRules { get; set; }
        public string GeneralTerms { get; set; } = "Ramavtalets Allmänna villkor är tillämpliga på avtalsförhållandet mellan leverantören och avropande myndighet som har ingått kontrakt och utgör en bilaga till kontraktet. Allmänna villkor är tillämpliga för varje avrop som sker inom ramen för ramavtalet oberoende av om detta anges i avropsförfrågan eller inte.";

        public ContractDefinitionAttribute()
        {
        }

        public ContractDefinition ContractDefinition => new ContractDefinition
        {
            Usage = Usage,
            Includes = Includes,
            ExcludedServices = ExcludedServices,
            RankingRules = RankingRules,
            GeneralTerms = GeneralTerms
        };
    }
}

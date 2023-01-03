using System;

namespace Tolk.BusinessLogic.Utilities
{
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false)]
    public class ContractDefinitionAttribute : Attribute
    {
        public string Usage => "Ramavtalen för tolkförmedlingstjänster kan nyttjas av statliga myndigheter, samt offentligt styrda organ som lämnat bekräftelse (fullmakt).";
        public string Includes => "Ramavtalen omfattar förmedling och genomförande av tolktjänster.";
        public string ExcludedServices => "Konferenstolkning, teckenspråkstolkning, skrivtolkning samt skriftlig översättning och språkgranskning ingår inte i detta ramavtalsområde.";
        public string RankingRules { get; set; }
        public string GeneralTerms => "Ramavtalets Allmänna villkor är tillämpliga på avtalsförhållandet mellan leverantören och avropande myndighet som har ingått kontrakt och utgör en bilaga till kontraktet. Allmänna villkor är tillämpliga för varje avrop som sker inom ramen för ramavtalet oberoende av om detta anges i avropsförfrågan eller inte.";
        public string ReplacementError { get; set; }
        public string TravelConditionKilometers { get; set; }
        public string TravelConditionHours { get; set; }

        public ContractDefinitionAttribute()
        {
        }

        public ContractDefinition ContractDefinition => new()
        {
            Usage = Usage,
            Includes = Includes,
            ExcludedServices = ExcludedServices,
            RankingRules = RankingRules,
            GeneralTerms = GeneralTerms
        };
    }
}

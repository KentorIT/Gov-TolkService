﻿using System;

namespace Tolk.BusinessLogic.Utilities
{
    public class ExternalLinksSettings
    {
        public Uri CurrentInfo { get; set; }
        public Uri GoodInterpreterPractice { get; set; }
        public Uri ActiveAgreementInfo { get; set; }
        public Uri ExpiredAgreementInfo { get; set; }
        public Uri NoActiveAgreementInfo { get; set; }
        public Uri GovernmentalCentralPurchasing { get; set; }
        public Uri RegistryOfInterpreters { get; set; }
        public Uri GitHubSourceCode { get; set; }
        public string Description => $"Aktuell info: {CurrentInfo}\nGod tolksed: {GoodInterpreterPractice}\nAktivt avtal: {ActiveAgreementInfo}\nInaktivt avtal: {ExpiredAgreementInfo}\nInfo om inget aktivt avtal: {NoActiveAgreementInfo}\nStatens inköpssentral: {GovernmentalCentralPurchasing}\nTolkregister: {RegistryOfInterpreters}\nKällkod: {GitHubSourceCode}";

    }
}

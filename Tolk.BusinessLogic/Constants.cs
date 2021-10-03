namespace Tolk.BusinessLogic
{
    public static class Constants
    {
        public static readonly string SystemName = "Kammarkollegiets avropstjänst för tolkar";
        public static readonly string SelectNoUnit = "Koppla inte till någon enhet";
        public const int NewInterpreterId = -1;
        public const int DeclineInterpreterId = -2;

        //Order Agreement constants
        public const string NotApplicableNotification = "N/A";
        public const string PeppolSchemeId = "0088";
        public const string OrganizationNumberSchemeId = "0007";
        public const string IdPrefix = "KamK-ID ";
        public const string CustomizationId = "urn:fdc:peppol.eu:poacc:trns:order_agreement:3";
        public const string ProfileId = "urn:fdc:peppol.eu:poacc:bis:order_agreement:3";
        public const string Currency = "SEK";
        public const string ContractNumber = "23.3-9066-16";

        public const string cbc = "urn:oasis:names:specification:ubl:schema:xsd:CommonBasicComponents-2";
        public const string cac = "urn:oasis:names:specification:ubl:schema:xsd:CommonAggregateComponents-2";
        public const string defaultNamespace = "urn:oasis:names:specification:ubl:schema:xsd:OrderResponse-2";

    }
}

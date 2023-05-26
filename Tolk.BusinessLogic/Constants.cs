namespace Tolk.BusinessLogic
{
    public static class Constants
    {
        public static readonly string SystemName = "Kammarkollegiets avropstjänst för tolkar";
        public static readonly string SelectNoUnit = "Koppla inte till någon enhet";
        public const int NewInterpreterId = -1;
        public const int DeclineInterpreterId = -2;

        //Peppol constants
        public const string NotApplicableNotification = "NA";
        public const string PeppolIdByGLNSchemeId = "0088";
        public const string PeppolIdByOrganizationNumberSchemeId = "0007";
        public const string Currency = "SEK";
        public const string cbc = "urn:oasis:names:specification:ubl:schema:xsd:CommonBasicComponents-2";
        public const string cac = "urn:oasis:names:specification:ubl:schema:xsd:CommonAggregateComponents-2";
        public const string sh = "urn:sfti:documents:StandardBusinessDocumentHeader";
        public const string defaultNamespace = "urn:oasis:names:specification:ubl:schema:xsd:OrderResponse-2";

        //Order Agreement constants
        public const string IdPrefix = "KamK-ID ";
        public const string OrderAgreementCustomizationId = "urn:fdc:peppol.eu:poacc:trns:order_agreement:3";
        public const string OrderAgreementProfileId = "urn:fdc:peppol.eu:poacc:bis:order_agreement:3";
        public const string ContractPrice = "CON";

        //Order Response constants
        public const string OrderResponseProfileId = "urn:fdc:peppol.eu:poacc:bis:ordering:3";
        public const string OrderResponseCustomizationId = "urn:fdc:peppol.eu:poacc:trns:order_response:3";
        public const string LineAcceptedWithChange = "3";
        public const string LineAcceptedWithoutChange = "5";
        public const string OrderConditionallyAccepted = "CA";

        //Invoice Constants        
        public const string InvoiceCustomizationId = "urn:cen.eu:en16931:2017#compliant#urn:fdc:peppol.eu:2017:poacc:billing:3.0";
        public const string InvoiceProfileId = "urn:fdc:peppol.eu:2017:poacc:billing:01:1.0";
        public const string invoiceDefaultNamespace = "urn:oasis:names:specification:ubl:schema:xsd:Invoice-2";
        public const string CommercialInvoiceTypeCode = "380";
        public const string CreditTransferPaymentMeansCode = "30";

    }
}

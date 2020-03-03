namespace Tolk.Api.Payloads.WebHookPayloads
{
    public abstract class CustomerInformationBaseModel
    {
        public string InvoiceReference { get; set; }
        public string UnitName { get; set; }
        public string DepartmentName { get; set; }
        public string ReferenceNumber { get; set; }
    }
}

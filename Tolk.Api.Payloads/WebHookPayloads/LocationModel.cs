namespace Tolk.Api.Payloads.WebHookPayloads
{
    public class LocationModel : LocationBaseModel
    {
        public string Key { get; set; }
        public int Rank { get; set; }

        public string City { get; set; }
    }
}

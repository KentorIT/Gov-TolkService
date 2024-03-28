namespace Tolk.BusinessLogic.Utilities
{
    public class PeppolSettings
    {
        public bool UsePeppol { get; set; }
        public string SenderIdentifier { get; set; }
        public bool UseEnvelope { get; set; }
        public SftpSettings SftpSettings { get; set; }

        public string Description => $"Använd Peppol: {UsePeppol.ToSwedishString()}\nAnvänd envelope: {UseEnvelope.ToSwedishString()}\n Avsändare: {SenderIdentifier}\nSftp:\n\tHost: {SftpSettings.Host}\n\tPort: {SftpSettings.Port}\n\tAnvändare: {SftpSettings.UserName}\n\tMapp: {SftpSettings.UploadFolder}";
    }
}

namespace Tolk.BusinessLogic.Utilities
{
    public class SmtpSettings
    {
        public string Host { get; set; }

        public int Port { get; set; }

        public string UserName { get; set; }

        public string Password { get; set; }

        public string FromAddress { get; set; }

        public bool UseAuthentication { get; set; } = true;

        public string Description => $"Host: {Host}\nPort: {Port}\nFrånadress: {FromAddress}\nAnvänd anv/lösen: {UseAuthentication.ToSwedishString()}\nAnvändarnamn: {UserName}\nLösenord (antal tkn): {Password.Length}";
    }
}

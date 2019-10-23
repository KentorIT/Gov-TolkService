namespace Tolk.BusinessLogic.Utilities
{
    public class StatusCheckerSettings
    {
        public string Enabled { get; set; }
        public bool CheckUptimeRobot { get; set; }
        public string UptimeRobotApiKey { get; set; }
        public string UptimeRobotCheckUrl { get; set; }
    }
}

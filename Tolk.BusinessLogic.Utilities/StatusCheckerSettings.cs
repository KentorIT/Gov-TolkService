using System;

namespace Tolk.BusinessLogic.Utilities
{
    public class StatusCheckerSettings
    {
        public bool CheckUptimeRobot { get; set; }
        public string UptimeRobotApiKey { get; set; }
        public Uri UptimeRobotCheckUrl { get; set; }
        public string Description => $"Kolla uptime robot: {CheckUptimeRobot.ToSwedishString()}\n\tUptimeUrl: {UptimeRobotCheckUrl}\n\tApinyckel (längd): {UptimeRobotApiKey?.Length ?? 0}";
    }
}

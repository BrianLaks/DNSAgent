namespace DNSAgent.Service.Configuration
{
    public class DnsAgentSettings
    {
        public string UpstreamDns { get; set; } = "8.8.8.8";
        public string BlocklistUrl { get; set; } = "https://raw.githubusercontent.com/StevenBlack/hosts/master/hosts";
        public string BlocklistUpdateSchedule { get; set; } = "0 2 * * *"; // 2 AM daily
        public bool EnableWebUI { get; set; } = true;
        public int WebUIPort { get; set; } = 5123;
        public bool EnableLogging { get; set; } = true;
        public int LogRetentionDays { get; set; } = 30;
        public bool EnableBlocking { get; set; } = true; // Master switch for blocking
    }
}

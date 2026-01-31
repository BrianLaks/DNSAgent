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
        public int YouTubeRetentionDays { get; set; } = 90;
        public bool EnableBlocking { get; set; } = true;

        // Security v1.3 Features
        public string UpstreamProtocol { get; set; } = "UDP"; // UDP, DoH
        public string DoHUrl { get; set; } = "https://dns.google/dns-query";
        public bool EnforceDnssec { get; set; } = false;
    }
}

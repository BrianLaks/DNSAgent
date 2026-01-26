using System;

namespace DNSAgent.Service.Data
{
    public class SentinelReport
    {
        public int Id { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        public string ClientId { get; set; } = string.Empty;
        public string Domain { get; set; } = string.Empty;
        public string ReportType { get; set; } = "Tracker"; // "Tracker", "Bypass", or "Threat"
        public string? PageUrl { get; set; }
        public bool IsAutoBlocked { get; set; } = false;
        public string? Metadata { get; set; } // JSON blob for extra info
    }
}

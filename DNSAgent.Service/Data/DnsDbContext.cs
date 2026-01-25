using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;

namespace DNSAgent.Service.Data
{
    public class DnsDbContext : IdentityDbContext<IdentityUser>
    {
        public DnsDbContext(DbContextOptions<DnsDbContext> options) : base(options)
        {
        }

        public DbSet<QueryLog> QueryLogs { get; set; }
        public DbSet<WhitelistedDomain> WhitelistedDomains { get; set; }
        public DbSet<YouTubeStat> YouTubeStats { get; set; }
    }

    public class YouTubeStat
    {
        public int Id { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        public int AdsBlocked { get; set; }
        public int AdsFailed { get; set; }
        public int SponsorsSkipped { get; set; }
        public double TimeSavedSeconds { get; set; }
        public string? DeviceName { get; set; }
        public string FilterVersion { get; set; } = string.Empty;
    }

    public class QueryLog
    {
        public int Id { get; set; }
        public DateTime Timestamp { get; set; }
        public string SourceIP { get; set; }
        public string SourceHostname { get; set; } = string.Empty; // Reverse DNS lookup
        public string Domain { get; set; }
        public string Status { get; set; } // "Blocked" or "Allowed"
        public string Transport { get; set; } = "UDP"; // "UDP" or "DoH"
        public bool IsDnssec { get; set; } = false; // Validated via AD bit
        public long ResponseTimeMs { get; set; }
    }

    public class WhitelistedDomain
    {
        public int Id { get; set; }
        public string Domain { get; set; }
        public DateTime AddedAt { get; set; }
    }
}

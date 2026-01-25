using System;
using Microsoft.EntityFrameworkCore;

namespace DNSAgent.Web.Data
{
    public class DnsDbContext : DbContext
    {
        public DnsDbContext(DbContextOptions<DnsDbContext> options) : base(options)
        {
        }

        public DbSet<QueryLog> QueryLogs { get; set; }
        public DbSet<WhitelistedDomain> WhitelistedDomains { get; set; }
        // Future: Settings, ClientIPs
    }

    public class QueryLog
    {
        public int Id { get; set; }
        public DateTime Timestamp { get; set; }
        public string SourceIP { get; set; }
        public string Domain { get; set; }
        public string Status { get; set; } // "Blocked" or "Allowed"
        public long ResponseTimeMs { get; set; }
    }

    public class WhitelistedDomain
    {
        public int Id { get; set; }
        public string Domain { get; set; }
        public DateTime AddedAt { get; set; }
    }
}

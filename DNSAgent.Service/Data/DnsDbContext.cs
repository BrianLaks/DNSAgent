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
    }

    public class QueryLog
    {
        public int Id { get; set; }
        public DateTime Timestamp { get; set; }
        public string SourceIP { get; set; }
        public string SourceHostname { get; set; } = string.Empty; // Reverse DNS lookup
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

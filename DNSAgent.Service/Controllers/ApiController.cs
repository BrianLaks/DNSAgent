using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using DNSAgent.Service.Services;
using DNSAgent.Service.Data;
using DNSAgent.Service.Configuration;
using Microsoft.EntityFrameworkCore;

namespace DNSAgent.Service.Controllers
{
    [ApiController]
    [Route("api")]
    public class ApiController : ControllerBase
    {
        private readonly DnsWorker _dnsWorker;
        private readonly DnsDbContext _db;
        private readonly DeArrowService _deArrowService;

        public ApiController(DnsWorker dnsWorker, DnsDbContext db, DeArrowService deArrowService)
        {
            _dnsWorker = dnsWorker;
            _db = db;
            _deArrowService = deArrowService;
        }

        /// <summary>
        /// GET /api/status - Service health check (no authentication required)
        /// </summary>
        [HttpGet("status")]
        public IActionResult GetStatus()
        {
            return Ok(new
            {
                version = Constants.AppVersion,
                uptime = GetUptime(),
                dnsEnabled = _dnsWorker.ProtectionEnabled,
                dohEnabled = _dnsWorker.UpstreamProtocol == "DoH",
                dnssecEnabled = _dnsWorker.EnforceDnssec,
                blockedDomains = _dnsWorker.GetBlockedDomainCount(),
                timestamp = DateTime.UtcNow
            });
        }

        /// <summary>
        /// GET /api/stats - Real-time statistics (authentication required)
        /// </summary>
        [HttpGet("stats")]
        [Authorize]
        public async Task<IActionResult> GetStats()
        {
            var today = DateTime.Today;
            var totalQueries = await _db.QueryLogs.CountAsync();
            var blockedToday = await _db.QueryLogs
                .Where(q => q.Timestamp >= today && q.Status == "Blocked")
                .CountAsync();

            var topBlocked = await _db.QueryLogs
                .Where(q => q.Status == "Blocked")
                .GroupBy(q => q.Domain)
                .OrderByDescending(g => g.Count())
                .Take(10)
                .Select(g => new { domain = g.Key, count = g.Count() })
                .ToListAsync();

            return Ok(new
            {
                totalQueries,
                blockedToday,
                topBlockedDomains = topBlocked,
                protectionEnabled = _dnsWorker.ProtectionEnabled,
                dnssecEnabled = _dnsWorker.EnforceDnssec
            });
        }

        /// <summary>
        /// POST /api/block - Add domain to blocklist (authentication required)
        /// </summary>
        [HttpPost("block")]
        [Authorize]
        public IActionResult BlockDomain([FromBody] BlockDomainRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Domain))
                return BadRequest(new { error = "Domain is required" });

            // Validate domain format
            if (!IsValidDomain(request.Domain))
                return BadRequest(new { error = "Invalid domain format" });

            // Add to blocklist
            _dnsWorker.AddToBlocklist(request.Domain);

            return Ok(new
            {
                success = true,
                domain = request.Domain,
                reason = request.Reason ?? "User blocked from extension",
                timestamp = DateTime.UtcNow
            });
        }

        /// <summary>
        /// POST /api/whitelist - Remove domain from blocklist (authentication required)
        /// </summary>
        [HttpPost("whitelist")]
        [Authorize]
        public IActionResult WhitelistDomain([FromBody] WhitelistDomainRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Domain))
                return BadRequest(new { error = "Domain is required" });

            // Remove from blocklist
            _dnsWorker.RemoveFromBlocklist(request.Domain);

            return Ok(new
            {
                success = true,
                domain = request.Domain,
                timestamp = DateTime.UtcNow
            });
        }

        // DeArrow Proxy Endpoints
        [HttpGet("dearrow/branding/{hashPrefix}")]
        public async Task<IActionResult> GetDeArrowBranding(string hashPrefix)
        {
            var result = await _deArrowService.GetBrandingAsync(hashPrefix);
            if (string.IsNullOrEmpty(result)) return NotFound();
            return Content(result, "application/json");
        }

        [HttpGet("dearrow/v1/getThumbnail")]
        public async Task<IActionResult> GetDeArrowThumbnail([FromQuery] string videoID)
        {
            var query = new Dictionary<string, string> { { "videoID", videoID } };
            var result = await _deArrowService.ProxyV1Async("getThumbnail", query);
            if (string.IsNullOrEmpty(result)) return NotFound();
            return Content(result, "application/json");
        }

        [HttpGet("dearrow/v1/branding")]
        public async Task<IActionResult> GetDeArrowV1Branding([FromQuery] string videoID)
        {
            var query = new Dictionary<string, string> { { "videoID", videoID } };
            var result = await _deArrowService.ProxyV1Async("branding", query);
            if (string.IsNullOrEmpty(result)) return NotFound();
            return Content(result, "application/json");
        }

        /// <summary>
        /// GET /api/youtube-filters - Get latest YouTube ad blocking selectors
        /// </summary>
        [HttpGet("youtube-filters")]
        public IActionResult GetYouTubeFilters()
        {
            var filters = new
            {
                version = "2026.01.25.1",
                lastUpdated = DateTime.UtcNow,
                cssSelectors = new[]
                {
                    // Video player ads
                    ".video-ads",
                    ".ytp-ad-module",
                    ".ytp-ad-overlay-container",
                    ".ytp-ad-text-overlay",
                    ".ytp-ad-player-overlay",
                    
                    // Sidebar ads
                    "#player-ads",
                    ".ytd-display-ad-renderer",
                    ".ytd-promoted-sparkles-web-renderer",
                    ".ytd-compact-promoted-item-renderer",
                    
                    // Banner ads
                    "#masthead-ad",
                    ".ytd-banner-promo-renderer",
                    
                    // Sponsored content
                    ".ytd-ad-slot-renderer",
                    "[class*='ad-showing']",
                    "[id*='player-ads']"
                },
                skipButtonSelectors = new[]
                {
                    ".ytp-ad-skip-button",
                    ".ytp-skip-ad-button",
                    "[class*='skip'][class*='button']",
                    ".ytp-ad-skip-button-modern"
                },
                urlPatterns = new[]
                {
                    "/doubleclick\\.net/",
                    "/googlesyndication\\.com/",
                    "/googleadservices\\.com/",
                    "/youtube\\.com\\/api\\/stats\\/ads/",
                    "/youtube\\.com\\/pagead\\//",
                    "/youtube\\.com\\/ptracking/"
                }
            };

            return Ok(filters);
        }

        /// <summary>
        /// POST /api/youtube-stats - Report YouTube ad blocking statistics
        /// </summary>
        [HttpPost("youtube-stats")]
        public async Task<IActionResult> ReportYouTubeStats([FromBody] YouTubeStatsRequest request)
        {
            // Log for visibility
            Console.WriteLine($"[YouTube Stats] Blocked: {request.AdsBlocked}, Sponsors: {request.SponsorsSkipped}, Saved: {request.TimeSavedSeconds}s");

            // Persist to database
            var stat = new YouTubeStat
            {
                AdsBlocked = request.AdsBlocked,
                AdsFailed = request.AdsFailed,
                SponsorsSkipped = request.SponsorsSkipped,
                TitlesCleaned = request.TitlesCleaned,
                ThumbnailsReplaced = request.ThumbnailsReplaced,
                TimeSavedSeconds = request.TimeSavedSeconds,
                FilterVersion = request.FilterVersion,
                DeviceName = Request.Headers["User-Agent"].ToString() ?? "Unknown Extension"
            };

            _db.YouTubeStats.Add(stat);
            await _db.SaveChangesAsync();

            return Ok(new { success = true, received = DateTime.UtcNow });
        }

        // Helper methods
        private string GetUptime()
        {
            var uptime = DateTime.UtcNow - System.Diagnostics.Process.GetCurrentProcess().StartTime.ToUniversalTime();
            return $"{uptime.Days}d {uptime.Hours}h {uptime.Minutes}m";
        }

        private bool IsValidDomain(string domain)
        {
            // Basic domain validation
            return !string.IsNullOrWhiteSpace(domain) &&
                   domain.Length > 3 &&
                   domain.Contains('.') &&
                   !domain.Contains(' ') &&
                   !domain.StartsWith('.') &&
                   !domain.EndsWith('.');
        }
    }

    // Request models
    public class BlockDomainRequest
    {
        public string Domain { get; set; } = string.Empty;
        public string? Reason { get; set; }
    }

    public class WhitelistDomainRequest
    {
        public string Domain { get; set; } = string.Empty;
    }

    public class YouTubeStatsRequest
    {
        public int AdsBlocked { get; set; }
        public int AdsFailed { get; set; }
        public int SponsorsSkipped { get; set; }
        public int TitlesCleaned { get; set; }
        public int ThumbnailsReplaced { get; set; }
        public double TimeSavedSeconds { get; set; }
        public string FilterVersion { get; set; } = string.Empty;
    }
}

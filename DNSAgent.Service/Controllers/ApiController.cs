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
        private readonly SponsorBlockService _sponsorBlockService;

        public ApiController(DnsWorker dnsWorker, DnsDbContext db, DeArrowService deArrowService, SponsorBlockService sponsorBlockService)
        {
            _dnsWorker = dnsWorker;
            _db = db;
            _deArrowService = deArrowService;
            _sponsorBlockService = sponsorBlockService;
        }

        /// <summary>
        /// GET /api/status - Service health check (no authentication required)
        /// </summary>
        [HttpGet("status")]
        public async Task<IActionResult> GetStatus()
        {
            var ip = Request.HttpContext.Connection.RemoteIpAddress?.ToString();
            var dnsActive = false;
            
            if (!string.IsNullOrEmpty(ip))
            {
                var fiveMinsAgo = DateTime.Now.AddMinutes(-5);
                dnsActive = await _db.QueryLogs.AnyAsync(q => q.SourceIP == ip && q.Timestamp >= fiveMinsAgo);
            }

            return Ok(new
            {
                version = Constants.AppVersion,
                uptime = GetUptime(),
                dnsEnabled = _dnsWorker.ProtectionEnabled,
                dohEnabled = _dnsWorker.UpstreamProtocol == "DoH",
                dnssecEnabled = _dnsWorker.EnforceDnssec,
                blockedDomains = _dnsWorker.GetBlockedDomainCount(),
                clientDnsActive = dnsActive,
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
            var today = DateTime.UtcNow.Date;
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
        public async Task<IActionResult> BlockDomain([FromBody] BlockDomainRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Domain))
                return BadRequest(new { error = "Domain is required" });

            // Validate domain format
            if (!IsValidDomain(request.Domain))
                return BadRequest(new { error = "Invalid domain format" });

            // Add to persistent blacklist
            if (!await _db.BlacklistedDomains.AnyAsync(b => b.Domain == request.Domain))
            {
                _db.BlacklistedDomains.Add(new BlacklistedDomain
                {
                    Domain = request.Domain.ToLowerInvariant(),
                    Reason = request.Reason ?? "Blocked from extension",
                    AddedAt = DateTime.UtcNow
                });
                await _db.SaveChangesAsync();
                await _dnsWorker.RefreshBlacklistAsync();
            }

            // Also add to in-memory blocklist for immediate effect
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
            await LogProxyActivity(videoID);
            var query = new Dictionary<string, string> { { "videoID", videoID } };
            var result = await _deArrowService.ProxyV1Async("getThumbnail", query);
            if (string.IsNullOrEmpty(result)) return NotFound();
            return Content(result, "application/json");
        }

        [HttpGet("dearrow/v1/branding")]
        public async Task<IActionResult> GetDeArrowV1Branding([FromQuery] string videoID)
        {
            await LogProxyActivity(videoID);
            var query = new Dictionary<string, string> { { "videoID", videoID } };
            var result = await _deArrowService.ProxyV1Async("branding", query);
            if (string.IsNullOrEmpty(result)) return NotFound();
            return Content(result, "application/json");
        }

        [HttpGet("sponsorblock/skipSegments")]
        public async Task<IActionResult> GetSponsorBlockVideoSegments([FromQuery] string videoID)
        {
            await LogProxyActivity(videoID);
            var query = new Dictionary<string, string> { { "videoID", videoID } };
            var result = await _sponsorBlockService.ProxyAsync("skipSegments", query);
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
                version = "2026.01.25.2",
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
        /// POST /api/heartbeat - Client identification and status
        /// </summary>
        [HttpPost("heartbeat")]
        public async Task<IActionResult> DeviceHeartbeat([FromBody] HeartbeatRequest request)
        {
            if (string.IsNullOrEmpty(request.ClientId))
                return BadRequest(new { error = "ClientId is required" });

            var ip = Request.HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown";
            
            var device = await _db.Devices.FindAsync(request.ClientId);
            if (device == null)
            {
                device = new DeviceInfo { Id = request.ClientId };
                _db.Devices.Add(device);
            }

            device.MachineName = request.MachineName ?? "Unknown Machine";
            device.UserName = request.UserName ?? "Unknown User";
            device.LastIP = ip;
            device.ExtensionVersion = request.Version;
            device.LastSeen = DateTime.UtcNow;

            await _db.SaveChangesAsync();
            await _dnsWorker.RefreshDeviceMapAsync();

            return Ok(new { success = true, registeredAs = device.MachineName, serverVersion = Constants.AppVersion });
        }

        /// <summary>
        /// POST /api/youtube-stats - Report YouTube ad blocking statistics
        /// </summary>
        [HttpPost("youtube-stats")]
        public async Task<IActionResult> ReportYouTubeStats([FromBody] YouTubeStatsRequest request)
        {
            var logPath = Path.Combine(AppContext.BaseDirectory, "youtube_ingest.log");
            try 
            {
                // Persist to database
                var stat = new YouTubeStat
                {
                    AdsBlocked = request.AdsBlocked,
                    AdsFailed = request.AdsFailed,
                    SponsorsSkipped = request.SponsorsSkipped,
                    TitlesCleaned = request.TitlesCleaned,
                    ThumbnailsReplaced = request.ThumbnailsReplaced,
                    TimeSavedSeconds = request.TimeSavedSeconds,
                    Timestamp = DateTime.UtcNow,
                    FilterVersion = request.FilterVersion ?? "unknown",
                    DeviceName = request.MachineName ?? Request.Headers["User-Agent"].ToString() ?? "Unknown Extension"
                };

                _db.YouTubeStats.Add(stat);
                await _db.SaveChangesAsync();

                System.IO.File.AppendAllText(logPath, $"[{DateTime.Now}] SUCCESS: Logged {request.AdsBlocked} ads from {stat.DeviceName}\n");
                return Ok(new { success = true, received = DateTime.UtcNow });
            }
            catch (Exception ex)
            {
                System.IO.File.AppendAllText(logPath, $"[{DateTime.Now}] ERROR: {ex.Message}\n");
                return StatusCode(500, new { error = "Database write failed" });
            }
        }

        /// <summary>
        /// POST /api/youtube/activity - Report YouTube watch activity
        /// </summary>
        [HttpPost("youtube/activity")]
        public async Task<IActionResult> ReportYouTubeActivity([FromBody] YouTubeActivityRequest request)
        {
            try
            {
                var activity = new YouTubeActivity
                {
                    VideoId = request.VideoId,
                    Title = request.Title,
                    Channel = request.Channel,
                    DurationSeconds = request.DurationSeconds,
                    DeviceName = request.MachineName ?? Request.Headers["User-Agent"].ToString() ?? "Unknown Extension",
                    YouTubeHandle = request.YouTubeUser,
                    Timestamp = DateTime.UtcNow
                };

                _db.YouTubeActivities.Add(activity);
                await _db.SaveChangesAsync();

                return Ok(new { success = true });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Database write failed" });
            }
        }

        /// <summary>
        /// POST /api/youtube/ad-event - Report detailed YouTube ad blocking event
        /// </summary>
        [HttpPost("youtube/ad-event")]
        public async Task<IActionResult> ReportYouTubeAdEvent([FromBody] YouTubeAdEventRequest request)
        {
            try
            {
                var adEvent = new YouTubeAdEvent
                {
                    VideoId = request.VideoId,
                    AdType = request.AdType,
                    ActionTaken = request.ActionTaken,
                    Metadata = request.Metadata,
                    DeviceName = request.MachineName ?? Request.Headers["User-Agent"].ToString() ?? "Unknown Extension",
                    YouTubeHandle = request.YouTubeUser,
                    Timestamp = DateTime.UtcNow
                };

                _db.YouTubeAdEvents.Add(adEvent);
                await _db.SaveChangesAsync();

                return Ok(new { success = true });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Database write failed" });
            }
        }

        /// <summary>
        /// GET /api/youtube/history - Get YouTube watch history for search augmentation
        /// </summary>
        [HttpGet("youtube/history")]
        public async Task<IActionResult> GetYouTubeHistory([FromQuery] string? query, [FromQuery] string? user, [FromQuery] int limit = 100)
        {
            var history = _db.YouTubeActivities.AsQueryable();

            if (!string.IsNullOrEmpty(user))
            {
                history = history.Where(h => h.YouTubeHandle == user);
            }
            
            if (!string.IsNullOrEmpty(query))
            {
                history = history.Where(a => a.Title.Contains(query) || a.Channel.Contains(query));
            }

            var results = await history
                .OrderByDescending(a => a.Timestamp)
                .Take(limit)
                .Select(a => new { a.VideoId, a.Title, a.Channel, a.Timestamp })
                .ToListAsync();

            return Ok(results);
        }

        /// <summary>
        /// DELETE /api/youtube/history - Clear all YouTube watch history and ad events
        /// </summary>
        [HttpDelete("youtube/history")]
        public async Task<IActionResult> ClearYouTubeHistory()
        {
            _db.YouTubeActivities.RemoveRange(_db.YouTubeActivities);
            _db.YouTubeAdEvents.RemoveRange(_db.YouTubeAdEvents);
            await _db.SaveChangesAsync();
            return Ok(new { success = true });
        }

        /// <summary>
        /// GET /api/youtube/mappings - Get all IP-to-Profile mappings
        /// </summary>
        [HttpGet("youtube/mappings")]
        public async Task<IActionResult> GetYouTubeMappings()
        {
            var mappings = await _db.YouTubeProfileMappings.OrderByDescending(m => m.LastUsed).ToListAsync();
            return Ok(mappings);
        }

        /// <summary>
        /// POST /api/youtube/mappings - Create or update a profile mapping
        /// </summary>
        [HttpPost("youtube/mappings")]
        public async Task<IActionResult> UpdateYouTubeMapping([FromBody] MappingRequest request)
        {
            if (string.IsNullOrEmpty(request.DeviceIdentifier) || string.IsNullOrEmpty(request.YouTubeHandle))
                return BadRequest("DeviceIdentifier and YouTubeHandle are required");

            var existing = await _db.YouTubeProfileMappings
                .FirstOrDefaultAsync(m => m.DeviceIdentifier == request.DeviceIdentifier);

            if (existing != null)
            {
                existing.YouTubeHandle = request.YouTubeHandle;
                existing.LastUsed = DateTime.UtcNow;
            }
            else
            {
                _db.YouTubeProfileMappings.Add(new YouTubeProfileMapping
                {
                    DeviceIdentifier = request.DeviceIdentifier,
                    YouTubeHandle = request.YouTubeHandle,
                    LastUsed = DateTime.UtcNow
                });
            }

            await _db.SaveChangesAsync();
            return Ok(new { success = true });
        }

        /// <summary>
        /// DELETE /api/youtube/mappings/{id} - Remove a profile mapping
        /// </summary>
        [HttpDelete("youtube/mappings/{id}")]
        public async Task<IActionResult> DeleteYouTubeMapping(int id)
        {
            var mapping = await _db.YouTubeProfileMappings.FindAsync(id);
            if (mapping != null)
            {
                _db.YouTubeProfileMappings.Remove(mapping);
                await _db.SaveChangesAsync();
            }
            return Ok(new { success = true });
        }

        /// <summary>
        /// GET /api/youtube/unmapped-devices - Find proxy IPs without a profile assigned
        /// </summary>
        [HttpGet("youtube/unmapped-devices")]
        public async Task<IActionResult> GetUnmappedDevices()
        {
            var mappedIps = await _db.YouTubeProfileMappings.Select(m => m.DeviceIdentifier).ToListAsync();
            
            var unmappedIps = await _db.YouTubeActivities
                .Where(a => a.DeviceName.StartsWith("Proxy:"))
                .ToListAsync();

            var distinctUnmapped = unmappedIps
                .Select(a => a.DeviceName.Replace("Proxy: ", ""))
                .Distinct()
                .Where(ip => !mappedIps.Contains(ip))
                .ToList();

            return Ok(distinctUnmapped);
        }

        public class MappingRequest {
            public string DeviceIdentifier { get; set; } = string.Empty;
            public string YouTubeHandle { get; set; } = string.Empty;
        }

        /// <summary>
        /// POST /api/sentinel/report - Report tracker detection or DNS bypass
        /// </summary>
        [HttpPost("sentinel/report")]
        public async Task<IActionResult> ReportSentinel([FromBody] SentinelReportRequest request)
        {
            try
            {
                var report = new SentinelReport
                {
                    Timestamp = DateTime.UtcNow,
                    ClientId = request.ClientId,
                    Domain = request.Domain.ToLowerInvariant(),
                    ReportType = request.ReportType,
                    PageUrl = request.PageUrl,
                    Metadata = request.Metadata
                };

                // If it's a confirmed tracker, we can auto-block it network-wide
                if (request.ReportType == "Tracker")
                {
                    if (!await _db.BlacklistedDomains.AnyAsync(b => b.Domain == report.Domain))
                    {
                        report.IsAutoBlocked = true;
                        _db.BlacklistedDomains.Add(new BlacklistedDomain
                        {
                            Domain = report.Domain,
                            Reason = $"Sentinel: Tracker detected on {request.PageUrl}",
                            AddedAt = DateTime.UtcNow
                        });
                        await _dnsWorker.RefreshBlacklistAsync();
                    }
                }

                _db.SentinelReports.Add(report);
                await _db.SaveChangesAsync();

                return Ok(new { success = true, autoBlocked = report.IsAutoBlocked });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        /// <summary>
        /// GET /api/sentinel/audit - Check if domains resolved in browser were logged by DNS
        /// </summary>
        [HttpGet("sentinel/audit")]
        public async Task<IActionResult> AuditBypasses([FromQuery] string domain, [FromQuery] string clientId)
        {
            // Look for this domain in the DNS logs for this client in the last 60 seconds
            var recent = DateTime.UtcNow.AddSeconds(-60);
            var wasLogged = await _db.QueryLogs
                .AnyAsync(q => q.Domain.Contains(domain) && q.Timestamp >= recent);

            return Ok(new { 
                domain, 
                wasLogged, 
                recommendation = wasLogged ? "None" : "DoH Bypass Detected - Inspect Browser Settings"
            });
        }

        /// <summary>
        /// GET /api/download/extension - Direct download for the browser extension
        /// </summary>
        [HttpGet("download/extension")]
        public IActionResult DownloadExtension()
        {
            var fileName = $"DNSAgent_Extension_v{Constants.AppVersion}.zip";
            
            // Comprehensive path search (standard publish, source dev, or sibling)
            var paths = new List<string>
            {
                Path.Combine(AppContext.BaseDirectory, "wwwroot", "assets", fileName),
                Path.Combine(AppContext.BaseDirectory, "assets", fileName),
                Path.Combine(Directory.GetParent(AppContext.BaseDirectory)?.FullName ?? "", "wwwroot", "assets", fileName),
                Path.Combine(Directory.GetParent(AppContext.BaseDirectory)?.Parent?.Parent?.Parent?.FullName ?? "", "DNSAgent.Service", "wwwroot", "assets", fileName),
                Path.Combine(AppContext.BaseDirectory, fileName)
            };

            // Add recursive search for common patterns
            var searchRoots = new[] { 
                AppContext.BaseDirectory, 
                Directory.GetParent(AppContext.BaseDirectory)?.FullName ?? ""
            };

            foreach (var path in paths)
            {
                if (System.IO.File.Exists(path)) return PhysicalFile(path, "application/zip", fileName);
            }

            // Final fallback: Search for ANY v* zip matching the pattern in common areas
            foreach (var root in searchRoots)
            {
                if (Directory.Exists(root))
                {
                    var file = Directory.GetFiles(root, "DNSAgent_Extension_v*.zip", SearchOption.AllDirectories)
                        .OrderByDescending(f => f).FirstOrDefault();
                    if (file != null) return PhysicalFile(file, "application/zip", Path.GetFileName(file));
                }
            }

            return NotFound($"Extension package not found ({fileName}). BaseDir: {AppContext.BaseDirectory}");
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

        private async Task LogProxyActivity(string videoId)
        {
            if (string.IsNullOrEmpty(videoId)) return;

            // Only log if not already logged in last 5 minutes to avoid spam from multiple proxy calls
            var recentlyLogged = await _db.YouTubeActivities
                .AnyAsync(a => a.VideoId == videoId && a.Timestamp >= DateTime.UtcNow.AddMinutes(-5));

            if (!recentlyLogged)
            {
                var ip = Request.HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown";
                
                // Check if this IP is mapped to a specific profile (e.g. SmartTube on a specific TV)
                var mapping = await _db.YouTubeProfileMappings
                    .FirstOrDefaultAsync(m => m.DeviceIdentifier == ip);

                var handle = mapping?.YouTubeHandle ?? "[Proxy Captured]";

                _db.YouTubeActivities.Add(new YouTubeActivity
                {
                    VideoId = videoId,
                    Timestamp = DateTime.UtcNow,
                    DeviceName = $"Proxy: {ip}",
                    YouTubeHandle = handle,
                    Title = "[Proxy Captured]",
                    Channel = "[Proxy Captured]"
                });
                await _db.SaveChangesAsync();
            }
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
        public string? FilterVersion { get; set; }
        public string? MachineName { get; set; }
    }

    public class SentinelReportRequest
    {
        public string ClientId { get; set; } = string.Empty;
        public string Domain { get; set; } = string.Empty;
        public string ReportType { get; set; } = "Tracker";
        public string? PageUrl { get; set; }
        public string? Metadata { get; set; }
    }

    public class HeartbeatRequest
    {
        public string ClientId { get; set; } = string.Empty;
        public string? MachineName { get; set; }
        public string? UserName { get; set; }
        public string? Version { get; set; }
    }

    public class YouTubeActivityRequest
    {
        public string VideoId { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Channel { get; set; } = string.Empty;
        public double DurationSeconds { get; set; }
        public string? MachineName { get; set; }
        public string? YouTubeUser { get; set; }
    }

    public class YouTubeAdEventRequest
    {
        public string? VideoId { get; set; }
        public string AdType { get; set; } = string.Empty;
        public string ActionTaken { get; set; } = string.Empty;
        public string? Metadata { get; set; }
        public string? MachineName { get; set; }
        public string? YouTubeUser { get; set; }
    }
}

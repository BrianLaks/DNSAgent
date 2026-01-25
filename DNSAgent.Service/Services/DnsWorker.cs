using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using DNSAgent.Service.Data;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using DNSAgent.Service.Configuration;

namespace DNSAgent.Service.Services
{
    public class DnsWorker : BackgroundService
    {
        private readonly ILogger<DnsWorker> _logger;
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly HashSet<string> _blockedDomains = new(StringComparer.OrdinalIgnoreCase);
        // Optimize: Use a concurrent dictionary or similar if we want thread-safe updates from UI
        // For now, reload occasionally or use a reader/writer lock.
        private readonly Lock _listLock = new(); 

        private readonly IOptions<DnsAgentSettings> _settings;

        public bool ProtectionEnabled { get; set; } = true;
        public bool EnforceDnssec 
        { 
            get => _settings.Value.EnforceDnssec; 
            set => _settings.Value.EnforceDnssec = value; 
        }

        public DnsWorker(ILogger<DnsWorker> logger, IServiceScopeFactory scopeFactory, Microsoft.Extensions.Options.IOptions<DnsAgentSettings> settings)
        {
            _logger = logger;
            _scopeFactory = scopeFactory;
            _settings = settings;
            ProtectionEnabled = _settings.Value.EnableBlocking;
        }

        public void UpdateSecuritySettings(string protocol, string dohUrl, bool dnssec)
        {
            _settings.Value.UpstreamProtocol = protocol;
            _settings.Value.DoHUrl = dohUrl;
            _settings.Value.EnforceDnssec = dnssec;
            _logger.LogInformation("Security settings updated: Protocol={Protocol}, DNSSEC={DNSSEC}", protocol, dnssec);
        }


        private readonly HashSet<string> _whitelistedDomains = new(StringComparer.OrdinalIgnoreCase);

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("DNS Worker starting...");
            await LoadBlockListAsync();
            await RefreshWhitelistAsync(); // Load initial whitelist

            IPEndPoint localEndPoint = new IPEndPoint(IPAddress.Any, 53);
            
            try 
            {
                using var listener = new UdpClient(localEndPoint);
                _logger.LogInformation("Listening on 0.0.0.0:53");

                // Background Tasks loop
                _ = Task.Run(async () => 
                {
                    while (!stoppingToken.IsCancellationRequested)
                    {
                        await CleanupOldLogsAsync();
                        // Also auto-refresh blocklist daily
                        await LoadBlockListAsync();
                        await Task.Delay(TimeSpan.FromHours(24), stoppingToken);
                    }
                }, stoppingToken);

                while (!stoppingToken.IsCancellationRequested)
                {
                    try
                    {
                        var result = await listener.ReceiveAsync(stoppingToken);
                        _ = HandleRequestAsync(listener, result.Buffer, result.RemoteEndPoint);
                    }
                    catch (OperationCanceledException) { break; }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error receiving DNS packet");
                    }
                }
            }
            catch (SocketException ex)
            {
                _logger.LogCritical("Could not bind to port 53: {Message}", ex.Message);
            }
        }

        public async Task LoadBlockListAsync()
        {
            _logger.LogInformation("Loading blocklists and threat feeds...");
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var config = scope.ServiceProvider.GetRequiredService<Microsoft.Extensions.Configuration.IConfiguration>();
                
                var sources = new List<string> {
                    config.GetValue<string>("DnsAgent:BlocklistUrl") ?? "https://raw.githubusercontent.com/StevenBlack/hosts/master/hosts",
                    "https://small.oisd.nl/" // OISD Basic Threat Feed
                };

                int totalCount = 0;
                using var client = new HttpClient();
                
                var newBlockedDomains = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

                foreach (var url in sources)
                {
                    try
                    {
                        _logger.LogInformation("Fetching source: {Url}", url);
                        var content = await client.GetStringAsync(url);
                        using var reader = new StringReader(content);
                        string? line;
                        
                        while ((line = reader.ReadLine()) != null)
                        {
                            line = line.Trim();
                            if (string.IsNullOrEmpty(line) || line.StartsWith("#")) continue;
                            
                            var parts = line.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
                            
                            // Handle standard hosts format (0.0.0.0 domain) or simple domain list (oisd)
                            if (parts.Length >= 2 && (parts[0] == "0.0.0.0" || parts[0] == "127.0.0.1"))
                            {
                                if (parts[1] != "0.0.0.0" && parts[1] != "127.0.0.1" && parts[1] != "localhost")
                                {
                                    newBlockedDomains.Add(parts[1]);
                                    totalCount++;
                                }
                            }
                            else if (parts.Length == 1 && !parts[0].Contains("/"))
                            {
                                newBlockedDomains.Add(parts[0]);
                                totalCount++;
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning("Failed to load source {Url}: {Message}", url, ex.Message);
                    }
                }

                lock (_listLock)
                {
                    _blockedDomains.Clear();
                    foreach (var domain in newBlockedDomains)
                    {
                         _blockedDomains.Add(domain);
                    }
                }
                _logger.LogInformation("Total threat protection active: {Count} domains.", _blockedDomains.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load blocklists");
            }
        }

        private async Task CleanupOldLogsAsync()
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<DnsDbContext>();
                var config = scope.ServiceProvider.GetRequiredService<Microsoft.Extensions.Configuration.IConfiguration>();
                var days = config.GetValue<int>("DnsAgent:LogRetentionDays", 5);

                var cutoff = DateTime.Now.AddDays(-days);
                var oldLogs = db.QueryLogs.Where(l => l.Timestamp < cutoff);
                
                int count = await oldLogs.CountAsync();
                if (count > 0)
                {
                    db.QueryLogs.RemoveRange(oldLogs);
                    await db.SaveChangesAsync();
                    _logger.LogInformation("Cleaned up {Count} logs older than {Days} days.", count, days);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to cleanup old logs");
            }
        }

        public async Task RefreshWhitelistAsync()
        {
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<DnsDbContext>();
            var list = await db.WhitelistedDomains.ToListAsync();
            
            lock (_listLock)
            {
                _whitelistedDomains.Clear();
                foreach (var item in list)
                {
                    _whitelistedDomains.Add(item.Domain);
                }
            }
            _logger.LogInformation("Refreshed whitelist. {Count} domains.", _whitelistedDomains.Count);
        }

        private async Task HandleRequestAsync(UdpClient listener, byte[] buffer, IPEndPoint clientEndPoint)
        {
            string domain = ParseDomain(buffer);
            bool blocked = false;
            
            if (!string.IsNullOrEmpty(domain))
            {
                if (!ProtectionEnabled)
                {
                    blocked = false;
                }
                else
                {
                    lock (_listLock)
                    {
                        // Whitelist takes precedence
                        if (_whitelistedDomains.Contains(domain))
                        {
                            blocked = false;
                        }
                        else if (_blockedDomains.Contains(domain))
                        {
                            blocked = true;
                        }
                    }
                }
            }


        // Log to DB
            _ = LogQueryAsync(clientEndPoint.Address.ToString(), domain, blocked ? "Blocked" : "Allowed", "Local", false);

            if (blocked)
            {
                byte[] response = CreateNxDomainResponse(buffer);
                await listener.SendAsync(response, response.Length, clientEndPoint);
                _logger.LogInformation("Blocked: {Domain} from {Client}", domain, clientEndPoint);
            }
            else
            {
                // Forward
                await ForwardToUpstreamAsync(listener, buffer, clientEndPoint);
            }
        }

        private readonly ConcurrentDictionary<string, string> _hostnameCache = new();

        private async Task<string> GetHostnameAsync(string ip)
        {
            if (_hostnameCache.TryGetValue(ip, out var cached)) return cached;

            try
            {
                var entry = await Dns.GetHostEntryAsync(ip);
                _hostnameCache[ip] = entry.HostName;
                return entry.HostName;
            }
            catch
            {
                return ip; // Fallback to IP if lookup fails
            }
        }

        private async Task LogQueryAsync(string ip, string domain, string status, string transport, bool isDnssec)
        {
            try
            {
                string hostname = await GetHostnameAsync(ip);

                using var scope = _scopeFactory.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<DnsDbContext>();
                db.QueryLogs.Add(new QueryLog 
                { 
                    Timestamp = DateTime.Now, 
                    SourceIP = ip, 
                    SourceHostname = hostname,
                    Domain = domain, 
                    Status = status,
                    Transport = transport,
                    IsDnssec = isDnssec
                });
                await db.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to log query to DB");
            }
        }

        private async Task ForwardToUpstreamAsync(UdpClient listener, byte[] buffer, IPEndPoint clientEndPoint)
        {
            string domain = ParseDomain(buffer);
            try
            {
                if (_settings.Value.UpstreamProtocol == "DoH")
                {
                    await ForwardToDoHAsync(listener, buffer, clientEndPoint);
                }
                else
                {
                    using var upstreamClient = new UdpClient();
                    await upstreamClient.SendAsync(buffer, buffer.Length, _settings.Value.UpstreamDns, 53);

                    var receiveTask = upstreamClient.ReceiveAsync();
                    var timeoutTask = Task.Delay(2000);

                    if (await Task.WhenAny(receiveTask, timeoutTask) == receiveTask)
                    {
                        var result = await receiveTask;
                        bool isDnssec = (result.Buffer.Length > 3 && (result.Buffer[3] & 0x20) == 0x20);
                        
                        _ = LogQueryAsync(clientEndPoint.Address.ToString(), domain, "Allowed", "UDP", isDnssec);
                        await listener.SendAsync(result.Buffer, result.Buffer.Length, clientEndPoint);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to forward query to upstream");
            }
        }

        private async Task ForwardToDoHAsync(UdpClient listener, byte[] buffer, IPEndPoint clientEndPoint)
        {
            string domain = ParseDomain(buffer);
            try
            {
                using var client = new HttpClient();
                var content = new ByteArrayContent(buffer);
                content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/dns-message");

                var response = await client.PostAsync(_settings.Value.DoHUrl, content);
                if (response.IsSuccessStatusCode)
                {
                    var responseData = await response.Content.ReadAsByteArrayAsync();
                    bool isDnssec = (responseData.Length > 3 && (responseData[3] & 0x20) == 0x20);

                    _ = LogQueryAsync(clientEndPoint.Address.ToString(), domain, "Allowed", "DoH", isDnssec);
                    await listener.SendAsync(responseData, responseData.Length, clientEndPoint);
                }
                else
                {
                    _logger.LogWarning("DoH request failed with status: {Status}", response.StatusCode);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in DoH forwarding");
            }
        }

        // --- Helpers from DnsPacket.cs (embedded here for simplicity/service access) ---
        private string ParseDomain(byte[] buffer)
        {
            try
            {
                if (buffer.Length < 12) return null;
                int offset = 12;
                System.Text.StringBuilder domain = new();
                while (offset < buffer.Length)
                {
                    byte len = buffer[offset++];
                    if (len == 0) break;
                    if ((len & 0xC0) == 0xC0) return null; // Compression not supported in question
                    if (domain.Length > 0) domain.Append('.');
                    for (int i = 0; i < len; i++)
                    {
                        if (offset >= buffer.Length) return null;
                        domain.Append((char)buffer[offset++]);
                    }
                }
                return domain.ToString();
            }
            catch { return null; }
        }

        private byte[] CreateNxDomainResponse(byte[] request)
        {
             byte[] response = new byte[request.Length];
            Array.Copy(request, response, request.Length);
            response[2] = (byte)(request[2] | 0x80); // QR=1
            response[3] = 0x83; // RA=1, RCode=3 (NXDOMAIN)
            return response;
        }
    }
}

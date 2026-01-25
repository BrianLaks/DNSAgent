using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using DNSAgent.Web.Data;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Linq;
using Microsoft.EntityFrameworkCore;

namespace DNSAgent.Web.Services
{
    public class DnsWorker : BackgroundService
    {
        private readonly ILogger<DnsWorker> _logger;
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly HashSet<string> _blockedDomains = new(StringComparer.OrdinalIgnoreCase);
        // Optimize: Use a concurrent dictionary or similar if we want thread-safe updates from UI
        // For now, reload occasionally or use a reader/writer lock.
        private readonly Lock _listLock = new(); 

        public DnsWorker(ILogger<DnsWorker> logger, IServiceScopeFactory scopeFactory)
        {
            _logger = logger;
            _scopeFactory = scopeFactory;
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

        private async Task LoadBlockListAsync()
        {
            _logger.LogInformation("Loading blocklist...");
            try
            {
                // TODO: Store this URL in DB/Settings
                string url = "https://raw.githubusercontent.com/StevenBlack/hosts/master/hosts";
                using var client = new HttpClient();
                var content = await client.GetStringAsync(url);
                
                using var reader = new StringReader(content);
                string line;
                int count = 0;
                
                lock (_listLock)
                {
                    _blockedDomains.Clear();
                    while ((line = reader.ReadLine()) != null)
                    {
                        line = line.Trim();
                        if (string.IsNullOrEmpty(line) || line.StartsWith("#")) continue;
                        
                        var parts = line.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
                        if (parts.Length >= 2 && parts[0] == "0.0.0.0" && parts[1] != "0.0.0.0")
                        {
                            _blockedDomains.Add(parts[1]);
                            count++;
                        }
                    }
                }
                _logger.LogInformation("Loaded {Count} domains into blocklist.", count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load blocklist");
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


            // Log to DB
            _ = LogQueryAsync(clientEndPoint.Address.ToString(), domain, blocked ? "Blocked" : "Allowed");

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

        private async Task LogQueryAsync(string ip, string domain, string status)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<DnsDbContext>();
                db.QueryLogs.Add(new QueryLog 
                { 
                    Timestamp = DateTime.Now, 
                    SourceIP = ip, 
                    Domain = domain, 
                    Status = status 
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
             try
            {
                using var upstreamClient = new UdpClient();
                // Google DNS
                await upstreamClient.SendAsync(buffer, buffer.Length, "8.8.8.8", 53);

                var receiveTask = upstreamClient.ReceiveAsync();
                var timeoutTask = Task.Delay(2000);

                if (await Task.WhenAny(receiveTask, timeoutTask) == receiveTask)
                {
                    var result = await receiveTask;
                    await listener.SendAsync(result.Buffer, result.Buffer.Length, clientEndPoint);
                }
            }
            catch { /* Ignore */ }
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

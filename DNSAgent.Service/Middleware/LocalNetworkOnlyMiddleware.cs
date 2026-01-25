using Microsoft.AspNetCore.Http;
using System.Net;
using System.Threading.Tasks;

namespace DNSAgent.Service.Middleware
{
    /// <summary>
    /// Middleware to restrict API access to local network IP addresses only
    /// Allows: 127.0.0.1, 192.168.x.x, 10.x.x.x, 172.16-31.x.x
    /// </summary>
    public class LocalNetworkOnlyMiddleware
    {
        private readonly RequestDelegate _next;

        public LocalNetworkOnlyMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            // Only apply to /api/* endpoints
            if (context.Request.Path.StartsWithSegments("/api"))
            {
                var remoteIp = context.Connection.RemoteIpAddress;

                if (remoteIp == null || !IsLocalNetwork(remoteIp))
                {
                    context.Response.StatusCode = 403;
                    await context.Response.WriteAsJsonAsync(new
                    {
                        error = "Forbidden",
                        message = "API access is restricted to local network only"
                    });
                    return;
                }
            }

            await _next(context);
        }

        private bool IsLocalNetwork(IPAddress ipAddress)
        {
            // Handle IPv4-mapped IPv6 addresses (::ffff:192.168.1.1)
            if (ipAddress.IsIPv4MappedToIPv6)
            {
                ipAddress = ipAddress.MapToIPv4();
            }

            // Localhost
            if (IPAddress.IsLoopback(ipAddress))
                return true;

            // IPv6 localhost
            if (ipAddress.Equals(IPAddress.IPv6Loopback))
                return true;

            // Only check IPv4 private ranges
            if (ipAddress.AddressFamily != System.Net.Sockets.AddressFamily.InterNetwork)
                return false;

            byte[] bytes = ipAddress.GetAddressBytes();

            // 10.0.0.0/8
            if (bytes[0] == 10)
                return true;

            // 172.16.0.0/12
            if (bytes[0] == 172 && bytes[1] >= 16 && bytes[1] <= 31)
                return true;

            // 192.168.0.0/16
            if (bytes[0] == 192 && bytes[1] == 168)
                return true;

            // 169.254.0.0/16 (link-local)
            if (bytes[0] == 169 && bytes[1] == 254)
                return true;

            return false;
        }
    }
}

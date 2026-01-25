using System;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;

namespace DNSAgent.Service.Services
{
    public class DeArrowService
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<DeArrowService> _logger;
        private const string ApiBaseUrl = "https://dearrow.ajay.app/api/v1/";

        public DeArrowService(HttpClient httpClient, ILogger<DeArrowService> logger)
        {
            _httpClient = httpClient;
            _logger = logger;
            _httpClient.DefaultRequestHeaders.Add("User-Agent", "DNSAgent/1.6");
        }

        /// <summary>
        /// Proxies branding requests to DeArrow using a SHA256 hash prefix for privacy (K-anonymity)
        /// </summary>
        public async Task<string> GetBrandingAsync(string hashPrefix)
        {
            try
            {
                var response = await _httpClient.GetStringAsync($"{ApiBaseUrl}branding/{hashPrefix}");
                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching DeArrow branding for prefix {Prefix}", hashPrefix);
                return string.Empty;
            }
        }

        /// <summary>
        /// General proxy for DeArrow V1 endpoints
        /// </summary>
        public async Task<string> ProxyV1Async(string endpoint, IDictionary<string, string>? queryParams = null)
        {
            try
            {
                var url = $"{ApiBaseUrl}{endpoint}";
                if (queryParams != null && queryParams.Count > 0)
                {
                    var query = await new FormUrlEncodedContent(queryParams).ReadAsStringAsync();
                    url += "?" + query;
                }

                var response = await _httpClient.GetStringAsync(url);
                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error proxying DeArrow endpoint {Endpoint}", endpoint);
                return string.Empty;
            }
        }
    }
}

using System;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;

namespace DNSAgent.Service.Services
{
    public class SponsorBlockService
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<SponsorBlockService> _logger;
        private const string ApiBaseUrl = "https://sponsor.ajay.app/api/";

        public SponsorBlockService(HttpClient httpClient, ILogger<SponsorBlockService> _logger)
        {
            this._httpClient = httpClient;
            this._logger = _logger;
            this._httpClient.DefaultRequestHeaders.Add("User-Agent", "DNSAgent/1.6");
        }

        /// <summary>
        /// General proxy for SponsorBlock endpoints
        /// </summary>
        public async Task<string> ProxyAsync(string endpoint, IDictionary<string, string>? queryParams = null)
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
                _logger.LogError(ex, "Error proxying SponsorBlock endpoint {Endpoint}", endpoint);
                return string.Empty;
            }
        }
    }
}

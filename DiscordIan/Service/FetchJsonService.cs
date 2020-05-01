using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using DiscordIan.Model;
using Microsoft.Extensions.Logging;

namespace DiscordIan.Service
{
    public class FetchJsonService
    {
        private readonly ILogger<FetchJsonService> _logger;
        private readonly HttpClient _httpClient;

        public FetchJsonService(ILogger<FetchJsonService> logger,
            HttpClient httpClient)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            _httpClient.Timeout = TimeSpan.FromSeconds(10);
        }

        public async Task<JsonResponse<T>> GetAsync<T>(Uri requestUri,
            IDictionary<string, string> headers = null) where T : class
        {
            var stopwatch = Stopwatch.StartNew();
            var response = new JsonResponse<T>();
            try
            {
                if (headers?.Count > 0)
                {
                    foreach (var header in headers)
                    {
                        _httpClient.DefaultRequestHeaders.Add(header.Key, header.Value);
                    }
                }
                var httpResult = await _httpClient.GetAsync(requestUri);
                if (!httpResult.IsSuccessStatusCode)
                {
                    _logger.LogWarning("FetchJson failed with HTTP status {HttpStats} on request {RequestURI}",
                        httpResult.StatusCode,
                        requestUri);
                    response.IsSuccessful = false;
                    response.Elapsed = stopwatch.Elapsed;
                    response.Message = $"HTTP status code {httpResult.StatusCode}";
                }

                response.Data = await JsonSerializer.DeserializeAsync<T>(
                    await httpResult.Content.ReadAsStreamAsync(),
                        new JsonSerializerOptions
                        {
                            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                        });
                response.IsSuccessful = true;
                response.Elapsed = stopwatch.Elapsed;
                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "FetchJson errored trying to fetch {RequestURI}: {ErrorMessage}",
                    requestUri,
                    ex.Message);
                response.IsSuccessful = false;
                response.Message = ex.Message;
                response.Elapsed = stopwatch.Elapsed;
                return response;
            }
        }
    }
}

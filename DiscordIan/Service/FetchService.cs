using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using System.Xml.Serialization;
using DiscordIan.Model;
using Microsoft.Extensions.Logging;

namespace DiscordIan.Service
{
    public class FetchService
    {
        private const string json = "application/json";
        private const string xml = "application/xml";

        private readonly ILogger<FetchService> _logger;
        private readonly IHttpClientFactory _httpClientFactory;

        public FetchService(ILogger<FetchService> logger,
            IHttpClientFactory httpClientFactory)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _httpClientFactory = httpClientFactory 
                ?? throw new ArgumentNullException(nameof(httpClientFactory));
        }

        public async Task<Response<T>> GetAsync<T>(Uri requestUri,
            IDictionary<string, string> headers = null) where T : class
        {
            var stopwatch = Stopwatch.StartNew();
            var response = new Response<T>();

            using var _httpClient = _httpClientFactory.CreateClient();
            _httpClient.Timeout = TimeSpan.FromSeconds(10);

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
                    _logger.LogWarning("Fetch failed with HTTP status {HttpStats} on request {RequestURI}",
                        httpResult.StatusCode,
                        requestUri);
                    response.IsSuccessful = false;
                    response.Elapsed = stopwatch.Elapsed;
                    response.Message = $"HTTP status code {httpResult.StatusCode}";
                }

                response.Data = await DeserializeObjectAsync<T>(
                    httpResult.Content);
                response.IsSuccessful = true;
                response.Elapsed = stopwatch.Elapsed;
                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Fetch errored trying to fetch {RequestURI}: {ErrorMessage}",
                    requestUri,
                    ex.Message);
                response.IsSuccessful = false;
                response.Message = ex.Message;
                response.Elapsed = stopwatch.Elapsed;
                return response;
            }
        }

        public async Task<T> DeserializeObjectAsync<T>(HttpContent content) where T : class
        {
            if (content?.Headers?.ContentType?.MediaType == json)
            {
                var contentStream = await content.ReadAsStreamAsync();
                var deserialized = JsonSerializer.DeserializeAsync<T>(
                    contentStream,
                    new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });
                return deserialized.Result;
            }

            if (content?.Headers?.ContentType?.MediaType == xml)
            {
                var contentString = content.ReadAsStringAsync().Result;
                using TextReader reader = new StringReader(contentString);
                var xmlSerializer = new XmlSerializer(typeof(T));
                return xmlSerializer.Deserialize(reader) as T;
            }

            throw new Exception($"Unknown response type: {content?.Headers?.ContentType?.MediaType}");
        }
    }
}

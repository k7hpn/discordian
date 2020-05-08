using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Serialization;
using DiscordIan.Model;
using Microsoft.Extensions.Logging;

namespace DiscordIan.Service
{
    public class FetchXMLService
    {
        private readonly ILogger<FetchXMLService> _logger;
        private readonly HttpClient _httpClient;

        public FetchXMLService(ILogger<FetchXMLService> logger,
            HttpClient httpClient)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            _httpClient.Timeout = TimeSpan.FromSeconds(10);
        }

        public async Task<Response<T>> GetAsync<T>(Uri requestUri,
            IDictionary<string, string> headers = null) where T : class
        {
            var stopwatch = Stopwatch.StartNew();
            var response = new Response<T>();
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
                    _logger.LogWarning("FetchXML failed with HTTP status {HttpStats} on request {RequestURI}",
                        httpResult.StatusCode,
                        requestUri);
                    response.IsSuccessful = false;
                    response.Elapsed = stopwatch.Elapsed;
                    response.Message = $"HTTP status code {httpResult.StatusCode}";
                }
                
                response.Data = await DeserializeObjectAsync<T>(
                    await httpResult.Content.ReadAsStringAsync());
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

        public async Task<T> DeserializeObjectAsync<T>(string xml)
        {
            using (TextReader reader = new StringReader(xml))
            {
                XmlSerializer xmlSerializer =
                    new XmlSerializer(typeof(T));
                T xmlData = (T)xmlSerializer.Deserialize(reader);

                return xmlData;
            }
        }
        
    }
}

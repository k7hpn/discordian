using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Web;
using Discord.Commands;
using DiscordIan.Service;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Options;

namespace DiscordIan.Module
{
    public class Weather : BaseModule
    {
        private readonly IDistributedCache _cache;
        private readonly FetchJsonService _fetchJsonService;
        private readonly Model.Options _options;

        public Weather(IDistributedCache cache,
            FetchJsonService fetchJsonService,
            IOptionsMonitor<Model.Options> optionsAccessor)
        {
            _cache = cache ?? throw new ArgumentNullException(nameof(cache));
            _fetchJsonService = fetchJsonService
                ?? throw new ArgumentNullException(nameof(fetchJsonService));
            _options = optionsAccessor.CurrentValue
                ?? throw new ArgumentNullException(nameof(optionsAccessor));
        }

        private string WeatherCacheKey
        {
            get
            {
                return string.Format(Key.Cache.Weather, Context.User.Id);
            }
        }

        private string WeatherLocaleCacheKey
        {
            get
            {
                return string.Format(Key.Cache.WeatherLocale, Context.User.Id);
            }
        }

        [Command("wx", RunMode = RunMode.Async)]
        [Summary("Look up a weather forcast for a provided address.")]
        [Alias("weather", "w")]
        public async Task WeatherAsync([Remainder]
            [Summary("The address or location for current weather conditions")] string location)
        {
            Model.Geocodio.Location coordinates = null;

            string forecast = null;
            string locale = null;

            if (string.IsNullOrEmpty(location))
            {
                forecast = await _cache.GetStringAsync(WeatherCacheKey);
                locale = await _cache.GetStringAsync(WeatherLocaleCacheKey);
            }

            if (string.IsNullOrEmpty(forecast) && string.IsNullOrEmpty(location))
            {
                await ReplyAsync("Please provide a location.");
                return;
            }

            if (string.IsNullOrEmpty(forecast))
            {
                try
                {
                    coordinates = await GeocodeAddressAsync(location);
                    if (coordinates == null)
                    {
                        throw new Exception();
                    }
                }
                catch (Exception ex)
                {
                    string details = null;
                    if (!string.IsNullOrEmpty(ex.Message))
                    {
                        details = $" ({ex.Message})";
                    }
                    await ReplyAsync("Unable to geocode address" + details);
                    return;
                }
            }
            var (message, embed) = await GetCurrentAsync(coordinates, forecast, locale);
            if (message == null)
            {
                await ReplyAsync("No weather found, sorry!");
            }
            else
            {
                await ReplyAsync(message, false, embed);
            }
        }

        [Command("f", RunMode = RunMode.Async)]
        [Summary("Look up a weather forcast for a provided address.")]
        [Alias("fx", "forecast")]
        public async Task ForecastAsync([Remainder]
            [Summary("The address or location for a weather forcast")] string location)
        {
            Model.Geocodio.Location coordinates = null;

            string forecast = null;
            string locale = null;

            if (string.IsNullOrEmpty(location))
            {
                forecast = await _cache.GetStringAsync(WeatherCacheKey);
                locale = await _cache.GetStringAsync(WeatherLocaleCacheKey);
            }

            if (string.IsNullOrEmpty(forecast) && string.IsNullOrEmpty(location))
            {
                await ReplyAsync("Please provide a location.");
                return;
            }

            if (string.IsNullOrEmpty(forecast))
            {
                try
                {
                    coordinates = await GeocodeAddressAsync(location);
                    if (coordinates == null)
                    {
                        throw new Exception();
                    }
                }
                catch (Exception ex)
                {
                    string details = null;
                    if (!string.IsNullOrEmpty(ex.Message))
                    {
                        details = $" ({ex.Message})";
                    }
                    await ReplyAsync("Unable to geocode address" + details);
                    return;
                }
            }
            var (message, embed) = await GetForecastAsync(coordinates, forecast, locale);
            if (message == null)
            {
                await ReplyAsync("No weather found, sorry!");
            }
            else
            {
                await ReplyAsync(message, false, embed);
            }
        }

        private async Task<Model.Geocodio.Location> GeocodeAddressAsync(string location)
        {
            Model.Geocodio.Location geocode = null;

            var cachedString = await _cache
                .GetStringAsync(string.Format(Key.Cache.Geocode, location));

            if (cachedString?.Length > 0)
            {
                geocode = JsonSerializer.Deserialize<Model.Geocodio.Location>(cachedString);
            }

            if (geocode != null)
            {
                return geocode;
            }

            if (string.IsNullOrEmpty(_options.IanGeocodioEndpoint)
                || string.IsNullOrEmpty(_options.IanGeocodioKey))
            {
                throw new Exception("Geocoding is not configured.");
            }

            var uri = new Uri(string.Format(_options.IanGeocodioEndpoint,
                _options.IanGeocodioKey,
                location));

            var response = await _fetchJsonService.GetAsync<Model.Geocodio.Response>(uri);

            if (response.IsSuccessful)
            {
                if (response?.Data?.Results != null)
                {
                    geocode = response?.Data?.Results[0]?.Location;
                    if (geocode != null)
                    {
                        await _cache.SetStringAsync(string.Format(Key.Cache.Geocode, location),
                            JsonSerializer.Serialize(geocode), new DistributedCacheEntryOptions
                            {
                                AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(4)
                            });
                    }
                }
            }
            else
            {
                throw new Exception(response.Message);
            }

            return geocode;
        }

        private async Task<(string, Discord.Embed)>
            GetCurrentAsync(Model.Geocodio.Location coordinates, string forecast, string locale)
        {
            var headers = new Dictionary<string, string>
            {
                { "User-Agent", "DiscorIan Discord bot" }
            };
            if (string.IsNullOrEmpty(forecast))
            {
                var uri = new Uri(string.Format(_options.IanWeatherGovPointsEndpoint,
                    HttpUtility.UrlEncode($"{coordinates.Lat},{coordinates.Lng}")));
                var pointsResponse = await _fetchJsonService
                    .GetAsync<Model.WeatherGov.PointsResponse>(uri, headers);

                if (pointsResponse.IsSuccessful)
                {
                    forecast = pointsResponse?.Data?.Properties?.Forecast;
                    locale = pointsResponse?.Data?.Properties?.RelativeLocation?.Properties?.City
                        + ", "
                        + pointsResponse?.Data?.Properties?.RelativeLocation?.Properties?.State;
                    if (locale == ", ")
                    {
                        locale = "?";
                    }
                    await _cache.SetStringAsync(WeatherCacheKey, forecast);
                    await _cache.SetStringAsync(WeatherLocaleCacheKey, locale);
                }
            }

            if (string.IsNullOrEmpty(forecast))
            {
                return (null, null);
            }

            var forecastResponse = await _fetchJsonService
                .GetAsync<Model.WeatherGov.GridpointsResponse>(new Uri(forecast), headers);

            if (forecastResponse.IsSuccessful)
            {
                var f = forecastResponse.Data.Properties?.Periods[0];

                var sb = new StringBuilder("**")
                    .Append(locale)
                    .Append("**  ");

                if (f == null)
                {
                    sb.Append(":question:");
                }
                else
                {
                    foreach (var tempChar in f.Temperature.ToString())
                    {
                        switch (tempChar)
                        {
                            case '0':
                                sb.Append(":zero:");
                                break;
                            case '1':
                                sb.Append(":one:");
                                break;
                            case '2':
                                sb.Append(":two:");
                                break;
                            case '3':
                                sb.Append(":three:");
                                break;
                            case '4':
                                sb.Append(":four:");
                                break;
                            case '5':
                                sb.Append(":five:");
                                break;
                            case '6':
                                sb.Append(":six:");
                                break;
                            case '7':
                                sb.Append(":seven:");
                                break;
                            case '8':
                                sb.Append(":eight:");
                                break;
                            case '9':
                                sb.Append(":nine:");
                                break;
                        }
                    }

                    sb.Append("°")
                        .Append(f.TemperatureUnit)
                        .Append("   :wind_blowing_face: ")
                        .Append(f.WindDirection)
                        .Append(" ")
                        .AppendLine(f.WindSpeed)
                        .Append("**__")
                        .Append(f.Name)
                        .Append("__**: ")
                        .AppendLine(f.DetailedForecast);
                }

                return (sb.ToString().Trim(), null);
            }

            return (null, null);
        }

        private async Task<(string, Discord.Embed)>
            GetForecastAsync(Model.Geocodio.Location coordinates, string forecast, string locale)
        {
            var headers = new Dictionary<string, string>
            {
                { "User-Agent", "DiscorIan Discord bot" }
            };
            if (string.IsNullOrEmpty(forecast))
            {
                var uri = new Uri(string.Format(_options.IanWeatherGovPointsEndpoint,
                    HttpUtility.UrlEncode($"{coordinates.Lat},{coordinates.Lng}")));
                var pointsResponse = await _fetchJsonService
                    .GetAsync<Model.WeatherGov.PointsResponse>(uri, headers);

                if (pointsResponse.IsSuccessful)
                {
                    forecast = pointsResponse?.Data?.Properties?.Forecast;
                    locale = pointsResponse?.Data?.Properties?.RelativeLocation?.Properties?.City
                        + ", "
                        + pointsResponse?.Data?.Properties?.RelativeLocation?.Properties?.State;
                    if (locale == ", ")
                    {
                        locale = "?";
                    }
                    await _cache.SetStringAsync(WeatherCacheKey, forecast);
                    await _cache.SetStringAsync(WeatherLocaleCacheKey, locale);
                }
            }

            if (string.IsNullOrEmpty(forecast))
            {
                return (null, null);
            }

            var forecastResponse = await _fetchJsonService
                .GetAsync<Model.WeatherGov.GridpointsResponse>(new Uri(forecast), headers);

            if (forecastResponse.IsSuccessful)
            {
                var f = forecastResponse.Data.Properties?.Periods[0];

                var sb = new StringBuilder("**")
                    .Append(locale)
                    .Append("**  ");

                for (int i = 0; i < 4; i++)
                {
                    var period = forecastResponse.Data.Properties?.Periods[i];
                    if (!string.IsNullOrEmpty(f?.Name))
                    {
                        sb.Append("**__")
                            .Append(period.Name)
                            .Append("__**: ")
                            .AppendLine(period.DetailedForecast);
                    }
                }

                return (sb.ToString().Trim(), null);
            }

            return (null, null);
        }
    }
}

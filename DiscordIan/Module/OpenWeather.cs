using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using Discord.Commands;
using DiscordIan.Model.MapQuest;
using DiscordIan.Model.OpenWeatherMap;
using DiscordIan.Service;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Options;

namespace DiscordIan.Module
{
    public class OpenWeather : BaseModule
    {
        private readonly IDistributedCache _cache;
        private readonly FetchService _fetchService;
        private readonly Model.BotOptions _options;

        public OpenWeather(IDistributedCache cache,
            FetchService fetchService,
            IOptionsMonitor<Model.BotOptions> optionsAccessor)
        {
            _cache = cache ?? throw new ArgumentNullException(nameof(cache));
            _fetchService = fetchService
                ?? throw new ArgumentNullException(nameof(fetchService));
            _options = optionsAccessor?.CurrentValue
                ?? throw new ArgumentNullException(nameof(optionsAccessor));
        }

        [Command("wz", RunMode = RunMode.Async)]
        [Summary("Look up current weather for a provided address.")]
        [Alias("weather", "w", "wx")]
        public async Task CurrentAsync([Remainder]
            [Summary("The address or location for current weather conditions")] string location)
        {
            string coords = await _cache.GetStringAsync(location);
            string locale = null;
            if (!string.IsNullOrEmpty(coords))
            {
                locale = await _cache.GetStringAsync(coords);
            }

            if (string.IsNullOrEmpty(coords) && string.IsNullOrEmpty(location))
            {
                await ReplyAsync("Please provide a location.");
                return;
            }

            if (string.IsNullOrEmpty(coords) || string.IsNullOrEmpty(locale))
            {
                try
                {
                    (coords, locale) = await GeocodeAddressAsync(location);

                    if (!string.IsNullOrEmpty(coords) && !string.IsNullOrEmpty(locale))
                    {
                        await _cache.SetStringAsync(location, coords,
                            new DistributedCacheEntryOptions
                            {
                                AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(4)
                            });

                        await _cache.SetStringAsync(coords, locale,
                            new DistributedCacheEntryOptions
                            {
                                AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(4)
                            });
                    }
                }
                catch (Exception)
                {
                    //Geocode failed, revert to OpenWeatherMap's built-in resolve.
                }
            }

            string message;
            Discord.Embed embed;

            if (string.IsNullOrEmpty(coords) || string.IsNullOrEmpty(locale))
            {
                (message, embed) = await GetWeatherResultAsync(location);
            }
            else
            {
                (message, embed) = await GetWeatherResultAsync(coords, locale);
            }

            if (string.IsNullOrEmpty(message))
            {
                await ReplyAsync("No weather found, sorry!");
            }
            else
            {
                await ReplyAsync(message, false, embed);
            }
        }

        private string FormatResults(WeatherCurrent.Current data,
            string location,
            WeatherForecast.Daily foreData = null)
        {
            var sb = new StringBuilder()
                    .AppendFormat(">>> **{0}:** {1} {2}",
                        location,
                        TitleCase(data.Weather.Value),
                        WeatherIcon(data.Weather.Icon))
                    .AppendLine()

                    .AppendFormat("**Temp:** {0}°{1} **Feels Like:** {2}°{3} **Humidity:** {4}{5}",
                        data.Temperature.Value,
                        data.Temperature.Unit.ToUpper()[0],
                        data.Feels_like.Value,
                        data.Feels_like.Unit.ToUpper()[0],
                        data.Humidity.Value,
                        data.Humidity.Unit)
                    .AppendLine()

                    .AppendFormat("**Wind:** {0} **Speed:** {1}{2} {3}",
                        data.Wind.Speed.Name,
                        data.Wind.Speed.Value,
                        data.Wind.Speed.Unit,
                        data.Wind.Direction.Code);

            if (!string.IsNullOrEmpty(data.Wind.Gusts))
            {
                sb.AppendFormat(" **Gusts:** {0}{1}",
                        data.Wind.Gusts,
                        data.Wind.Speed.Unit);
            }

            if (foreData != null)
            {
                sb.AppendLine()
                    .AppendFormat("**High:** {0}°{1} **Low:** {2}°{3}",
                        foreData.Temp.Max.ToString(),
                        data.Temperature.Unit.ToUpper()[0],
                        foreData.Temp.Min.ToString(),
                        data.Temperature.Unit.ToUpper()[0])
                    .AppendLine()
                    .AppendFormat("**Forecast:** {0} {1}",
                        TitleCase(foreData.Weather[0].Description),
                        WeatherIcon(foreData.Weather[0].Icon));
            }

            return sb.ToString().Trim();
        }

        private async Task<(string, string)> GeocodeAddressAsync(string location)
        {
            if (string.IsNullOrEmpty(_options.IanMapQuestEndpoint)
                || string.IsNullOrEmpty(_options.IanMapQuestKey))
            {
                throw new Exception("Geocoding is not configured.");
            }

            var uri = new Uri(string.Format(_options.IanMapQuestEndpoint,
                HttpUtility.UrlEncode(location),
                _options.IanMapQuestKey));

            var response = await _fetchService.GetAsync<MapQuest>(uri);

            if (response.IsSuccessful)
            {
                if (response?.Data?.Results != null)
                {
                    var loc = response?.Data?.Results[0]?.Locations[0];
                    string precision = loc.GeocodeQuality;

                    string geocode = string.Format("{0},{1}", loc.LatLng.Lat, loc.LatLng.Lng);
                    string locale;

                    if (precision == "COUNTRY")
                    {
                        locale = loc.AdminArea1;
                    }
                    else if (precision == "STATE")
                    {
                        locale = string.Format("{0}, {1}", loc.AdminArea3, loc.AdminArea1);
                    }
                    else if (precision == "CITY" && string.IsNullOrEmpty(loc.AdminArea3))
                    {
                        locale = string.Format("{0}, {1}", loc.AdminArea5, loc.AdminArea1);
                    }
                    else if (string.IsNullOrEmpty(loc.AdminArea5))
                    {
                        locale = string.Format("{0}, {1}", loc.AdminArea3, loc.AdminArea1);
                    }
                    else
                    {
                        locale = string.Format("{0}, {1}", loc.AdminArea5, loc.AdminArea3);
                    }

                    return (geocode, locale);
                }
            }
            else
            {
                throw new Exception(response.Message);
            }

            return (null, null);
        }

        private async Task<(string, Discord.Embed)>
                            GetWeatherResultAsync(string coordinates, string location)
        {
            var headers = new Dictionary<string, string>
            {
                { "User-Agent", "DiscordIan Discord bot" }
            };

            var uriCurrent = new Uri(string.Format(CultureInfo.InvariantCulture,
                _options.IanOpenWeatherMapEndpointCoords,
                HttpUtility.UrlEncode(coordinates.Split(",")[0]),
                HttpUtility.UrlEncode(coordinates.Split(",")[1]),
                _options.IanOpenWeatherKey));

            var uriForecast = new Uri(string.Format(CultureInfo.InvariantCulture,
                _options.IanOpenWeatherMapEndpointForecast,
                HttpUtility.UrlEncode(coordinates.Split(",")[0]),
                HttpUtility.UrlEncode(coordinates.Split(",")[1]),
                _options.IanOpenWeatherKey));

            var responseCurrent = await _fetchService
                .GetAsync<WeatherCurrent.Current>(uriCurrent, headers);

            if (responseCurrent.IsSuccessful)
            {
                string message;
                var currentData = responseCurrent.Data;

                var responseForecast = await _fetchService
                    .GetAsync<WeatherForecast.Forecast>(uriForecast, headers);

                if (responseForecast.IsSuccessful)
                {
                    var forecastData = responseForecast.Data;

                    if (forecastData.Daily == null || forecastData.Daily.Length == 0)
                        throw new Exception("Today doesn't exist?!");

                    var today = forecastData.Daily[0];

                    message = FormatResults(currentData, location, today);
                }
                else
                {
                    message = FormatResults(currentData, location);
                }

                return (message, null);
            }

            return (null, null);
        }

        private async Task<(string, Discord.Embed)> GetWeatherResultAsync(string input)
        {
            var headers = new Dictionary<string, string>
            {
                { "User-Agent", "DiscordIan Discord bot" }
            };

            var uri = new Uri(string.Format(System.Globalization.CultureInfo.InvariantCulture,
                _options.IanOpenWeatherMapEndpointQ,
                HttpUtility.UrlEncode(input),
                _options.IanOpenWeatherKey));

            var response = await _fetchService
                .GetAsync<WeatherCurrent.Current>(uri, headers);

            if (response.IsSuccessful)
            {
                var data = response.Data;
                var locale = string.Format(CultureInfo.InvariantCulture,
                    "{0}, {1}",
                    data.City.Name,
                    data.City.Country);
                var latlong = string.Format(CultureInfo.InvariantCulture,
                    "{0},{1}",
                    data.City.Coord.Lat,
                    data.City.Coord.Lon);

                await _cache.SetStringAsync(input,
                    latlong,
                    new DistributedCacheEntryOptions
                    {
                        AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(4)
                    });

                await _cache.SetStringAsync(latlong,
                    locale,
                    new DistributedCacheEntryOptions
                    {
                        AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(4)
                    });

                string message = FormatResults(data, locale);

                return (message, null);
            }

            return (null, null);
        }

        private static string TitleCase(string str)
        {
            return CultureInfo.CurrentCulture.TextInfo.ToTitleCase(str);
        }

        private static string WeatherIcon(string iconCode)
        {
            return iconCode switch
            {
                "01d" => ":sunny:",
                "01n" => ":first_quarter_moon_with_face:",
                "02d" => ":white_sun_cloud:",
                "02n" or "03d" or "03n" or "04d" or "04n" => ":cloud:",
                "09d" or "09n" or "10n" => ":cloud_rain:",
                "10d" => ":white_sun_rain_cloud:",
                "11d" or "11n" => ":thunder_cloud_rain:",
                "13d" or "13n" => ":snowflake:",
                "50d" or "50n" => ":sweat_drops:",
                _ => string.Empty,
            };
        }
    }
}
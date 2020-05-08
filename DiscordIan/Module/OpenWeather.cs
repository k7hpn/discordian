using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using Discord.Commands;
using DiscordIan.Model.OpenWeatherMap;
using DiscordIan.Service;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Options;
using Microsoft.VisualBasic.CompilerServices;

namespace DiscordIan.Module
{
    public class OpenWeather : BaseModule
    {
        private readonly IDistributedCache _cache;
        private readonly FetchJsonService _fetchJsonService;
        private readonly FetchXMLService _fetchXMLService;
        private readonly Model.Options _options;
        private const string Current = "weather";
        private const string Forecast = "forecast";

        public OpenWeather(IDistributedCache cache,
            FetchJsonService fetchJsonService,
            FetchXMLService fetchXMLService,
            IOptionsMonitor<Model.Options> optionsAccessor)
        {
            _cache = cache ?? throw new ArgumentNullException(nameof(cache));
            _fetchJsonService = fetchJsonService
                ?? throw new ArgumentNullException(nameof(fetchJsonService));
            _fetchXMLService = fetchXMLService
                ?? throw new ArgumentNullException(nameof(fetchXMLService));
            _options = optionsAccessor.CurrentValue
                ?? throw new ArgumentNullException(nameof(optionsAccessor));
        }

        [Command("wz", RunMode = RunMode.Async)]
        [Summary("Look up current weather for a provided address.")]
        [Alias("weather", "w")]
        public async Task CurrentAsync([Remainder]
            [Summary("The address or location for current weather conditions")] string location)
        {
            string coords = await _cache.GetStringAsync(location);
            string locale = coords == null ? null : await _cache.GetStringAsync(coords);

            if (string.IsNullOrEmpty(coords) && string.IsNullOrEmpty(location))
            {
                await ReplyAsync("Please provide a location.");
                return;
            }
            Model.Geocodio.Location coordinates = null;
            Model.Geocodio.AddressComponents address = null;
            if (coords == null || locale == null)
            {
                try
                {
                    (coordinates, address) = await GeocodeAddressAsync(location);

                    if (coordinates != null && address != null)
                    {
                        locale = address.City + ", " + address.State;
                        string latlong = String.Format("{0},{1}", coordinates.Lat.ToString(), coordinates.Lng.ToString());

                        await _cache.SetStringAsync(location, latlong,
                            new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(4) });

                        await _cache.SetStringAsync(latlong, locale,
                            new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(4) });
                    }
                }
                catch (Exception ex)
                {
                    //Geocode failed, revert to OpenWeatherMap's built-in resolve.
                }
            }
            else
            {
                coordinates = new Model.Geocodio.Location() { Lat = double.Parse(coords.Split(",")[0]), Lng = double.Parse(coords.Split(",")[1]) };
            }

            string message;
            Discord.Embed embed;

            if (coordinates == null || locale == null)
                (message, embed) = await GetWeatherResultAsync(location);
            else
                (message, embed) = await GetWeatherResultAsync(coordinates, locale);


            if (message == null)
            {
                await ReplyAsync("No weather found, sorry!");
            }
            else
            {
                await ReplyAsync(message, false, embed);
            }
        }

        private async Task<(string, Discord.Embed)>
            GetWeatherResultAsync(Model.Geocodio.Location coordinates, string location)
        {
            var headers = new Dictionary<string, string>
            {
                { "User-Agent", "DiscorIan Discord bot" }
            };

            var uriCurr = new Uri(string.Format(_options.IanOpenWeatherMapEndpointCoords,
                HttpUtility.UrlEncode($"{coordinates.Lat}"),
                HttpUtility.UrlEncode($"{coordinates.Lng}"),
                _options.IanOpenWeatherKey));

            var uriFore = new Uri(string.Format(_options.IanOpenWeatherMapEndpointForecast,
                HttpUtility.UrlEncode($"{coordinates.Lat}"),
                HttpUtility.UrlEncode($"{coordinates.Lng}"),
                _options.IanOpenWeatherKey));

            var responseCurr = await _fetchXMLService
                .GetAsync<WeatherCurrent.Current>(uriCurr, headers);

            if (responseCurr.IsSuccessful)
            {
                string message = string.Empty;
                var data = responseCurr.Data;

                var responseFore = await _fetchJsonService
                    .GetAsync<WeatherForecast.Forecast>(uriFore, headers);

                if (responseFore.IsSuccessful)
                {
                    var foredata = responseFore.Data;
                    var today = foredata.Daily[0];

                    message = FormatResults(data, location, today);
                }
                else 
                    message = FormatResults(data, location);

                return (message, null);
            }

            return (null, null);
        }

        private async Task<(string, Discord.Embed)> GetWeatherResultAsync(string input)
        {
            var headers = new Dictionary<string, string>
            {
                { "User-Agent", "DiscorIan Discord bot" }
            };

            var uri = new Uri(string.Format(_options.IanOpenWeatherMapEndpointQ,
                HttpUtility.UrlEncode($"{input}"),
                _options.IanOpenWeatherKey));

            var response = await _fetchXMLService
                .GetAsync<WeatherCurrent.Current>(uri, headers);

            if (response.IsSuccessful)
            {
                var data = response.Data;
                var locale = String.Format("{0}, {1}", data.City.Name, data.City.Country);
                string latlong = String.Format("{0},{1}", data.City.Coord.Lat.ToString(), data.City.Coord.Lon.ToString());

                await _cache.SetStringAsync(input, latlong,
                            new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(4) });

                await _cache.SetStringAsync(latlong, locale,
                    new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(4) });

                string message = FormatResults(data, locale);

                return (message, null);
            }

            return (null, null);
        }

        private async Task<(Model.Geocodio.Location, Model.Geocodio.AddressComponents)> GeocodeAddressAsync(string location)
        {
            Model.Geocodio.Location geocode = null;
            Model.Geocodio.AddressComponents address = null;

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
                    address = response?.Data?.Results[0]?.AddressComponents;
                }
            }
            else
            {
                throw new Exception(response.Message);
            }

            return (geocode, address);
        }

        private string FormatResults(WeatherCurrent.Current data, string location, WeatherForecast.Daily foreData = null)
        {
            var sb = new StringBuilder()
                    .AppendLine(String.Format(">>> **{0}:** {1} {2}",
                        location,
                        TitleCase(data.Weather.Value),
                        WeatherIcon(data.Weather.Icon)))

                    .AppendLine(String.Format("**Temp:** {0}°{1} **Feels Like:** {2}°{3} **Humidity:** {4}{5}",
                        data.Temperature.Value,
                        data.Temperature.Unit.ToUpper()[0],
                        data.Feels_like.Value,
                        data.Feels_like.Unit.ToUpper()[0],
                        data.Humidity.Value,
                        data.Humidity.Unit))

                    .Append(String.Format("**Wind:** {0} **Speed:** {1}{2} {3}",
                        data.Wind.Speed.Name,
                        data.Wind.Speed.Value,
                        data.Wind.Speed.Unit,
                        data.Wind.Direction.Code));

            if (data.Wind.Gusts != "")
            {
                sb.Append(String.Format(" **Gusts:** {0}{1}",
                        data.Wind.Gusts,
                        data.Wind.Speed.Unit));
            }

            if (foreData != null)
            {
                sb.AppendLine()
                    .AppendLine(String.Format("**High:** {0}°{1} **Low:** {2}°{3}",
                        foreData.Temp.Max.ToString(),
                        data.Temperature.Unit.ToUpper()[0],
                        foreData.Temp.Min.ToString(),
                        data.Temperature.Unit.ToUpper()[0]))
                    .Append(String.Format("**Forecast:** {0} {1}",
                        TitleCase(foreData.Weather[0].Description),
                        WeatherIcon(foreData.Weather[0].Icon)));
            }

            return sb.ToString().Trim();
        }

        private string TitleCase(string str)
        {
            TextInfo textInfo = new CultureInfo("en-US", false).TextInfo;

            return textInfo.ToTitleCase(str);
        }

        private string WeatherIcon(string iconCode)
        {
            string result = string.Empty;

            switch (iconCode)
            {
                case "01d":
                    result = ":sunny:";
                    break;
                case "01n":
                    result = ":first_quarter_moon_with_face:";
                    break;
                case "02d":
                    result = ":white_sun_cloud:";
                    break;
                case "02n":
                case "03d":
                case "03n":
                case "04d":
                case "04n":
                    result = ":cloud:";
                    break;
                case "09d":
                case "09n":
                case "10n":
                    result = ":cloud_rain:";
                    break;
                case "10d":
                    result = ":white_sun_rain_cloud:";
                    break;
                case "11d":
                case "11n":
                    result = ":thunder_cloud_rain:";
                    break;
                case "13d":
                case "13n":
                    result = ":snowflake:";
                    break;
                case "50d":
                case "50n":
                    result = ":sweat_drops:";
                    break;

            }
            
            return result;    
        }
    }
}

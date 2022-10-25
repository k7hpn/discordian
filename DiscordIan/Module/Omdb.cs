using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Web;
using Discord;
using Discord.Commands;
using DiscordIan.Model.Omdb;
using DiscordIan.Service;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Options;

namespace DiscordIan.Module
{
    public class Omdb : BaseModule
    {
        private readonly IDistributedCache _cache;
        private readonly FetchService _fetchService;
        private readonly Model.BotOptions _options;

        public Omdb(IDistributedCache cache,
            FetchService fetchService,
            IOptionsMonitor<Model.BotOptions> optionsAccessor)
        {
            _cache = cache ?? throw new ArgumentNullException(nameof(cache));
            _fetchService = fetchService
                ?? throw new ArgumentNullException(nameof(fetchService));
            _options = optionsAccessor?.CurrentValue
                ?? throw new ArgumentNullException(nameof(optionsAccessor));
        }

        [Command("rt", RunMode = RunMode.Async)]
        [Summary("Look up movie/tv ratings")]
        public async Task CurrentAsync([Remainder]
            [Summary("Name of movie/show")] string input)
        {
            string cachedResponse = await _cache.GetStringAsync(
                string.Format(System.Globalization.CultureInfo.InvariantCulture,
                    Key.CacheKey.Omdb,
                    input?.Trim()));
            Movie omdbResponse;

            if (string.IsNullOrEmpty(cachedResponse))
            {
                try
                {
                    omdbResponse = await GetMovieAsync(input);
                }
                catch (Exception ex)
                {
                    await ReplyAsync($"Error! {ex.Message}");
                    return;
                }
            }
            else
            {
                omdbResponse = JsonSerializer.Deserialize<Movie>(
                    cachedResponse,
                    new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });
            }

            if (omdbResponse?.Response != "True")
            {
                await ReplyAsync("No result found, sorry!");
            }
            else
            {
                await ReplyAsync(null,
                    false,
                    FormatOmdbResponse(omdbResponse));
            }
        }

        private static string ConvertDateTime(string dateString)
        {
            if (DateTime.TryParse(dateString, out DateTime date))
            {
                return date.ToString("MMMM dd, yyyy");
            }

            return dateString;
        }

        private Embed FormatOmdbResponse(Movie response)
        {
            string titleUrl = string.Empty;
            string poster = string.Empty;
            var ratings = new StringBuilder("*none*");

            if (response.Ratings?.Length > 0)
            {
                ratings.Clear();

                foreach (var rating in response.Ratings)
                {
                    ratings.AppendFormat(System.Globalization.CultureInfo.InvariantCulture,
                            "{0}: {1}",
                            rating.Source.Replace("Internet Movie Database", "IMDB"),
                            rating.Value)
                        .AppendLine();
                }
            }

            var ratingField = new EmbedFieldBuilder
            {
                Name = "Ratings:",
                Value = ratings.ToString().Trim()
            };

            if (!string.IsNullOrEmpty(response.ImdbId))
            {
                titleUrl = string.Format(System.Globalization.CultureInfo.InvariantCulture,
                    _options.IanImdbIdLink,
                    response.ImdbId);
            }

            if (response.Poster.IsAbsoluteUri)
            {
                poster = response.Poster.AbsoluteUri.ToString();
            }

            return new EmbedBuilder
            {
                Author = new EmbedAuthorBuilder
                {
                    Name = response.Title,
                    Url = titleUrl
                },
                Description = response.Plot,
                ThumbnailUrl = poster,
                Fields = new List<EmbedFieldBuilder>()
                    {
                        { new EmbedFieldBuilder {
                            Name = "Released:",
                            Value = ConvertDateTime(response.Released) } },
                        { new EmbedFieldBuilder {
                            Name = "Actors:",
                            Value = response.Actors } },
                        { ratingField }
                    }
            }.Build();
        }

        private async Task<Movie> GetMovieAsync(string input)
        {
            var headers = new Dictionary<string, string>
            {
                { "User-Agent", "DiscordIan Discord bot" }
            };

            var uri = new Uri(string.Format(System.Globalization.CultureInfo.InvariantCulture,
                _options.IanOmdbEndpoint,
                HttpUtility.UrlEncode(input),
                _options.IanOmdbKey));

            var response = await _fetchService.GetAsync<Movie>(uri, headers);

            if (response.IsSuccessful)
            {
                var data = response.Data;

                if (data == null)
                {
                    throw new Exception("Invalid response data.");
                }

                await _cache.SetStringAsync(
                    string.Format(System.Globalization.CultureInfo.InvariantCulture,
                        Key.CacheKey.Omdb,
                        input.Trim()),
                    JsonSerializer.Serialize(data),
                    new DistributedCacheEntryOptions
                    {
                        AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(4)
                    });

                return data;
            }

            return null;
        }
    }
}
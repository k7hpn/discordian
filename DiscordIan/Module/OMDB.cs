using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Web;
using Discord;
using Discord.Commands;
using DiscordIan.Model.OMDB;
using DiscordIan.Service;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Options;

namespace DiscordIan.Module
{
    public class OMDB : BaseModule
    {
        private readonly IDistributedCache _cache;
        private readonly FetchService _fetchService;
        private readonly Model.Options _options;

        public OMDB(IDistributedCache cache,
            FetchService fetchService,
            IOptionsMonitor<Model.Options> optionsAccessor)
        {
            _cache = cache ?? throw new ArgumentNullException(nameof(cache));
            _fetchService = fetchService
                ?? throw new ArgumentNullException(nameof(fetchService));
            _options = optionsAccessor.CurrentValue
                ?? throw new ArgumentNullException(nameof(optionsAccessor));
        }

        [Command("rt", RunMode = RunMode.Async)]
        [Summary("Look up movie/tv ratings.")]
        public async Task CurrentAsync([Remainder]
            [Summary("Name of movie/show.")] string input)
        {
            string cachedResponse = await _cache.GetStringAsync(input);
            Embed embed;
            Movie omdbResponse;

            if (string.IsNullOrEmpty(cachedResponse))
            {
                omdbResponse = await GetMovieAsync(input);
            }
            else
            {
                omdbResponse = JsonSerializer.Deserialize<Movie>(
                    cachedResponse,
                    new JsonSerializerOptions { 
                        PropertyNameCaseInsensitive = true });
            }

            if (omdbResponse == null || omdbResponse?.Response == "False")
            {
                await ReplyAsync("No result found, sorry!");
            }
            else
            {
                embed = FormatOmdbResponse(omdbResponse);

                await ReplyAsync(null, false, embed);
            }
        }

        private async Task<Movie>
            GetMovieAsync(string input)
        {
            var headers = new Dictionary<string, string>
            {
                { "User-Agent", "DiscorIan Discord bot" }
            };

            var uri = new Uri(string.Format(_options.IanOmdbEndpoint,
                HttpUtility.UrlEncode(input),
                _options.IanOmdbKey));

            var response = await _fetchService
                .GetAsync<Movie>(uri, headers);

            if (response.IsSuccessful)
            {
                var data = response.Data;

                if (data == null)
                    throw new Exception("Invalid response data.");

                await _cache.SetStringAsync(
                    input, 
                    JsonSerializer.Serialize<Movie>(data),
                    new DistributedCacheEntryOptions
                    {
                        AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(4)
                    });

                return data;
            }

            return null;
        }

        private Embed FormatOmdbResponse(Movie response)
        {
            string titleUrl = string.Empty;
            var ratings = new StringBuilder("*none*");

            if (response.Ratings?.Length > 0)
            {
                ratings.Clear();

                foreach (var rating in response.Ratings)
                {
                    ratings.AppendFormat("{0}: {1}",
                            rating.Source.Replace("Internet Movie Database", "IMDB"),
                            rating.Value)
                        .AppendLine();
                }
            }

            EmbedFieldBuilder ratingField = new EmbedFieldBuilder
                {
                    Name = "Ratings:",
                    Value = ratings.ToString().Trim()
                };

            if (!string.IsNullOrEmpty(response.ImdbId))
            {
                titleUrl = string.Format(_options.IanImdbIdUrl,
                        response.ImdbId);
            }

            var embed = new EmbedBuilder()
            {
                Author = new EmbedAuthorBuilder { 
                    Name = response.Title,
                    Url = titleUrl},
                Description = response.Plot,
                ThumbnailUrl = response.Poster.AbsoluteUri,
                Fields = new List<EmbedFieldBuilder>()
                    {
                        { new EmbedFieldBuilder {
                            Name = "Released:",
                            Value = Convert.ToDateTime(response.Released)
                                        .ToString("MMMM dd, yyyy")} },
                        { new EmbedFieldBuilder {
                            Name = "Actors:",
                            Value = response.Actors } },
                        { ratingField }
                    }
            }.Build();

            return embed;
        }
    }
}

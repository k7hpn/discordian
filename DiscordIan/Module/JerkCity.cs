using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using System.Web;
using Discord;
using Discord.Commands;
using DiscordIan.Helper;
using DiscordIan.Key;
using DiscordIan.Model.JerkCity;
using DiscordIan.Service;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Options;

namespace DiscordIan.Module
{
    public class JerkCity : BaseModule
    {
        private readonly FetchService _fetchService;
        private readonly Model.Options _options;
        private readonly IDistributedCache _cache;

        private string CacheKey
        {
            get
            {
                return string.Format(Cache.JerkCity, Context.User.Id);
            }
        }

        public JerkCity(FetchService fetchService,
            IOptionsMonitor<Model.Options> optionsAccessor,
            IDistributedCache cache)
        {
            _fetchService = fetchService
                ?? throw new ArgumentNullException(nameof(fetchService));
            _options = optionsAccessor.CurrentValue
                ?? throw new ArgumentNullException(nameof(optionsAccessor));
            _cache = cache
                ?? throw new ArgumentNullException(nameof(cache));
        }

        [Command("jerk", RunMode = RunMode.Async)]
        [Summary("Returns Jerk City quotes.")]
        public async Task JerkCityAsync([Remainder]
            [Summary("Search Criteria")] string input = null)
        {
            if (string.IsNullOrEmpty(input))
            {
                await JerkCityRandom();
                return;
            }

            if (string.IsNullOrWhiteSpace(_options.JerkCityEndpoint))
            {
                await ReplyAsync("You must configure Jerk City to obtain your jerks!");
                return;
            }

            var headers = new Dictionary<string, string>
            {
                { "User-Agent", "DiscorIan Discord bot" }
            };

            var uri = new Uri(string.Format(_options.JerkCityEndpoint,
                HttpUtility.UrlEncode(input)
                ));

            var jerkResult = await _fetchService
                .GetAsync<JerkCityModel.JerkResponse>(uri, headers);

            if (jerkResult.IsSuccessful)
            {
                var response = jerkResult.Data;

                if (response == null)
                {
                    await ReplyAsync("Error in Jerk City response.");
                    return;
                }
                if (response.episodes.Count() == 0)
                {
                    await ReplyAsync("No Jerks Found.");
                    return;
                }

                await _cache.RemoveAsync(CacheKey);
                await _cache.SetStringAsync(CacheKey,
                    JsonSerializer.Serialize(new CachedJerks
                    {
                        CreatedAt = DateTime.Now,
                        JerkList = jerkResult.Data.episodes.Select(j =>
                            uri.BaseUrl() + j.image).ToArray(),
                        SearchString = input
                    }));

                var url = uri.BaseUrl() + response.episodes[0].image;

                await ReplyAsync(null, 
                    false,
                    FormatJerks(input, 1, response.episodes.Count(), url));
            }
            else
            {
                await ReplyAsync($"Jerk City failure: {jerkResult.Message}");
            }
        }

        [Command("jerknext", RunMode = RunMode.Async)]
        [Summary("Returns next cached Jerk City quote.")]
        public async Task JerkCityNextAsync()
        {
            var cachedString = await _cache.GetStringAsync(CacheKey);

            if (cachedString?.Length == 0)
            {
                await ReplyAsync($"I've got nothing for you, {Context.User.Username}");
                return;
            }
            else
            {
                var cached = JsonSerializer.Deserialize<CachedJerks>(cachedString);
                cached.LastViewedJerk++;

                if (string.IsNullOrEmpty(cached.JerkList[cached.LastViewedJerk]))
                {
                    await ReplyAsync($"I've got nothing for you, {Context.User.Username}");
                    return;
                }

                if (cached.JerkList.Length > cached.LastViewedJerk)
                {
                    await _cache.RemoveAsync(CacheKey);
                    await _cache.SetStringAsync(CacheKey,
                        JsonSerializer.Serialize(cached));

                    await ReplyAsync(null,
                        false,
                        FormatJerks(cached.SearchString, 
                            cached.LastViewedJerk + 1, 
                            cached.JerkList.Length, 
                            cached.JerkList[cached.LastViewedJerk]));
                }
            }
        }

        private async Task JerkCityRandom()
        {
            await _cache.RemoveAsync(CacheKey);

            if (string.IsNullOrWhiteSpace(_options.JerkCityEndpoint))
            {
                await ReplyAsync("You must configure Jerk City to obtain your jerks!");
                return;
            }

            var headers = new Dictionary<string, string>
            {
                { "User-Agent", "DiscorIan Discord bot" }
            };

            var uri = new Uri(_options.JerkCityBaseEndpoint);

            var jerkCurrent = await _fetchService
                .GetAsync<JerkCityModel.JerkResponse>(uri, headers);

            if (jerkCurrent.IsSuccessful)
            {
                var high = jerkCurrent.Data.meta.high;
                var rand = RandomNumberGenerator.GetInt32(high);
                if (rand == 0)
                {
                    rand++;
                }

                uri = new Uri(uri.AbsoluteUri +
                    string.Format("/episode/{0}", rand.ToString()));

                var jerkResult = await _fetchService
                .GetAsync<JerkCityModel.JerkResponse>(uri, headers);

                if (jerkResult.IsSuccessful)
                {
                    var response = jerkResult.Data;

                    if (response == null)
                    {
                        await ReplyAsync("Error in Jerk City response.");
                        return;
                    }
                    if (response.episodes.Count() == 0)
                    {
                        await ReplyAsync("No Jerks Found.");
                        return;
                    }

                    var url = uri.BaseUrl() + response.episodes[0].image;

                    await ReplyAsync(null,
                        false,
                        FormatJerks(string.Format("Episode {0}", rand.ToString()), 
                            0, 
                            0, 
                            url));
                }
            }
            else
            {
                await ReplyAsync($"Jerk City failure: {jerkCurrent.Message}");
            }
        }

        private Embed FormatJerks(string title, int index, int total, string url)
        {
            if (index > 0 && total > 0)
            {
                title = string.Format("{0}: ({1}/{2})",
                    title,
                    index.ToString(),
                    total.ToString());
            }

            return new EmbedBuilder()
            {
                Description = title,
                ImageUrl = url
            }.Build();
        }
    }
}

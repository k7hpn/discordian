﻿using System;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web;
using Discord.Commands;
using Discord.WebSocket;
using DiscordIan.Model.UrbanDictionary;
using DiscordIan.Service;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Options;

namespace DiscordIan.Module
{
    public class UrbanDictionary : BaseModule
    {
        private const string NotConfigured = "Please configure Urban Dictionary before using it.";

        private readonly IDistributedCache _cache;
        private readonly FetchService _fetchService;
        private readonly Model.BotOptions _options;

        public UrbanDictionary(IDistributedCache cache,
            FetchService fetchService,
            IOptionsMonitor<Model.BotOptions> optionsAccessor)
        {
            _cache = cache ?? throw new ArgumentNullException(nameof(cache));
            _fetchService = fetchService
                ?? throw new ArgumentNullException(nameof(fetchService));
            _options = optionsAccessor.CurrentValue
                ?? throw new ArgumentNullException(nameof(optionsAccessor));
        }

        private string CacheKey
        {
            get
            {
                return string.Format(Key.CacheKey.UrbanDictionary, Context.User.Id);
            }
        }

        [Command("ud", RunMode = RunMode.Async)]
        [Summary("Looks up a term in Urban Dicationary.")]
        public async Task UrbanDictionaryAsync([Remainder] [Summary("The term to look up")]
            string text)
        {
            if (string.IsNullOrWhiteSpace(_options.IanUrbanDictionaryEndpoint))
            {
                await ReplyAsync(NotConfigured);
                return;
            }

            await ReplyAsync(await GetDefinition(text));
        }

        [Command("udnext", RunMode = RunMode.Async)]
        [Summary("Shows the next Urban Dictionary definition for your most recently searched term.")]
        public async Task UrbanDictionaryNextAsync()
        {
            if (string.IsNullOrWhiteSpace(_options.IanUrbanDictionaryEndpoint))
            {
                await ReplyAsync(NotConfigured);
                return;
            }

            await ReplyAsync(await GetCachedDefinition());
        }

        [Command("udstat", RunMode = RunMode.Async)]
        [Alias("udinfo")]
        [Summary("Shows info on your last Urban Dictionary inquiry or that of another user.")]
        public async Task UrbanDictionaryStatAsync([Summary("The optional user to look up")]
            SocketUser user = null)
        {
            string key = user == null
                ? CacheKey
                : string.Format(Key.CacheKey.UrbanDictionary, user.Id);

            var cachedString = await _cache.GetStringAsync(key);

            if (cachedString?.Length > 0)
            {
                var lastLookup = JsonSerializer.Deserialize<CachedDefinitions>(cachedString);

                string who = user == null ? "You" : user.Username;

                await ReplyAsync($"{who} last looked up {lastLookup.List[0].Word} with {lastLookup.List.Length} definitions on {lastLookup.CreatedAt}");
            }
            else
            {
                await ReplyAsync("No idea.");
            }
        }

        private string FormatDefinition(UrbanDefinition[] definitions, int index)
        {
            var definition = definitions[index];
            var response = new StringBuilder(definition.Word);

            if (definitions.Length > 1)
            {
                response.Append(" (")
                    .Append(index + 1)
                    .Append("/")
                    .Append(definitions.Length)
                    .Append(")");
            }

            if (!string.IsNullOrEmpty(definition.ThumbsUp))
            {
                response.Append(" \uD83D\uDC4D:")
                    .Append(definition.ThumbsUp);
            }
            if (!string.IsNullOrEmpty(definition.ThumbsDown))
            {
                response.Append(" \uD83D\uDC4E:")
                    .Append(definition.ThumbsDown);
            }
            response.Append(" ")
                .Append(Regex.Replace(definition.Definition, @"\[(.+?)\]", "__$1__"));

            return response.ToString();
        }

        private async Task<string> GetCachedDefinition()
        {
            var cachedString = await _cache.GetStringAsync(CacheKey);

            if (cachedString?.Length == 0)
            {
                return $"I've got nothing for you, {Context.User.Username}";
            }
            else
            {
                var cached = JsonSerializer.Deserialize<CachedDefinitions>(cachedString);
                cached.LastViewedDefinition++;
                if (cached.List.Length > cached.LastViewedDefinition)
                {
                    await _cache.RemoveAsync(CacheKey);
                    await _cache.SetStringAsync(CacheKey,
                        JsonSerializer.Serialize(cached));

                    return FormatDefinition(cached.List, cached.LastViewedDefinition);
                }
            }
            return "That's all, folks.";
        }

        private async Task<string> GetDefinition(string term)
        {
            try
            {
                var uri = new Uri(string.Format(_options.IanUrbanDictionaryEndpoint,
                    HttpUtility.UrlEncode(term)));
                var response = await _fetchService.GetAsync<UrbanResponse>(uri);

                if (response?.IsSuccessful == true)
                {
                    var definitions = response.Data;

                    if (definitions?.List?.Length > 0)
                    {
                        string[] swaps = null;

                        if (!string.IsNullOrEmpty(_options.IanUrbanDictionarySwap))
                        {
                            swaps = _options.IanUrbanDictionarySwap.Contains(',',
                                    StringComparison.OrdinalIgnoreCase)
                                ? _options.IanUrbanDictionarySwap.Split(',')
                                : swaps = new string[1] { _options.IanUrbanDictionarySwap }; ;
                        }

                        if (definitions.List.Length >= 2
                            && swaps?.SingleOrDefault(_ => _ == term.Trim()) != null)
                        {
                            var revised = new UrbanDefinition[definitions.List.Length];
                            Array.Copy(definitions.List, 1, revised, 0, 1);
                            Array.Copy(definitions.List, 0, revised, 1, 1);
                            Array.Copy(definitions.List, 2, revised, 2, definitions.List.Length - 2);
                            definitions.List = revised;
                        }
                        await _cache.RemoveAsync(CacheKey);
                        await _cache.SetStringAsync(CacheKey,
                            JsonSerializer.Serialize(new CachedDefinitions
                            {
                                CreatedAt = DateTime.Now,
                                List = definitions.List
                            }));
                        return FormatDefinition(definitions.List, 0);
                    }
                    else
                    {
                        return $"You are making stuff up, **{term}** is not a word!";
                    }
                }
                else
                {
                    return $"The Urban Gods are angered by your query: {response?.Message ?? "...and I don't know why."}";
                }
            }
            catch (Exception ex)
            {
                return $"The Urban Gods have rejected your query ({ex.Message}).";
            }
        }
    }
}
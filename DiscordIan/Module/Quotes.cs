using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord.Commands;
using DiscordIan.Helper;
using DiscordIan.Key;
using DiscordIan.Model.Quotes;
using Microsoft.Extensions.Caching.Distributed;
using Newtonsoft.Json;

namespace DiscordIan.Module
{
    public class Quotes : BaseModule
    {
        private readonly IDistributedCache _cache;
        private TimeSpan apiTiming = new TimeSpan();
        private string CacheKey => string.Format(Cache.Quote, Context.Channel.Id);

        public Quotes(IDistributedCache cache)
        {
            _cache = cache ?? throw new ArgumentNullException(nameof(cache));
        }

        [Command("quote", RunMode = RunMode.Async)]
        [Summary("Look up stock quotes.")]
        [Alias("q", "quo", "quotes")]
        public async Task GetQuotesAsync([Remainder][Summary("Quote keyword.")] string input = null)
        {
            input = input.IsNullOrEmptyReplace("%");

            string cachedQuotes = await _cache.GetStringAsync(string.Format(Cache.Quote, input.Trim()));
            string[] quoteList;

            if (string.IsNullOrWhiteSpace(cachedQuotes))
            {
                quoteList = SqliteHelper.GetQuotes(input);

                await _cache.SetStringAsync(
                    string.Format(Cache.Quote, input.Trim()), 
                    JsonConvert.SerializeObject(quoteList),
                    new DistributedCacheEntryOptions
                    {
                        AbsoluteExpirationRelativeToNow = TimeSpan.FromDays(4)
                    });
            }
            else
            {
                quoteList = JsonConvert.DeserializeObject<string[]>(cachedQuotes);
            }

            if (!quoteList.Any())
            {
                await ReplyAsync("No quotes found.");
                return;
            }

            var model = new CachedQuotes
            {
                CreatedAt = DateTime.Now,
                QuoteList = quoteList,
                LastViewedQuote = 0,
                SearchString = input
            };

            await _cache.RemoveAsync(CacheKey);
            await _cache.SetStringAsync(
                CacheKey,
                JsonConvert.SerializeObject(model),
                new DistributedCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromDays(4)
                });

            quoteList.Shuffle();

            if (input == "%")
            {
                await ReplyAsync(quoteList[0]);
            }
            else
            {
                await ReplyAsync(FormatQuote(model));
            }

            HistoryAdd(_cache, GetType().Name, input, apiTiming);
        }

        [Command("quotenext", RunMode = RunMode.Async)]
        [Summary("Shows the next Quote for your most recently searched keyword.")]
        [Alias("qnext", "quonext", "quotesnext")]
        public async Task QuoteNextAsync()
        {
            var cachedString = await _cache.GetStringAsync(CacheKey);

            if (cachedString?.Length == 0)
            {
                await ReplyAsync("No definitions queued.");
                return;
            }
            else
            {
                var cached = JsonConvert.DeserializeObject<CachedQuotes>(cachedString);
                cached.LastViewedQuote++;

                if (cached.QuoteList.Length > cached.LastViewedQuote)
                {
                    await _cache.RemoveAsync(CacheKey);
                    await _cache.SetStringAsync(CacheKey,
                        JsonConvert.SerializeObject(cached));

                    await ReplyAsync(FormatQuote(cached));

                    return;
                }

                await _cache.RemoveAsync(CacheKey);
                await ReplyAsync("That's all, folks.");
            }
        }

        private string FormatQuote(CachedQuotes model)
        {
            return $"{model.SearchString} ({model.LastViewedQuote + 1}/{model.QuoteList.Length}): {model.QuoteList[model.LastViewedQuote]}";
        }
    }
}

using System;
using System.Globalization;
using System.IO;
using System.Text.Json;
using DiscordIan.Key;
using DiscordIan.Model;
using Microsoft.Extensions.Caching.Distributed;

namespace DiscordIan.Helper
{
    public static class Extensions
    {
        public static string ToTitleCase(this string str)
        {
            return CultureInfo.CurrentCulture.TextInfo.ToTitleCase(str);
        }

        public static string ValidateUri(this Uri uri)
        {
            if (uri?.IsAbsoluteUri ?? false)
            {
                return uri.AbsoluteUri;
            }

            return string.Empty;
        }

        public static string WordSwap(this string str, IDistributedCache cache)
        {
            if (str == null)
            {
                return null;
            }

            var cachedSwaps = cache.GetString(Cache.WordSwap);

            if (string.IsNullOrEmpty(cachedSwaps))
            {
                cachedSwaps = WordSwapHelper.GetListFromFile();

                if (!string.IsNullOrEmpty(cachedSwaps))
                {
                    cache.SetStringAsync(Cache.WordSwap,
                        cachedSwaps);
                }
            }

            if (string.IsNullOrEmpty(cachedSwaps))
            {
                return str;
            }

            var wordSwaps = JsonSerializer.Deserialize<WordSwaps>(cachedSwaps);

            if (wordSwaps != null)
            {
                 foreach (var swap in wordSwaps.SwapList)
                    {
                        str = str.Replace(swap.inbound, swap.outbound);
                    }
            }

            return str;
        }
    }
}

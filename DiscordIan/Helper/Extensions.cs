using System;
using System.Globalization;
using System.IO;
using System.Text.Json;
using DiscordIan.Model;

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

        public static string WordSwap(this string str)
        {
            if (str == null)
            {
                return null;
            }

            var file = "WordSwap.json";
            if (File.Exists(file))
            {
                using (StreamReader r = new StreamReader(file))
                {
                    string json = r.ReadToEnd();
                    var swaps = JsonSerializer.Deserialize<Swaps>(
                            json,
                            new JsonSerializerOptions
                            {
                                PropertyNameCaseInsensitive = true
                            });

                    foreach (var swap in swaps.WordSwaps)
                    {
                        str = str.Replace(swap.inbound, swap.outbound);
                    }
                }
            }

            return str;
        }
    }
}

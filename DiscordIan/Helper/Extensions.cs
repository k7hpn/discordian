using System;
using System.Globalization;

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
    }
}

using System;

namespace DiscordIan.Helper
{
    public static class DateFormat
    {
        public static string ToWesternDate(string dateString)
        {
            if (DateTime.TryParse(dateString, out DateTime date))
            {
                return date.ToString("MMMM dd, yyyy");
            }

            return dateString;
        }

        public static string UnixTimeToDate(double unixTimeStamp)
        {
            TimeZoneInfo tzi = TimeZoneInfo.FindSystemTimeZoneById("Eastern Standard Time");
            DateTime dtDateTime = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc)
                .AddSeconds(unixTimeStamp);

            return TimeZoneInfo.ConvertTimeFromUtc(dtDateTime, tzi)
                .ToString("MM/dd/yyyy HH:mm tt EST");
        }
    }
}

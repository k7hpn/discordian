using System;

namespace DiscordIan.Model
{
    public class DiscordIanException : Exception
    {
        public DiscordIanException(string message) : base(message)
        {
        }

        public DiscordIanException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        public DiscordIanException()
        {
        }
    }
}
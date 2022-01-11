using System;
using System.Collections.Generic;
using System.Text;

namespace DiscordIan.Model.Quotes
{
    public class CachedQuotes
    {
        public DateTime CreatedAt { get; set; }
        public int LastViewedQuote { get; set; }
        public string[] QuoteList { get; set; }
        public string SearchString { get; set; }
    }
}

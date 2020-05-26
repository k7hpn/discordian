using System;
using System.Collections.Generic;
using System.Text;

namespace DiscordIan.Model
{
    public class Swaps
    {
        public List<WordSwap> WordSwaps { get; set; }
    }

    public class WordSwap
    {
        public string inbound { get; set; }
        public string outbound { get; set; }
    }
}

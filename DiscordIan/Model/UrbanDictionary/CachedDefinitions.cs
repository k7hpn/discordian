using System;

namespace DiscordIan.Model.UrbanDictionary
{
    internal class CachedDefinitions
    {
        public DateTime CreatedAt { get; set; }
        public int LastViewedDefinition { get; set; }
        public UrbanDefinition[] List { get; set; }
    }
}

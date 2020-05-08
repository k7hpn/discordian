using System.Text.Json.Serialization;

namespace DiscordIan.Model.Geocodio
{
    public class Result
    {
        [JsonPropertyName("address_components")]
        public AddressComponents AddressComponents { get; set; }

        [JsonPropertyName("formatted_address")]
        public string FormattedAddress { get; set; }
        
        public Location Location { get; set; }
    }
}

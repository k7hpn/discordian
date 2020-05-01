using System;

namespace DiscordIan.Model.WeatherGov
{
    public class GridpointsPeriod
    {
        public string DetailedForecast { get; set; }
        public DateTime EndTime { get; set; }
        public string Icon { get; set; }
        public bool IsDaytime { get; set; }
        public string Name { get; set; }
        public int Number { get; set; }
        public string ShortForecast { get; set; }
        public DateTime StartTime { get; set; }
        public int Temperature { get; set; }
        public string TemperatureTrend { get; set; }
        public string TemperatureUnit { get; set; }
        public string WindDirection { get; set; }
        public string WindSpeed { get; set; }
    }
}

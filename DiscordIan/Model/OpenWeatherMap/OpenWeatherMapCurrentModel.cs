﻿using System.Xml.Serialization;

namespace DiscordIan.Model.OpenWeatherMap
{
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Design",
        "CA1034:Nested types should not be visible",
        Justification = "XML mapping of external object")]
    public static class WeatherCurrent
    {
        [XmlRoot(ElementName = "city")]
        public class City
        {
            [XmlElement(ElementName = "coord")]
            public Coord Coord { get; set; }

            [XmlElement(ElementName = "country")]
            public string Country { get; set; }

            [XmlAttribute(AttributeName = "id")]
            public string Id { get; set; }

            [XmlAttribute(AttributeName = "name")]
            public string Name { get; set; }

            [XmlElement(ElementName = "sun")]
            public Sun Sun { get; set; }

            [XmlElement(ElementName = "timezone")]
            public string Timezone { get; set; }
        }

        [XmlRoot(ElementName = "clouds")]
        public class Clouds
        {
            [XmlAttribute(AttributeName = "name")]
            public string Name { get; set; }

            [XmlAttribute(AttributeName = "value")]
            public string Value { get; set; }
        }

        [XmlRoot(ElementName = "coord")]
        public class Coord
        {
            [XmlAttribute(AttributeName = "lat")]
            public string Lat { get; set; }

            [XmlAttribute(AttributeName = "lon")]
            public string Lon { get; set; }
        }

        [XmlRoot(ElementName = "current")]
        public class Current
        {
            [XmlElement(ElementName = "city")]
            public City City { get; set; }

            [XmlElement(ElementName = "clouds")]
            public Clouds Clouds { get; set; }

            [XmlElement(ElementName = "feels_like")]
            public Feels_like Feels_like { get; set; }

            [XmlElement(ElementName = "humidity")]
            public Humidity Humidity { get; set; }

            [XmlElement(ElementName = "lastupdate")]
            public Lastupdate Lastupdate { get; set; }

            [XmlElement(ElementName = "precipitation")]
            public Precipitation Precipitation { get; set; }

            [XmlElement(ElementName = "pressure")]
            public Pressure Pressure { get; set; }

            [XmlElement(ElementName = "temperature")]
            public Temperature Temperature { get; set; }

            [XmlElement(ElementName = "visibility")]
            public string Visibility { get; set; }

            [XmlElement(ElementName = "weather")]
            public Weather Weather { get; set; }

            [XmlElement(ElementName = "wind")]
            public Wind Wind { get; set; }
        }

        [XmlRoot(ElementName = "direction")]
        public class Direction
        {
            [XmlAttribute(AttributeName = "code")]
            public string Code { get; set; }

            [XmlAttribute(AttributeName = "name")]
            public string Name { get; set; }

            [XmlAttribute(AttributeName = "value")]
            public string Value { get; set; }
        }

        [XmlRoot(ElementName = "feels_like")]
        public class Feels_like
        {
            [XmlAttribute(AttributeName = "unit")]
            public string Unit { get; set; }

            [XmlAttribute(AttributeName = "value")]
            public string Value { get; set; }
        }

        [XmlRoot(ElementName = "humidity")]
        public class Humidity
        {
            [XmlAttribute(AttributeName = "unit")]
            public string Unit { get; set; }

            [XmlAttribute(AttributeName = "value")]
            public string Value { get; set; }
        }

        [XmlRoot(ElementName = "lastupdate")]
        public class Lastupdate
        {
            [XmlAttribute(AttributeName = "value")]
            public string Value { get; set; }
        }

        [XmlRoot(ElementName = "precipitation")]
        public class Precipitation
        {
            [XmlAttribute(AttributeName = "mode")]
            public string Mode { get; set; }

            [XmlAttribute(AttributeName = "unit")]
            public string Unit { get; set; }

            [XmlAttribute(AttributeName = "value")]
            public string Value { get; set; }
        }

        [XmlRoot(ElementName = "pressure")]
        public class Pressure
        {
            [XmlAttribute(AttributeName = "unit")]
            public string Unit { get; set; }

            [XmlAttribute(AttributeName = "value")]
            public string Value { get; set; }
        }

        [XmlRoot(ElementName = "speed")]
        public class Speed
        {
            [XmlAttribute(AttributeName = "name")]
            public string Name { get; set; }

            [XmlAttribute(AttributeName = "unit")]
            public string Unit { get; set; }

            [XmlAttribute(AttributeName = "value")]
            public string Value { get; set; }
        }

        [XmlRoot(ElementName = "sun")]
        public class Sun
        {
            [XmlAttribute(AttributeName = "rise")]
            public string Rise { get; set; }

            [XmlAttribute(AttributeName = "set")]
            public string Set { get; set; }
        }

        [XmlRoot(ElementName = "temperature")]
        public class Temperature
        {
            [XmlAttribute(AttributeName = "max")]
            public string Max { get; set; }

            [XmlAttribute(AttributeName = "min")]
            public string Min { get; set; }

            [XmlAttribute(AttributeName = "unit")]
            public string Unit { get; set; }

            [XmlAttribute(AttributeName = "value")]
            public string Value { get; set; }
        }

        [XmlRoot(ElementName = "weather")]
        public class Weather
        {
            [XmlAttribute(AttributeName = "icon")]
            public string Icon { get; set; }

            [XmlAttribute(AttributeName = "number")]
            public string Number { get; set; }

            [XmlAttribute(AttributeName = "value")]
            public string Value { get; set; }
        }

        [XmlRoot(ElementName = "wind")]
        public class Wind
        {
            [XmlElement(ElementName = "direction")]
            public Direction Direction { get; set; }

            [XmlElement(ElementName = "gusts")]
            public string Gusts { get; set; }

            [XmlElement(ElementName = "speed")]
            public Speed Speed { get; set; }
        }
    }
}
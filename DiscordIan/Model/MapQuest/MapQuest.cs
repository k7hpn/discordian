using System;

namespace DiscordIan.Model.MapQuest
{
    public class MapQuest
    {
        public Info Info { get; set; }

        public Options Options { get; set; }

        public Result[] Results { get; set; }
    }

    public class Info
    {
        public long Statuscode { get; set; }

        public Copyright Copyright { get; set; }

        public object[] Messages { get; set; }
    }

    public class Copyright
    {
        public string Text { get; set; }

        public Uri ImageUrl { get; set; }

        public string ImageAltText { get; set; }
    }

    public class Options
    {
        public long MaxResults { get; set; }

        public bool ThumbMaps { get; set; }

        public bool IgnoreLatLngInput { get; set; }
    }

    public class Result
    {
        public ProvidedLocation ProvidedLocation { get; set; }

        public Location[] Locations { get; set; }
    }

    public class Location
    {
        public string Street { get; set; }

        public string AdminArea6 { get; set; }

        public string AdminArea6Type { get; set; }

        public string AdminArea5 { get; set; }

        public string AdminArea5Type { get; set; }

        public string AdminArea4 { get; set; }

        public string AdminArea4Type { get; set; }

        public string AdminArea3 { get; set; }

        public string AdminArea3Type { get; set; }

        public string AdminArea1 { get; set; }

        public string AdminArea1Type { get; set; }

        public string PostalCode { get; set; }

        public string GeocodeQualityCode { get; set; }

        public string GeocodeQuality { get; set; }

        public bool DragPoint { get; set; }

        public string SideOfStreet { get; set; }

        public string LinkId { get; set; }

        public string UnknownInput { get; set; }

        public string Type { get; set; }

        public LatLng LatLng { get; set; }

        public LatLng DisplayLatLng { get; set; }
    }

    public class LatLng
    {
        public double Lat { get; set; }

        public double Lng { get; set; }
    }

    public class ProvidedLocation
    {
        public string Location { get; set; }
    }
}

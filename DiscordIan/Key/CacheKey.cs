namespace DiscordIan.Key
{
    public struct CacheKey
    {
        public static readonly string Omdb = "omdb.{0}";
        public static readonly string UrbanDictionary = "ud.{0}";

        public static bool operator !=(CacheKey left, CacheKey right)
        {
            return !(left == right);
        }

        public static bool operator ==(CacheKey left, CacheKey right)
        {
            return left.Equals(right);
        }

        public override bool Equals(object obj)
        {
            return obj is CacheKey;
        }

        public override int GetHashCode()
        {
            return 42;
        }
    }
}
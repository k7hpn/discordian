using System.Threading.Tasks;

namespace DiscordIan
{
    internal static class Program
    {
        internal static async Task Main(string[] args)
        {
            await new Ian().GoAsync(args);
        }
    }
}

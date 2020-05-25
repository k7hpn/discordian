using System.Threading.Tasks;
using Discord;
using Discord.Commands;

namespace DiscordIan.Module
{
    public abstract class BaseModule : ModuleBase<SocketCommandContext>
    {
        private const int MaxReplyLength = 2000;
        private const string ForgetIt = "\u2026 never mind, I'm tired of typing";

        protected override async Task<IUserMessage> ReplyAsync(string message = null,
            bool isTTS = false,
            Embed embed = null,
            RequestOptions options = null)
        {
            string response = message;

            if (!string.IsNullOrWhiteSpace(response) && response.Length > 2000)
            {
                await base.ReplyAsync(message.Substring(0, MaxReplyLength - 1) + "\u2026",
                    isTTS,
                    embed,
                    options);

                if (message.Length <= MaxReplyLength * 2)
                {
                    response = message.Substring(MaxReplyLength - 1);
                }
                else
                {
                    response = message.Substring(MaxReplyLength - 1,
                        MaxReplyLength - 1 - ForgetIt.Length)
                        + ForgetIt;
                }
            }

            return await base.ReplyAsync(response, isTTS, embed, options);
        }
    }
}

using System;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;

namespace DiscordIan.Module
{
    public abstract class BaseModule : ModuleBase<SocketCommandContext>
    {
        private const string ForgetIt = "\u2026 never mind, I'm tired of typing";
        private const int MaxReplyLength = 2000;

        protected override async Task<IUserMessage> ReplyAsync(string message = null,
            bool isTTS = false,
            Embed embed = null,
            RequestOptions options = null,
            AllowedMentions allowedMentions = null,
            MessageReference messageReference = null,
            MessageComponent components = null,
            ISticker[] stickers = null,
            Embed[] embeds = null)
        {
            string response = message;

            if (!string.IsNullOrWhiteSpace(response) && response.Length > 2000)
            {
                await base.ReplyAsync(message[..(MaxReplyLength - 1)] + "\u2026",
                    isTTS,
                    embed,
                    options);

                if (message.Length <= MaxReplyLength * 2)
                {
                    response = message[(MaxReplyLength - 1)..];
                }
                else
                {
                    response = string.Concat(message.AsSpan(MaxReplyLength - 1,
                        message.Length - (MaxReplyLength - 1 + ForgetIt.Length)), ForgetIt);
                }
            }

            return await base.ReplyAsync(response, isTTS, embed, options);
        }
    }
}
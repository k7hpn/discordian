using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Discord.Commands;

namespace DiscordIan.Module
{
    public class BotInfo : BaseModule
    {
        private const string ContainerKey = "DOTNET_RUNNING_IN_CONTAINER";
        private const string DotnetVersionKey = "DOTNET_VERSION";
        private const string OpenContainerCreated = "org.opencontainers.image.created";
        private const string OpenContainerVersion = "org.opencontainers.image.version";

        [Command("info", RunMode = RunMode.Async)]
        [Summary("Returns information about the currently-running bot")]
        public async Task Info()
        {
            var sb = new StringBuilder();

            sb.Append(Assembly
                    .GetEntryAssembly()?
                    .GetCustomAttribute<AssemblyTitleAttribute>()?
                    .Title ?? nameof(DiscordIan))
                .Append(" v")
                .Append(Assembly
                    .GetEntryAssembly()?
                    .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?
                    .InformationalVersion ?? "Unknown")
                .Append(" up ")
                .Append(DateTime.Now > Ian.Startup
                    ? DateTime.Now - Ian.Startup
                    : new TimeSpan());

            var envKeys = Environment.GetEnvironmentVariables().Keys.Cast<string>().ToList();

            if (envKeys.Contains(DotnetVersionKey))
            {
                sb.Append(" .NET v").Append(Environment.GetEnvironmentVariable(DotnetVersionKey));
            }

            bool containerized = envKeys.Contains(ContainerKey)
                && !string.IsNullOrEmpty(Environment.GetEnvironmentVariable(ContainerKey));

            if (containerized)
            {
                sb.Append(" - container");
                if (envKeys.Contains(OpenContainerVersion))
                {
                    sb.Append(" v")
                        .Append(Environment.GetEnvironmentVariable(OpenContainerVersion));
                }
                if (envKeys.Contains(OpenContainerCreated))
                {
                    sb.Append(" created ")
                        .Append(Environment.GetEnvironmentVariable(OpenContainerCreated));
                }
            }

            await ReplyAsync(sb.ToString());
        }
    }
}
using DSharpPlus.Entities;
using DSharpPlus.Exceptions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EuDef
{
    public static class ErrorHandler
    {
        public static void HandleError(Exception exception, DiscordGuild guild, ErrorType errorType)
        {
            if (exception is DiscordException)
            {
                LogDiscordError((DiscordException)exception, errorType, guild);
            }
            else
            {
                LogError(exception, errorType, guild);
            }
        }

        private static void LogDiscordError(DiscordException exception, ErrorType errorType, DiscordGuild guild = null)
        {
            if (errorType == ErrorType.Error)
            {
                using (StreamWriter sw = new StreamWriter(Directory.GetCurrentDirectory() + "//errorLog.txt", true))
                {
                    sw.Write("-------------------------------------------\n" + DateTime.Now + "\n");
                    sw.Write(exception.ToString() + "\n\n" + exception.JsonMessage + "\n\n" + "\n-------------------------------------------\n");

                }
            }

            if (guild == null)
                return;

            var embed = new DiscordEmbedBuilder();
            embed.WithColor(DiscordColor.Red)
                .WithTitle("Encountered an error!")
                .AddField("Message", exception.Message);

            if (errorType == ErrorType.Warning)
                embed.WithColor(DiscordColor.Orange).WithTitle("Warning!");

            guild.GetChannel(Helpers.GetLogChannelID(guild.Id)).SendMessageAsync(embed: embed);
        }

        private static void LogError(Exception exception, ErrorType errorType, DiscordGuild guild = null)
        {
            if (errorType == ErrorType.Error)
            {
                using (StreamWriter sw = new StreamWriter(Directory.GetCurrentDirectory() + "//errorLog.txt", true))
                {
                    sw.Write("-------------------------------------------\n" + DateTime.Now + "\n");
                    sw.Write(exception.ToString() + "\n-------------------------------------------\n");
                }
            }

            if (guild == null)
                return;

            var embed = new DiscordEmbedBuilder();
            embed.WithColor(DiscordColor.Red)
                .WithTitle("Encountered an error!")
                .AddField("Message", exception.Message);

            if (errorType == ErrorType.Warning)
                embed.WithColor(DiscordColor.Orange).WithTitle("Warning!");

            guild.GetChannel(Helpers.GetLogChannelID(guild.Id)).SendMessageAsync(embed: embed);
        }

        public enum ErrorType
        {
            Error,
            Warning
        }
    }
}

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
		public static void HandleError(Exception exception, DiscordGuild guild, ErrorType errorType, string additionalInfo = "")
		{
			if (exception is DiscordException)
			{
				LogDiscordError((DiscordException)exception, errorType, guild, additionalInfo);
			}
			else
			{
				LogError(exception, errorType, guild, additionalInfo);
			}
		}

		private static void LogDiscordError(DiscordException exception, ErrorType errorType, DiscordGuild guild = null, string additionalInfo = "")
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
				.AddField("Message", exception.Message + "\n" + additionalInfo);

			if (errorType == ErrorType.Warning)
				embed.WithColor(DiscordColor.Orange).WithTitle("Warning!");

			guild.GetChannel(Helpers.GetLogChannelID(guild.Id)).SendMessageAsync(embed: embed);
		}

		private static void LogError(Exception exception, ErrorType errorType, DiscordGuild guild = null, string additionalInfo = "")
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
				.AddField("Message", exception.Message + "\n" + additionalInfo);

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

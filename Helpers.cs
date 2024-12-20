﻿using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using DSharpPlus.Exceptions;
using DSharpPlus.Interactivity;
using DSharpPlus.Interactivity.Extensions;
using DSharpPlus.SlashCommands;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Collections;

namespace EuDef
{
	public static class Helpers
	{
		/*
        =====================================================================================================================================================================================
         Generic getting
        =====================================================================================================================================================================================
        */

		public static async Task<string[]> GetNicknameByIdArray(string[] iDs, DiscordGuild guild, string path)
		{
			var sanitizedIDs = iDs.ToList();

			foreach (var id in iDs)
			{
				if (string.IsNullOrEmpty(id))
				{
					sanitizedIDs.Remove(id);
				}
			}

			string[] nicknames = new string[sanitizedIDs.Count];

			for (int i = 0; i < sanitizedIDs.Count; i++)
			{
				try
				{
					var member = await guild.GetMemberAsync(Convert.ToUInt64(sanitizedIDs[i]));
					nicknames[i] = member.DisplayName;
				}
				catch (DiscordException e)
				{
					Console.WriteLine(e.Message);
					nicknames[i] = "?!NotFound!?";
				}
			}

			if (sanitizedIDs.Count != iDs.Length && path != "")
			{
				File.WriteAllLines(path, sanitizedIDs);
			}

			return nicknames;
		}

		public static string GetFileDirectoryWithContent(InteractionContext ctx, string content)
		{
			var allFiles = Directory.GetFiles(Directory.GetCurrentDirectory() + "//" + ctx.Guild.Id + "//Events", "forumPostID.txt", SearchOption.AllDirectories);
			string? directory = null;
			foreach (string file in allFiles)
			{
				string text = File.ReadAllText(file);
				if (text.Contains(content))
				{
					directory = file.Remove(file.Length - "forumPostID.txt".Length).Replace(@"\", "/");
					break;
				}
			}

			if (directory == null)
				return "null";
			return directory;
		}
		/*
        =====================================================================================================================================================================================
         Channel getting
        =====================================================================================================================================================================================
        */

		public static DiscordThreadChannel GetThreadChannelByID(DiscordForumChannel channel, string id)
		{
			foreach (var threadChannel in channel.Threads)
			{
				if (threadChannel.Id == Convert.ToUInt64(id))
					return threadChannel;
			}
			throw new Exception("Thread channel " + id + " doesn't exist");
		}

		public static ulong GetBotChannelID(ulong guildID)
		{
			var botchannelId = File.ReadAllText(Path.Combine(Directory.GetCurrentDirectory() + "//" + guildID, "bot_channel.txt"));
			return Convert.ToUInt64(botchannelId);
		}

		public static ulong GetCollectionChannelID(ulong guildID)
		{
			var collectionChannelId = File.ReadAllText(Path.Combine(Directory.GetCurrentDirectory() + "//" + guildID, "collection_channel.txt"));
			return Convert.ToUInt64(collectionChannelId);
		}

		public static ulong GetLogChannelID(ulong guildID)
		{
			var logchannelId = File.ReadAllText(Path.Combine(Directory.GetCurrentDirectory() + "//" + guildID, "log_channel.txt"));
			return Convert.ToUInt64(logchannelId);
		}

		public static ulong GetEventForumID(ulong guildID)
		{
			var eventchannelID = File.ReadAllText(Path.Combine(Directory.GetCurrentDirectory() + "//" + guildID, "event_channel.txt"));
			return Convert.ToUInt64(eventchannelID);
		}

		public static ulong GetMeetingPointID(ulong guildID)
		{
			var meetingPointID = File.ReadAllText(Path.Combine(Directory.GetCurrentDirectory() + "//" + guildID, "meeting_channel.txt"));
			return Convert.ToUInt64(meetingPointID);
		}

		public static ulong GetLongTermSignoffID(ulong guildID)
		{
			var meetingPointID = File.ReadAllText(Path.Combine(Directory.GetCurrentDirectory() + "//" + guildID, "longTermSignoff_channel.txt"));
			return Convert.ToUInt64(meetingPointID);
		}

		/*
        =====================================================================================================================================================================================
         Role getting
        =====================================================================================================================================================================================
        */

		public static ulong GetMemberRoleID(ulong guildID)
		{
			var memberRoleID = File.ReadAllText(Path.Combine(Directory.GetCurrentDirectory() + "//" + guildID, "member_role.txt"));
			return Convert.ToUInt64(memberRoleID);
		}

		public static ulong[] GetDividerRoleIDs(ulong guildID)
		{
			var text = File.ReadAllText(Path.Combine(Directory.GetCurrentDirectory() + "//" + guildID, "divider_roles.txt"));
			ulong[] dividerRoleIds = Array.ConvertAll(text.Split(Environment.NewLine), s => ulong.Parse(s));
			return dividerRoleIds;
		}

		/*
        =====================================================================================================================================================================================
         Generic helpers
        =====================================================================================================================================================================================
        */

		public static bool IsValidDate(string dateString, string dateFormat)
		{
			DateTime dateValue;

			// Try to parse the date string
			bool validDate = DateTime.TryParseExact(
				dateString,
				dateFormat,
				CultureInfo.InvariantCulture,
				DateTimeStyles.None,
				out dateValue);

			// Check if the date is also in the future
			if (validDate)
			{
				return dateValue > DateTime.Today; // Returns true if the date is future
			}

			return false; // If parsing fails or date is in the past or today
		}

		public static Dictionary<string, int> LoadDictionary(string filePath)
		{
			string json = File.ReadAllText(filePath);
			return JsonConvert.DeserializeObject<Dictionary<string, int>>(json);
		}

		public static bool SaveDictionary(string filePath, Dictionary<string, int> dict)
		{
			try
			{
				string json = JsonConvert.SerializeObject(dict, Formatting.Indented);
				File.WriteAllText(filePath, json);

				return true;
			}
			catch
			{
				return false;
			}
		}

		public static Dictionary<string, int> SortDictionaryByValue(Dictionary<string, int> dictionary, SlashCommands.SortEnum sortType)
		{
			// Convert dictionary to list of key-value pairs
			List<KeyValuePair<string, int>> list = dictionary.ToList();

			// Sort the list based on values
			if (sortType == SlashCommands.SortEnum.most)
				list.Sort((x, y) => y.Value.CompareTo(x.Value));
			else
				list.Sort((x, y) => x.Value.CompareTo(y.Value));

			// Convert sorted list back to dictionary
			Dictionary<string, int> sortedDictionary = new Dictionary<string, int>();
			foreach (var kvp in list)
			{
				sortedDictionary.Add(kvp.Key, kvp.Value);
			}

			return sortedDictionary;
		}
		public static async void NotifyRole(InteractionContext ctx, DiscordRole role, string message, string? eventName, DiscordMessage? Message, bool editMessage, DiscordScheduledGuildEvent? discordEvent)
		{
			try
			{
				int messagesSent = 0;
				foreach (var member in ctx.Guild.Members.Values)
				{

					var notifyEmbed = new DiscordEmbedBuilder()
								.WithDescription(message)
								.WithTitle("Neue Nachricht");

					if (!editMessage && discordEvent != null)
					{
						notifyEmbed.WithTitle($"Neues Event: {eventName}").AddField("Link", $"Nachricht: {Message.JumpLink}\nEvent: https://discord.com/events/{discordEvent.Guild.Id}/{discordEvent.Id}").WithColor(DiscordColor.Green);
					}
					else if (!editMessage)
					{
						notifyEmbed.WithTitle($"Neues Event: {eventName}").AddField("Link", $"Nachricht: {Message.JumpLink}\n").WithColor(DiscordColor.Green);
					}

					foreach (var userRole in member.Roles)
					{
						if (member.IsBot)
							break;
						if (userRole == role && !member.IsBot)
						{
							try
							{
								await member.SendMessageAsync(embed: notifyEmbed);
							}
							catch (UnauthorizedException badBoy)
							{
								//Console.WriteLine(member.DisplayName + " has Direct Messages turned off :(");

								var embedBad = new DiscordEmbedBuilder()
									.WithColor(DiscordColor.Gray)
									.WithTitle("Couldn't send direct message (Turned off / blocked)")
									.WithDescription(member.Mention)
									.AddField("Error message", badBoy.Message);

								await ctx.Guild.GetChannel(GetLogChannelID(ctx.Guild.Id)).SendMessageAsync(embedBad);
							}
							messagesSent++;
							break;
						}
					}
				}
				if (editMessage)
					await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent("Sent " + messagesSent + " messages to " + role.Mention));

				var embed = new DiscordEmbedBuilder();
				embed.AddField("Sent to", role.Mention)
						.WithAuthor(ctx.Member.Username, ctx.Member.BannerUrl, ctx.Member.AvatarUrl)
						.WithTitle("Notifications sent")
						.WithDescription(messagesSent + " user(s) notified")
						.WithColor(DiscordColor.Green)
						.AddField("Message", message);

				var mess = await ctx.Guild.GetChannel(GetLogChannelID(ctx.Guild.Id)).SendMessageAsync(embed: embed);

			}
			catch (Exception e)
			{
				ErrorHandler.HandleError(e, ctx.Guild, ErrorHandler.ErrorType.Error);
			}
		}

		public static async void NotifyRoleFromEvent(DiscordMessage messageInformation, DiscordRole role, string message, string? eventName, DiscordMessage? Message, DiscordScheduledGuildEvent? discordEvent, Dictionary<string, string> userData = null)
		{


			try
			{
				int messagesSent = 0;
				foreach (var member in messageInformation.Channel.Guild.Members.Values)
				{

					var notifyEmbed = new DiscordEmbedBuilder()
								.WithDescription(message)
								.WithTitle("Neue Nachricht");


					notifyEmbed.WithTitle($"Neues Event: {eventName}").AddField("Link", $"Nachricht: {Message.JumpLink}\nEvent: https://discord.com/events/{discordEvent.Guild.Id}/{discordEvent.Id}").WithColor(DiscordColor.Green);

					foreach (var userRole in member.Roles)
					{
						if (member.IsBot)
							break;
						if (userRole == role && !member.IsBot)
						{
							if (userData != null)
							{
								if (userData.ContainsKey(member.Id.ToString()))
									break;
							}
							try
							{
								await member.SendMessageAsync(embed: notifyEmbed);
							}
							catch (UnauthorizedException badBoy)
							{
								//Console.WriteLine(member.DisplayName + " has Direct Messages turned off :(");

								var embedBad = new DiscordEmbedBuilder()
									.WithColor(DiscordColor.Gray)
									.WithTitle("Couldn't send direct message (Turned off / blocked)")
									.WithDescription(member.Mention)
									.AddField("Error message", badBoy.Message);

								await messageInformation.Channel.Guild.GetChannel(GetLogChannelID(messageInformation.Channel.Guild.Id)).SendMessageAsync(embedBad);
							}
							messagesSent++;
							break;
						}
					}
				}

				var embed = new DiscordEmbedBuilder();
				embed.AddField("Sent to", role.Mention)
						.WithAuthor(messageInformation.Author.Username, messageInformation.Author.BannerUrl, messageInformation.Author.AvatarUrl)
						.WithTitle("Notifications sent")
						.WithDescription(messagesSent + " user(s) notified")
						.WithColor(DiscordColor.Green)
						.AddField("Message", message);

				var mess = await messageInformation.Channel.Guild.GetChannel(GetLogChannelID(messageInformation.Channel.Guild.Id)).SendMessageAsync(embed: embed);

			}
			catch (Exception e)
			{
				ErrorHandler.HandleError(e, messageInformation.Channel.Guild, ErrorHandler.ErrorType.Error);
			}
		}
	}
}

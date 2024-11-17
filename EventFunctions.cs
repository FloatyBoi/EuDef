using DSharpPlus.Entities;
using DSharpPlus;
using DSharpPlus.EventArgs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DSharpPlus.Interactivity;
using DSharpPlus.Exceptions;
using System.Globalization;
using Microsoft.VisualBasic;
using DSharpPlus.Interactivity.Extensions;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using Newtonsoft.Json;

namespace EuDef
{
	public static class EventFunctions
	{
		public enum DoVote
		{
			TagAbstimmen,
			FesterTag
		}
		public static async void HandleEventRegistration(ComponentInteractionCreateEventArgs e, string buttonType, string Id)
		{

			string signupPath = Directory.GetCurrentDirectory() + "//" + e.Guild.Id + "//Events" + $"//{Id}" + $"//signup.txt";
			string signoffPath = Directory.GetCurrentDirectory() + "//" + e.Guild.Id + "//Events" + $"//{Id}" + $"//signoff.txt";
			string undecidedPath = Directory.GetCurrentDirectory() + "//" + e.Guild.Id + "//Events" + $"//{Id}" + $"//undecided.txt";

			if (buttonType == "status")
			{
				try
				{
					await e.Interaction.DeferAsync(ephemeral: true);

					//JUST FOR longTermSignoff
					string[] signoff = File.ReadAllLines(signoffPath);

					//Check longTermSignoff for now Entries
					var path = Directory.GetCurrentDirectory() + $"//{e.Guild.Id}//longTermSignoff.txt";
					var userData = JsonConvert.DeserializeObject<Dictionary<string, string>>(File.ReadAllText(path));

					if (userData != null)
					{
						foreach (var userId in userData)
						{
							if (!signoff.Contains(userId.Key))
							{
								DateTime signoffUntil;
								DateTime.TryParseExact(userId.Value, "dd.MM.yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out signoffUntil);

								DateTime dateTimeBegin;
								path = Directory.GetCurrentDirectory() + "//" + e.Guild.Id + "//Events" + $"//{Id}";

								if (File.Exists(path + "//startTimeForCollection.txt"))
								{
									dateTimeBegin = DateTime.ParseExact(File.ReadAllText(path + "//startTimeForCollection.txt"), "dd.MM.yyyy,HH:mm", CultureInfo.InvariantCulture);
								}
								else
									dateTimeBegin = DateTime.UtcNow;

								if (signoffUntil >= dateTimeBegin)
								{
									var signOffList = signoff.ToList();
									signOffList.Add(userId.Key);
									signoff = signOffList.ToArray();
								}

							}

						}

						//Write updated string to file
						File.WriteAllLines(signoffPath, signoff);
					}

					string[] signon = await Helpers.GetNicknameByIdArray(File.ReadAllLines(signupPath), e.Guild, signupPath);
					signoff = await Helpers.GetNicknameByIdArray(File.ReadAllLines(signoffPath), e.Guild, signoffPath);
					string[] undecided = await Helpers.GetNicknameByIdArray(File.ReadAllLines(undecidedPath), e.Guild, undecidedPath);

					var embed = new DiscordEmbedBuilder()
						.WithTitle("Status")
						.AddField($"Angemeldet: {File.ReadAllLines(signupPath).Length}", $"------------------------------------------\n{String.Join("\n", signon)}\n------------------------------------------\n")
						.AddField($"Abgemeldet: {File.ReadAllLines(signoffPath).Length}", $"------------------------------------------\n{String.Join("\n", signoff)}\n------------------------------------------\n")
						.AddField($"Komme Verspätet: {File.ReadAllLines(undecidedPath).Length}", $"------------------------------------------\n{String.Join("\n", undecided)}\n------------------------------------------\n");

					await e.Interaction.EditOriginalResponseAsync(new DiscordWebhookBuilder().AddEmbed(embed: embed));
					return;
				}
				catch (Exception ex)
				{
					ErrorHandler.HandleError(ex, e.Guild, ErrorHandler.ErrorType.Error);

					var embed = new DiscordEmbedBuilder()
						.WithColor(DiscordColor.Red)
						.WithTitle("Fehlgeschlagen!")
						.WithDescription("Error: " + ex.Message);
					await e.Interaction.EditOriginalResponseAsync(new DiscordWebhookBuilder().AddEmbed(embed: embed));

				}
			}
			else if (File.Exists(Directory.GetCurrentDirectory() + "//" + e.Guild.Id + "//Events" + $"//{Id}" + "//closed.txt"))
			{
				await e.Interaction.CreateResponseAsync(DiscordInteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder().WithContent("Event ist geschlossen").AsEphemeral());
				return;
			}



			if (buttonType == "signup")
			{
				await e.Interaction.DeferAsync(ephemeral: true);

				if (HandleRegistration(e, Id, "signup.txt"))
					await e.Interaction.EditOriginalResponseAsync(new DiscordWebhookBuilder().WithContent("Erfolgreich Angemeldet"));
				else
					await e.Interaction.EditOriginalResponseAsync(new DiscordWebhookBuilder().WithContent("Anmeldung Aufgehoben"));
			}
			else if (buttonType == "signoff")
			{
				try
				{
					if (File.ReadAllLines(signoffPath).Contains($"{e.User.Id}"))
					{
						if (HandleRegistration(e, Id, "signoff.txt"))
							await e.Interaction.CreateResponseAsync(DiscordInteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder().WithContent("Erfolgreich Abgemeldet").AsEphemeral());
						else
							await e.Interaction.CreateResponseAsync(DiscordInteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder().WithContent("Anmeldung aufgehoben").AsEphemeral());
					}
					else
					{
						var modal = new DiscordInteractionResponseBuilder()
							.WithTitle("Grund für Abmeldung")
							.WithCustomId($"id-signoff-reason-{e.Interaction.Id}")
							.AddComponents(new DiscordTextInputComponent(label: "Grund", placeholder: "Kein Interesse, da... / Keine Zeit...", customId: "id-reason", style: DiscordTextInputStyle.Paragraph));

						await e.Interaction.CreateResponseAsync(DiscordInteractionResponseType.Modal, modal);
						var interactivity = Program.discordClient.GetInteractivity();
						var response = await interactivity.WaitForModalAsync($"id-signoff-reason-{e.Interaction.Id}", user: e.User, timeoutOverride: TimeSpan.FromSeconds(1800));
						string signoffReason = response.Result.Values["id-reason"];

						if (!response.TimedOut)
						{
							DiscordMember member = (DiscordMember)e.User;

							var signoffEmbed = new DiscordEmbedBuilder()
								.WithTitle("Abmeldung")
								.WithDescription(e.User.Mention + "\n" + e.Message.JumpLink)
								.WithColor(DiscordColor.Red)
								.WithAuthor(member.DisplayName, member.AvatarUrl, member.AvatarUrl)
								.AddField("Grund", signoffReason);

							await e.Guild.GetChannel(Helpers.GetBotChannelID(e.Guild.Id)).SendMessageAsync(signoffEmbed);

							if (HandleRegistration(e, Id, "signoff.txt"))
								await response.Result.Interaction.CreateResponseAsync(DiscordInteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder().WithContent("Erfolgreich Abgemeldet").AsEphemeral());
							else
								await response.Result.Interaction.CreateResponseAsync(DiscordInteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder().WithContent("Anmeldung aufgehoben").AsEphemeral());
						}
					}
				}
				catch (Exception ex)
				{
					ErrorHandler.HandleError(ex, e.Guild, ErrorHandler.ErrorType.Error);
				}
			}
			else if (buttonType == "undecided")
			{
				await e.Interaction.DeferAsync(ephemeral: true);

				if (HandleRegistration(e, Id, "undecided.txt"))
					await e.Interaction.EditOriginalResponseAsync(new DiscordWebhookBuilder().WithContent("Als Unentschieden Eingetragen"));
				else
					await e.Interaction.EditOriginalResponseAsync(new DiscordWebhookBuilder().WithContent("Nichtmehr als Unentschieden Eingetragen"));
			}
		}

		public static async void HandleEventCreationUpdate(string buttonType, string buttonId, DiscordGuild guild, DiscordInteraction interaction)
		{
			try
			{
				var messageId = File.ReadAllText(Directory.GetCurrentDirectory() + $"//{guild.Id}//EventCreationCache//{buttonId}//messageId.txt");
				var message = await guild.GetChannel(Helpers.GetBotChannelID(guild.Id)).GetMessageAsync(Convert.ToUInt64(messageId));

				var modal = new DiscordInteractionResponseBuilder().WithContent("Eingeben").WithCustomId($"{buttonId}_eventCreateUpdate_{buttonType}");

				if (buttonType == "addTitle")
				{
					modal.WithTitle("Titel");
					modal.AddComponents(
						new DiscordTextInputComponent(label: "Titel", customId: "id-title", value: message.Embeds[0].Title, style: DiscordTextInputStyle.Short));
					await interaction.CreateResponseAsync(DiscordInteractionResponseType.Modal, modal);
				}

				if (buttonType == "addDescription")
				{
					modal.WithTitle("Beschreibung");
					modal.AddComponents(
						new DiscordTextInputComponent(label: "Beschreibung", customId: "id-description", value: message.Embeds[0].Description, style: DiscordTextInputStyle.Paragraph));
					await interaction.CreateResponseAsync(DiscordInteractionResponseType.Modal, modal);
				}

				if (buttonType == "addDateTime")
				{
					var timeAndDateString = message.Embeds[0].Fields[0].Value
					.Substring(message.Embeds[0].Fields[0].Value.IndexOf(':') + 2, "12.01.2000,19:30".Length + 1) + "_" + message.Embeds[0].Fields[0].Value
					.Substring(message.Embeds[0].Fields[0].Value.IndexOf("Ende:") + 6);

					modal.WithTitle("Datum und Zeit");
					modal.AddComponents(
						new DiscordTextInputComponent(label: "Beginn [12.01.2000,19:30]",
						placeholder: "12.01.2000,19:30",
						value: timeAndDateString.Substring(0, timeAndDateString.IndexOf('_') - 1),
						customId: "id-datetimebegin", style: DiscordTextInputStyle.Short))
						.AddComponents(
						new DiscordTextInputComponent(label: "Ende [12.01.2000,21:30]",
						placeholder: "12.01.2000,21:30",
						value: timeAndDateString.Substring(timeAndDateString.IndexOf('_') + 1),
						customId: "id-datetimeend", style: DiscordTextInputStyle.Short));
					try
					{
						await interaction.CreateResponseAsync(DiscordInteractionResponseType.Modal, modal);
					}
					catch (NullReferenceException ex)
					{
						//Console.WriteLine(ex.ToString());
					}
				}

				if (buttonType == "addNotifyMessage")
				{
					modal.WithTitle("Benachrichtigung");
					modal.AddComponents(
						new DiscordTextInputComponent(label: "Benachrichtigung", customId: "id-notify", value: message.Embeds[0].Fields[1].Value, style: DiscordTextInputStyle.Paragraph));
					await interaction.CreateResponseAsync(DiscordInteractionResponseType.Modal, modal);
				}

				if (buttonType == "createEvent")
				{
					try
					{
						if (File.Exists(Directory.GetCurrentDirectory() + "//" + guild.Id + "//EventCreationCache" + $"//{buttonId}" + "//endTimeForVote.txt"))
						{
							await interaction.CreateResponseAsync(DiscordInteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder().WithContent("Event wurde bereits erstellt. Abstimmung am Laufen.").AsEphemeral());
							return;
						}
						await FinalizeEventCreation(message, Convert.ToUInt64(buttonId), guild, interaction); ;
					}
					catch (Exception ex)
					{
						ErrorHandler.HandleError(ex, guild, ErrorHandler.ErrorType.Error);
						await interaction.CreateResponseAsync(DiscordInteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder().WithContent("Etwas ist schiefgelaufen! Sind alle wichtigen Felder gefüllt?").AsEphemeral());
					}
				}

				if (buttonType == "cancelEvent")
				{
					if (File.Exists(Directory.GetCurrentDirectory() + "//" + guild.Id + "//EventCreationCache" + $"//{buttonId}" + "//endTimeForVote.txt"))
					{
						await interaction.CreateResponseAsync(DiscordInteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder().WithContent("Event wurde bereits erstellt. Abstimmung am laufen.").AsEphemeral());
						return;
					}
					RemoveCachedEvent(buttonId, guild);
					await message.DeleteAsync();
				}
			}
			catch (Exception ex)
			{
				ErrorHandler.HandleError(ex, guild, ErrorHandler.ErrorType.Error);
			}
		}

		public static async Task FinalizeEventCreation(DiscordMessage message, ulong buttonId, DiscordGuild guild, DiscordInteraction? interaction)
		{
			bool doVote = File.Exists(Directory.GetCurrentDirectory() + $"//{guild.Id}//EventCreationCache//{buttonId}//doVote.txt");

			var user = message.Interaction.User;

			if (doVote)
			{
				var voteTimeModal = new DiscordInteractionResponseBuilder()
					.WithTitle("Optionen für Abstimmung")
					.WithCustomId($"{buttonId}_voteDateTime")
					.AddComponents(new DiscordTextInputComponent(label: "Erster Name",
					placeholder: "Erste Option", value: "Erste Option",
					customId: "id-datetimeoptiononename", style: DiscordTextInputStyle.Short))
					.AddComponents(new DiscordTextInputComponent(label: "Erste Option [12.01.2000,19:30]",
					placeholder: "12.01.2000,19:30",
					customId: "id-datetimeoptionone", style: DiscordTextInputStyle.Short))
					.AddComponents(new DiscordTextInputComponent(label: "Zweiter Name",
					placeholder: "Zweite Option", value: "Zweite Option",
					customId: "id-datetimeoptiontwoname", style: DiscordTextInputStyle.Short))
					.AddComponents(new DiscordTextInputComponent(label: "Zweite Option [12.01.2000,19:30]",
					placeholder: "12.01.2000,19:30",
					customId: "id-datetimeoptiontwo", style: DiscordTextInputStyle.Short))
					.AddComponents(new DiscordTextInputComponent(label: "Ende der Abstimmung [12.01.2000,19:30]",
					placeholder: "12.01.2000,19:30",
						customId: "id-datetimevoteend", style: DiscordTextInputStyle.Short));
				await interaction.CreateResponseAsync(DiscordInteractionResponseType.Modal, voteTimeModal);
				return;
			}

			var notifyRole = guild.GetRole(Helpers.GetMemberRoleID(guild.Id));

			//Getting Time and Date for event
			//Format: "Anfang: " + timeAndDateBegin + "\nEnde: " + timeAndDateEnd;
			var timeAndDateString = message.Embeds[0].Fields[0].Value
				.Substring(message.Embeds[0].Fields[0].Value.IndexOf(':') + 2, "12.01.2000,19:30".Length + 1) + "_" + message.Embeds[0].Fields[0].Value
				.Substring(message.Embeds[0].Fields[0].Value.IndexOf("Ende:") + 6);
			var timeAndDateBegin = DateTime.ParseExact(timeAndDateString.Substring(0, timeAndDateString.IndexOf('_') - 1), "dd.MM.yyyy,HH:mm", CultureInfo.InvariantCulture);
			var timeAndDateEnd = DateTime.ParseExact(timeAndDateString.Substring(timeAndDateString.IndexOf('_') + 1), "dd.MM.yyyy,HH:mm", CultureInfo.InvariantCulture);


			var signUpButton = new DiscordButtonComponent(
					DiscordButtonStyle.Success,
					buttonId + "_signup",
					"Anmelden"
					);

			var signOffButton = new DiscordButtonComponent(
				DiscordButtonStyle.Danger,
				buttonId + "_signoff",
				"Abmelden"
				);

			var undecidedButton = new DiscordButtonComponent(
				DiscordButtonStyle.Primary,
				buttonId + "_undecided",
				"Komme Verspätet"
				);

			var statusButton = new DiscordButtonComponent(
				DiscordButtonStyle.Secondary,
				buttonId + "_status",
				"Status");

			var buttons = new DiscordComponent[]
			{
					signUpButton,
					signOffButton,
					undecidedButton,
					statusButton
			};

			DiscordEmbedBuilder finalizedEmbed = new DiscordEmbedBuilder(message.Embeds[0])
				.RemoveFieldAt(1);
			finalizedEmbed.Fields[0].Value = Formatter.Timestamp(timeAndDateBegin, TimestampFormat.LongDate) + " (" + Formatter.Timestamp(timeAndDateBegin, TimestampFormat.RelativeTime) + ")" + "\nAnfang: " + Formatter.Timestamp(timeAndDateBegin, TimestampFormat.ShortTime) + "\nEnde: " + Formatter.Timestamp(timeAndDateEnd, TimestampFormat.ShortTime);

			DiscordForumChannel forumChannel = (DiscordForumChannel)guild.GetChannel(Helpers.GetEventForumID(guild.Id));
			DiscordForumPostStarter forumPost = await forumChannel.CreateForumPostAsync(new ForumPostBuilder().WithName($"{message.Embeds[0].Title}").WithMessage(new DiscordMessageBuilder().WithContent(notifyRole.Mention).WithAllowedMentions(new IMention[] { new RoleMention(notifyRole) }).AddEmbed(embed: finalizedEmbed).AddComponents(buttons)).WithAutoArchiveDuration(DiscordAutoArchiveDuration.Week));

			//Pin Message
			await forumPost.Message.PinAsync();

			Directory.CreateDirectory(Directory.GetCurrentDirectory() + "//" + guild.Id + "//Events");
			Directory.CreateDirectory(Directory.GetCurrentDirectory() + "//" + guild.Id + "//Events" + $"//{buttonId}");
			StreamWriter writer = new StreamWriter((Directory.GetCurrentDirectory() + "//" + guild.Id + "//Events" + $"//{buttonId}" + "//forumPostID.txt"));
			writer.WriteLine(forumPost.Channel.Id);
			writer.Dispose();

			string signupPath = Directory.GetCurrentDirectory() + "//" + guild.Id + "//Events" + $"//{buttonId}" + $"//signup.txt";
			string signoffPath = Directory.GetCurrentDirectory() + "//" + guild.Id + "//Events" + $"//{buttonId}" + $"//signoff.txt";
			string undecidedPath = Directory.GetCurrentDirectory() + "//" + guild.Id + "//Events" + $"//{buttonId}" + $"//undecided.txt";
			FileStream file;
			if (!File.Exists(signupPath))
			{
				file = File.Create(signupPath);
				file.Close();
			}
			file = File.Create(signoffPath);
			file.Close();
			file = File.Create(undecidedPath);
			file.Dispose();

			//Get Long-Term signoffs and apply them
			var path = Directory.GetCurrentDirectory() + $"//{guild.Id}//longTermSignoff.txt";
			var userData = JsonConvert.DeserializeObject<Dictionary<string, string>>(File.ReadAllText(path));

			if (File.Exists(path) && userData != null)
			{


				string[] enumString = new string[userData.Count];

				for (int i = 0; i < userData.Count; i++)
				{
					DateTime signoffUntil;
					DateTime.TryParseExact(userData.ToArray()[i].Value, "dd.MM.yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out signoffUntil);

					if (signoffUntil >= timeAndDateBegin)
					{
						enumString[i] = userData.ToArray()[i].Key;
					}
				}
				if (enumString.Length > 0)
				{
					if (enumString[0] is not null)
						File.AppendAllLines(signoffPath, enumString);
				}
			}


			var createEmbed = new DiscordEmbedBuilder()
				.WithAuthor(user.Username, user.BannerUrl, user.AvatarUrl)
				.WithTitle("Event Created")
				.WithDescription(message.Embeds[0].Title)
				.AddField("Link", $"{forumPost.Message.JumpLink}")
				.WithColor(DiscordColor.Aquamarine);

			await guild.GetChannel(Helpers.GetLogChannelID(guild.Id)).SendMessageAsync(embed: createEmbed);
			try
			{
				DiscordScheduledGuildEvent discordEvent = null;
				try
				{
					discordEvent = await guild.CreateEventAsync(name: message.Embeds[0].Title, description: $"{forumPost.Message.JumpLink}", channelId: Helpers.GetMeetingPointID(guild.Id), type: DiscordScheduledGuildEventType.VoiceChannel, privacyLevel: DiscordScheduledGuildEventPrivacyLevel.GuildOnly, start: new DateTimeOffset(timeAndDateBegin), end: new DateTimeOffset(timeAndDateEnd));
				}
				catch (BadRequestException ex)
				{
					ErrorHandler.HandleError(ex, guild, ErrorHandler.ErrorType.Error);
				}

				var notifyMessage = message.Embeds[0].Fields[1].Value;

				Helpers.NotifyRoleFromEvent(message, notifyRole, notifyMessage, message.Embeds[0].Title, forumPost.Message, discordEvent, userData);

			}
			catch (Exception wellShit)
			{
				ErrorHandler.HandleError(wellShit, guild, ErrorHandler.ErrorType.Error);
			}

			//Create File with time
			File.WriteAllText(Directory.GetCurrentDirectory() + "//" + guild.Id + "//Events" + $"//{buttonId}" + $"//startTimeForCollection.txt", timeAndDateString.Substring(0, timeAndDateString.IndexOf('_') - 1));
			//Create File for undecided reminder
			DateTime onedaybefore;
			if (timeAndDateBegin.AddDays(-1) > DateTime.Now)
			{
				onedaybefore = timeAndDateBegin.AddDays(-1);
			}
			else
			{
				onedaybefore = timeAndDateBegin;
			}

			File.WriteAllText(Directory.GetCurrentDirectory() + "//" + guild.Id + "//Events" + $"//{buttonId}" + $"//remindUndecided.txt", $"{onedaybefore.ToString("dd.MM.yyyy,HH:mm")}");

			//Cleanup
			RemoveCachedEvent(buttonId.ToString(), guild);
			await message.DeleteAsync();
		}



		private static void RemoveCachedEvent(string buttonId, DiscordGuild guild)
		{
			//Cleanup
			File.Delete(Directory.GetCurrentDirectory() + $"//{guild.Id}//EventCreationCache//{buttonId}//channelId.txt");
			File.Delete(Directory.GetCurrentDirectory() + $"//{guild.Id}//EventCreationCache//{buttonId}//messageId.txt");

			if (File.Exists(Directory.GetCurrentDirectory() + $"//{guild.Id}//EventCreationCache//{buttonId}//doVote.txt"))
				File.Delete(Directory.GetCurrentDirectory() + $"//{guild.Id}//EventCreationCache//{buttonId}//doVote.txt");

			Directory.Delete(Directory.GetCurrentDirectory() + $"//{guild.Id}//EventCreationCache//{buttonId}");
		}

		private static bool HandleRegistration(ComponentInteractionCreateEventArgs e, string interactionId, string fileName)
		{
			string signupPath = Directory.GetCurrentDirectory() + "//" + e.Guild.Id + "//Events" + $"//{interactionId}" + $"//signup.txt";
			string signoffPath = Directory.GetCurrentDirectory() + "//" + e.Guild.Id + "//Events" + $"//{interactionId}" + $"//signoff.txt";
			string undecidedPath = Directory.GetCurrentDirectory() + "//" + e.Guild.Id + "//Events" + $"//{interactionId}" + $"//undecided.txt";

			DiscordMember member = (DiscordMember)e.User;

			if (fileName == "signup.txt")
			{
				if (File.ReadAllLines(signoffPath).Contains(member.Id.ToString()))
				{

					var file = File.ReadAllLines(signoffPath).ToList();
					file.Remove(member.Id.ToString());
					File.Delete(signoffPath);
					File.WriteAllLines(signoffPath, file);
				}

				if (File.ReadAllLines(undecidedPath).Contains(member.Id.ToString()))
				{
					var file = File.ReadAllLines(undecidedPath).ToList();
					file.Remove(member.Id.ToString());
					File.Delete(undecidedPath);
					File.WriteAllLines(undecidedPath, file);
				}
			}

			if (fileName == "signoff.txt")
			{
				if (File.ReadAllLines(signupPath).Contains(member.Id.ToString()))
				{
					var file = File.ReadAllLines(signupPath).ToList();
					file.Remove(member.Id.ToString());
					File.Delete(signupPath);
					File.WriteAllLines(signupPath, file);
				}

				if (File.ReadAllLines(undecidedPath).Contains(member.Id.ToString()))
				{
					var file = File.ReadAllLines(undecidedPath).ToList();
					file.Remove(member.Id.ToString());
					File.Delete(undecidedPath);
					File.WriteAllLines(undecidedPath, file);
				}
			}

			if (fileName == "undecided.txt")
			{
				if (File.ReadAllLines(signupPath).Contains(member.Id.ToString()))
				{
					var file = File.ReadAllLines(signupPath).ToList();
					file.Remove(member.Id.ToString());
					File.Delete(signupPath);
					File.WriteAllLines(signupPath, file);
				}

				if (File.ReadAllLines(signoffPath).Contains(member.Id.ToString()))
				{
					var file = File.ReadAllLines(signoffPath).ToList();
					file.Remove(member.Id.ToString());
					File.Delete(signoffPath);
					File.WriteAllLines(signoffPath, file);
				}
			}

			if (!File.ReadAllLines(Directory.GetCurrentDirectory() + "//" + e.Guild.Id + "//Events" + $"//{interactionId}" + $"//{fileName}").Contains(member.Id.ToString()))
			{
				StreamWriter writer = new StreamWriter(Directory.GetCurrentDirectory() + "//" + e.Guild.Id + "//Events" + $"//{interactionId}" + $"//{fileName}", true);
				writer.WriteLine(member.Id.ToString());
				writer.Dispose();
				return true;
			}
			else
			{
				var file = File.ReadAllLines(Directory.GetCurrentDirectory() + "//" + e.Guild.Id + "//Events" + $"//{interactionId}" + $"//{fileName}").ToList();
				file.Remove(member.Id.ToString());
				File.Delete(Directory.GetCurrentDirectory() + "//" + e.Guild.Id + "//Events" + $"//{interactionId}" + $"//{fileName}");
				File.WriteAllLines(Directory.GetCurrentDirectory() + "//" + e.Guild.Id + "//Events" + $"//{interactionId}" + $"//{fileName}", file);
				return false;
			}
		}
	}
}


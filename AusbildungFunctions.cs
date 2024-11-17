using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using DSharpPlus.Exceptions;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EuDef
{
	public static class AusbildungFunctions
	{
		public static async void HandleAusbildungCreationUpdate(string buttonType, string buttonId, DiscordGuild guild, DiscordInteraction interaction)
		{
			try
			{
				var messageId = File.ReadAllText(Directory.GetCurrentDirectory() + $"//{guild.Id}//AusbildungCreationCache//{buttonId}//messageId.txt");
				var message = await guild.GetChannel(Helpers.GetBotChannelID(guild.Id)).GetMessageAsync(Convert.ToUInt64(messageId));

				var modal = new DiscordInteractionResponseBuilder().WithContent("Eingeben").WithCustomId($"{buttonId}_AusbildungCreateUpdate_{buttonType}");

				if (buttonType == "addAusbildungTitle")
				{
					modal.WithTitle("Titel");
					modal.AddComponents(
						new DiscordTextInputComponent(label: "Titel", customId: "id-title", value: message.Embeds[0].Title, style: DiscordTextInputStyle.Short));
					await interaction.CreateResponseAsync(DiscordInteractionResponseType.Modal, modal);
				}

				if (buttonType == "addAusbildungDescription")
				{
					modal.WithTitle("Beschreibung");
					modal.AddComponents(
						new DiscordTextInputComponent(label: "Beschreibung", customId: "id-description", value: message.Embeds[0].Description, style: DiscordTextInputStyle.Paragraph));
					await interaction.CreateResponseAsync(DiscordInteractionResponseType.Modal, modal);
				}

				if (buttonType == "addAusbildungDateTime")
				{
					var timeAndDateString = message.Embeds[0].Fields[2].Value
					.Substring(message.Embeds[0].Fields[2].Value.IndexOf(':') + 2, "12.01.2000,19:30".Length + 1) + "_" + message.Embeds[0].Fields[2].Value
					.Substring(message.Embeds[0].Fields[2].Value.IndexOf("Dauer:") + 6);

					modal.WithTitle("Datum und Zeit");
					modal.AddComponents(
						new DiscordTextInputComponent(label: "Beginn [12.01.2000,19:30]",
						placeholder: "12.01.2000,19:30",
						value: timeAndDateString.Substring(0, timeAndDateString.IndexOf('_') - 1),
						customId: "id-datetimebegin", style: DiscordTextInputStyle.Short))
						.AddComponents(
						new DiscordTextInputComponent(label: "Dauer [03:00]",
						placeholder: "03:00",
						value: timeAndDateString.Substring(timeAndDateString.IndexOf('_') + 1),
						customId: "id-duration", style: DiscordTextInputStyle.Short));
					try
					{
						await interaction.CreateResponseAsync(DiscordInteractionResponseType.Modal, modal);
					}
					catch (NullReferenceException ex)
					{
						ErrorHandler.HandleError(ex, guild, ErrorHandler.ErrorType.Warning);
					}
				}

				if (buttonType == "createAusbildung")
				{
					try
					{
						await FinalizeAusbildungCreation(message, Convert.ToUInt64(buttonId), guild, interaction); ;
					}
					catch (Exception ex)
					{
						ErrorHandler.HandleError(ex, guild, ErrorHandler.ErrorType.Error);
						await interaction.CreateResponseAsync(DiscordInteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder().WithContent("Etwas ist schiefgelaufen! Sind alle wichtigen Felder gefüllt?").AsEphemeral());
					}
				}

				if (buttonType == "cancelAusbildung")
				{

					RemoveCachedAusbildung(buttonId, guild);
					await message.DeleteAsync();
				}
			}
			catch (Exception ex)
			{
				ErrorHandler.HandleError(ex, guild, ErrorHandler.ErrorType.Error);
			}
		}

		public static async Task FinalizeAusbildungCreation(DiscordMessage message, ulong buttonId, DiscordGuild guild, DiscordInteraction? interaction)
		{

			var user = message.Interaction.User;

			var notifyRole = guild.GetRole(Helpers.GetMemberRoleID(guild.Id));

			//Getting Time and Date for Ausbildung
			//Format: "Anfang: " + timeAndDateBegin + "\nDauer: " + timeDuration;
			var partOne = message.Embeds[0].Fields[2].Value
				.Substring(message.Embeds[0].Fields[2].Value.IndexOf(':') + 2, "12.01.2000,19:30".Length + 1);
			var partTwo = message.Embeds[0].Fields[2].Value.Substring(message.Embeds[0].Fields[2].Value.IndexOf("Dauer:") + "Dauer:".Length + 1);
			var timeAndDateString = partOne + "_" + partTwo;
			var timeAndDateBegin = DateTime.ParseExact(timeAndDateString.Substring(0, timeAndDateString.IndexOf('_') - 1), "dd.MM.yyyy,HH:mm", CultureInfo.InvariantCulture);
			TimeSpan timeDuration = TimeSpan.Parse(timeAndDateString.Substring(timeAndDateString.IndexOf('_') + 1));


			var signUpButton = new DiscordButtonComponent(
					DiscordButtonStyle.Success,
					buttonId + "_signupAusbildung",
					"Anmelden"
					);

			var statusButton = new DiscordButtonComponent(
				DiscordButtonStyle.Secondary,
				buttonId + "_statusAusbildung",
				"Status");

			var buttons = new DiscordComponent[]
			{
					signUpButton,
					statusButton
			};

			DiscordEmbedBuilder finalizedEmbed = new DiscordEmbedBuilder(message.Embeds[0]);

			finalizedEmbed.Fields[2].Value = Formatter.Timestamp(timeAndDateBegin, TimestampFormat.LongDate) + " (" + Formatter.Timestamp(timeAndDateBegin, TimestampFormat.RelativeTime) + ")" + "\nAnfang: " + Formatter.Timestamp(timeAndDateBegin, TimestampFormat.ShortTime) + "\nDauer: " + timeAndDateString.Substring(timeAndDateString.IndexOf('_') + 1) + "h (Voraussichtlich)";

			DiscordForumChannel forumChannel = (DiscordForumChannel)guild.GetChannel(Helpers.GetEventForumID(guild.Id));
			DiscordForumPostStarter forumPost = await forumChannel.CreateForumPostAsync(new ForumPostBuilder().WithName($"{message.Embeds[0].Title}").WithMessage(new DiscordMessageBuilder().WithContent(notifyRole.Mention + message.Embeds[0].Fields[0].Value).WithAllowedMentions(new IMention[] { RoleMention.All }).AddEmbed(embed: finalizedEmbed).AddComponents(buttons)).WithAutoArchiveDuration(DiscordAutoArchiveDuration.Week));

			//Pin Message
			await forumPost.Message.PinAsync();

			Directory.CreateDirectory(Directory.GetCurrentDirectory() + "//" + guild.Id + "//Ausbildungen");
			Directory.CreateDirectory(Directory.GetCurrentDirectory() + "//" + guild.Id + "//Ausbildungen" + $"//{buttonId}");
			StreamWriter writer = new StreamWriter((Directory.GetCurrentDirectory() + "//" + guild.Id + "//Ausbildungen" + $"//{buttonId}" + "//forumPostID.txt"));
			writer.WriteLine(forumPost.Channel.Id);
			writer.Dispose();

			string signupPath = Directory.GetCurrentDirectory() + "//" + guild.Id + "//Ausbildungen" + $"//{buttonId}" + $"//signup.txt";

			FileStream file;
			if (!File.Exists(signupPath))
			{
				file = File.Create(signupPath);
				file.Close();
			}

			var createEmbed = new DiscordEmbedBuilder()
				.WithAuthor(user.Username, user.BannerUrl, user.AvatarUrl)
				.WithTitle("Ausbildung erstellt")
				.WithDescription(message.Embeds[0].Title)
				.AddField("Link", $"{forumPost.Message.JumpLink}")
				.WithColor(DiscordColor.Aquamarine);

			await guild.GetChannel(Helpers.GetLogChannelID(guild.Id)).SendMessageAsync(embed: createEmbed);
			try
			{
				DiscordScheduledGuildEvent discordEvent = null;
				try
				{
					discordEvent = await guild.CreateEventAsync(name: message.Embeds[0].Title, description: $"{forumPost.Message.JumpLink}", channelId: Helpers.GetMeetingPointID(guild.Id), type: DiscordScheduledGuildEventType.VoiceChannel, privacyLevel: DiscordScheduledGuildEventPrivacyLevel.GuildOnly, start: new DateTimeOffset(timeAndDateBegin), end: new DateTimeOffset(timeAndDateBegin.Add(timeDuration)));
				}
				catch (BadRequestException ex)
				{
					ErrorHandler.HandleError(ex, guild, ErrorHandler.ErrorType.Error);
				}

				var notifyMessage = message.Embeds[0].Description;

				Helpers.NotifyRoleFromEvent(message, notifyRole, notifyMessage, message.Embeds[0].Title, forumPost.Message, discordEvent);

			}
			catch (Exception wellShit)
			{
				ErrorHandler.HandleError(wellShit, guild, ErrorHandler.ErrorType.Error);
			}

			//Create File with time
			File.WriteAllText(Directory.GetCurrentDirectory() + "//" + guild.Id + "//Ausbildungen" + $"//{buttonId}" + $"//startTimeForCollection.txt", timeAndDateString.Substring(0, timeAndDateString.IndexOf('_') - 1));
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



			//Cleanup
			RemoveCachedAusbildung(buttonId.ToString(), guild);
			await message.DeleteAsync();
		}

		public static async void HandleAusbildungRegistration(ComponentInteractionCreateEventArgs e, string buttonType, string Id)
		{

			string signupPath = Directory.GetCurrentDirectory() + "//" + e.Guild.Id + "//Ausbildungen" + $"//{Id}" + $"//signup.txt";

			if (buttonType == "statusAusbildung")
			{
				try
				{
					await e.Interaction.DeferAsync(ephemeral: true);

					string[] signon = await Helpers.GetNicknameByIdArray(File.ReadAllLines(signupPath), e.Guild, signupPath);



					var embed = new DiscordEmbedBuilder()
						.WithTitle("Status")
						.AddField($"Angemeldet: {File.ReadAllLines(signupPath).Length}", $"------------------------------------------\n{String.Join("\n", signon)}\n------------------------------------------\n");

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



			if (buttonType == "signupAusbildung")
			{
				await e.Interaction.DeferAsync(ephemeral: true);

				DiscordMessage message = e.Message;
				DiscordMember member = (DiscordMember)e.User;

				var fieldContent = message.Embeds[0].Fields[1].Value;
				if (fieldContent != "-")
				{
					if (!member.Roles.Contains(e.Guild.GetRole(Convert.ToUInt64(fieldContent.Remove(fieldContent.Length - 1).Substring(3)))))
					{
						await e.Interaction.EditOriginalResponseAsync(new DiscordWebhookBuilder().WithContent("Unzureichende Ausbildungen!"));
						return;
					}
				}

				if (HandleRegistration(e, Id, "signup.txt"))
					await e.Interaction.EditOriginalResponseAsync(new DiscordWebhookBuilder().WithContent("Erfolgreich Angemeldet"));
				else
					await e.Interaction.EditOriginalResponseAsync(new DiscordWebhookBuilder().WithContent("Anmeldung Aufgehoben"));
			}
		}

		private static bool HandleRegistration(ComponentInteractionCreateEventArgs e, string interactionId, string fileName)
		{
			string signupPath = Directory.GetCurrentDirectory() + "//" + e.Guild.Id + "//Ausbildungen" + $"//{interactionId}" + $"//signup.txt";

			DiscordMember member = (DiscordMember)e.User;

			if (!File.ReadAllLines(Directory.GetCurrentDirectory() + "//" + e.Guild.Id + "//Ausbildungen" + $"//{interactionId}" + $"//{fileName}").Contains(member.Id.ToString()))
			{
				StreamWriter writer = new StreamWriter(Directory.GetCurrentDirectory() + "//" + e.Guild.Id + "//Ausbildungen" + $"//{interactionId}" + $"//{fileName}", true);
				writer.WriteLine(member.Id.ToString());
				writer.Dispose();
				return true;
			}
			else
			{
				var file = File.ReadAllLines(Directory.GetCurrentDirectory() + "//" + e.Guild.Id + "//Ausbildungen" + $"//{interactionId}" + $"//{fileName}").ToList();
				file.Remove(member.Id.ToString());
				File.Delete(Directory.GetCurrentDirectory() + "//" + e.Guild.Id + "//Ausbildungen" + $"//{interactionId}" + $"//{fileName}");
				File.WriteAllLines(Directory.GetCurrentDirectory() + "//" + e.Guild.Id + "//Ausbildungen" + $"//{interactionId}" + $"//{fileName}", file);
				return false;
			}
		}

		private static void RemoveCachedAusbildung(string buttonId, DiscordGuild guild)
		{
			//Cleanup
			File.Delete(Directory.GetCurrentDirectory() + $"//{guild.Id}//AusbildungCreationCache//{buttonId}//channelId.txt");
			File.Delete(Directory.GetCurrentDirectory() + $"//{guild.Id}//AusbildungCreationCache//{buttonId}//messageId.txt");

			Directory.Delete(Directory.GetCurrentDirectory() + $"//{guild.Id}//AusbildungCreationCache//{buttonId}");
		}
	}
}

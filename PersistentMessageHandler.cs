using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using DSharpPlus.Interactivity.Extensions;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace EuDef
{
	public static class PersistentMessageHandler
	{
		public static async Task CheckPersitantMessages(IReadOnlyDictionary<ulong, DiscordGuild> guilds)
		{
			foreach (var guildPair in guilds)
			{
				try
				{
					var channelID = Helpers.GetLongTermSignoffID(guildPair.Key);

					var channel = guildPair.Value.GetChannel(channelID);

					var message = (await channel.GetPinnedMessagesAsync()).FirstOrDefault();

					if (message is null)
					{
						//Message doesnt exist
						await CreateLongTermSignoffMessage(channel);
					}
				}
				catch (FileNotFoundException ex)
				{
					//Guild doesnt have that channel set, so whatever
				}
			}
		}

		private static async Task CreateLongTermSignoffMessage(DiscordChannel channel)
		{
			File.WriteAllText(Directory.GetCurrentDirectory() + $"//{channel.Guild.Id}//longTermSignoff.txt", JsonConvert.SerializeObject(new Dictionary<string, string>()));
			var userData = JsonConvert.DeserializeObject<Dictionary<string, string>>(File.ReadAllText(Directory.GetCurrentDirectory() + $"//{channel.Guild.Id}//longTermSignoff.txt"));

			var embed = await FormatEmbedForLongTermSignoff(userData, channel.Guild);

			var addButton = new DiscordButtonComponent
				(
					DiscordButtonStyle.Success,
					channel.Id + "_addSignoffEntry",
					"Eintragen"
				);

			var removeButton = new DiscordButtonComponent
				(
					DiscordButtonStyle.Danger,
					channel.Id + "_removeSignoffEntry",
					"Austragen"
				);



			// Send the embed to the same channel as the command was sent in
			var message = await channel.SendMessageAsync(new DiscordMessageBuilder().AddEmbed(embed).AddComponents(new DiscordComponent[] { addButton, removeButton }));
			foreach (var item in (await channel.GetPinnedMessagesAsync())) { await item.UnpinAsync(); }
			await message.PinAsync();
		}

		public static async Task<DiscordEmbed> FormatEmbedForLongTermSignoff(Dictionary<string, string> userData, DiscordGuild guild)
		{
			// Example entries
			//userData = new Dictionary<string, string>
			//{
			//	 { "337259424118734850", "31.12.2024" },
			//	 { "417210516839071744", "31.12.2024" },
			//	 { "1223672850527948820", "31.12.2024" },
			//	 { "356803415407460353", "31.12.2024" }
			//};

			List<string> displayNames = new List<string>();

			foreach (var user in userData)
				displayNames.Add((await guild.GetMemberAsync(Convert.ToUInt64(user.Key))).DisplayName);

			var embed = new DiscordEmbedBuilder
			{
				Title = "Langzeit-Abmeldungen [Automatische Abmeldung bei neuen Events]",
				Color = DiscordColor.Red // You can change the color as needed
			};

			if (userData.Count > 0)
			{
				var maxLength = displayNames.Max(name => name.Length);
				StringBuilder description = new StringBuilder("```\nName" + new string(' ', maxLength - 4 + 3) + "Abgemeldet bis\n");

				// Adding rows to the description
				for (int i = 0; i < displayNames.Count; i++)
				{
					description.Append(displayNames[i] + new string(' ', maxLength - displayNames[i].Length + 3) + userData.ToArray()[i].Value + "\n");
				}
				description.Append("```");
				embed.Description = description.ToString();
			}
			else
			{
				embed.Description = "```\nKeine Abmeldungen\n```";
			}

			return embed;
		}

		public static async Task UpdateSignoffEntry(string buttonId, ComponentInteractionCreateEventArgs e, bool remove)
		{
			var path = Directory.GetCurrentDirectory() + $"//{e.Guild.Id}//longTermSignoff.txt";

			var userData = JsonConvert.DeserializeObject<Dictionary<string, string>>(File.ReadAllText(path));

			if (remove)
			{
				if (userData.Count > 0)
				{

					if (userData.ContainsKey(e.User.Id.ToString()))
					{
						userData.Remove(e.User.Id.ToString());
						File.WriteAllText(path, JsonConvert.SerializeObject(userData));
						await e.Interaction.CreateResponseAsync(DiscordInteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder().WithContent("Eintrag aufgelöst").AsEphemeral());
					}
				}
				else
				{
					await e.Interaction.CreateResponseAsync(DiscordInteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder().WithContent("Kein Eintrag gefunden").AsEphemeral());
					return;
				}
			}
			else
			{
				if (userData.ContainsKey(e.User.Id.ToString()))
				{
					await e.Interaction.CreateResponseAsync(DiscordInteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder().WithContent("Bereits eingetragen, bitte zuerst Eintrag entfernen").AsEphemeral());
					return;
				}
				else
				{
					var modal = new DiscordInteractionResponseBuilder()
						.WithTitle("Langzeit-Abmeldung")
						.WithCustomId($"id-signoff-date-{e.Interaction.Id}")
						.AddComponents(new DiscordTextInputComponent(label: "Abgemeldet bis [31.12.2024]", placeholder: "31.12.2024", customId: "id-date", style: DiscordTextInputStyle.Short));

					await e.Interaction.CreateResponseAsync(DiscordInteractionResponseType.Modal, modal);
					var interactivity = Program.discordClient.GetInteractivity();
					var response = await interactivity.WaitForModalAsync($"id-signoff-date-{e.Interaction.Id}", user: e.User, timeoutOverride: TimeSpan.FromSeconds(1800));

					if (!response.TimedOut)
					{
						if (Helpers.IsValidDate(response.Result.Values["id-date"], "dd.MM.yyyy"))
						{
							userData.Add(e.User.Id.ToString(), response.Result.Values["id-date"]);
							File.WriteAllText(path, JsonConvert.SerializeObject(userData));

							await response.Result.Interaction.CreateResponseAsync(DiscordInteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder().WithContent($"Eintrag erfolgreich erstellt").AsEphemeral());
						}
						else
						{
							await response.Result.Interaction.CreateResponseAsync(DiscordInteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder().WithContent($"{response.Result.Values["id-date"]} ist kein valides Datum").AsEphemeral());
							return;
						}
					}
				}
			}


			await UpdateLongTermSignoffMessage(e.Message, userData);
		}

		public static async Task UpdateLongTermSignoffMessage(DiscordMessage message, Dictionary<string, string> userData)
		{

			var discordMessage = new DiscordMessageBuilder().AddComponents(message.Components)
				.AddEmbed(await FormatEmbedForLongTermSignoff(userData, message.Channel.Guild));

			await message.ModifyAsync(discordMessage);
		}
	}
}

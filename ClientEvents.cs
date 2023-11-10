﻿using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using DSharpPlus.Exceptions;
using DSharpPlus.SlashCommands;
using DSharpPlus.SlashCommands.Attributes;
using DSharpPlus.SlashCommands.EventArgs;
using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace EuDef
{
    public static class ClientEvents
    {
        public static void RegisterClientEvents(DiscordClient client, SlashCommandsExtension slash)
        {
            client.GuildMemberAdded += async (s, e) => await GuildMemberAdded(e);
            client.GuildMemberUpdated += async (s, e) => await GuildMemberUpdated(e);
            client.GuildMemberRemoved += async (s, e) => await GuildMemberRemoved(e);
            client.ComponentInteractionCreated += async (s, e) => await ComponentInteractionCreated(e);
            client.ClientErrored += async (s, e) => await ClientErrored(e);
            client.ModalSubmitted += async (s, e) => await ModalSubmitted(e);
            slash.SlashCommandErrored += async (s, e) => await SlashCommandErrored(e);
        }

        private static async Task ModalSubmitted(ModalSubmitEventArgs e)
        {
            //Modal.WithCustomId($"{buttonId}_eventCreateUpdate_{buttonType}")

            //NOTE: ONLY WORKS FOR EVENT CREATION UPDATE MODALS AS OF RIGHT NOW

            var customID = e.Interaction.Data.CustomId;

            if (!customID.Contains('_'))
                return;

            var interactionId = customID.Substring(0, customID.IndexOf('_'));
            var buttonType = customID.Substring(customID.LastIndexOf('_') + 1);

            var messageId = File.ReadAllText(Directory.GetCurrentDirectory() + $"//{e.Interaction.Guild.Id}//EventCreationCache//{interactionId}//messageId.txt");
            var message = e.Interaction.Guild.GetChannel(Helpers.GetBotChannelID(e.Interaction.Guild.Id)).GetMessageAsync(Convert.ToUInt64(messageId)).Result;

            var embed = new DiscordEmbedBuilder(message.Embeds[0]);

            if (buttonType == "addTitle")
            {
                embed.WithTitle(e.Values["id-title"]);

                await e.Interaction.CreateResponseAsync(InteractionResponseType.UpdateMessage, new DiscordInteractionResponseBuilder()
                    .AddEmbed(embed)
                    .AddComponents(message.Components));
            }

            if (buttonType == "addDescription")
            {
                embed.Fields[1].Value = (e.Values["id-description"]);

                await e.Interaction.CreateResponseAsync(InteractionResponseType.UpdateMessage, new DiscordInteractionResponseBuilder()
                    .AddEmbed(embed)
                    .AddComponents(message.Components));
            }

            if (buttonType == "addDateTime")
            {
                try
                {
                    //Checks whether or not its a valid format
                    var timeAndDateString = e.Values["id-datetime"];
                    var timeAndDate = DateTime.ParseExact(timeAndDateString, "dd.MM.yyyy,HH:mm", CultureInfo.InvariantCulture);
                    var timeAndDateOffset = DateTimeOffset.ParseExact(timeAndDateString, "dd.MM.yyyy,HH:mm", CultureInfo.InvariantCulture);

                    if (timeAndDate < DateTime.Now)
                    {
                        throw new Exception();
                    }

                    embed.Fields[0].Value = timeAndDateString;

                    await e.Interaction.CreateResponseAsync(InteractionResponseType.UpdateMessage, new DiscordInteractionResponseBuilder()
                    .AddEmbed(embed)
                    .AddComponents(message.Components));
                }
                catch
                {
                    var errorEmbed = new DiscordEmbedBuilder()
                        .WithColor(DiscordColor.Red)
                        .WithDescription("Falsches Datum/Zeit-Format");
                    await e.Interaction.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder().AddEmbed(errorEmbed).AsEphemeral());
                }
            }

            if (buttonType == "addNotifyMessage")
            {
                embed.Fields[2].Value = (e.Values["id-notify"]);

                await e.Interaction.CreateResponseAsync(InteractionResponseType.UpdateMessage, new DiscordInteractionResponseBuilder()
                    .AddEmbed(embed)
                    .AddComponents(message.Components));
            }

        }

        private static async Task GuildMemberAdded(GuildMemberAddEventArgs e)
        {
            if (Directory.Exists(Directory.GetCurrentDirectory() + "//" + e.Guild.Id + "//WelcomeMessage"))
            {
                if (e.Member.IsBot)
                    return;
                //Welcome Message
                var embedBuilder = new DiscordEmbedBuilder()
                 .WithTitle($"{File.ReadAllText(Path.Combine(Directory.GetCurrentDirectory() + "//" + e.Guild.Id + "//WelcomeMessage", "welcome_message_titel.txt"))}")
                    .WithDescription($"{String.Join("", File.ReadAllLines(Path.Combine(Directory.GetCurrentDirectory() + "//" + e.Guild.Id + "//WelcomeMessage", "welcome_message.txt")))}")
                    .AddField($"{File.ReadAllText(Path.Combine(Directory.GetCurrentDirectory() + "//" + e.Guild.Id + "//WelcomeMessage", "welcome_message_rules_titel.txt"))}", $"{e.Guild.GetChannel(Convert.ToUInt64(File.ReadAllText(Path.Combine(Directory.GetCurrentDirectory() + "//" + e.Guild.Id + "//WelcomeMessage", "welcome_message_rules_id.txt")))).Mention}");

                await e.Member.SendMessageAsync(embedBuilder);

            }

            //Grant divider Roles
            var dividerRoleIds = Helpers.GetDividerRoleIDs(e.Guild.Id);

            await e.Member.GrantRoleAsync(e.Guild.GetRole(dividerRoleIds[0]));
            await e.Member.GrantRoleAsync(e.Guild.GetRole(dividerRoleIds[1]));
            await e.Member.GrantRoleAsync(e.Guild.GetRole(dividerRoleIds[2]));
        }

        private static async Task GuildMemberUpdated(GuildMemberUpdateEventArgs e)
        {
            //TODO: Anwärter-Sytem? Klären ob noch gebraucht
        }

        private static async Task GuildMemberRemoved(GuildMemberRemoveEventArgs e)
        {
            //TODO: Anwärter-Sytem? Klären ob noch gebraucht
        }

        private static async Task ComponentInteractionCreated(ComponentInteractionCreateEventArgs e)
        {
            var memberId = Helpers.GetMemberRoleID(e.Guild.Id);

            var buttonId = e.Id;
            var buttonType = e.Id.Substring(e.Id.LastIndexOf('_') + 1);
            var Id = e.Id.Substring(0, e.Id.IndexOf('_'));

            Console.WriteLine("Component Interaction received : " + buttonType + " : " + Id);

            //Event related
            if (buttonType == "status" || buttonType == "signup" || buttonType == "signoff" || buttonType == "undecided")
                EventFunctions.HandleEventRegistration(e, buttonType, Id);

            if (buttonType == "addTitle" || buttonType == "addDescription" || buttonType == "addDateTime" || buttonType == "addNotifyMessage" || buttonType == "createEvent" || buttonType == "cancelEvent")
                EventFunctions.HandleEventCreationUpdate(buttonType, Id, e);

            //Role granting
            if (buttonType == "rolebutton")
            {

                DiscordMember member = (DiscordMember)e.User;
                var role = e.Guild.GetRole(Convert.ToUInt64(Id));

                if (!member.Roles.Contains(role))
                {
                    await member.GrantRoleAsync(role);
                    await e.Interaction.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder().WithContent("Rolle hinzugefügt: " + role.Mention).AsEphemeral());
                }
                else
                {
                    await member.RevokeRoleAsync(role);
                    await e.Interaction.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder().WithContent("Rolle entfernt: " + role.Mention).AsEphemeral());
                }
            }
        }

        private static async Task ClientErrored(ClientErrorEventArgs e)
        {
            ErrorHandler.HandleError(e.Exception, null, ErrorHandler.ErrorType.Error);
        }

        private static async Task SlashCommandErrored(SlashCommandErrorEventArgs e)
        {
            //Failed because user is lacking permissions
            if (e.Exception is SlashExecutionChecksFailedException slex)
            {
                foreach (var check in slex.FailedChecks)
                {
                    if (check is SlashRequire.RequireRoleAttribute)
                        await e.Context.EditResponseAsync(new DiscordWebhookBuilder().WithContent("Insufficient Permissions!"));
                    else if (check is SlashCooldownAttribute)
                        await e.Context.EditResponseAsync(new DiscordWebhookBuilder().WithContent("On Cooldown!"));
                }
            }

            else
                ErrorHandler.HandleError(e.Exception, null, ErrorHandler.ErrorType.Error);
        }
    }
}

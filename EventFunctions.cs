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

namespace EuDef
{
    public static class EventFunctions
    {
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

                    string[] signon = await Helpers.GetNicknameByIdArray(File.ReadAllLines(signupPath), e.Guild);
                    string[] signoff = await Helpers.GetNicknameByIdArray(File.ReadAllLines(signoffPath), e.Guild);
                    string[] undecided = await Helpers.GetNicknameByIdArray(File.ReadAllLines(undecidedPath), e.Guild);



                    var embed = new DiscordEmbedBuilder()
                        .WithTitle("Status")
                        .AddField($"Angemeldet: {File.ReadAllLines(signupPath).Length}", $"------------------------------------------\n{String.Join("\n", signon)}\n------------------------------------------\n")
                        .AddField($"Abgemeldet: {File.ReadAllLines(signoffPath).Length}", $"------------------------------------------\n{String.Join("\n", signoff)}\n------------------------------------------\n")
                        .AddField($"Unentschieden: {File.ReadAllLines(undecidedPath).Length}", $"------------------------------------------\n{String.Join("\n", undecided)}\n------------------------------------------\n");

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
                await e.Interaction.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder().WithContent("Event ist geschlossen").AsEphemeral());
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
                await e.Interaction.DeferAsync(ephemeral: true);

                if (HandleRegistration(e, Id, "signoff.txt"))
                    await e.Interaction.EditOriginalResponseAsync(new DiscordWebhookBuilder().WithContent("Erfolgreich Abgemeldet"));
                else
                    await e.Interaction.EditOriginalResponseAsync(new DiscordWebhookBuilder().WithContent("Abmeldung Aufgehoben"));
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

        public static async void HandleEventCreationUpdate(string buttonType, string buttonId, ComponentInteractionCreateEventArgs e)
        {
            var messageId = File.ReadAllText(Directory.GetCurrentDirectory() + $"//{e.Guild.Id}//EventCreationCache//{buttonId}//messageId.txt");
            var message = e.Guild.GetChannel(Helpers.GetBotChannelID(e.Guild.Id)).GetMessageAsync(Convert.ToUInt64(messageId)).Result;

            var modal = new DiscordInteractionResponseBuilder().WithContent("Eingeben").WithCustomId($"{buttonId}_eventCreateUpdate_{buttonType}");

            if (buttonType == "addTitle")
            {
                modal.WithTitle("Titel");
                modal.AddComponents(
                    new TextInputComponent(label: "Titel", customId: "id-title", value: message.Embeds[0].Title, style: TextInputStyle.Short));
                await e.Interaction.CreateResponseAsync(InteractionResponseType.Modal, modal);
            }

            if (buttonType == "addDescription")
            {
                modal.WithTitle("Beschreibung");
                modal.AddComponents(
                    new TextInputComponent(label: "Beschreibung", customId: "id-description", value: message.Embeds[0].Fields[1].Value, style: TextInputStyle.Paragraph));
                await e.Interaction.CreateResponseAsync(InteractionResponseType.Modal, modal);
            }

            if (buttonType == "addDateTime")
            {
                var timeAndDateString = message.Embeds[0].Fields[0].Value
                .Substring(message.Embeds[0].Fields[0].Value.IndexOf(':') + 2, "12.01.2000,19:30".Length + 1) + "_" + message.Embeds[0].Fields[0].Value
                .Substring(message.Embeds[0].Fields[0].Value.IndexOf("Ende:") + 6);

                modal.WithTitle("Datum und Zeit");
                modal.AddComponents(
                    new TextInputComponent(label: "Beginn [12.01.2000,19:30]",
                    placeholder: "12.01.2000,19:30",
                    value: timeAndDateString.Substring(0, timeAndDateString.IndexOf('_') - 1),
                    customId: "id-datetimebegin", style: TextInputStyle.Short))
                    .AddComponents(
                    new TextInputComponent(label: "Ende [12.01.2000,21:30]",
                    placeholder: "12.01.2000,21:30",
                    value: timeAndDateString.Substring(timeAndDateString.IndexOf('_') + 1),
                    customId: "id-datetimeend", style: TextInputStyle.Short));
                try
                {
                    await e.Interaction.CreateResponseAsync(InteractionResponseType.Modal, modal);
                }
                catch (NullReferenceException ex)
                {
                    Console.WriteLine(ex.ToString());
                }
            }

            if (buttonType == "addNotifyMessage")
            {
                modal.WithTitle("Benachrichtigung");
                modal.AddComponents(
                    new TextInputComponent(label: "Benachrichtigung", customId: "id-notify", value: message.Embeds[0].Fields[2].Value, style: TextInputStyle.Paragraph));
                await e.Interaction.CreateResponseAsync(InteractionResponseType.Modal, modal);
            }

            if (buttonType == "createEvent")
            {
                try
                {
                    await FinalizeEventCreation(message, Convert.ToUInt64(buttonId), e);

                    RemoveCachedEvent(buttonId, e);
                    await message.DeleteAsync();
                }
                catch (Exception ex)
                {
                    ErrorHandler.HandleError(ex, e.Guild, ErrorHandler.ErrorType.Warning);
                    await e.Interaction.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder().WithContent("Etwas ist schiefgelaufen! Sind alle wichtigen Felder gefüllt?").AsEphemeral());
                }
            }

            if (buttonType == "cancelEvent")
            {
                RemoveCachedEvent(buttonId, e);
                await message.DeleteAsync();
            }
        }

        private static async Task FinalizeEventCreation(DiscordMessage message, ulong buttonId, ComponentInteractionCreateEventArgs e)
        {
            var notifyRole = e.Guild.GetRole(Helpers.GetMemberRoleID(e.Guild.Id));

            //Getting Time and Date for event
            //Format: "Anfang: " + timeAndDateBegin + "\nEnde: " + timeAndDateEnd;
            var timeAndDateString = message.Embeds[0].Fields[0].Value
                .Substring(message.Embeds[0].Fields[0].Value.IndexOf(':') + 2, "12.01.2000,19:30".Length + 1) + "_" + message.Embeds[0].Fields[0].Value
                .Substring(message.Embeds[0].Fields[0].Value.IndexOf("Ende:") + 6);
            var timeAndDateBegin = DateTime.ParseExact(timeAndDateString.Substring(0, timeAndDateString.IndexOf('_') - 1), "dd.MM.yyyy,HH:mm", CultureInfo.InvariantCulture);
            var timeAndDateEnd = DateTime.ParseExact(timeAndDateString.Substring(timeAndDateString.IndexOf('_') + 1), "dd.MM.yyyy,HH:mm", CultureInfo.InvariantCulture);

            //Really bad solution: Remove one hour to match timezone
            timeAndDateBegin = timeAndDateBegin.AddHours(-1);
            timeAndDateEnd = timeAndDateEnd.AddHours(-1);

            var signUpButton = new DiscordButtonComponent(
                    ButtonStyle.Success,
                    buttonId + "_signup",
                    "Anmelden"
                    );

            var signOffButton = new DiscordButtonComponent(
                ButtonStyle.Danger,
                buttonId + "_signoff",
                "Abmelden"
                );

            var undecidedButton = new DiscordButtonComponent(
                ButtonStyle.Primary,
                buttonId + "_undecided",
                "Unentschieden"
                );

            var statusButton = new DiscordButtonComponent(
                ButtonStyle.Secondary,
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
                .RemoveFieldAt(2);
            finalizedEmbed.Fields[0].Value = Formatter.Timestamp(timeAndDateBegin, TimestampFormat.LongDate) + " (" + Formatter.Timestamp(timeAndDateBegin, TimestampFormat.RelativeTime) + ")" + "\nAnfang: " + Formatter.Timestamp(timeAndDateBegin, TimestampFormat.ShortTime) + "\nEnde: " + Formatter.Timestamp(timeAndDateEnd, TimestampFormat.ShortTime);

            DiscordForumChannel forumChannel = (DiscordForumChannel)e.Guild.GetChannel(Helpers.GetEventForumID(e.Guild.Id));
            DiscordForumPostStarter forumPost = await forumChannel.CreateForumPostAsync(new ForumPostBuilder().WithName($"{message.Embeds[0].Title}").WithMessage(new DiscordMessageBuilder().WithContent(notifyRole.Mention).WithAllowedMentions(new IMention[] { new RoleMention(notifyRole) }).WithEmbed(embed: finalizedEmbed).AddComponents(buttons)).WithAutoArchiveDuration(AutoArchiveDuration.Week));

            //Pin Message
            await forumPost.Message.PinAsync();

            Directory.CreateDirectory(Directory.GetCurrentDirectory() + "//" + e.Guild.Id + "//Events");
            Directory.CreateDirectory(Directory.GetCurrentDirectory() + "//" + e.Guild.Id + "//Events" + $"//{buttonId}");
            StreamWriter writer = new StreamWriter((Directory.GetCurrentDirectory() + "//" + e.Guild.Id + "//Events" + $"//{buttonId}" + "//forumPostID.txt"));
            writer.WriteLine(forumPost.Channel.Id);
            writer.Dispose();

            string signupPath = Directory.GetCurrentDirectory() + "//" + e.Guild.Id + "//Events" + $"//{buttonId}" + $"//signup.txt";
            string signoffPath = Directory.GetCurrentDirectory() + "//" + e.Guild.Id + "//Events" + $"//{buttonId}" + $"//signoff.txt";
            string undecidedPath = Directory.GetCurrentDirectory() + "//" + e.Guild.Id + "//Events" + $"//{buttonId}" + $"//undecided.txt";
            FileStream file;
            file = File.Create(signupPath);
            file.Close();
            file = File.Create(signoffPath);
            file.Close();
            file = File.Create(undecidedPath);
            file.Dispose();

            var createEmbed = new DiscordEmbedBuilder()
                .WithAuthor(e.User.Username, e.User.BannerUrl, e.User.AvatarUrl)
                .WithTitle("Event Created")
                .WithDescription(message.Embeds[0].Title)
                .AddField("Link", $"{forumPost.Message.JumpLink}")
                .WithColor(DiscordColor.Aquamarine);

            await e.Guild.GetChannel(Helpers.GetLogChannelID(e.Guild.Id)).SendMessageAsync(embed: createEmbed);
            try
            {
                DiscordScheduledGuildEvent discordEvent = null;
                try
                {
                    discordEvent = await e.Guild.CreateEventAsync(name: message.Embeds[0].Title, description: $"{forumPost.Message.JumpLink}", channelId: Helpers.GetMeetingPointID(e.Guild.Id), type: ScheduledGuildEventType.VoiceChannel, privacyLevel: ScheduledGuildEventPrivacyLevel.GuildOnly, start: new DateTimeOffset(timeAndDateBegin), end: new DateTimeOffset(timeAndDateEnd));
                }
                catch (BadRequestException ex)
                {
                    ErrorHandler.HandleError(ex, e.Guild, ErrorHandler.ErrorType.Error);
                }

                var notifyMessage = message.Embeds[0].Fields[2].Value;

                Helpers.NotifyRoleFromEvent(message, notifyRole, notifyMessage, message.Embeds[0].Title, forumPost.Message, discordEvent);

            }
            catch (Exception wellShit)
            {
                ErrorHandler.HandleError(wellShit, e.Guild, ErrorHandler.ErrorType.Error);
            }

            //Create File with time
            File.WriteAllText(Directory.GetCurrentDirectory() + "//" + e.Guild.Id + "//Events" + $"//{buttonId}" + $"//startTimeForCollection.txt", timeAndDateString.Substring(0, timeAndDateString.IndexOf('_') - 1));
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

            File.WriteAllText(Directory.GetCurrentDirectory() + "//" + e.Guild.Id + "//Events" + $"//{buttonId}" + $"//remindUndecided.txt", $"{onedaybefore.ToString("dd.MM.yyyy,HH:mm")}");

        }



        private static void RemoveCachedEvent(string buttonId, ComponentInteractionCreateEventArgs e)
        {
            //Cleanup
            File.Delete(Directory.GetCurrentDirectory() + $"//{e.Guild.Id}//EventCreationCache//{buttonId}//channelId.txt");
            File.Delete(Directory.GetCurrentDirectory() + $"//{e.Guild.Id}//EventCreationCache//{buttonId}//messageId.txt");
            Directory.Delete(Directory.GetCurrentDirectory() + $"//{e.Guild.Id}//EventCreationCache//{buttonId}");
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


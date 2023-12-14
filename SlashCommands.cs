using DSharpPlus.Entities;
using DSharpPlus.Interactivity.Extensions;
using DSharpPlus.Interactivity;
using DSharpPlus;
using DSharpPlus.SlashCommands;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DSharpPlus.Exceptions;
using System.Globalization;
using System.Net.Http.Headers;

namespace EuDef
{
    public class SlashCommands : ApplicationCommandModule
    {

        [SlashCommand("notify", "Notify specified Role")]
        [SlashCommandPermissions(Permissions.Administrator)]
        public async Task Notify(InteractionContext ctx, [Option("role", "Role to notify")] DiscordRole role, [Option("message", "Message")] string message)
        {
            DiscordEmbedBuilder embed = new DiscordEmbedBuilder();
            var confirmButton = new DiscordButtonComponent(
                ButtonStyle.Success,
                $"{ctx.InteractionId}_confirmNotify",
                "Confirm"
                );

            var cancelButton = new DiscordButtonComponent(
                ButtonStyle.Danger,
                $"{ctx.InteractionId}_cancelNotify",
                "Cancel"
                );

            embed.AddField("Sending to", role.Mention)
                        .WithAuthor(ctx.Member.Username, ctx.Member.BannerUrl, ctx.Member.AvatarUrl)
                        .WithColor(DiscordColor.Yellow)
                        .AddField("Message", message);
            await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder().AddComponents(confirmButton, cancelButton).AddEmbed(embed).AsEphemeral());

            var interactivity = ctx.Client.GetInteractivity();

            var originalMessage = await ctx.GetOriginalResponseAsync();

            InteractivityResult<DSharpPlus.EventArgs.ComponentInteractionCreateEventArgs> result = await interactivity.WaitForButtonAsync(originalMessage, ctx.User, TimeSpan.FromSeconds(60));
            if (result.TimedOut)
            {
                await ctx.DeleteFollowupAsync(originalMessage.Id);
            }
            else if (result.Result.Id == $"{ctx.InteractionId}_cancelNotify")
            {
                await ctx.DeleteFollowupAsync(originalMessage.Id);
            }
            else if (result.Result.Id == $"{ctx.InteractionId}_confirmNotify")
            {
                Helpers.NotifyRole(ctx, role, message, null, null, true, null);
            }
            await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent("Notifications sent!"));
        }

        [SlashCommandGroup("event", "Event management")]

        public class Event
        {

            [SlashCommand("create", "Start the event builder")]
            [SlashCommandPermissions(Permissions.Administrator)]
            public async Task Create(InteractionContext ctx,
                [Option("abstimmung", "Setzt ob über den Eventtag abgestimt werden soll")] EventFunctions.DoVote doVote)
            {
                if (ctx.Channel.Id != Helpers.GetBotChannelID(ctx.Guild.Id))
                {
                    await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder().WithContent("Bitte im Bot-Kanal ausführen! " + ctx.Guild.GetChannel(Helpers.GetBotChannelID(ctx.Guild.Id)).Mention).AsEphemeral());
                    return;
                }

                var addTitleButton = new DiscordButtonComponent(
                    ButtonStyle.Secondary,
                    $"{ctx.InteractionId}_addTitle",
                    "Titel"
                    );
                var addDescriptionButton = new DiscordButtonComponent(
                    ButtonStyle.Secondary,
                    $"{ctx.InteractionId}_addDescription",
                    "Beschreibung"
                    );
                var addDateTimeButton = new DiscordButtonComponent(
                    ButtonStyle.Secondary,
                    $"{ctx.InteractionId}_addDateTime",
                    "Datum"
                    );
                var addNotifyMessageButton = new DiscordButtonComponent(
                    ButtonStyle.Secondary,
                    $"{ctx.InteractionId}_addNotifyMessage",
                    "Benachrichtigung"
                    );
                var addCreateEventButton = new DiscordButtonComponent(
                    ButtonStyle.Success,
                    $"{ctx.InteractionId}_createEvent",
                    "Erstellen"
                    );
                var addCancelEventButton = new DiscordButtonComponent(
                    ButtonStyle.Danger,
                    $"{ctx.InteractionId}_cancelEvent",
                    "Abbrechen"
                    );

                var embed = new DiscordEmbedBuilder()
                    .WithTitle("Platzhalter")
                    .WithDescription("Platzhalter")
                    //Spaces are nececary... because wonky code
                    .AddField("Datum", "Anfang: " + "Platzhalter       \nEnde: Platzhalter       ")
                    .AddField("Benachrichtigungstext", "Platzhalter");

                if (doVote == EventFunctions.DoVote.TagAbstimmen)
                {
                    embed.Fields[0].Value = "Über Abstimmung";
                    await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder().AddComponents(addTitleButton, addDescriptionButton, addNotifyMessageButton).AddComponents(addCreateEventButton, addCancelEventButton).AddEmbed(embed));
                }
                else
                    await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder().AddComponents(addTitleButton, addDescriptionButton, addDateTimeButton, addNotifyMessageButton).AddComponents(addCreateEventButton, addCancelEventButton).AddEmbed(embed));

                //Caching
                var originalResponse = await ctx.GetOriginalResponseAsync();

                Directory.CreateDirectory(Directory.GetCurrentDirectory() + $"//{ctx.Guild.Id}//EventCreationCache");
                Directory.CreateDirectory(Directory.GetCurrentDirectory() + $"//{ctx.Guild.Id}//EventCreationCache//{ctx.InteractionId}");
                File.WriteAllText(Directory.GetCurrentDirectory() + $"//{ctx.Guild.Id}//EventCreationCache//{ctx.InteractionId}//channelId.txt", ctx.Channel.Id.ToString());
                File.WriteAllText(Directory.GetCurrentDirectory() + $"//{ctx.Guild.Id}//EventCreationCache//{ctx.InteractionId}//messageId.txt", originalResponse.Id.ToString());

                if (doVote == EventFunctions.DoVote.TagAbstimmen)
                    File.WriteAllText(Directory.GetCurrentDirectory() + $"//{ctx.Guild.Id}//EventCreationCache//{ctx.InteractionId}//doVote.txt", ctx.InteractionId.ToString());
            }

            [SlashCommand("edit", "Edit event")]
            [SlashCommandPermissions(Permissions.Administrator)]
            public async Task Modify(InteractionContext ctx, [Option("thread_id", "Id of thread")] string id)
            {
                DiscordMessage message;
                DiscordForumChannel channel = (DiscordForumChannel)ctx.Guild.GetChannel(Helpers.GetEventForumID(ctx.Guild.Id));
                try
                {
                    DiscordThreadChannel threadChannel = Helpers.GetThreadChannelByID(channel, id);
                    var pinnedMessages = await threadChannel.GetPinnedMessagesAsync();
                    message = pinnedMessages.First();
                }
                catch
                {
                    await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder().WithContent("Event doesn't exist. Check your Thread ID").AsEphemeral());
                    return;
                }

                var modal = new DiscordInteractionResponseBuilder()
                    .WithTitle("Edit Event")
                    .WithCustomId($"id-event-edit-{ctx.InteractionId}")
                    .AddComponents(new TextInputComponent(label: "Name", customId: "id-name", value: message.Embeds[0].Title, style: TextInputStyle.Short))
                    .AddComponents(new TextInputComponent(label: "Beschreibung", value: message.Embeds[0].Description, customId: "id-description", style: TextInputStyle.Paragraph));

                await ctx.CreateResponseAsync(InteractionResponseType.Modal, modal);
                var interactivity = ctx.Client.GetInteractivity();
                var response = await interactivity.WaitForModalAsync($"id-event-edit-{ctx.InteractionId}", user: ctx.User, timeoutOverride: TimeSpan.FromSeconds(1800));
                var embedBuilder = new DiscordEmbedBuilder(message.Embeds[0]);
                embedBuilder
                    .WithTitle(response.Result.Values["id-name"])
                    .Description = response.Result.Values["id-description"];
                var embed = embedBuilder.Build();
                await message.ModifyAsync(message.Content, embed: embed);

                await Helpers.GetThreadChannelByID(channel, id).ModifyAsync(x => x.Name = embed.Title);

                await response.Result.Interaction.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder().WithContent("Editing Event").AsEphemeral());
            }

            //TODO: Remove?
            [SlashCommand("close", "Closes all interactions with this event")]
            [SlashCommandPermissions(Permissions.Administrator)]
            public async Task Close(InteractionContext ctx, [Option("thread_id", "Id of thread")] string id)
            {
                DiscordMessage eventMessage;
                DiscordForumChannel channel = (DiscordForumChannel)ctx.Guild.GetChannel(Helpers.GetEventForumID(ctx.Guild.Id));
                try
                {
                    DiscordThreadChannel threadChannel = Helpers.GetThreadChannelByID(channel, id);
                    var pinnedMessages = await threadChannel.GetPinnedMessagesAsync();
                    eventMessage = pinnedMessages.First();
                }
                catch
                {
                    await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder().WithContent("Event doesn't exist. Check your Thread ID").AsEphemeral());
                    return;
                }

                if (File.Exists(Helpers.GetFileDirectoryWithContent(ctx, eventMessage.Id.ToString()) + "closed.txt"))
                {
                    await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder().AsEphemeral().WithContent("Event is already closed"));
                    return;
                }

                await ctx.DeferAsync(ephemeral: true);

                string directory = Helpers.GetFileDirectoryWithContent(ctx, eventMessage.Id.ToString());

                if (directory == "null")
                    await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent("Event doesn't exist"));
                else
                {
                    await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent("Closing Event"));
                    File.Create(directory + "closed.txt").Close();

                    var eventEmbed = new DiscordEmbedBuilder();
                    eventEmbed
                        .WithAuthor(ctx.Member.DisplayName, ctx.Member.AvatarUrl, ctx.Member.AvatarUrl)
                        .WithColor(DiscordColor.Orange)
                        .WithTitle("Event Closed")
                        .WithDescription(eventMessage.Embeds[0].Title)
                        .AddField("Link", $"{eventMessage.JumpLink}");

                    await ctx.Guild.GetChannel(Helpers.GetLogChannelID(ctx.Guild.Id)).SendMessageAsync(eventEmbed);


                    DiscordMessage message = eventMessage;

                    var embedBuilder = new DiscordEmbedBuilder(message.Embeds[0]);
                    embedBuilder
                        .WithTitle("[GESCHLOSSEN] " + message.Embeds[0].Title);
                    var embed = embedBuilder.Build();
                    await message.ModifyAsync(new DiscordMessageBuilder().WithContent(message.Content).WithEmbed(embed).AddComponents(message.Components));

                    string[] signon = await Helpers.GetNicknameByIdArray(File.ReadAllLines(directory + "//signup.txt"), ctx.Guild);
                    string[] signoff = await Helpers.GetNicknameByIdArray(File.ReadAllLines(directory + "//signoff.txt"), ctx.Guild);
                    string[] undecided = await Helpers.GetNicknameByIdArray(File.ReadAllLines(directory + "//undecided.txt"), ctx.Guild);

                    var noticeEmbed = new DiscordEmbedBuilder()
                                .WithTitle($"Anmeldungen bei Schließung:\n {eventMessage.Embeds[0].Title}")
                                .WithDescription($"{eventMessage.JumpLink}")
                                .AddField($"Angemeldet: {File.ReadAllLines(directory + "//signup.txt").Length}", $"------------------------------------------\n{String.Join("\n", signon)}\n------------------------------------------\n")
                                .AddField($"Abgemeldet: {File.ReadAllLines(directory + "//signoff.txt").Length}", $"------------------------------------------\n{String.Join("\n", signoff)}\n------------------------------------------\n")
                                .AddField($"Unentschieden: {File.ReadAllLines(directory + "//undecided.txt").Length}", $"------------------------------------------\n{String.Join("\n", undecided)}\n------------------------------------------\n");
                    await Helpers.GetThreadChannelByID(channel, id).SendMessageAsync(noticeEmbed);

                    await Helpers.GetThreadChannelByID(channel, id).ModifyAsync(x => { x.Name = "[GESCHLOSSEN] " + Helpers.GetThreadChannelByID(channel, id).Name; x.IsArchived = true; x.Locked = true; });
                }
            }

            //TODO: Remove?
            [SlashCommand("reopen", "Reopens an Event")]
            [SlashCommandPermissions(Permissions.Administrator)]
            public async Task ReOpen(InteractionContext ctx, [Option("thread_id", "Id of thread")] string id)
            {
                DiscordForumChannel channel = (DiscordForumChannel)ctx.Guild.GetChannel(Helpers.GetEventForumID(ctx.Guild.Id));
                DiscordMessage message;
                try
                {
                    DiscordThreadChannel threadChannel = Helpers.GetThreadChannelByID(channel, id);
                    var pinnedMessages = await threadChannel.GetPinnedMessagesAsync();
                    message = pinnedMessages.First();
                }
                catch
                {
                    await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder().WithContent("Event doesn't exist. Check your Thread ID").AsEphemeral());
                    return;
                }

                if (!File.Exists(Helpers.GetFileDirectoryWithContent(ctx, message.Id.ToString()) + "closed.txt"))
                {
                    await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder().AsEphemeral().WithContent("Event is already open"));
                    return;
                }


                await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder().WithContent("Opening Event").AsEphemeral());
                var directory = Helpers.GetFileDirectoryWithContent(ctx, message.Id.ToString());
                File.Delete(directory + "closed.txt");

                var embed = new DiscordEmbedBuilder();
                embed
                    .WithAuthor(ctx.Member.DisplayName, ctx.Member.AvatarUrl, ctx.Member.AvatarUrl)
                    .WithColor(DiscordColor.Orange)
                    .WithTitle("Event Opened")
                    .WithDescription(message.Embeds[0].Title)
                    .AddField("Link", $"{message.JumpLink}");

                await ctx.Guild.GetChannel(Helpers.GetLogChannelID(ctx.Guild.Id)).SendMessageAsync(embed);

                DiscordMessage mess = message;

                await Helpers.GetThreadChannelByID(channel, id).ModifyAsync(x => { x.IsArchived = false; x.Locked = false; });
                await Helpers.GetThreadChannelByID(channel, id).ModifyAsync(x => { x.Name = Helpers.GetThreadChannelByID(channel, id).Name.Replace("[GESCHLOSSEN] ", ""); });

                var embedBuilder = new DiscordEmbedBuilder();
                embedBuilder
                    .WithTitle(mess.Embeds[0].Title.Replace("[GESCHLOSSEN] ", ""));
                var emb = embedBuilder.Build();
                await mess.ModifyAsync(new DiscordMessageBuilder().WithContent(message.Content).WithEmbed(emb).AddComponents(message.Components));
            }
        }
        [SlashCommand("welcomemessage", "Set Welcome Message")]
        [SlashCommandPermissions(Permissions.Administrator)]
        public async Task SetWelcomeMessage(InteractionContext ctx)
        {
            var modal = new DiscordInteractionResponseBuilder()
                .WithTitle("Welcome Message")
                .WithCustomId($"id-welcomemessage-{ctx.InteractionId}")
                .AddComponents(new TextInputComponent(label: "Titel", customId: "id-titel", style: TextInputStyle.Short))
                .AddComponents(new TextInputComponent(label: "Message", customId: "id-message", style: TextInputStyle.Paragraph))
                .AddComponents(new TextInputComponent(label: "Rules Titel", customId: "id-rulestitel", value: "Vergiss nicht unsere Regeln zu beachten", style: TextInputStyle.Short))
                .AddComponents(new TextInputComponent(label: "Rules Channel Id", customId: "id-ruleschannelId", style: TextInputStyle.Short));


            await ctx.CreateResponseAsync(InteractionResponseType.Modal, modal);
            var interactivity = ctx.Client.GetInteractivity();
            var response = await interactivity.WaitForModalAsync($"id-welcomemessage-{ctx.InteractionId}", user: ctx.User, timeoutOverride: TimeSpan.FromSeconds(1800));

            var embedBuilder = new DiscordEmbedBuilder()
                .WithTitle($"{response.Result.Values["id-titel"]}")
                .WithDescription($"{response.Result.Values["id-message"]}")
                .AddField($"{response.Result.Values["id-rulestitel"]}", $"{ctx.Guild.GetChannel(Convert.ToUInt64(response.Result.Values["id-ruleschannelId"])).Mention}");

            Directory.CreateDirectory(Directory.GetCurrentDirectory() + "//" + ctx.Guild.Id + "//WelcomeMessage");


            File.WriteAllText(Path.Combine(Directory.GetCurrentDirectory() + "//" + ctx.Guild.Id + "//WelcomeMessage", "welcome_message.txt"), response.Result.Values["id-message"]);
            File.WriteAllText(Path.Combine(Directory.GetCurrentDirectory() + "//" + ctx.Guild.Id + "//WelcomeMessage", "welcome_message_titel.txt"), response.Result.Values["id-titel"]);
            File.WriteAllText(Path.Combine(Directory.GetCurrentDirectory() + "//" + ctx.Guild.Id + "//WelcomeMessage", "welcome_message_rules_id.txt"), response.Result.Values["id-ruleschannelId"]);
            File.WriteAllText(Path.Combine(Directory.GetCurrentDirectory() + "//" + ctx.Guild.Id + "//WelcomeMessage", "welcome_message_rules_titel.txt"), response.Result.Values["id-rulestitel"]);

            await response.Result.Interaction.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder().WithContent("Set new welcome message").AddEmbed(embedBuilder).AsEphemeral());
        }

        [SlashCommandGroup("setchannel", "Set channel")]
        [SlashCommandPermissions(Permissions.Administrator)]
        public class SetChannel
        {
            [SlashCommand("botchannel", "Set this channel to the Guild Bot channel")]
            public async Task SetBotChannel(InteractionContext ctx)
            {
                Directory.CreateDirectory(Directory.GetCurrentDirectory() + "//" + ctx.Guild.Id);
                StreamWriter writer = new StreamWriter(Path.Combine(Directory.GetCurrentDirectory() + "//" + ctx.Guild.Id, "bot_channel.txt"));
                writer.WriteLine(ctx.Channel.Id);
                writer.Dispose();
                await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder().WithContent("Succesfully set " + ctx.Channel.Mention + " as the bot channel").AsEphemeral());
            }

            [SlashCommand("logchannel", "Set this channel to the Guild Log channel")]
            public async Task SetBotLogChannel(InteractionContext ctx)
            {
                Directory.CreateDirectory(Directory.GetCurrentDirectory() + "//" + ctx.Guild.Id);
                StreamWriter writer = new StreamWriter(Path.Combine(Directory.GetCurrentDirectory() + "//" + ctx.Guild.Id, "log_channel.txt"));
                writer.WriteLine(ctx.Channel.Id);
                writer.Dispose();
                await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder().WithContent("Succesfully set " + ctx.Channel.Mention + " as the log channel").AsEphemeral());
            }

            [SlashCommand("eventforum", "Set this channel to the Guild Event forum")]
            public async Task SetEventForum(InteractionContext ctx, [Option("id", "Id of the forum")] string id)
            {
                if (!(ctx.Guild.GetChannel(Convert.ToUInt64(id)) is DiscordForumChannel))
                {
                    await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder().WithContent("Error! Channel must be a forum").AsEphemeral());
                    return;
                }

                Directory.CreateDirectory(Directory.GetCurrentDirectory() + "//" + ctx.Guild.Id);
                StreamWriter writer = new StreamWriter(Path.Combine(Directory.GetCurrentDirectory() + "//" + ctx.Guild.Id, "event_channel.txt"));
                writer.WriteLine(id);
                writer.Dispose();
                await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder().WithContent("Succesfully set " + ctx.Guild.GetChannel(Convert.ToUInt64(id)).Mention + " as the event forum").AsEphemeral());

            }

            [SlashCommand("meetingpoint", "Set this channel to the Guild meeting point channel")]
            public async Task SetMeetingPoint(InteractionContext ctx, [Option("id", "Id of the voice channel")] string id)
            {
                if (ctx.Guild.GetChannel(Convert.ToUInt64(id)).Type != ChannelType.Voice)
                {
                    await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder().WithContent("Error: Channel must be a voice channel").AsEphemeral());
                    return;
                }

                Directory.CreateDirectory(Directory.GetCurrentDirectory() + "//" + ctx.Guild.Id);
                StreamWriter writer = new StreamWriter(Path.Combine(Directory.GetCurrentDirectory() + "//" + ctx.Guild.Id, "meeting_channel.txt"));
                writer.WriteLine(id);
                writer.Dispose();
                await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder().WithContent("Succesfully set " + ctx.Guild.GetChannel(Convert.ToUInt64(id)).Mention + " as the meeting channel").AsEphemeral());
            }
        }

        [SlashCommandGroup("setrole", "Set role")]
        [SlashCommandPermissions(Permissions.Administrator)]
        public class SetRole
        {

            [SlashCommand("member", "Set this role")]
            public async Task SetMemberRole(InteractionContext ctx, [Option("role", "role")] DiscordRole role)
            {

                Directory.CreateDirectory(Directory.GetCurrentDirectory() + "//" + ctx.Guild.Id);
                StreamWriter writer = new StreamWriter(Path.Combine(Directory.GetCurrentDirectory() + "//" + ctx.Guild.Id, "member_role.txt"));
                writer.WriteLine(role.Id);
                writer.Dispose();

                await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder().WithContent("Succesfully set " + role.Mention + " as the member role").AsEphemeral());
            }

            [SlashCommand("dividerRoles", "Set divider roles to be automatically added on join")]
            public async Task SetDividerRoles(InteractionContext ctx, [Option("role_1", "role")] DiscordRole role_1, [Option("role_2", "role")] DiscordRole role_2, [Option("role_3", "role")] DiscordRole role_3)
            {

                Directory.CreateDirectory(Directory.GetCurrentDirectory() + "//" + ctx.Guild.Id);
                StreamWriter writer = new StreamWriter(Path.Combine(Directory.GetCurrentDirectory() + "//" + ctx.Guild.Id, "divider_roles.txt"));
                writer.WriteLine(role_1.Id);
                writer.WriteLine(role_2.Id);
                writer.WriteLine(role_3.Id);
                writer.Dispose();

                await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder().WithContent("Succesfully set divider roles").AsEphemeral());
            }
        }

        [SlashCommandGroup("reactions", "Manage reactions to... reactions")]
        [SlashCommandPermissions(Permissions.Administrator)]
        public class Reactions
        {

            [SlashCommand("addRoleToThread", "Which role to grant for a reaction here, removes existing one")]
            public async Task AddRoleToThread(InteractionContext ctx, [Option("role", "Role to add, removes existing role")] DiscordRole role)
            {
                if (ctx.Channel.IsThread)
                {

                    string reactionPath = Directory.GetCurrentDirectory() + $"//{ctx.Guild.Id}//ReactionManagement";
                    Directory.CreateDirectory(reactionPath);

                    File.WriteAllText(reactionPath + $"//{ctx.Channel.Id}.txt", role.Id.ToString());

                    await ctx.CreateResponseAsync(new DiscordInteractionResponseBuilder().WithContent($"Rolle {role.Mention} zu {ctx.Channel.Mention} hinzugefügt ").AsEphemeral());
                }
                else
                {
                    await ctx.CreateResponseAsync(new DiscordInteractionResponseBuilder().WithContent($"Please execute in a thread channel...").AsEphemeral());
                }

            }

            [SlashCommand("removeRoleFromThread", "Remove a role from the thread")]
            public async Task RemoveRoleFromThread(InteractionContext ctx, [Option("role", "Role to remove")] DiscordRole role)
            {
                if (ctx.Channel.IsThread)
                {
                    string reactionPath = Directory.GetCurrentDirectory() + $"//{ctx.Guild.Id}//ReactionManagement";
                    Directory.CreateDirectory(reactionPath);

                    File.Delete(reactionPath + $"//{ctx.Channel.Id}.txt");

                    await ctx.CreateResponseAsync(new DiscordInteractionResponseBuilder().WithContent($"Rolle {role.Mention} von {ctx.Channel.Mention} entfernt ").AsEphemeral());
                }
                else
                {
                    await ctx.CreateResponseAsync(new DiscordInteractionResponseBuilder().WithContent($"Please execute in a thread channel...").AsEphemeral());
                }

            }

            [SlashCommand("resetReactions", "Clears all checkmark reactions and removes role")]
            public async Task ResetReactions(InteractionContext ctx)
            {
                if (ctx.Channel.IsThread)
                {
                    string reactionPath = Directory.GetCurrentDirectory() + $"//{ctx.Guild.Id}//ReactionManagement";

                    DiscordMessage originalMessage = await ctx.Channel.GetMessageAsync(ctx.Channel.Id);

                    var reactedMembers = await originalMessage.GetReactionsAsync(DiscordEmoji.FromUnicode("✅"));

                    var role = ctx.Guild.GetRole(Convert.ToUInt64(File.ReadAllText(reactionPath + $"//{ctx.Channel.Id}.txt")));

                    foreach (var user in reactedMembers)
                        await (await ctx.Guild.GetMemberAsync(user.Id)).RevokeRoleAsync(role);

                    File.Delete(reactionPath + $"//{ctx.Channel.Id}.txt");

                    await originalMessage.DeleteReactionsEmojiAsync(DiscordEmoji.FromUnicode("✅"));

                    await ctx.CreateResponseAsync(new DiscordInteractionResponseBuilder().WithContent($"Zugewiesene Rollen sowie Reaktionen zurückgesetzt").AsEphemeral());
                }
                else
                {
                    await ctx.CreateResponseAsync(new DiscordInteractionResponseBuilder().WithContent($"Please execute in a thread channel...").AsEphemeral());
                }
            }
        }


        [SlashCommand("rolemessage", "Create a role message")]
        [SlashCommandPermissions(Permissions.Administrator)]
        public async Task WelcomeButtonPrompt(InteractionContext ctx,
                                  [Option("message", "message")] string message,
                                  [Option("button1text", "text for this button")] string button1text,
                                  [Option("button1role", "role for this button")] DiscordRole button1role,
                                  [Option("button2text", "text for this button")] string button2text,
                                  [Option("button2role", "role for this button")] DiscordRole button2role,
                                  [Option("button3text", "text for this button")] string button3text,
                                  [Option("button3role", "role for this button")] DiscordRole button3role)
        {
            await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder().WithContent("Creating Message").AsEphemeral());
            var embed = new DiscordEmbedBuilder().WithDescription(message);

            var button1 = new DiscordButtonComponent(
                        ButtonStyle.Success,
                        button1role.Id + "_rolebutton",
                        button1text
                        );

            var button2 = new DiscordButtonComponent(
                        ButtonStyle.Success,
                        button2role.Id + "_rolebutton",
                        button2text
                        );

            var button3 = new DiscordButtonComponent(
                        ButtonStyle.Success,
                        button3role.Id + "_rolebutton",
                        button3text
                        );

            var buttons = new DiscordComponent[]
            {
            button1,
            button2,
            button3
            };
            try
            {
                await ctx.Channel.SendMessageAsync(new DiscordMessageBuilder().WithEmbed(embed: embed).AddComponents(buttons));
            }
            catch (BadRequestException exception)
            {
                ErrorHandler.HandleError(exception, ctx.Guild, ErrorHandler.ErrorType.Error);
            }


        }
    }
}


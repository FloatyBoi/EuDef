using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using DSharpPlus.Exceptions;
using DSharpPlus.Interactivity;
using DSharpPlus.Interactivity.Extensions;
using DSharpPlus.SlashCommands;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace EuDef
{
    public static class Helpers
    {
        /*
        =====================================================================================================================================================================================
         Generic getting
        =====================================================================================================================================================================================
        */

        public static string[] GetNicknameByIdArray(string[] iDs, DiscordGuild guild)
        {
            string[] nicknames = new string[iDs.Length];

            for (int i = 0; i < iDs.Length; i++)
            {
                try
                {
                    nicknames[i] = guild.GetMemberAsync(Convert.ToUInt64(iDs[i])).Result.DisplayName;
                }
                catch (ServerErrorException e)
                {
                    Console.WriteLine(e.Message);
                    nicknames[i] = "?!NotFound!?";
                }
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
                                Console.WriteLine(member.DisplayName + " has Direct Messages turned off :(");

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

        public static async void NotifyRoleFromEvent(DiscordMessage messageInformation, DiscordRole role, string message, string? eventName, DiscordMessage? Message, DiscordScheduledGuildEvent? discordEvent)
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
                            try
                            {
                                await member.SendMessageAsync(embed: notifyEmbed);
                            }
                            catch (UnauthorizedException badBoy)
                            {
                                Console.WriteLine(member.DisplayName + " has Direct Messages turned off :(");

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

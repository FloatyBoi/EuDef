using DSharpPlus.Entities;
using DSharpPlus.Exceptions;
using DSharpPlus;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using System.IO;
using Microsoft.VisualBasic;
using DSharpPlus.EventArgs;

namespace EuDef
{
    static class TimerManager
    {
        public static void StartTimers(DiscordClient discordClient)
        {
            var thirtySecondTimer = new System.Timers.Timer(30000);

            try
            {
                thirtySecondTimer.Elapsed += (sender, e) => ThirtySecondTask(sender, e, discordClient);
                thirtySecondTimer.AutoReset = true;
                thirtySecondTimer.Enabled = true;
            }
            catch (Exception ex)
            {
                ErrorHandler.HandleError(ex, null, ErrorHandler.ErrorType.Error);
            }
        }

        //This gets executed every thirty seconds
        private static async void ThirtySecondTask(Object source, ElapsedEventArgs e, DiscordClient client)
        {
            string[] collectionFilePaths = Directory.GetFiles(Directory.GetCurrentDirectory(), "startTimeForCollection.txt", SearchOption.AllDirectories);
            string[] undecidedReminderFilePaths = Directory.GetFiles(Directory.GetCurrentDirectory(), "remindUndecided.txt", SearchOption.AllDirectories);

            string[] voteEndFilePaths = Directory.GetFiles(Directory.GetCurrentDirectory(), "endTimeForVote.txt", SearchOption.AllDirectories);

            //Found a file for collection
            if (collectionFilePaths.Length > 0)
            {
                foreach (string path in collectionFilePaths)
                {
                    //Guild getting (Hacky but should work)

                    string parentDirectory = path.Replace("startTimeForCollection.txt", "");
                    parentDirectory = parentDirectory.Replace("\\", "/");
                    string guildIdPath = parentDirectory.Remove(parentDirectory.IndexOf(@"/Events"));
                    ulong guildId = Convert.ToUInt64(guildIdPath.Substring(guildIdPath.LastIndexOf(@"/") + 1));

                    DateTime dateTime = DateTime.ParseExact(File.ReadAllText(path), "dd.MM.yyyy,HH:mm", CultureInfo.InvariantCulture);
                    DateTime currentTime = DateTime.UtcNow;

                    //Event data collection & sending to bot channel
                    if (dateTime.AddHours(-2) <= currentTime)
                    {

                        //Console.WriteLine("\nCollecting Data for event...");
                        //Console.WriteLine($"DateTime: {dateTime}\nCurrent Time: {currentTime}");

                        //Console.WriteLine(guildIdPath);

                        //Console.WriteLine("Guild ID: " + guildId + "\n");

                        string[] signon = await Helpers.GetNicknameByIdArray(File.ReadAllLines(parentDirectory + "//signup.txt"), await client.GetGuildAsync(guildId));
                        string[] signoff = await Helpers.GetNicknameByIdArray(File.ReadAllLines(parentDirectory + "//signoff.txt"), await client.GetGuildAsync(guildId));
                        string[] undecided = await Helpers.GetNicknameByIdArray(File.ReadAllLines(parentDirectory + "//undecided.txt"), await client.GetGuildAsync(guildId));

                        try
                        {
                            var guild = await client.GetGuildAsync(guildId);
                            DiscordThreadChannel channel = Helpers.GetThreadChannelByID((DiscordForumChannel)guild.GetChannel(Helpers.GetEventForumID(guildId)), File.ReadAllText(parentDirectory + "forumPostID.txt"));
                            var pinnedMessaged = await channel.GetPinnedMessagesAsync();

                            var embed = new DiscordEmbedBuilder()
                                .WithTitle($"Bisherige Anmeldungen für:\n {pinnedMessaged.First().Embeds[0].Title}")
                                .WithDescription($"{pinnedMessaged.First().JumpLink}")
                                .AddField($"Angemeldet: {File.ReadAllLines(parentDirectory + "//signup.txt").Length}", $"------------------------------------------\n{String.Join("\n", signon)}\n------------------------------------------\n")
                                .AddField($"Abgemeldet: {File.ReadAllLines(parentDirectory + "//signoff.txt").Length}", $"------------------------------------------\n{String.Join("\n", signoff)}\n------------------------------------------\n")
                                .AddField($"Unentschieden: {File.ReadAllLines(parentDirectory + "//undecided.txt").Length}", $"------------------------------------------\n{String.Join("\n", undecided)}\n------------------------------------------\n");


                            await guild.GetChannel(Helpers.GetBotChannelID(guildId)).SendMessageAsync(embed);
                        }
                        catch (Exception ex) { ErrorHandler.HandleError(ex, await client.GetGuildAsync(guildId), ErrorHandler.ErrorType.Warning); }
                        File.Delete(path);



                    }
                }
            }

            if (undecidedReminderFilePaths.Length > 0)
            {
                //Event undecided reminder
                foreach (string path in undecidedReminderFilePaths)
                {
                    string parentDirectory = path.Replace("remindUndecided.txt", "");
                    parentDirectory = parentDirectory.Replace("\\", "/");
                    string guildIdPath = parentDirectory.Remove(parentDirectory.IndexOf(@"/Events"));
                    ulong guildId = Convert.ToUInt64(guildIdPath.Substring(guildIdPath.LastIndexOf(@"/") + 1));

                    var memberId = Helpers.GetMemberRoleID(guildId);
                    try
                    {
                        DateTime dateTime = DateTime.ParseExact(File.ReadAllText(path), "dd.MM.yyyy,HH:mm", CultureInfo.InvariantCulture);

                        if (dateTime.Day <= DateTime.Today.Day && dateTime.Month == DateTime.Today.Month && dateTime.Year == DateTime.Today.Year)
                        {
                            var guild = await client.GetGuildAsync(guildId);

                            DiscordThreadChannel channel = Helpers.GetThreadChannelByID((DiscordForumChannel)guild.GetChannel(Helpers.GetEventForumID(guildId)), File.ReadAllText(parentDirectory + "forumPostID.txt"));
                            var pinnedMessages = await channel.GetPinnedMessagesAsync();
                            var message = pinnedMessages.First();

                            var embed = new DiscordEmbedBuilder()
                                .WithTitle("Erinnerung: " + message.Embeds[0].Title)
                                .WithDescription($"Noch unentschieden\n{message.JumpLink}");

                            string[] signupIds = File.ReadAllLines(Directory.GetParent(path) + "//signup.txt");
                            string[] signoffIds = File.ReadAllLines(Directory.GetParent(path) + "//signoff.txt");
                            var members = guild.Members;


                            foreach (var member in members)
                            {
                                if (member.Value.Roles.Contains(guild.GetRole(memberId)))
                                {
                                    if (!signupIds.Contains(member.Key.ToString()) && !signoffIds.Contains(member.Key.ToString()))
                                    {
                                        try
                                        {
                                            if (!member.Value.IsBot)
                                                await member.Value.SendMessageAsync(embed);
                                        }
                                        catch (UnauthorizedException badBoy)
                                        {
                                            //Console.WriteLine(member.Value.DisplayName + " has Direct Messages turned off :(");

                                            var embedBad = new DiscordEmbedBuilder()
                                                .WithColor(DiscordColor.Gray)
                                                .WithTitle("Couldn't send direct message (Turned off / blocked)")
                                                .WithDescription(member.Value.Mention)
                                                .AddField("Error message", badBoy.Message);

                                            await guild.GetChannel(Helpers.GetLogChannelID(guildId)).SendMessageAsync(embedBad);
                                        }
                                    }
                                }


                            }



                            File.Delete(path);
                        }
                    }
                    catch (Exception ex)
                    {
                        ErrorHandler.HandleError(ex, await client.GetGuildAsync(guildId), ErrorHandler.ErrorType.Error);
                    }
                }
            }

            if (voteEndFilePaths.Length > 0)
            {
                foreach (string path in voteEndFilePaths)
                {
                    string parentDirectory = path.Replace("endTimeForVote.txt", "");
                    parentDirectory = parentDirectory.Replace("\\", "/");
                    string guildIdPath = parentDirectory.Remove(parentDirectory.IndexOf(@"/EventCreationCache"));
                    ulong guildId = Convert.ToUInt64(guildIdPath.Substring(guildIdPath.LastIndexOf(@"/") + 1));

                    DateTime dateTime = DateTime.ParseExact(File.ReadAllText(path), "dd.MM.yyyy,HH:mm", CultureInfo.InvariantCulture);
                    DateTime currentTime = DateTime.UtcNow;


                    if (dateTime <= DateTime.Now)
                    {

                        Console.WriteLine("\nEnding Vote...");
                        Console.WriteLine($"DateTime: {dateTime}\nCurrent Time: {currentTime}");

                        Console.WriteLine(guildIdPath);

                        Console.WriteLine("Guild ID: " + guildId + "\n");

                        string[] optionOne = File.ReadAllLines(parentDirectory + "//optionOne.txt");
                        string[] optionTwo = File.ReadAllLines(parentDirectory + "//optionTwo.txt");

                        string dateTimeText;

                        var guild = await client.GetGuildAsync(guildId);

                        var doVoteContent = File.ReadAllText(parentDirectory + "//doVote.txt");

                        Directory.CreateDirectory(Directory.GetCurrentDirectory() + "//" + guild.Id + "//Events" + $"//{doVoteContent}");
                        //First Option won, or same amount of votes
                        if (optionOne.Length >= optionTwo.Length)
                        {
                            File.WriteAllLines(Directory.GetCurrentDirectory() + "//" + guild.Id + "//Events" + $"//{doVoteContent}" + "//signup.txt", optionOne);
                            dateTimeText = File.ReadAllText(parentDirectory + "//optionOneTime.txt");
                        }
                        //Second Option won
                        else
                        {
                            File.WriteAllLines(Directory.GetCurrentDirectory() + "//" + guild.Id + "//Events" + $"//{doVoteContent}" + "//signup.txt", optionTwo);
                            dateTimeText = File.ReadAllText(parentDirectory + "//optionTwoTime.txt");
                        }

                        DiscordForumChannel forumChannel = (DiscordForumChannel)guild.GetChannel(Helpers.GetEventForumID(guild.Id));
                        DiscordThreadChannel threadChannel = Helpers.GetThreadChannelByID(forumChannel, File.ReadAllText(parentDirectory + "//forumPostId.txt"));
                        await threadChannel.DeleteAsync();

                        File.Delete(parentDirectory + "//doVote.txt");
                        File.Delete(parentDirectory + "//endTimeForVote.txt");

                        var messageId = File.ReadAllText(Directory.GetCurrentDirectory() + $"//{guild.Id}//EventCreationCache//{doVoteContent}//messageId.txt");
                        var message = await guild.GetChannel(Helpers.GetBotChannelID(guild.Id)).GetMessageAsync(Convert.ToUInt64(messageId));

                        var dateTimeThreeHoursLater = DateTime.ParseExact(dateTimeText, "dd.MM.yyyy,HH:mm", CultureInfo.InvariantCulture).AddHours(3);

                        var embed = message.Embeds[0];
                        embed.Fields[0].Value = "Anfang: " + dateTimeText +
                            "\nEnde: " + dateTimeThreeHoursLater.ToString("dd.MM.yyyy,HH:mm");

                        await message.ModifyAsync(embed);

                        File.Delete(parentDirectory + "//optionOne.txt");
                        File.Delete(parentDirectory + "//optionTwo.txt");
                        File.Delete(parentDirectory + "//optionOneTime.txt");
                        File.Delete(parentDirectory + "//optionTwoTime.txt");
                        File.Delete(parentDirectory + "//forumPostId.txt");

                        try
                        {
                            EventFunctions.HandleEventCreationUpdate("createEvent", doVoteContent, guild, null);
                        }
                        catch (Exception ex)
                        {
                            ErrorHandler.HandleError(ex, guild, ErrorHandler.ErrorType.Error);
                        }
                    }
                }
            }
        }
    }
}

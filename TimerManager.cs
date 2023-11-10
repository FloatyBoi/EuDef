﻿using DSharpPlus.Entities;
using DSharpPlus.Exceptions;
using DSharpPlus;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;

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



            //Found a file
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
                    if (dateTime.Day <= DateTime.Today.Day && dateTime.Month == DateTime.Today.Month && dateTime.Year == DateTime.Today.Year)
                    {

                        Console.WriteLine("\nCollecting Data for event...");
                        Console.WriteLine($"DateTime: {dateTime}\nCurrent Time: {currentTime}");

                        Console.WriteLine(guildIdPath);

                        Console.WriteLine("Guild ID: " + guildId + "\n");

                        string[] signon = Helpers.GetNicknameByIdArray(File.ReadAllLines(parentDirectory + "//signup.txt"), client.GetGuildAsync(guildId).Result);
                        string[] signoff = Helpers.GetNicknameByIdArray(File.ReadAllLines(parentDirectory + "//signoff.txt"), client.GetGuildAsync(guildId).Result);
                        string[] undecided = Helpers.GetNicknameByIdArray(File.ReadAllLines(parentDirectory + "//undecided.txt"), client.GetGuildAsync(guildId).Result);


                        try
                        {
                            DiscordThreadChannel channel = Helpers.GetThreadChannelByID((DiscordForumChannel)client.GetGuildAsync(guildId).Result.GetChannel(Helpers.GetEventForumID(guildId)), File.ReadAllText(parentDirectory + "forumPostID.txt"));

                            var embed = new DiscordEmbedBuilder()
                                .WithTitle($"Bisherige Anmeldungen für:\n {channel.GetPinnedMessagesAsync().Result.First().Embeds[0].Title}")
                                .WithDescription($"{channel.GetPinnedMessagesAsync().Result.First().JumpLink}")
                                .AddField($"Angemeldet: {File.ReadAllLines(parentDirectory + "//signup.txt").Length}", $"------------------------------------------\n{String.Join("\n", signon)}\n------------------------------------------\n")
                                .AddField($"Abgemeldet: {File.ReadAllLines(parentDirectory + "//signoff.txt").Length}", $"------------------------------------------\n{String.Join("\n", signoff)}\n------------------------------------------\n")
                                .AddField($"Unentschieden: {File.ReadAllLines(parentDirectory + "//undecided.txt").Length}", $"------------------------------------------\n{String.Join("\n", undecided)}\n------------------------------------------\n");

                            await client.GetGuildAsync(guildId).Result.GetChannel(Helpers.GetBotChannelID(guildId)).SendMessageAsync(embed);
                        }
                        catch (Exception ex) { ErrorHandler.HandleError(ex, client.GetGuildAsync(guildId).Result, ErrorHandler.ErrorType.Warning); }
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

                            DiscordThreadChannel channel = Helpers.GetThreadChannelByID((DiscordForumChannel)client.GetGuildAsync(guildId).Result.GetChannel(Helpers.GetEventForumID(guildId)), File.ReadAllText(parentDirectory + "forumPostID.txt"));
                            var message = channel.GetPinnedMessagesAsync().Result.First();


                            var embed = new DiscordEmbedBuilder()
                                .WithTitle("Erinnerung: " + message.Embeds[0].Title)
                                .WithDescription($"Noch unentschieden\n{message.JumpLink}");

                            string[] signupIds = File.ReadAllLines(Directory.GetParent(path) + "//signup.txt");
                            string[] signoffIds = File.ReadAllLines(Directory.GetParent(path) + "//signoff.txt");
                            var members = client.GetGuildAsync(guildId).Result.Members;


                            foreach (var member in members)
                            {
                                if (member.Value.Roles.Contains(client.GetGuildAsync(guildId).Result.GetRole(memberId)))
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
                                            Console.WriteLine(member.Value.DisplayName + " has Direct Messages turned off :(");

                                            var embedBad = new DiscordEmbedBuilder()
                                                .WithColor(DiscordColor.Gray)
                                                .WithTitle("Couldn't send direct message (Turned off / blocked)")
                                                .WithDescription(member.Value.Mention)
                                                .AddField("Error message", badBoy.Message);

                                            await client.GetGuildAsync(guildId).Result.GetChannel(Helpers.GetLogChannelID(guildId)).SendMessageAsync(embedBad);
                                        }
                                    }
                                }


                            }



                            File.Delete(path);
                        }
                    }
                    catch (Exception ex)
                    {
                        ErrorHandler.HandleError(ex, client.GetGuildAsync(guildId).Result, ErrorHandler.ErrorType.Error);
                    }
                }
            }



        }
    }
}

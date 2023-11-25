using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EuDef
{
    public static class VoteHandler
    {
        public static async Task DoVote(DateTime timeOptionOne, DateTime timeOptionTwo, DateTime endVoteTime, string interactionId, DiscordInteraction interaction)
        {
            await interaction.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder().AsEphemeral().WithContent("Abstimmung gestartet"));

            var guild = interaction.Guild;

            var embed = new DiscordEmbedBuilder()
                .WithTitle("Abstimmung")
                .WithDescription($"Wann soll das nächste Event stattfinden?\nEndet in: {Formatter.Timestamp(endVoteTime.AddHours(-1), TimestampFormat.RelativeTime)}")
                .AddField("Erste Option", $"{Formatter.Timestamp(timeOptionOne.AddHours(-1), TimestampFormat.LongDateTime)}")
                .AddField("Zweite Option", $"{Formatter.Timestamp(timeOptionTwo.AddHours(-1), TimestampFormat.LongDateTime)}");

            var optionOne = new DiscordButtonComponent(
                ButtonStyle.Primary,
                interactionId + "_optionOne",
                "Erste Option");

            var optionTwo = new DiscordButtonComponent(
                ButtonStyle.Primary,
                interactionId + "_optionTwo",
                "Zweite Option");

            var optionBoth = new DiscordButtonComponent(
                ButtonStyle.Primary,
                interactionId + "_optionBoth",
                "Beides Passt");

            var status = new DiscordButtonComponent(
                ButtonStyle.Secondary,
                interactionId + "_voteStatus",
                "Status");

            var buttons = new DiscordComponent[]
            {
                optionOne, optionTwo, optionBoth, status
            };

            try
            {
                DiscordForumChannel forumChannel = (DiscordForumChannel)guild.GetChannel(Helpers.GetEventForumID(guild.Id));
                DiscordForumPostStarter forumPost = await forumChannel.CreateForumPostAsync(new ForumPostBuilder().WithName($"Abstimmung für nächstes Event").WithMessage(new DiscordMessageBuilder().WithContent(guild.GetRole(Helpers.GetMemberRoleID(guild.Id)).Mention).WithAllowedMentions(new IMention[] { new RoleMention(guild.GetRole(Helpers.GetMemberRoleID(guild.Id))) }).WithEmbed(embed: embed).AddComponents(buttons)).WithAutoArchiveDuration(AutoArchiveDuration.Week));

                await File.WriteAllTextAsync(Directory.GetCurrentDirectory() + "//" + guild.Id + "//EventCreationCache" + $"//{interactionId}" + "//forumPostId.txt", forumPost.Channel.Id.ToString());


                //dd.MM.yyyy,HH:mm ,TODO: Last character randomly goes missing, fuck my life NOTE: ASYNC DONT DO SHIT
                await File.WriteAllTextAsync(Directory.GetCurrentDirectory() + "//" + guild.Id + "//EventCreationCache" + $"//{interactionId}" + "//endTimeForVote.txt", endVoteTime.ToString("dd.MM.yyyy,HH:mm"));
                await File.WriteAllTextAsync(Directory.GetCurrentDirectory() + "//" + guild.Id + "//EventCreationCache" + $"//{interactionId}" + "//optionOneTime.txt", timeOptionOne.ToString("dd.MM.yyyy,HH:mm"));
                await File.WriteAllTextAsync(Directory.GetCurrentDirectory() + "//" + guild.Id + "//EventCreationCache" + $"//{interactionId}" + "//optionTwoTime.txt", timeOptionTwo.ToString("dd.MM.yyyy,HH:mm"));

                File.Create(Directory.GetCurrentDirectory() + "//" + guild.Id + "//EventCreationCache" + $"//{interactionId}" + "//optionOne.txt").Close();
                File.Create(Directory.GetCurrentDirectory() + "//" + guild.Id + "//EventCreationCache" + $"//{interactionId}" + "//optionTwo.txt").Close();
            }
            catch (Exception ex)
            {
                ErrorHandler.HandleError(ex, guild, ErrorHandler.ErrorType.Error);
            }
        }

        public static async Task UpdateVote(string buttonType, DiscordGuild guild, string Id, ComponentInteractionCreateEventArgs e)
        {

            string optionOnePath = Directory.GetCurrentDirectory() + "//" + guild.Id + "//EventCreationCache" + $"//{Id}" + $"//optionOne.txt";
            string optionTwoPath = Directory.GetCurrentDirectory() + "//" + guild.Id + "//EventCreationCache" + $"//{Id}" + $"//optionTwo.txt";

            if (buttonType == "voteStatus")
            {
                try
                {
                    await e.Interaction.DeferAsync(ephemeral: true);

                    string[] optionOne = await Helpers.GetNicknameByIdArray(File.ReadAllLines(optionOnePath), e.Guild);
                    string[] optionTwo = await Helpers.GetNicknameByIdArray(File.ReadAllLines(optionTwoPath), e.Guild);



                    var embed = new DiscordEmbedBuilder()
                        .WithTitle("Status")
                        .AddField($"Option Eins: {File.ReadAllLines(optionOnePath).Length}", $"------------------------------------------\n{String.Join("\n", optionOne)}\n------------------------------------------\n")
                        .AddField($"Option Zwei: {File.ReadAllLines(optionTwoPath).Length}", $"------------------------------------------\n{String.Join("\n", optionTwo)}\n------------------------------------------\n");

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

            if (buttonType == "optionOne")
            {
                await e.Interaction.DeferAsync(ephemeral: true);

                HandleVoteUpdate(e, Id, "optionOne.txt");
                await e.Interaction.EditOriginalResponseAsync(new DiscordWebhookBuilder().WithContent("Erfolgreich für 'Option Eins' Abgestimmt"));

            }
            else if (buttonType == "optionTwo")
            {
                await e.Interaction.DeferAsync(ephemeral: true);

                HandleVoteUpdate(e, Id, "optionTwo.txt");
                await e.Interaction.EditOriginalResponseAsync(new DiscordWebhookBuilder().WithContent("Erfolgreich für 'Option Zwei' Abgestimmt"));

            }
            else if (buttonType == "optionBoth")
            {
                await e.Interaction.DeferAsync(ephemeral: true);

                HandleVoteUpdate(e, Id, "optionBoth.txt");
                await e.Interaction.EditOriginalResponseAsync(new DiscordWebhookBuilder().WithContent("Erfolgreich für beides Abgestimmt"));

            }
        }

        private static void HandleVoteUpdate(ComponentInteractionCreateEventArgs e, string interactionId, string fileName)
        {
            string optionOnePath = Directory.GetCurrentDirectory() + "//" + e.Guild.Id + "//EventCreationCache" + $"//{interactionId}" + $"//optionOne.txt";
            string optionTwoPath = Directory.GetCurrentDirectory() + "//" + e.Guild.Id + "//EventCreationCache" + $"//{interactionId}" + $"//optionTwo.txt";

            DiscordMember member = (DiscordMember)e.User;

            if (fileName == "optionOne.txt")
            {
                if (File.ReadAllLines(optionTwoPath).Contains(member.Id.ToString()))
                {
                    var file = File.ReadAllLines(optionTwoPath).ToList();
                    file.Remove(member.Id.ToString());
                    File.Delete(optionTwoPath);
                    File.WriteAllLines(optionTwoPath, file);
                }

                if (!File.ReadAllLines(optionOnePath).Contains(member.Id.ToString()))
                {
                    var file = File.ReadAllLines(optionOnePath).ToList();
                    file.Add(member.Id.ToString());
                    File.Delete(optionOnePath);
                    File.WriteAllLines(optionOnePath, file);
                }
            }

            if (fileName == "optionTwo.txt")
            {
                if (File.ReadAllLines(optionOnePath).Contains(member.Id.ToString()))
                {
                    var file = File.ReadAllLines(optionOnePath).ToList();
                    file.Remove(member.Id.ToString());
                    File.Delete(optionOnePath);
                    File.WriteAllLines(optionOnePath, file);
                }

                if (!File.ReadAllLines(optionTwoPath).Contains(member.Id.ToString()))
                {
                    var file = File.ReadAllLines(optionTwoPath).ToList();
                    file.Add(member.Id.ToString());
                    File.Delete(optionTwoPath);
                    File.WriteAllLines(optionTwoPath, file);
                }
            }

            if (fileName == "optionBoth.txt")
            {
                if (!File.ReadAllLines(optionOnePath).Contains(member.Id.ToString()))
                {
                    var file = File.ReadAllLines(optionOnePath).ToList();
                    file.Add(member.Id.ToString());
                    File.Delete(optionOnePath);
                    File.WriteAllLines(optionOnePath, file);
                }

                if (!File.ReadAllLines(optionTwoPath).Contains(member.Id.ToString()))
                {
                    var file = File.ReadAllLines(optionTwoPath).ToList();
                    file.Add(member.Id.ToString());
                    File.Delete(optionTwoPath);
                    File.WriteAllLines(optionTwoPath, file);
                }
            }
        }
    }
}

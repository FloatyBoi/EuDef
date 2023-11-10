using DSharpPlus.Entities;
using DSharpPlus;
using DSharpPlus.EventArgs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
                await e.Interaction.DeferAsync(ephemeral: true);

                string[] signon = Helpers.GetNicknameByIdArray(File.ReadAllLines(signupPath), e.Guild);
                string[] signoff = Helpers.GetNicknameByIdArray(File.ReadAllLines(signoffPath), e.Guild);
                string[] undecided = Helpers.GetNicknameByIdArray(File.ReadAllLines(undecidedPath), e.Guild);



                var embed = new DiscordEmbedBuilder()
                    .WithTitle("Status")
                    .AddField($"Angemeldet: {File.ReadAllLines(signupPath).Length}", $"------------------------------------------\n{String.Join("\n", signon)}\n------------------------------------------\n")
                    .AddField($"Abgemeldet: {File.ReadAllLines(signoffPath).Length}", $"------------------------------------------\n{String.Join("\n", signoff)}\n------------------------------------------\n")
                    .AddField($"Unentschieden: {File.ReadAllLines(undecidedPath).Length}", $"------------------------------------------\n{String.Join("\n", undecided)}\n------------------------------------------\n");

                await e.Interaction.EditOriginalResponseAsync(new DiscordWebhookBuilder().AddEmbed(embed: embed));
                return;
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


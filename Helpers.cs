using DSharpPlus.Entities;
using DSharpPlus.Exceptions;
using DSharpPlus.SlashCommands;
using System;
using System.Collections.Generic;
using System.Linq;
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
    }
}

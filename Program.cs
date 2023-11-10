using DSharpPlus;
using DSharpPlus.SlashCommands;
using Microsoft.Extensions.Logging;
using System.Net.Sockets;

namespace EuDef
{
    class Program
    {
        static async Task Main(string[] args)
        {

            //Debug
            bool debug = false;
            Console.WriteLine("Debug? (y/n)");
            string? ans = Console.ReadLine();
            if (ans?.ToLower() == "y")
            {
                debug = true;
                Console.WriteLine("Entering Debug Mode...\n");
            }


            var config = new DiscordConfiguration()
            {
                Token = Secret.GetToken(debug),
                TokenType = TokenType.Bot,
                Intents = DiscordIntents.All,
                AlwaysCacheMembers = true,
                MinimumLogLevel = LogLevel.Error
            };

            if (debug)
                config.MinimumLogLevel = LogLevel.Debug;

            //Setup
            var discordClient = new DiscordClient(config);
            var slash = discordClient.UseSlashCommands();


            //Start Timers
            TimerManager.StartTimers(discordClient);

            //Command Registering
            if (debug)
                slash.RegisterCommands<SlashCommands>(1006898069792632923);
            slash.RegisterCommands<SlashCommands>(1154741242320658493);

            Console.WriteLine(Directory.GetCurrentDirectory());

            //TODO: Die ganzen Events regeln (discordClient.On...)

        }
    }
}
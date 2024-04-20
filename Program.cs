using DSharpPlus;
using DSharpPlus.Interactivity.Extensions;
using DSharpPlus.SlashCommands;
using Microsoft.Extensions.Logging;
using System.Net.Sockets;

namespace EuDef
{
	static class Program
	{
		public static DiscordClient discordClient;

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

			foreach (var arg in args)
			{
				switch (arg.ToLower())
				{
					case "-nw":
						config.MinimumLogLevel = LogLevel.None;
						break;
					default:
						Console.WriteLine("Unknown arg: " + arg);
						return;

				}
			}

			//Setup
			discordClient = new DiscordClient(config);
			var slash = discordClient.UseSlashCommands();


			//Start Timers
			TimerManager.StartTimers(discordClient);

			//Command Registering
			if (debug)
				slash.RegisterCommands<SlashCommands>(1006898069792632923);
			else
				slash.RegisterCommands<SlashCommands>(1154741242320658493);

			Console.WriteLine(Directory.GetCurrentDirectory());

			await ClientEvents.RegisterClientEvents(discordClient, slash);

			discordClient.UseInteractivity(new DSharpPlus.Interactivity.InteractivityConfiguration()
			{
				PollBehaviour = DSharpPlus.Interactivity.Enums.PollBehaviour.KeepEmojis,
				Timeout = TimeSpan.FromSeconds(30)
			});

			await discordClient.ConnectAsync();

			Console.WriteLine("##################################################\nSetup Complete! Running...\n##################################################");

			await Task.Delay(-1);

		}
	}
}
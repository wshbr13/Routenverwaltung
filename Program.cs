using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.EventArgs;
using DSharpPlus.Interactivity;
using DSharpPlus.Interactivity.Extensions;
using DSharpPlus.SlashCommands;
using routenVerwaltung.commands;
using routenVerwaltung.config;
using System;
using System.Threading.Tasks;

namespace routenVerwaltung
{
    public class Program
    {
        static DiscordClient Client;
        static CommandsNextExtension Commands;

        public static void Main(string[] args)
        {
            MainAsync(args).ConfigureAwait(false).GetAwaiter().GetResult();
        }

        static async Task MainAsync(string[] args)
        {
            await InitializeBot();
        }

        static async Task InitializeBot()
        {
            // Erhalten Sie die Details Ihrer config.json-Datei, indem Sie sie deserialisieren
            var jsonReader = new JSONReader();
            await jsonReader.ReadJSON();

            // Einrichten der Bot-Konfiguration
            var discordConfig = new DiscordConfiguration()
            {
                Intents = DiscordIntents.All,
                Token = jsonReader.token,
                TokenType = TokenType.Bot,
                AutoReconnect = true
            };

            // Wenden Sie diese Konfiguration auf unseren DiscordClient an
            Client = new DiscordClient(discordConfig);

            // Legen Sie die Standardzeitüberschreitung für Befehle fest, die Interaktivität verwenden
            Client.UseInteractivity(new InteractivityConfiguration()
            {
                Timeout = TimeSpan.FromMinutes(2)
            });

            // Einrichten des Ereignisses "Task Handler Ready"
            Client.Ready += OnClickReady;

            var commandsConfig = new CommandsNextConfiguration()
            {
                StringPrefixes = new string[] { jsonReader.prefix },
                EnableMentionPrefix = true,
                EnableDms = true,
                EnableDefaultHelp = false
            };

            // Initialisierung der Eigenschaft CommandsNextExtension
            Commands = Client.UseCommandsNext(commandsConfig);

            // Aktivieren des Clients zur Verwendung von Slash-Befehlen
            //var slashCommandsConfig = Client.UseSlashCommands();

            // Registrieren Sie Ihre Befehlsklassen
            Commands.RegisterCommands<RoutenCommands>();
            //slashCommandsConfig.RegisterCommands<commands.slash.RoutenCommands>();

            // Verbinden, um den Bot online zu bekommen
            await Client.ConnectAsync();
            await Task.Delay(-1);
        }

        //public static DiscordClient Client { get; set; }
        //public static ulong CustomChannelId = 1187727222883238010;

        //private static CommandsNextExtension Commands { get; set; }

        //static async Task Main(string[] args)
        //{
        //    // Erhalten Sie die Details Ihrer config.json-Datei, indem Sie sie deserialisieren
        //    var jsonReader = new JSONReader();
        //    await jsonReader.ReadJSON();

        //    // Einrichten der Bot-Konfiguration
        //    var discordConfig = new DiscordConfiguration()
        //    {
        //        Intents = DiscordIntents.All,
        //        Token = jsonReader.token,
        //        TokenType = TokenType.Bot,
        //        AutoReconnect = true
        //    };

        //    // Wenden Sie diese Konfiguration auf unseren DiscordClient an
        //    Client = new DiscordClient(discordConfig);

        //    // Legen Sie die Standardzeitüberschreitung für Befehle fest, die Interaktivität verwenden
        //    Client.UseInteractivity(new InteractivityConfiguration()
        //    {
        //        Timeout = TimeSpan.FromMinutes(2)
        //    });

        //    // Einrichten des Ereignisses "Task Handler Ready
        //    Client.Ready += OnClickReady;

        //    var commandsConfig = new CommandsNextConfiguration()
        //    {
        //        StringPrefixes = new string[] { jsonReader.prefix }, 
        //        EnableMentionPrefix = true,
        //        EnableDms = true,
        //        EnableDefaultHelp = false
        //    };

        //    // Initialisierung der Eigenschaft CommandsNextExtension
        //    Commands = Client.UseCommandsNext(commandsConfig);

        //    // Aktivieren des Clients zur Verwendung von Slash-Befehlen
        //    //var slashCommandsConfig = Client.UseSlashCommands();

        //    // Registrieren Sie Ihre Befehlsklassen
        //    Commands.RegisterCommands<RoutenCommands>();
        //    //slashCommandsConfig.RegisterCommands<commands.slash.RoutenCommands>();

        //    // Verbinden, um den Bot online zu bekommen
        //    await Client.ConnectAsync();
        //    var program = new Program();
        //    await Task.Delay(-1);
        //}

        private static Task OnClickReady(DiscordClient sender, ReadyEventArgs args)
        {
            return Task.CompletedTask;
        }
    }
}
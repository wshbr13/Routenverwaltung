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
            Client.ComponentInteractionCreated += ButtonClick;

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
            var slashCommandsConfig = Client.UseSlashCommands();

            // Registrieren Sie Ihre Befehlsklassen
            Commands.RegisterCommands<RoutenCommands>();
            slashCommandsConfig.RegisterCommands<commands.slash.AdminSlash>(1099734806893445271);
            slashCommandsConfig.RegisterCommands<commands.slash.UserSlash>(1099734806893445271);

            // Verbinden, um den Bot online zu bekommen
            await Client.ConnectAsync();
            await Task.Delay(-1);
        }

        private static async Task ButtonClick(DiscordClient sender, ComponentInteractionCreateEventArgs args)
        {
            var SelectedOption = args.Interaction.Data.Values.Length > 0 ? args. Interaction.Data.Values[0] : null;

            switch (SelectedOption)
            {
                case "dd_Sammler":
                    await args.Channel.SendMessageAsync("BILDLINK");
                    break;

                case "dd_Verarbeiter":
                    await args.Channel.SendMessageAsync("BILDLINK");
                    break;
                default:
                    await args.Channel.SendMessageAsync("ERROR");
                    break;
            }
        }

        private static Task OnClickReady(DiscordClient sender, ReadyEventArgs args)
        {
            return Task.CompletedTask;
        }
    }
}
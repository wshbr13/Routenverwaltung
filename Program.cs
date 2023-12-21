using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.CommandsNext.Exceptions;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using DSharpPlus.Interactivity;
using DSharpPlus.Interactivity.Extensions;
using DSharpPlus.SlashCommands;
using einfachNezuko.config;
using System;
using System.Threading.Tasks;
using einfachNezuko.Commands.Slash_Commands;
using einfachNezuko.Commands.Prefix;

namespace einfachNezuko
{
    internal class Program
    {
        public static DiscordClient Client { get; set;}
        private static CommandsNextExtension Commands { get; set;}
        static async Task Main(string[] args)
        {
            //1. Get the details of your config.json file by deserialising it
            var jsonReader = new JSONReader();
            await jsonReader.ReadJSON();

            //2. Setting up the Bot Configuration
            var discordConfig = new DiscordConfiguration()
            {
                Intents = DiscordIntents.All,
                Token = jsonReader.token,
                TokenType = TokenType.Bot,
                AutoReconnect = true
            };

            //3. Apply this config to our DiscordClient
            Client = new DiscordClient(discordConfig);

            //4. Set the default timeout for Commands that use interactivity
            Client.UseInteractivity(new InteractivityConfiguration()
            {
                Timeout = TimeSpan.FromMinutes(2)
            });

            //5. Set up the Task Handler Ready event
            Client.Ready += OnClickReady;
            Client.ComponentInteractionCreated += ButtonPressResponse;
            //Client.MessageCreated += MessageCreatedHandler;

            //6. Set up the Commands Configuration
            var commandsConfig = new CommandsNextConfiguration()
            {
                StringPrefixes = new string[] { jsonReader.prefix },
                EnableMentionPrefix = true,
                EnableDms = true,
                EnableDefaultHelp = false,
            };

            //Enabling the use of commands with our config & also enabling use of Slash Commands
            Commands = Client.UseCommandsNext(commandsConfig);
            var slashCommandsConfig = Client.UseSlashCommands();

            //7. Register your commands
            // Prefix Based Commands
            //Commands.RegisterCommands<TestCommands>();
            Commands.RegisterCommands<BasicCommands>();
            Commands.RegisterCommands<GameCommands>();

            // Slash Commands
            slashCommandsConfig.RegisterCommands<FunSL>();

            Commands.CommandErrored += CommandHandler;

            //8. Connect to get the Bot online
            await Client.ConnectAsync();
            await Task.Delay(-1);
        }

        private static async Task ButtonPressResponse(DiscordClient sender, ComponentInteractionCreateEventArgs e)
        {
            if (e.Interaction.Data.CustomId == "basicButton")
            {
                string basicCommandsList = "!poll => Erstelle eine Umfrage! \n" +
                                           "!help => Lasse dir alle Befehle anzeigen";

                await e.Interaction.CreateResponseAsync(InteractionResponseType.UpdateMessage, new DiscordInteractionResponseBuilder().WithContent(basicCommandsList));
            }
            else if (e.Interaction.Data.CustomId == "gameButton")
            {
                string gamesList = "!cardgame => Spiele ein einfaches Kartenspiel! Wer die höchste Karte zieht, gewinnt!";

                var gamesCommandList = new DiscordInteractionResponseBuilder()
                {
                    Title = "Liste aller Spiele- / Fun-Commands",
                    Content = gamesList
                };

                await e.Interaction.CreateResponseAsync(InteractionResponseType.UpdateMessage, gamesCommandList);
            }
        }

        private static async Task CommandHandler(CommandsNextExtension sender, CommandErrorEventArgs e)
        {
            if (e.Exception is ChecksFailedException exception)
            {
                string timeLeft = string.Empty;
                foreach (var check in exception.FailedChecks)
                {
                    var coolDown = (CooldownAttribute)check;
                    timeLeft = coolDown.GetRemainingCooldown(e.Context).ToString(@"hh\:mm\:ss");
                }

                var coolDownMessage = new DiscordEmbedBuilder
                {
                   Color = DiscordColor.Red,
                   Title = "Bitte warte bis der Cooldown abgelaufen ist.",
                   Description = $"Zeit: {timeLeft}"                                
                };

                await e.Context.Channel.SendMessageAsync(embed: coolDownMessage);
            }
        }

        // Event handler which is triggered when a message is sent to the channel.
        //private static async Task MessageCreatedHandler(DiscordClient sender, MessageCreateEventArgs e)
        //{
        //    await e.Channel.SendMessageAsync("This event handler was triggered");
        //}

        private static Task OnClickReady(DiscordClient sender, ReadyEventArgs args)
        {
            return Task.CompletedTask;
        }
    }
}

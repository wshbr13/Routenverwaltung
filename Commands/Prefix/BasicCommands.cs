using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using DSharpPlus.Interactivity.Extensions;
using System;
using System.Threading.Tasks;

namespace einfachNezuko.Commands.Prefix
{
    public class BasicCommands : BaseCommandModule
    {
        //[Command("test")]
        //public async Task TestCommand(CommandContext ctx)
        //{
        //    var interactivity = Program.Client.GetInteractivity();
        //    var messageToRetrieve = await interactivity.WaitForMessageAsync(message => message.Content == "Hello");
        //    if (messageToRetrieve.Result.Content == "Hello")
        //    {
        //        await ctx.Channel.SendMessageAsync($"{ctx.User.Username} said Hello");
        //    }

        //}

        [Command("poll")]
        public async Task Poll(CommandContext ctx, string option1, string option2, string option3, string option4, [RemainingText] string pollTitle)
        {
            var interactivity = Program.Client.GetInteractivity();
            var pollTime = TimeSpan.FromDays(1); // every poll goes 24 hours

            DiscordEmoji[] emojiOptions = { DiscordEmoji.FromName(Program.Client, ":one:"),
                                            DiscordEmoji.FromName(Program.Client, ":two:"),
                                            DiscordEmoji.FromName(Program.Client, ":three:"),
                                            DiscordEmoji.FromName(Program.Client, ":four:") };

            string optionsDescription = $"{emojiOptions[0]} | {option1} \n" +
                                        $"{emojiOptions[1]} | {option2} \n" +
                                        $"{emojiOptions[2]} | {option3} \n" +
                                        $"{emojiOptions[3]} | {option4}";

            var pollMessage = new DiscordEmbedBuilder
            {
                Color = DiscordColor.Red,
                Title = pollTitle,
                Description = optionsDescription
            };

            var sentPoll = await ctx.Channel.SendMessageAsync(embed: pollMessage);
            foreach (var emoji in emojiOptions)
            {
                await sentPoll.CreateReactionAsync(emoji);
            }

            var totalReactions = await interactivity.CollectReactionsAsync(sentPoll, pollTime);

            int count1 = 0;
            int count2 = 0;
            int count3 = 0;
            int count4 = 0;

            foreach (var emoji in totalReactions)
            {
                if (emoji.Emoji == emojiOptions[0])
                {
                    count1++;
                }
                if (emoji.Emoji == emojiOptions[1])
                {
                    count2++;
                }
                if (emoji.Emoji == emojiOptions[2])
                {
                    count3++;
                }
                if (emoji.Emoji == emojiOptions[3])
                {
                    count4++;
                }
            }

            int totalVotes = count1 + count2 + count3 + count4;
            string resultsDescription = $"{emojiOptions[0]}: {count1} Votes \n" +
                                        $"{emojiOptions[1]}: {count2} Votes \n" +
                                        $"{emojiOptions[2]}: {count3} Votes \n" +
                                        $"{emojiOptions[3]}: {count4} Votes \n\n" +
                                        $"Total Votes: {totalVotes}";

            var resultEmbed = new DiscordEmbedBuilder
            {
                Color = DiscordColor.Green,
                Title = "Results of the Poll",
                Description = resultsDescription
            };

            await ctx.Channel.SendMessageAsync(embed: resultEmbed);
        }

        [Command("help")]
        public async Task HelpCommand(CommandContext ctx)
        {
            var basicButton = new DiscordButtonComponent(ButtonStyle.Success, "basicButton", "Basics");
            var gameButton = new DiscordButtonComponent(ButtonStyle.Success, "gameButton", "Games");

            var helpMessage = new DiscordMessageBuilder()
                .AddEmbed(new DiscordEmbedBuilder()
                    .WithColor(DiscordColor.Azure)
                    .WithTitle("Help Menu")
                    .WithDescription("Please pick a button for more information on the commands"))
                .AddComponents(basicButton, gameButton);

            await ctx.Channel.SendMessageAsync(helpMessage);
        }
    }
}
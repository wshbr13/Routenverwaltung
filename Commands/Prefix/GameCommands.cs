using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using einfachNezuko.other;
using System.Threading.Tasks;

namespace einfachNezuko.Commands.Prefix
{
    public class GameCommands : BaseCommandModule
    {
        [Command("cardgame")]
        [Cooldown(1, 600, CooldownBucketType.User)]
        public async Task CardGame(CommandContext ctx)
        {
            var userCard = new CardSystem();

            var userCardEmbed = new DiscordEmbedBuilder
            {
                Title = $"Your card is {userCard.SelectedCard}",
                Color = DiscordColor.Lilac
            };

            await ctx.Channel.SendMessageAsync(embed: userCardEmbed);

            var botCard = new CardSystem();

            var botCardEmbed = new DiscordEmbedBuilder
            {
                Title = $"The Bots drew a {botCard.SelectedCard}",
                Color = DiscordColor.Orange
            };

            await ctx.Channel.SendMessageAsync(embed: botCardEmbed);

            if (userCard.SelectedNumber > botCard.SelectedNumber)
            {
                // User Wins
                var winMessage = new DiscordEmbedBuilder
                {
                    Title = "Congratulations, You Win!",
                    Color = DiscordColor.Green,
                };
                await ctx.Channel.SendMessageAsync(embed: winMessage);
            }
            else
            {
                // Bot Wins
                var loseMessage = new DiscordEmbedBuilder
                {
                    Title = "You Lost the Game!",
                    Color = DiscordColor.Red,
                };
                await ctx.Channel.SendMessageAsync(embed: loseMessage);
            }
        }
    }
}


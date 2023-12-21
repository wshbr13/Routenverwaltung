using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using einfachNezuko.other;
using System.Threading.Tasks;

namespace einfachNezuko.commands
{
    public class TestCommands : BaseCommandModule
    {
        [Command("test")]
        public async Task MyFirstCommand(CommandContext ctx)
        {
            await ctx.Channel.SendMessageAsync($"Hello {ctx.User.Username} ");
        }

        // Alternative Embed Methode
        //[Command("embed")]
        //public async Task EmbedMessage(CommandContext ctx)
        //{
        //    var message = new DiscordMessageBuilder()
        //        .AddEmbed(new DiscordEmbedBuilder()
        //            .WithTitle("This is my first Discord Embed")
        //            .WithDescription($"This command was executed by {ctx.User.Username}"));

        //    await ctx.Channel.SendMessageAsync(message);
        //}

        [Command("embed")]
        public async Task EmbedMessage(CommandContext ctx)
        {
            var message = new DiscordEmbedBuilder
            {
                Title = "This is my first Discord Embed",
                Description = $"This command was executed by {ctx.User.Username}",
                Color = DiscordColor.Blue
                
            };

            await ctx.Channel.SendMessageAsync(embed: message);
        }
    }
}

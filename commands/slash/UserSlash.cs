using DSharpPlus;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using DSharpPlus.SlashCommands;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace routenVerwaltung.commands.slash
{
    public class UserSlash: ApplicationCommandModule
    {
        private const string JsonFilePath = "user_data.json";

        [SlashCommand("help", "Zeigt eine Liste aller verfügbaren Befehle und deren Beschreibungen an.")]
        public async Task HelpCommand(InteractionContext ctx)
        {
            var embed = new DiscordEmbedBuilder
            {
                Title = "Hilfe",
                Description = "Hier ist eine Liste aller verfügbaren Befehle und deren Beschreibungen:",
                Color = DiscordColor.Gold
            };

            embed.AddField("Command: /user", "Zeigt die Statistiken eines bestimmten Benutzers an.");
            embed.AddField("Command: /info", "Zeigt die Informationen zur Route an.");
            embed.AddField("Command: /top", "Zeigt eine Rangliste der Mitglieder basierend auf gesammelten Punkten an.");
            embed.AddField("Command: /legends", "Zeigt eine Liste der Mitglieder mit Legendenstatus an.");
            embed.AddField("Command: /hdw", "Zeigt den 'Hustler der Woche' basierend auf den gesammelten Punkten an.");

            await ctx.CreateResponseAsync(embed: embed);
        }

        [SlashCommand("info", "Zeigt Informationen zu Route, Sammler und Verarbeiter an.")]
        public async Task InfoCommand(InteractionContext ctx)
        {
            var dropdownComponents = new List<DiscordSelectComponentOption>()
            {
                new DiscordSelectComponentOption("Sammler", "dd_Sammler", "Zeige dir den Sammler an", emoji: new DiscordComponentEmoji(DiscordEmoji.FromName(ctx.Client, ":round_pushpin:"))),
                new DiscordSelectComponentOption("Verarbeiter", "dd_Verarbeiter", "Zeige dir den Verarbeiter an", emoji: new DiscordComponentEmoji(DiscordEmoji.FromName(ctx.Client, ":construction_worker:")))
            };

            var dropdown = new DiscordSelectComponent("RoutenDropdown", "Was möchtest du dir anzeigen lassen?", dropdownComponents, false, 0, 1);

            var embed = new DiscordMessageBuilder().AddEmbed(new DiscordEmbedBuilder().WithColor(DiscordColor.Red).WithTitle("Routeninformationen").WithDescription("Aktuell sind wir auf der ... Route")).AddComponents(dropdown);

            await ctx.Channel.SendMessageAsync(embed);
        }

        [SlashCommand("top", "Zeigt eine Rangliste der Mitglieder basierend auf gesammelten Punkten an.")]
        [RequireRoles(RoleCheckMode.Any, "Techniker")] // Benutzer mit der Rolle "DeineRolleName" können diesen Befehl verwenden
        public async Task TopCommand(InteractionContext ctx)
        {
            if (!ctx.Member.Roles.Any(r => r.Name == "Techniker"))
            {
                await ctx.CreateResponseAsync("Du hast nicht die erforderliche Rolle, um diesen Befehl auszuführen.");
                return;
            }

            // Lese alle Benutzer aus der Datei
            UserData[] users = ReadAllUsers();

            // Sortiere die Benutzer nach den meisten gesammelten Punkten
            var sortedUsers = users.OrderByDescending(u => u.Gesammelt).ToArray();

            // Erstelle ein Embed für die Top-Benutzer
            var embed = new DiscordEmbedBuilder
            {
                Title = "Top-Liste der Mitglieder",
                Color = DiscordColor.Blue
            };

            var sb = new StringBuilder();

            // Überschriften
            sb.AppendLine("```md");
            sb.AppendLine("# Platz | Username        | Gesammelt");
            sb.AppendLine("------------------------------------");

            for (int i = 0; i < sortedUsers.Length; i++)
            {
                string legendentitelMarkierung = sortedUsers[i].Legendentitel ? "*" : "";
                sb.AppendLine($"  {i + 1,-5} | {sortedUsers[i].Name + legendentitelMarkierung,-14} | {sortedUsers[i].Gesammelt:F0}");
            }

            sb.AppendLine("```");

            embed.Description = sb.ToString();

            // Sende das Embed mit den Top-Benutzern
            await ctx.CreateResponseAsync(embed: embed);
        }

        [SlashCommand("user", "Zeigt die Statistiken eines bestimmten Benutzers an.")]
        //[RequireRoles(RoleCheckMode.Any, "Techniker")] // Benutzer mit der Rolle "DeineRolleName" können diesen Befehl verwenden
        public async Task UserStatsCommand(InteractionContext ctx, [Option("name", "Der Name des Benutzers")] string inputName)
        {
            if (!ctx.Member.Roles.Any(r => r.Name == "Techniker"))
            {
                await ctx.CreateResponseAsync("Du hast nicht die erforderliche Rolle, um diesen Befehl auszuführen.");
                return;
            }

            // Lese alle Benutzer aus der Datei
            UserData[] users = ReadAllUsers();

            // Finde den Benutzer in der Liste (unabhängig von Groß- und Kleinschreibung)
            UserData user = users.FirstOrDefault(u => u.Name.Equals(inputName, StringComparison.OrdinalIgnoreCase));

            // Überprüfe, ob der Benutzer gefunden wurde
            if (user == null)
            {
                await ctx.CreateResponseAsync($"Benutzer '{inputName}' nicht gefunden. Verwende !register, um ihn zu registrieren.");
                return;
            }

            // Erstelle ein Embed für die Statistiken des einzelnen Benutzers
            var embed = new DiscordEmbedBuilder
            {
                Title = $"{user.Name}'s Statistiken",
                Color = DiscordColor.Orange
            };

            var userStatsSb = new StringBuilder();

            // Überschriften
            userStatsSb.AppendLine("```md");
            userStatsSb.AppendLine("# Username        | Gesammelt  | Betrag");
            userStatsSb.AppendLine("--------------------------------------");

            // Berechne den Betrag unter Berücksichtigung des Legendentitels
            decimal abgabe = user.Legendentitel ? 0 : AdminSlash.GlobalAbgabe;
            decimal betrag = (user.Gesammelt - abgabe) * AdminSlash.GlobalKurs;

            string legendentitelMarkierung = user.Legendentitel ? "*" : "";
            userStatsSb.AppendLine($"  {user.Name + legendentitelMarkierung,-14} | {user.Gesammelt,-10:F0} | {betrag:F0}");

            userStatsSb.AppendLine("```");

            embed.Description = userStatsSb.ToString();

            // Sende das Embed mit den Statistiken des einzelnen Benutzers
            await ctx.CreateResponseAsync(embed: embed);
        }

        [SlashCommand("legends", "Zeigt eine Liste der Mitglieder mit Legendenstatus an.")]
        [RequireRoles(RoleCheckMode.Any, "Techniker")] // Benutzer mit der Rolle "DeineRolleName" können diesen Befehl verwenden
        public async Task LegendsCommand(InteractionContext ctx)
        {
            if (!ctx.Member.Roles.Any(r => r.Name == "Techniker"))
            {
                await ctx.CreateResponseAsync("Du hast nicht die erforderliche Rolle, um diesen Befehl auszuführen.");
                return;
            }

            UserData[] allUsers = ReadAllUsers();

            // Filtere Benutzer mit Legendenstatus
            var legendUsers = allUsers.Where(user => user.Legendentitel);

            // Sortiere die Benutzer nach Gesammelten Punkten (absteigend)
            legendUsers = legendUsers.OrderByDescending(user => user.Gesammelt).ToArray();

            // Erstelle ein Embed für die Legenden
            var embed = new DiscordEmbedBuilder
            {
                Title = "Mitglieder mit Legendenstatus",
                Color = DiscordColor.Gold
            };

            var sb = new StringBuilder();
            sb.AppendLine("```md");
            sb.AppendLine("# User           | Gesammelt");
            sb.AppendLine("---------------------------");

            int rank = 1;
            foreach (UserData user in legendUsers)
            {
                sb.AppendLine($"{rank}. {user.Name,-15} | {user.Gesammelt}");
                rank++;
            }

            sb.AppendLine("```");

            embed.Description = sb.ToString();

            // Sende das Embed mit den Legenden
            await ctx.CreateResponseAsync(embed: embed);
        }

        [SlashCommand("hdw", "Zeigt den 'Hustler der Woche' basierend auf den gesammelten Punkten an.")]
        [RequireRoles(RoleCheckMode.Any, "Techniker")] // Benutzer mit der Rolle "DeineRolleName" können diesen Befehl verwenden
        public async Task HighscoreDisplayWinnerCommand(InteractionContext ctx)
        {
            if (!ctx.Member.Roles.Any(r => r.Name == "Techniker"))
            {
                await ctx.CreateResponseAsync("Du hast nicht die erforderliche Rolle, um diesen Befehl auszuführen.");
                return;

            }
            UserData[] allUsers = ReadAllUsers();

            // Finde den Benutzer mit der höchsten Gesamtpunktzahl
            var winner = allUsers.OrderByDescending(u => u.Gesammelt).FirstOrDefault();

            if (winner == null)
            {
                await ctx.CreateResponseAsync("Keine Benutzer gefunden.");
                return;
            }

            // Erstelle ein breiteres Embed für den Gewinner
            var embed = new DiscordEmbedBuilder
            {
                Title = "Hustler der Woche",
                Color = DiscordColor.Orange
            };

            // Füge die Informationen zum Gewinner hinzu
            embed.AddField("Username", winner.Name, true);
            embed.AddField("Gesammelt", winner.Gesammelt.ToString("F0"), true);

            // Füge eine leere Zeile für bessere Darstellung hinzu
            embed.AddField("\u200B", "\u200B", true);

            // Füge einen Fußzeilentext hinzu
            embed.WithFooter($"Herzlichen Glückwunsch an {winner.Name}!");

            // Sende das breitere Embed mit den Informationen zum Gewinner
            await ctx.CreateResponseAsync(embed: embed);
        }

        private UserData[] ReadAllUsers()
        {
            if (File.Exists(JsonFilePath))
            {
                string json = File.ReadAllText(JsonFilePath);
                return JsonConvert.DeserializeObject<UserData[]>(json);
            }

            return new UserData[0];
        }

    }
}


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
    public class RoutenCommands : ApplicationCommandModule
    {
        private const string JsonFilePath = "user_data.json";
        private static decimal GlobalAbgabe = 500; // Globale Variable für Abgabe
        private static decimal GlobalKurs = 47;   // Globale Variable für Kurs

        [SlashCommand("abgabe", "Setzt die globale Abgabe.")]
        [RequireRoles(RoleCheckMode.Any, "Techniker")]
        public async Task SetGlobalAbgabeCommand(InteractionContext ctx, [Option("abgabe", "Der neue Abgabe-Wert.")] double abgabe)
        {
            if (!ctx.Member.Roles.Any(r => r.Name == "Techniker"))
            {
                await ctx.CreateResponseAsync(DSharpPlus.InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder().WithContent("Du hast nicht die erforderliche Rolle, um diesen Befehl auszuführen."));
                return;
            }

            GlobalAbgabe = (decimal)abgabe;
            await ctx.CreateResponseAsync(DSharpPlus.InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder().WithContent($"Globale Abgabe auf {abgabe} festgelegt."));
        }


        [SlashCommand("kurs", "Setzt den globalen Kurs.")]
        [RequireRoles(RoleCheckMode.Any, "Techniker")] // Benutzer mit der Rolle "DeineRolleName" können diesen Befehl verwenden

        public async Task SetGlobalKursCommand(InteractionContext ctx, decimal kurs)
        {
            if (!ctx.Member.Roles.Any(r => r.Name == "Techniker"))
            {
                await ctx.CreateResponseAsync("Du hast nicht die erforderliche Rolle, um diesen Befehl auszuführen.");
                return;
            }

            GlobalKurs = kurs;
            await ctx.CreateResponseAsync($"Globaler Kurs auf {kurs} festgelegt.");
        }

        [SlashCommand("legende", "Schaltet den Legendentitel für einen Benutzer um.")]
        [RequireRoles(RoleCheckMode.Any, "Techniker")] // Benutzer mit der Rolle "DeineRolleName" können diesen Befehl verwenden
        public async Task SetLegendeCommand(InteractionContext ctx, string inputName)
        {
            if (!ctx.Member.Roles.Any(r => r.Name == "Techniker"))
            {
                await ctx.CreateResponseAsync("Du hast nicht die erforderliche Rolle, um diesen Befehl auszuführen.");
                return;
            }

            // Überprüfe, ob der Benutzer registriert ist
            UserData userData = GetUserByName(inputName);
            if (userData == null)
            {
                await ctx.CreateResponseAsync($"Benutzer '{inputName}' ist nicht registriert. Verwende !register, um ihn zu registrieren.");
                return;
            }

            userData.Legendentitel = !userData.Legendentitel; // Umschalten des Legendentitels

            SaveUser(userData);

            if (userData.Legendentitel)
            {
                await ctx.CreateResponseAsync($"Legendentitel für Benutzer '{inputName}' festgelegt.");
            }
            else
            {
                await ctx.CreateResponseAsync($"Legendentitel für Benutzer '{inputName}' entfernt.");
            }
        }

        [SlashCommand("sanktion", "Zeigt eine Übersicht der ausstehenden Sanktionen an.")]
        [RequireRoles(RoleCheckMode.Any, "Techniker")] // Benutzer mit der Rolle "DeineRolleName" können diesen Befehl verwenden
        public async Task SanktionCommand(InteractionContext ctx)
        {
            if (!ctx.Member.Roles.Any(r => r.Name == "Techniker"))
            {
                await ctx.CreateResponseAsync("Du hast nicht die erforderliche Rolle, um diesen Befehl auszuführen.");
                return;
            }

            UserData[] allUsers = ReadAllUsers();

            // Erstelle ein Embed für die Sanktionen
            var embed = new DiscordEmbedBuilder
            {
                Title = "Übersicht der ausstehenden Sanktionen",
                Color = DiscordColor.Red
            };

            var sb = new StringBuilder();

            foreach (UserData user in allUsers)
            {
                decimal abgabe = user.Legendentitel ? 0 : GlobalAbgabe; // Berücksichtige Legendentitel bei der Abgabe
                decimal betrag = (user.Gesammelt - abgabe) * GlobalKurs; // Berücksichtige Abgabe nur, wenn Legendentitel nicht vorhanden ist

                if (betrag < 0)
                {
                    string legendentitelMarkierung = user.Legendentitel ? "*" : "";
                    sb.AppendLine($"{user.Name + legendentitelMarkierung}: Hat {user.Gesammelt:F0} von \"{abgabe} gesammelt.");
                }
            }

            embed.Description = sb.ToString();

            // Sende das Embed mit den Sanktionen
            await ctx.CreateResponseAsync(embed: embed);
        }

        [SlashCommand("stats", "Zeigt eine Übersicht aller Benutzerstatistiken an.")]
        [RequireRoles(RoleCheckMode.Any, "Techniker")] // Benutzer mit der Rolle "DeineRolleName" können diesen Befehl verwenden
        public async Task StatsCommand(InteractionContext ctx)
        {
            if (!ctx.Member.Roles.Any(r => r.Name == "Techniker"))
            {
                await ctx.CreateResponseAsync("Du hast nicht die erforderliche Rolle, um diesen Befehl auszuführen.");
                return;
            }

            UserData[] allUsers = ReadAllUsers();

            // Erstelle ein Embed für die Statistiken
            var embed = new DiscordEmbedBuilder
            {
                Title = "Übersicht aller Mitglieder (Routenverwaltung)",
                Color = DiscordColor.Green
            };

            var sb = new StringBuilder();

            // Überschriften
            sb.AppendLine("```md");
            sb.AppendLine("# Username        | Gesammelt  | Abgabe  | Kurs  | Betrag");
            sb.AppendLine("--------------------------------------------------------");

            foreach (UserData user in allUsers)
            {
                decimal abgabe = user.Legendentitel ? 0 : GlobalAbgabe;
                string legendentitelMarkierung = user.Legendentitel ? "*" : "";
                decimal betrag = (user.Gesammelt - (user.Legendentitel ? 0 : abgabe)) * GlobalKurs;

                // Einrückung für bessere Ausrichtung
                sb.AppendLine($"  {user.Name + legendentitelMarkierung,-14} | {user.Gesammelt,-10:F0} | {abgabe,-7} | {GlobalKurs,-5} | {betrag,-7:F0}");
            }

            sb.AppendLine("```");

            embed.Description = sb.ToString();

            // Sende das Embed mit den Statistiken
            await ctx.CreateResponseAsync(embed: embed);
        }

        [SlashCommand("add", "Fügt dem angegebenen Benutzer Abgaben hinzu.")]
        [RequireRoles(RoleCheckMode.Any, "Techniker")] // Benutzer mit der Rolle "DeineRolleName" können diesen Befehl verwenden
        public async Task AddPointsCommand(InteractionContext ctx, string inputName, decimal points)
        {
            if (!ctx.Member.Roles.Any(r => r.Name == "Techniker"))
            {
                await ctx.CreateResponseAsync("Du hast nicht die erforderliche Rolle, um diesen Befehl auszuführen.");
                return;
            }

            // Überprüfe, ob der Benutzer registriert ist
            UserData userData = GetUserByName(inputName);
            if (userData == null)
            {
                await ctx.CreateResponseAsync($"Benutzer '{inputName}' ist nicht registriert. Verwende !register, um ihn zu registrieren.");
                return;
            }

            decimal abgabe = userData.Legendentitel ? 0 : GlobalAbgabe; // Berücksichtige Legendentitel bei der Abgabe
            userData.Gesammelt += points;
            userData.Betrag = (userData.Gesammelt - abgabe) * GlobalKurs; // Aktualisiere den Betrag

            SaveUser(userData);

            // Erstelle ein Embed für die Bestätigung
            var embed = new DiscordEmbedBuilder
            {
                Title = "Punkte Hinzugefügt",
                Color = DiscordColor.Green
            };

            embed.Description = $"Punkte wurden zu Benutzer '{inputName}' hinzugefügt. Neuer Gesammelt-Wert: {userData.Gesammelt}";

            // Sende das Embed mit der Bestätigung
            await ctx.CreateResponseAsync(embed: embed);
        }

        [SlashCommand("paycheck", "Zeigt die Beträge für alle Mitglieder an.")]
        [RequireRoles(RoleCheckMode.Any, "Techniker")] // Benutzer mit der Rolle "DeineRolleName" können diesen Befehl verwenden
        public async Task PaycheckCommand(InteractionContext ctx)
        {
            if (!ctx.Member.Roles.Any(r => r.Name == "Techniker"))
            {
                await ctx.CreateResponseAsync("Du hast nicht die erforderliche Rolle, um diesen Befehl auszuführen.");
                return;
            }

            UserData[] allUsers = ReadAllUsers();

            // Erstelle ein Embed für die Paychecks aller Benutzer
            var embed = new DiscordEmbedBuilder
            {
                Title = "Paychecks der Mitglieder",
                Color = DiscordColor.Purple
            };

            var paycheckSb = new StringBuilder();

            // Überschriften
            paycheckSb.AppendLine("```md");
            paycheckSb.AppendLine("# Username        | Betrag");
            paycheckSb.AppendLine("--------------------------------------");

            foreach (UserData user in allUsers)
            {
                string legendentitelMarkierung = user.Legendentitel ? "*" : "";
                paycheckSb.AppendLine($"  {user.Name + legendentitelMarkierung,-14} | {user.Betrag:F0}");
            }

            paycheckSb.AppendLine("```");

            embed.Description = paycheckSb.ToString();

            // Sende das Embed mit den Paychecks aller Benutzer
            await ctx.CreateResponseAsync(embed: embed);
        }

        [SlashCommand("register", "Registriert einen neuen Benutzer.")]
        [RequireRoles(RoleCheckMode.Any, "Techniker")] // Benutzer mit der Rolle "DeineRolleName" können diesen Befehl verwenden
        public async Task RegisterCommand(InteractionContext ctx, [Option("name", "Der Name des Benutzers")] string name)
        {
            if (!ctx.Member.Roles.Any(r => r.Name == "Techniker"))
            {
                await ctx.CreateResponseAsync("Du hast nicht die erforderliche Rolle, um diesen Befehl auszuführen.");
                return;
            }

            // Überprüfe, ob der Benutzer bereits registriert ist
            UserData userData = GetUserByName(name);
            if (userData != null)
            {
                // Erstelle ein Embed für die Fehlermeldung
                var embedError = new DiscordEmbedBuilder
                {
                    Title = "Fehler bei der Registrierung",
                    Color = DiscordColor.Red
                };

                embedError.Description = $"Benutzer '{name}' ist bereits registriert.";

                // Sende das Embed mit der Fehlermeldung
                await ctx.CreateResponseAsync(embed: embedError);
                return;
            }

            // Füge den neuen Benutzer hinzu
            userData = new UserData
            {
                Name = name,
                Gesammelt = 0,
                Betrag = 0
            };

            // Speichere den Benutzer im JSON-Datei
            SaveUser(userData);

            // Erstelle ein Embed für die Bestätigung
            var embed = new DiscordEmbedBuilder
            {
                Title = "Registrierung Erfolgreich",
                Color = DiscordColor.Green
            };

            embed.Description = $"Benutzer '{name}' wurde erfolgreich registriert.";

            // Sende das Embed mit der Bestätigung
            await ctx.CreateResponseAsync(embed: embed);
        }

        [SlashCommand("remove", "Entfernt dem angegebenen Benutzer Abgaben.")]
        [RequireRoles(RoleCheckMode.Any, "Techniker")] // Benutzer mit der Rolle "DeineRolleName" können diesen Befehl verwenden
        public async Task RemovePointsCommand(InteractionContext ctx, string inputName, decimal points)
        {
            if (!ctx.Member.Roles.Any(r => r.Name == "Techniker"))
            {
                await ctx.CreateResponseAsync("Du hast nicht die erforderliche Rolle, um diesen Befehl auszuführen.");
                return;
            }

            // Überprüfe, ob der Benutzer registriert ist
            UserData userData = GetUserByName(inputName);
            if (userData == null)
            {
                // Erstelle ein Embed für die Fehlermeldung
                var embedError = new DiscordEmbedBuilder
                {
                    Title = "Fehler beim Punkte Entfernen",
                    Color = DiscordColor.Red
                };

                embedError.Description = $"Benutzer '{inputName}' ist nicht registriert. Verwende !register, um ihn zu registrieren.";

                // Sende das Embed mit der Fehlermeldung
                await ctx.CreateResponseAsync(embed: embedError);
                return;
            }

            // Überprüfe, ob der Benutzer genügend Punkte hat
            if (userData.Gesammelt < points)
            {
                // Erstelle ein Embed für die Fehlermeldung
                var embedError = new DiscordEmbedBuilder
                {
                    Title = "Fehler beim Punkte Entfernen",
                    Color = DiscordColor.Red
                };

                embedError.Description = $"Benutzer '{inputName}' hat nicht genügend Punkte zum Entfernen.";

                // Sende das Embed mit der Fehlermeldung
                await ctx.CreateResponseAsync(embed: embedError);
                return;
            }

            // Ziehe die Punkte ab und speichere den Benutzer
            userData.Gesammelt -= points;
            SaveUser(userData);

            // Erstelle ein Embed für die Bestätigung
            var embed = new DiscordEmbedBuilder
            {
                Title = "Punkte Entfernt",
                Color = DiscordColor.Green
            };

            embed.Description = $"Vom Benutzer '{inputName}' wurden {points} Punkte abgezogen. Neuer Gesammelt-Wert: {userData.Gesammelt}";

            // Sende das Embed mit der Bestätigung
            await ctx.CreateResponseAsync(embed: embed);
        }

        [SlashCommand("delte", "Löscht einen registrierten Benutzer.")]
        [RequireRoles(RoleCheckMode.Any, "Techniker")] // Benutzer mit der Rolle "DeineRolleName" können diesen Befehl verwenden
        public async Task DeleteUserCommand(InteractionContext ctx, string inputName)
        {
            if (!ctx.Member.Roles.Any(r => r.Name == "Techniker"))
            {
                await ctx.CreateResponseAsync("Du hast nicht die erforderliche Rolle, um diesen Befehl auszuführen.");
                return;

            }
            // Überprüfe, ob der Benutzer registriert ist
            UserData userData = GetUserByName(inputName);
            if (userData == null)
            {
                // Erstelle ein Embed für die Fehlermeldung
                var embedError = new DiscordEmbedBuilder
                {
                    Title = "Fehler beim Löschen",
                    Color = DiscordColor.Red
                };

                embedError.Description = $"Benutzer '{inputName}' ist nicht registriert. Verwende !register, um ihn zu registrieren.";

                // Sende das Embed mit der Fehlermeldung
                await ctx.CreateResponseAsync(embed: embedError);
                return;
            }

            // Lösche den Benutzer und speichere die aktualisierte Benutzerliste
            DeleteUser(userData);

            // Erstelle ein Embed für die Bestätigung
            var embed = new DiscordEmbedBuilder
            {
                Title = "Benutzer Gelöscht",
                Color = DiscordColor.Green
            };

            embed.Description = $"Benutzer '{inputName}' wurde erfolgreich gelöscht.";

            // Sende das Embed mit der Bestätigung
            await ctx.CreateResponseAsync(embed: embed);
        }

        [SlashCommand("reset", "Setzt die Punkte aller Benutzer zurück.")]
        [RequireRoles(RoleCheckMode.Any, "Techniker")] // Benutzer mit der Rolle "DeineRolleName" können diesen Befehl verwenden
        public async Task ResetPointsCommand(InteractionContext ctx)
        {
            if (!ctx.Member.Roles.Any(r => r.Name == "Techniker"))
            {
                await ctx.CreateResponseAsync("Du hast nicht die erforderliche Rolle, um diesen Befehl auszuführen.");
                return;
            }

            // Lese alle Benutzer aus der Datei
            UserData[] users = ReadAllUsers();

            // Setze die Punkte aller Benutzer auf 0
            foreach (var user in users)
            {
                user.Gesammelt = 0;
            }

            // Speichere die aktualisierte Benutzerliste
            SaveAllUsers(users);

            // Erstelle ein Embed für die Bestätigung
            var embed = new DiscordEmbedBuilder
            {
                Title = "Punkte Zurückgesetzt",
                Color = DiscordColor.Green
            };

            embed.Description = "Alle Punkte wurden zurückgesetzt.";

            // Sende das Embed mit der Bestätigung
            await ctx.CreateResponseAsync(embed: embed);
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
        [RequireRoles(RoleCheckMode.Any, "Techniker")] // Benutzer mit der Rolle "DeineRolleName" können diesen Befehl verwenden
        public async Task UserStatsCommand(InteractionContext ctx, string inputName)
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
            decimal abgabe = user.Legendentitel ? 0 : GlobalAbgabe;
            decimal betrag = (user.Gesammelt - abgabe) * GlobalKurs;

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

        [SlashCommand("save", "Speichert die aktuellen Statistiken in einer Datei.")]
        [RequireRoles(RoleCheckMode.Any, "Techniker")] // Benutzer mit der Rolle "DeineRolleName" können diesen Befehl verwenden
        public async Task SaveStatsCommand(InteractionContext ctx)
        {
            if (!ctx.Member.Roles.Any(r => r.Name == "Techniker"))
            {
                await ctx.CreateResponseAsync("Du hast nicht die erforderliche Rolle, um diesen Befehl auszuführen.");
                return;

            }
            UserData[] allUsers = ReadAllUsers();

            // Erstelle einen Dateinamen mit Datum und Uhrzeit
            string fileName = $"Bloods-{DateTime.Now:dd-MM-yyyy}.txt";

            try
            {
                // Erstelle einen StreamWriter für die Datei
                using (StreamWriter sw = new StreamWriter(fileName))
                {
                    sw.WriteLine("Username        | Gesammelt  | Abgabe  | Kurs  | Betrag");
                    sw.WriteLine("--------------------------------------------------------");

                    foreach (UserData user in allUsers)
                    {
                        decimal abgabe = user.Legendentitel ? 0 : GlobalAbgabe;
                        decimal betrag = (user.Gesammelt - abgabe) * GlobalKurs;

                        // Schreibe die Zeile in die Datei
                        sw.WriteLine($"{user.Name,-15} | {user.Gesammelt,-10} | {abgabe,-7} | {GlobalKurs,-5} | {betrag,-7:F}");
                    }
                }

                // Sende eine Bestätigungsnachricht
                await ctx.CreateResponseAsync($"Die Statistiken wurden erfolgreich in die Datei '{fileName}' gespeichert.");
            }
            catch (Exception ex)
            {
                // Bei einem Fehler Sende eine Fehlermeldung
                await ctx.CreateResponseAsync($"Fehler beim Speichern der Statistiken: {ex.Message}");
            }
        }

        [SlashCommand("load", "Lädt Statistiken aus einer zuvor gespeicherten Datei.")]
        [RequireRoles(RoleCheckMode.Any, "Techniker")] // Benutzer mit der Rolle "DeineRolleName" können diesen Befehl verwenden
        public async Task LoadStatsCommand(InteractionContext ctx, string fileName)
        {
            if (!ctx.Member.Roles.Any(r => r.Name == "Techniker"))
            {
                await ctx.CreateResponseAsync("Du hast nicht die erforderliche Rolle, um diesen Befehl auszuführen.");
                return;
            }

            try
            {
                // Überprüfe, ob die Datei existiert
                if (!File.Exists(fileName))
                {
                    await ctx.CreateResponseAsync($"Die Datei '{fileName}' existiert nicht.");
                    return;
                }

                // Lese die Zeilen aus der Datei
                string[] lines = File.ReadAllLines(fileName);

                // Erstelle ein Embed für die geladenen Statistiken
                var embed = new DiscordEmbedBuilder
                {
                    Title = "Geladene Statistiken",
                    Color = DiscordColor.Blue
                };

                var sb = new StringBuilder();

                // Füge die Zeilen aus der Datei zum StringBuilder hinzu
                foreach (string line in lines)
                {
                    sb.AppendLine(line);
                }

                embed.Description = sb.ToString();

                // Sende das Embed mit den geladenen Statistiken
                await ctx.CreateResponseAsync(embed: embed);
            }
            catch (Exception ex)
            {
                // Bei einem Fehler Sende eine Fehlermeldung
                await ctx.CreateResponseAsync($"Fehler beim Laden der Statistiken: {ex.Message}");
            }
        }

        [SlashCommand("saves", "Zeigt eine Liste der gespeicherten Statistikdateien an.")]
        [RequireRoles(RoleCheckMode.Any, "Techniker")] // Benutzer mit der Rolle "DeineRolleName" können diesen Befehl verwenden
        public async Task ListSavedStatsFiles(InteractionContext ctx)
        {
            if (!ctx.Member.Roles.Any(r => r.Name == "Techniker"))
            {
                await ctx.CreateResponseAsync("Du hast nicht die erforderliche Rolle, um diesen Befehl auszuführen.");
                return;
            }

            try
            {
                // Suche alle .txt-Dateien im aktuellen Verzeichnis
                string[] files = Directory.GetFiles(Directory.GetCurrentDirectory(), "*.txt");

                // Filtere nach Dateien, die mit "Bloods-" beginnen
                files = files.Where(file => Path.GetFileName(file).StartsWith("Bloods-")).ToArray();

                // Überprüfe, ob es gespeicherte Dateien gibt
                if (files.Length == 0)
                {
                    await ctx.CreateResponseAsync("Es wurden keine gespeicherten Dateien gefunden.");
                    return;
                }

                // Sortiere die Dateinamen alphabetisch
                Array.Sort(files);

                // Erstelle ein Embed für die Liste der gespeicherten Dateien
                var embed = new DiscordEmbedBuilder
                {
                    Title = "Gespeicherte Statistikdateien",
                    Color = DiscordColor.Blue
                };

                var sb = new StringBuilder();

                // Füge die sortierten Dateinamen zum StringBuilder hinzu
                for (int i = 0; i < files.Length; i++)
                {
                    sb.AppendLine($"{i + 1}. {Path.GetFileName(files[i])}");
                }

                embed.Description = sb.ToString();

                // Sende das Embed mit der sortierten Liste der gespeicherten Dateien
                await ctx.CreateResponseAsync(embed: embed);
            }
            catch (Exception ex)
            {
                // Bei einem Fehler Sende eine Fehlermeldung
                await ctx.CreateResponseAsync($"Fehler beim Abrufen der Liste gespeicherter Dateien: {ex.Message}");
            }
        }

        [SlashCommand("help", "Gibt eine Liste aller vorhandenen Befehle aus.")]
        [RequireRoles(RoleCheckMode.Any, "Techniker")] // Benutzer mit der Rolle "DeineRolleName" können diesen Befehl verwenden
        public async Task HelpCommand(InteractionContext ctx)
        {
            if (!ctx.Member.Roles.Any(r => r.Name == "Techniker"))
            {
                await ctx.CreateResponseAsync("Du hast nicht die erforderliche Rolle, um diesen Befehl auszuführen.");
                return;
            }

            var embed = new DiscordEmbedBuilder
            {
                Title = "Befehlsübersicht",
                Color = DiscordColor.Brown
            };

            embed.Description = "Hier ist eine Liste aller verfügbaren Befehle und ihrer Beschreibungen:";

            // Hinzufügen von Befehlen und Beschreibungen
            embed.AddField("!abgabe [Betrag]", "Setzt die globale Abgabe für alle Benutzer.");
            embed.AddField("!kurs [Kurs]", "Setzt den globalen Kurs für alle Benutzer.");
            embed.AddField("!reset", "Setzt die Punkte aller Benutzer zurück.");
            embed.AddField("!saves", "Zeigt eine Liste der gespeicherten Statistikdateien an.");
            embed.AddField("!save", "Speichert die aktuellen Statistiken in einer Datei.");
            embed.AddField("!load [Dateinamen]", "Lädt Statistiken aus einer zuvor gespeicherten Datei.");
            embed.AddField("\u200B", "\u200B", true);
            embed.AddField("!register [Name]", "Registriert einen neuen Benutzer.");
            embed.AddField("!delete [Name]", "Löscht einen registrierten Benutzer.");
            embed.AddField("!legende [Name]", "Schaltet den Legendentitel für einen Benutzer um.");
            embed.AddField("\u200B", "\u200B", true);
            embed.AddField("!add [Name] [Anzahl]", "Fügt dem angegebenen Benutzer Abgaben hinzu.");
            embed.AddField("!remove [Name] [Anzahl]", "Entfernt dem angegebenen Benutzer Abgaben.");
            embed.AddField("\u200B", "\u200B", true);
            embed.AddField("!stats", "Zeigt eine Übersicht aller Benutzerstatistiken an.");
            embed.AddField("!user [Name]", "Zeigt die Statistiken eines bestimmten Benutzers an.");
            embed.AddField("!top", "Zeigt eine Rangliste der Mitglieder basierend auf gesammelten Punkten an.");
            embed.AddField("!legends", "Zeigt eine Liste der Mitglieder mit Legendenstatus an.");
            embed.AddField("!hdw", "Zeigt den 'Hustler der Woche' basierend auf den gesammelten Punkten an.");
            embed.AddField("!paycheck", "Zeigt die Beträge für alle Mitglieder an.");
            embed.AddField("!sanktion", "Zeigt eine Übersicht der ausstehenden Sanktionen an.");

            await ctx.CreateResponseAsync(embed: embed);
        }

        [SlashCommand("deleteall", "Löscht alle registrierten Benutzer.")]
        [RequireRoles(RoleCheckMode.Any, "Techniker")]
        public async Task DeleteAllUsersCommand(InteractionContext ctx)
        {
            if (!ctx.Member.Roles.Any(r => r.Name == "Techniker"))
            {
                await ctx.CreateResponseAsync("Du hast nicht die erforderliche Rolle, um diesen Befehl auszuführen.");
                return;
            }

            // Lese alle Benutzer aus der Datei
            UserData[] users = ReadAllUsers();

            if (users.Length == 0)
            {
                await ctx.CreateResponseAsync("Es gibt keine registrierten Benutzer zum Löschen.");
                return;
            }

            // Lösche alle Benutzer und speichere die aktualisierte Benutzerliste
            DeleteAllUsers();

            // Erstelle ein Embed für die Bestätigung
            var embed = new DiscordEmbedBuilder
            {
                Title = "Alle Benutzer gelöscht",
                Color = DiscordColor.Green
            };

            embed.Description = "Alle registrierten Benutzer wurden gelöscht.";

            // Sende das Embed mit der Bestätigung
            await ctx.CreateResponseAsync(embed: embed);
        }

        // Methode zum Löschen aller Benutzer
        private void DeleteAllUsers()
        {
            // Überschreibe die Datei mit einer leeren Liste von Benutzern
            SaveAllUsers(new UserData[0]);
        }


        private void DeleteUser(UserData user)
        {
            UserData[] users;

            if (File.Exists(JsonFilePath))
            {
                string json = File.ReadAllText(JsonFilePath);
                users = JsonConvert.DeserializeObject<UserData[]>(json);

                // Entferne den Benutzer aus der Liste
                users = users.Where(u => !u.Name.Equals(user.Name, StringComparison.OrdinalIgnoreCase)).ToArray();
            }
            else
            {
                // Kein Benutzer zum Löschen, da die Datei nicht existiert
                return;
            }

            // Speichere die aktualisierte Benutzerliste
            string updatedJson = JsonConvert.SerializeObject(users, Formatting.Indented);
            File.WriteAllText(JsonFilePath, updatedJson);
        }

        private UserData GetUserByName(string name)
        {
            // Lese alle Benutzer aus der Datei
            UserData[] users = ReadAllUsers();

            // Finde den Benutzer in der Liste
            return users.FirstOrDefault(u => u.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
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

        private void SaveAllUsers(UserData[] users)
        {
            // Speichere die aktualisierte Benutzerliste
            string updatedJson = JsonConvert.SerializeObject(users, Formatting.Indented);
            File.WriteAllText(JsonFilePath, updatedJson);
        }

        private void SaveUser(UserData userData)
        {
            // Lese alle Benutzer aus der Datei
            UserData[] users = ReadAllUsers();

            // Aktualisiere oder füge den Benutzer hinzu
            UserData existingUser = users.FirstOrDefault(u => u.Name.Equals(userData.Name, StringComparison.OrdinalIgnoreCase));
            if (existingUser != null)
            {
                existingUser.Gesammelt = userData.Gesammelt;
                existingUser.Betrag = userData.Betrag;
                existingUser.Legendentitel = userData.Legendentitel;
            }
            else
            {
                users = users.Append(userData).ToArray();
            }

            // Speichere die aktualisierte Benutzerliste
            string updatedJson = JsonConvert.SerializeObject(users, Formatting.Indented);
            File.WriteAllText(JsonFilePath, updatedJson);
        }
    }

    public class UserData
    {
        public string Name { get; set; }
        public decimal Gesammelt { get; set; }
        public decimal Betrag { get; set; }
        public bool Legendentitel { get; set; }
    }
}


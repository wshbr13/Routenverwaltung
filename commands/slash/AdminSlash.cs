using DSharpPlus;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using DSharpPlus.SlashCommands;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace routenVerwaltung.commands.slash
{
    public class AdminSlash : ApplicationCommandModule
    {
        private const string JsonFilePath = "user_data.json";
        public static decimal GlobalAbgabe = 500; // Globale Variable für Abgabe
        public static decimal GlobalKurs = 47;   // Globale Variable für Kurs

        [SlashCommand("ahelp", "Zeigt eine Liste aller verfügbaren Befehle und deren Beschreibungen an.")]
        [RequireRoles(RoleCheckMode.Any, "Techniker")] // Benutzer mit der Rolle "DeineRolleName" können diesen Befehl verwenden
        public async Task HelpCommand(InteractionContext ctx)
        {
            var embed = new DiscordEmbedBuilder
            {
                Title = "Hilfe",
                Description = "Hier ist eine Liste aller verfügbaren Befehle und deren Beschreibungen:",
                Color = DiscordColor.Gold
            };

            embed.AddField("Command: /abgabe", "Setzt die globale Abgabe.");
            embed.AddField("Command: /kurs", "Setzt den globalen Kurs.");
            embed.AddField("Command: /reset", "Setzt die Punkte aller Benutzer zurück.");
            embed.AddField("\u200B", "\u200B");
            embed.AddField("Command: /register", "Registriert einen neuen Benutzer.");
            embed.AddField("Command: /delete", "Löscht einen registrierten Benutzer.");
            embed.AddField("Command: /deleteall", "Löscht alle registrierten Benutzer.");
            embed.AddField("Command: /legende", "Schaltet den Legendentitel für einen Benutzer um.");
            embed.AddField("\u200B", "\u200B");
            embed.AddField("Command: /save", "Speichert die aktuellen Statistiken in einer Datei.");
            embed.AddField("Command: /load", "Lädt Statistiken aus einer zuvor gespeicherten Datei.");
            embed.AddField("Command: /saves", "Zeigt eine Liste der gespeicherten Statistikdateien an.");
            embed.AddField("\u200B", "\u200B");
            embed.AddField("Command: /remove", "Entfernt dem angegebenen Benutzer Abgaben.");
            embed.AddField("Command: /add", "Fügt dem angegebenen Benutzer Abgaben hinzu.");
            embed.AddField("\u200B", "\u200B");
            embed.AddField("Command: /sanktion", "Zeigt eine Übersicht der ausstehenden Sanktionen an.");
            embed.AddField("Command: /stats", "Zeigt eine Übersicht aller Benutzerstatistiken an.");
            embed.AddField("Command: /paycheck", "Zeigt die Beträge für alle Mitglieder an.");

            await ctx.CreateResponseAsync(embed: embed);
        }


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

        public async Task SetGlobalKursCommand(InteractionContext ctx, [Option("kurs", "Der neue Kurs.")] double kurs)
        {
            if (!ctx.Member.Roles.Any(r => r.Name == "Techniker"))
            {
                await ctx.CreateResponseAsync("Du hast nicht die erforderliche Rolle, um diesen Befehl auszuführen.");
                return;
            }

            GlobalKurs = (decimal)kurs;
            await ctx.CreateResponseAsync($"Globaler Kurs auf {kurs} festgelegt.");
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

        [SlashCommand("delete", "Löscht einen registrierten Benutzer.")]
        [RequireRoles(RoleCheckMode.Any, "Techniker")] // Benutzer mit der Rolle "DeineRolleName" können diesen Befehl verwenden
        public async Task DeleteUserCommand(InteractionContext ctx, [Option("name", "Der Name des Benutzers")] string inputName)
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

        [SlashCommand("legende", "Schaltet den Legendentitel für einen Benutzer um.")]
        [RequireRoles(RoleCheckMode.Any, "Techniker")] // Benutzer mit der Rolle "DeineRolleName" können diesen Befehl verwenden
        public async Task SetLegendeCommand(InteractionContext ctx, [Option("name", "Der Name des Benutzers")] string inputName)
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

        [SlashCommand("reset", "Setzt die Punkte aller Benutzer zurück.")]
        [RequireRoles(RoleCheckMode.Any, "Techniker")]
        public async Task ResetPointsCommand(InteractionContext ctx)
        {
            // Vor dem Zurücksetzen der Punkte die Statistiken speichern
            await SaveStatsCommand(ctx);

            // Überprüfe, ob der Benutzer die erforderliche Rolle hat
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
                Description = "Alle Punkte wurden zurückgesetzt.",
                Color = DiscordColor.Green
            };

            // Sende das Embed mit der Bestätigung
            await ctx.CreateResponseAsync(embed: embed);

            // Sende eine Nachricht in den festgelegten Kanal
            ulong channelId = 1187727222883238010; // channelId ist die ID des Kanals, in den die Nachricht gesendet werden soll
            var channel = ctx.Guild.GetChannel(channelId);
            if (channel != null)
            {
                var messageBuilder = new DiscordMessageBuilder()
                    .WithEmbed(embed)
                    .WithContent("Die Woche ist abgeschlossen, die Statistiken wurden zurückgesetzt! Viel Spaß in der neuen Woche!");

                await channel.SendMessageAsync(messageBuilder);
            }
            else
            {
                // Falls der Kanal nicht gefunden wurde, eine Fehlermeldung senden
                await ctx.CreateResponseAsync($"Der Kanal wurde nicht gefunden. Die Nachricht konnte nicht gesendet werden.");
            }
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
            string fileName = $"Bilanzen/OBLOCK-{DateTime.Now:dd-MM-yyyy}.txt";

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
                await ctx.CreateResponseAsync($"Die Bilanz der Woche wurde erfolgreich gespeichert!");
            }
            catch (Exception ex)
            {
                // Bei einem Fehler Sende eine Fehlermeldung
                await ctx.CreateResponseAsync($"Fehler beim Speichern der Statistiken: {ex.Message}");
            }
        }

        [SlashCommand("load", "Lädt Statistiken aus einer zuvor gespeicherten Datei.")]
        [RequireRoles(RoleCheckMode.Any, "Techniker")]
        public async Task LoadStatsCommand(InteractionContext ctx, [Option("Text", "Dateiname oder Nummer")] string input)
        {
            if (!ctx.Member.Roles.Any(r => r.Name == "Techniker"))
            {
                await ctx.CreateResponseAsync("Du hast nicht die erforderliche Rolle, um diesen Befehl auszuführen.");
                return;
            }

            try
            {
                string bilanzenFolderPath = Path.Combine(Directory.GetCurrentDirectory(), "Bilanzen");

                string[] files = Directory.GetFiles(bilanzenFolderPath, "*.txt");
                files = files.Where(file => Path.GetFileName(file).StartsWith("OBLOCK-")).ToArray();

                if (files.Length == 0)
                {
                    await ctx.CreateResponseAsync("Es wurden keine gespeicherten Dateien gefunden.");
                    return;
                }

                Array.Sort(files, (a, b) => DateTime.Compare(GetFileCreationDate(b), GetFileCreationDate(a)));

                string selectedFile = "";

                if (int.TryParse(input, out int index) && index > 0 && index <= files.Length)
                {
                    selectedFile = files[index - 1];
                }
                else
                {
                    selectedFile = files.FirstOrDefault(file => Path.GetFileName(file) == input);
                }

                if (selectedFile == null)
                {
                    await ctx.CreateResponseAsync($"Die Datei '{input}' existiert nicht oder die Eingabe ist ungültig.");
                    return;
                }

                string[] lines = File.ReadAllLines(selectedFile);

                var embed = new DiscordEmbedBuilder
                {
                    Title = "Geladene Statistiken",
                    Color = DiscordColor.Blue
                };

                var sb = new StringBuilder();

                foreach (string line in lines)
                {
                    sb.AppendLine(line);
                }

                embed.Description = sb.ToString();

                await ctx.CreateResponseAsync(embed: embed);
            }
            catch (Exception ex)
            {
                await ctx.CreateResponseAsync($"Fehler beim Laden der Statistiken: {ex.Message}");
            }
        }

        // Hilfsmethode zur Extraktion des Erstellungsdatums aus dem Dateinamen
        private DateTime GetFileCreationDate(string fileName)
        {
            string datePart = Path.GetFileNameWithoutExtension(fileName).Replace("OBLOCK-", "");
            return DateTime.ParseExact(datePart, "dd-MM-yyyy", CultureInfo.InvariantCulture);
        }


        // Hilfsmethode zur Extraktion des Datums aus dem Dateinamen
        private DateTime GetFileDate(string fileName)
        {
            string datePart = Path.GetFileNameWithoutExtension(fileName).Replace("OBLOCK-", "");
            return DateTime.ParseExact(datePart, "dd-MM-yyyy", CultureInfo.InvariantCulture);
        }


        [SlashCommand("saves", "Zeigt eine Liste der gespeicherten Statistikdateien an.")]
        [RequireRoles(RoleCheckMode.Any, "Techniker")]
        public async Task ListSavedStatsFiles(InteractionContext ctx)
        {
            if (!ctx.Member.Roles.Any(r => r.Name == "Techniker"))
            {
                await ctx.CreateResponseAsync("Du hast nicht die erforderliche Rolle, um diesen Befehl auszuführen.");
                return;
            }

            try
            {
                // Angepasster Pfad zum Ordner "Bilanzen"
                string bilanzenFolderPath = Path.Combine(Directory.GetCurrentDirectory(), "Bilanzen");

                // Suche alle .txt-Dateien im Ordner "Bilanzen"
                string[] files = Directory.GetFiles(bilanzenFolderPath, "*.txt");

                // Filtere nach Dateien, die mit "OBLOCK-" beginnen
                files = files.Where(file => Path.GetFileName(file).StartsWith("OBLOCK-")).ToArray();

                // Überprüfe, ob es gespeicherte Dateien gibt
                if (files.Length == 0)
                {
                    await ctx.CreateResponseAsync("Es wurden keine gespeicherten Dateien gefunden.");
                    return;
                }

                // Sortiere die Dateinamen nach dem Datum im Dateinamen (neuestes Datum zuerst)
                Array.Sort(files, (a, b) => DateTime.Compare(GetFileDateSave(b), GetFileDateSave(a)));

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

        // Hilfsmethode zur Extraktion des Datums aus dem Dateinamen
        private DateTime GetFileDateSave(string fileName)
        {
            string datePart = Path.GetFileNameWithoutExtension(fileName).Replace("OBLOCK-", "");
            return DateTime.ParseExact(datePart, "dd-MM-yyyy", CultureInfo.InvariantCulture);
        }


        [SlashCommand("remove", "Entfernt dem angegebenen Benutzer Abgaben.")]
        [RequireRoles(RoleCheckMode.Any, "Techniker")] // Benutzer mit der Rolle "DeineRolleName" können diesen Befehl verwenden
        public async Task RemovePointsCommand(InteractionContext ctx, [Option("name", "Der Name des Benutzers")] string inputName, [Option("Anzahl", "Anzahl der Abgabe")] double points)
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
            if (userData.Gesammelt < (decimal)points)
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
            userData.Gesammelt -= (decimal)points;
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

        [SlashCommand("add", "Fügt dem angegebenen Benutzer Abgaben hinzu.")]
        [RequireRoles(RoleCheckMode.Any, "Techniker")] // Benutzer mit der Rolle "DeineRolleName" können diesen Befehl verwenden
        public async Task AddPointsCommand(InteractionContext ctx, [Option("name", "Der Name des Benutzers")] string inputName, [Option("Anzahl", "Anzahl der Abgabe")] double points)
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
            userData.Gesammelt += (decimal)points;
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
}


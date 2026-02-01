/*
 * Services Hub User Extractor
 * 
 * Outil d'extraction des utilisateurs depuis Microsoft Services Hub.
 * Utilise WebView2 pour l'authentification SSO et récupère les données via l'API interne.
 * 
 * Auteur: [Votre nom]
 * Version: 1.0
 */

using CsvHelper;
using System.Globalization;
using System.Text.Json;
using Microsoft.Extensions.Configuration;

namespace GetServicesHubUser
{
    /// <summary>
    /// Point d'entrée principal de l'application.
    /// Gère le menu interactif, l'extraction des utilisateurs et l'export CSV.
    /// </summary>
    public class Program
    {
        /// <summary>
        /// Point d'entrée de l'application. Requiert un thread STA pour WebView2.
        /// </summary>
        [STAThread]
        public static void Main(string[] args)
        {
            MainAsync(args).GetAwaiter().GetResult();
        }

        /// <summary>
        /// Logique principale asynchrone : affiche le menu et lance l'extraction.
        /// </summary>
        public static async Task MainAsync(string[] args)
        {
            // Chargement de la configuration à partir du fichier appsettings.json
            var config = new ConfigurationBuilder()
                .SetBasePath(AppContext.BaseDirectory)
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .Build();   
            AppSettings appSettings = config.Get<AppSettings>()
                ?? throw new InvalidOperationException("Configuration appsettings.json invalide ou manquante");

            // Charger les workspaces depuis le fichier local (%APPDATA%)
            var workspaceList = WorkspaceManager.LoadWorkspaces();

            // Si la liste est vide, proposer d'importer depuis appsettings.json
            if (workspaceList.Count == 0 && appSettings.Workspaces.Count > 0)
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine("Aucun workspace dans votre liste personnelle.");
                Console.ResetColor();
                Console.ForegroundColor = ConsoleColor.Magenta;
                Console.Write("? ");
                Console.ForegroundColor = ConsoleColor.White;
                Console.Write($"Importer {appSettings.Workspaces.Count} workspace(s) depuis la configuration ? ");
                Console.ForegroundColor = ConsoleColor.DarkGray;
                Console.Write("[Y/n] ");
                Console.ResetColor();
                var importAnswer = Console.ReadLine();
                
                if (importAnswer == "Y" || importAnswer == "y" || string.IsNullOrEmpty(importAnswer))
                {
                    int imported = WorkspaceManager.ImportFromAppSettings(appSettings.Workspaces);
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine($"  ✓ {imported} workspace(s) importé(s)");
                    Console.ResetColor();
                    workspaceList = WorkspaceManager.LoadWorkspaces();
                }
            }

            // Afficher le chemin du fichier de workspaces
            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.WriteLine($"Fichier workspaces : {WorkspaceManager.GetWorkspacesFilePath()}");
            Console.ResetColor();

            var working = false;
            List<Value> resultAllup = [];

            // Menu de sélection
            Console.WriteLine();
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("═══════════════════════════════════════════════════════");
            Console.WriteLine("       Services Hub User Extractor");
            Console.WriteLine("═══════════════════════════════════════════════════════");
            Console.ResetColor();
            Console.WriteLine();
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine("Que souhaitez-vous faire ?");
            Console.ResetColor();
            Console.WriteLine();
            Console.ForegroundColor = ConsoleColor.DarkCyan;
            Console.Write("  [1] ");
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine("Extraire tous les utilisateurs de Services Hub");
            Console.ForegroundColor = ConsoleColor.DarkCyan;
            Console.Write("  [2] ");
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine("Extraire les utilisateurs de workspaces spécifiques");
            Console.ForegroundColor = ConsoleColor.DarkCyan;
            Console.Write("  [3] ");
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine("Gérer les workspaces");
            Console.WriteLine();
            
            // Afficher la liste des workspaces disponibles
            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.WriteLine("Workspaces disponibles :");
            foreach (var ws in workspaceList)
            {
                Console.WriteLine($"    • {ws.Name} ({ws.Id})");
            }
            Console.ResetColor();
            Console.WriteLine();
            
            Console.ForegroundColor = ConsoleColor.Magenta;
            Console.Write("? ");
            Console.ForegroundColor = ConsoleColor.White;
            Console.Write("Votre choix : ");
            Console.ResetColor();
            string? choix = Console.ReadLine()?.Trim();

            // Par défaut, option 1 si l'utilisateur appuie juste sur Entrée
            if (string.IsNullOrEmpty(choix)) choix = "1";

            List<WorkspaceInfo> selectedWorkspaces = new();
            List<WorkspaceInfo> unknownWorkspaces = new(); // Pour stocker les workspaces inconnus qui fonctionnent

            switch (choix)
            {
                case "1":
                    selectedWorkspaces = workspaceList.ToList();
                    break;

                case "2":
                    Console.WriteLine();
                    Console.ForegroundColor = ConsoleColor.DarkGray;
                    Console.WriteLine("Entrez les IDs des workspaces séparés par \";\"");
                    Console.WriteLine("Exemple: db7f4202-bc75-47ae-a6a9-1ad6ec518c5c;5e6f9931-58a0-4daa-a9a5-6154680348d4");
                    Console.ResetColor();
                    Console.ForegroundColor = ConsoleColor.Magenta;
                    Console.Write("? ");
                    Console.ForegroundColor = ConsoleColor.White;
                    Console.Write("IDs : ");
                    Console.ResetColor();
                    string? idsInput = Console.ReadLine();

                    if (!string.IsNullOrEmpty(idsInput))
                    {
                        var ids = idsInput.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                        foreach (var id in ids)
                        {
                            var found = workspaceList.FirstOrDefault(w => w.Id.Equals(id, StringComparison.OrdinalIgnoreCase));
                            if (found != null)
                            {
                                selectedWorkspaces.Add(found);
                                Console.ForegroundColor = ConsoleColor.Green;
                                Console.WriteLine($"  ✓ {found.Name}");
                                Console.ResetColor();
                            }
                            else
                            {
                                // Workspace inconnu - on l'ajoute quand même pour tester
                                var unknownWs = new WorkspaceInfo($"Inconnu_{id.Substring(0, Math.Min(8, id.Length))}", id);
                                selectedWorkspaces.Add(unknownWs);
                                unknownWorkspaces.Add(unknownWs);
                                Console.ForegroundColor = ConsoleColor.Yellow;
                                Console.WriteLine($"  ? ID inconnu, sera testé : {id}");
                                Console.ResetColor();
                            }
                        }
                    }

                    if (selectedWorkspaces.Count == 0)
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine("Aucun workspace sélectionné.");
                        Console.ResetColor();
                        return;
                    }
                    break;

                case "3":
                    ManageWorkspaces(ref workspaceList);
                    return;

                default:
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("Choix non reconnu.");
                    Console.ResetColor();
                return;
            }

            // Lancer l'extraction
            working = true;
            List<string> successfulUnknownIds = new(); // IDs inconnus qui ont réussi
            var apiResponses = await WebViewCookieHelper.GetAllWorkspacesDataAsync(
                selectedWorkspaces, 
                appSettings.Api.LoginPageUrlFormat, 
                appSettings.Api.ApiUrlFormat);

            foreach (var apiResponse in apiResponses)
                {
                    if (!string.IsNullOrEmpty(apiResponse))
                    {
                        var json = apiResponse;
                        string workspaceName = "Inconnu";

                        // Extraire le nom du workspace
                        if (json.StartsWith("WORKSPACE:"))
                        {
                            var wsEndIdx = json.IndexOf("|STATUS:");
                            if (wsEndIdx > 10)
                            {
                                workspaceName = json.Substring(10, wsEndIdx - 10);
                                json = json.Substring(wsEndIdx + 1); // Garde "STATUS:..."
                            }
                        }

                        // Extraire le JSON après "STATUS:xxx|"
                        if (json.StartsWith("STATUS:"))
                        {
                            var idx = json.IndexOf('|');
                            if (idx >= 0) json = json.Substring(idx + 1);
                        }

                        try
                        {
                            var root = JsonSerializer.Deserialize<Root>(json);
                            var users = root?.values ?? Enumerable.Empty<Value>();
                            // Filtrer les contacts @microsoft.com
                            var filteredUsers = users.Where(u => 
                                string.IsNullOrEmpty(u.accountName) || 
                                !u.accountName.EndsWith("@microsoft.com", StringComparison.OrdinalIgnoreCase));
                            int excluded = users.Count() - filteredUsers.Count();
                            foreach (var user in filteredUsers)
                            {
                                resultAllup.Add(user);
                            }
                            Console.ForegroundColor = ConsoleColor.DarkGray;
                            Console.Write($"    └─ ");
                            Console.ForegroundColor = ConsoleColor.White;
                            Console.Write($"{filteredUsers.Count()} utilisateurs");
                            if (excluded > 0)
                            {
                                Console.ForegroundColor = ConsoleColor.DarkYellow;
                                Console.Write($" ({excluded} @microsoft.com exclus)");
                            }
                            Console.WriteLine();
                            Console.ResetColor();

                            // Si c'est un workspace inconnu qui a réussi, le marquer
                            var unknownWs = unknownWorkspaces.FirstOrDefault(w => w.Name == workspaceName);
                            if (unknownWs != null && filteredUsers.Any())
                            {
                                successfulUnknownIds.Add(unknownWs.Id);
                            }
                        }
                        catch (Exception ex)
                        {
                            Console.ForegroundColor = ConsoleColor.Red;
                            Console.WriteLine($"    └─ Erreur : {ex.Message}");
                            Console.ForegroundColor = ConsoleColor.DarkGray;
                            Console.WriteLine($"       {json.Substring(0, Math.Min(80, json.Length))}...");
                            Console.ResetColor();
                        }
                    }
                }

            // Proposer d'ajouter les workspaces inconnus qui ont fonctionné
            if (successfulUnknownIds.Count > 0)
            {
                Console.WriteLine();
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine($"═══ {successfulUnknownIds.Count} workspace(s) inconnu(s) ont fonctionné ═══");
                Console.ResetColor();
                
                foreach (var wsId in successfulUnknownIds)
                {
                    Console.WriteLine();
                    Console.ForegroundColor = ConsoleColor.Magenta;
                    Console.Write("? ");
                    Console.ForegroundColor = ConsoleColor.White;
                    Console.Write($"Ajouter {wsId} aux workspaces connus ? ");
                    Console.ForegroundColor = ConsoleColor.DarkGray;
                    Console.Write("[Y/n] ");
                    Console.ResetColor();
                    var addAnswer = Console.ReadLine();
                    
                    if (addAnswer == "Y" || addAnswer == "y")
                    {
                        Console.ForegroundColor = ConsoleColor.Magenta;
                        Console.Write("? ");
                        Console.ForegroundColor = ConsoleColor.White;
                        Console.Write("Nom du workspace : ");
                        Console.ResetColor();
                        var wsName = Console.ReadLine();
                        
                        if (!string.IsNullOrWhiteSpace(wsName))
                        {
                            if (WorkspaceManager.AddWorkspace(wsName, wsId))
                            {
                                Console.ForegroundColor = ConsoleColor.Green;
                                Console.WriteLine($"  ✓ Workspace \"{wsName}\" ajouté à votre liste");
                                Console.ResetColor();
                            }
                        }
                    }
                }
            }

            if (working)
            {
                Console.WriteLine();
                Console.ForegroundColor = ConsoleColor.Cyan;
                Console.WriteLine($"═══ Résumé : {resultAllup.Count} utilisateurs au total ═══");
                Console.ResetColor();
                Console.WriteLine();
                Console.ForegroundColor = ConsoleColor.Magenta;
                Console.Write("? ");
                Console.ForegroundColor = ConsoleColor.White;
                Console.Write("Générer un fichier CSV ? ");
                Console.ForegroundColor = ConsoleColor.DarkGray;
                Console.Write("[Y/n] ");
                Console.ResetColor();
                var answer = Console.ReadLine();
                if(answer == "Y" || answer == "y")
                    {
                    // Enregistrement des résultats dans un fichier CSV
                    string sMonth = DateTime.Now.ToString("MMyyyy");
                    string csvFolder = appSettings.Output.CsvFolderPath;
                    if (csvFolder == "%DOWNLOADS%")
                    {
                        csvFolder = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
                        // Pour le dossier Téléchargements (Downloads) :
                        csvFolder = Path.Combine(csvFolder, "Downloads");
                    }
                    string csvName = string.Format(appSettings.Output.CsvNameFormat, sMonth);
                    string csvPath = Path.Combine(csvFolder, csvName + ".csv");
                    using (var textWriter = new StreamWriter(csvPath))
                    {
                        var writer = new CsvWriter(textWriter, CultureInfo.InvariantCulture);
                        writer.WriteRecords(resultAllup);
                    }
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.Write("✓ ");
                    Console.ForegroundColor = ConsoleColor.White;
                    Console.WriteLine($"Export terminé : {csvPath}");
                    Console.ResetColor();
                }
                Console.WriteLine();
                Console.ForegroundColor = ConsoleColor.DarkGray;
                Console.WriteLine("Appuyez sur une touche pour quitter...");
                Console.ResetColor();
                Console.ReadKey();
            }
        }

        /// <summary>
        /// Gestion des workspaces (ajout, suppression, modification)
        /// </summary>
        private static void ManageWorkspaces(ref List<WorkspaceInfo> workspaceList)
        {
            bool continueManaging = true;
            while (continueManaging)
            {
                Console.WriteLine();
                Console.ForegroundColor = ConsoleColor.Cyan;
                Console.WriteLine("═══ Gestion des workspaces ═══");
                Console.ResetColor();
                Console.WriteLine();
                Console.ForegroundColor = ConsoleColor.DarkCyan;
                Console.Write("  [A] ");
                Console.ForegroundColor = ConsoleColor.White;
                Console.WriteLine("Ajouter un workspace");
                Console.ForegroundColor = ConsoleColor.DarkCyan;
                Console.Write("  [S] ");
                Console.ForegroundColor = ConsoleColor.White;
                Console.WriteLine("Supprimer un workspace");
                Console.ForegroundColor = ConsoleColor.DarkCyan;
                Console.Write("  [M] ");
                Console.ForegroundColor = ConsoleColor.White;
                Console.WriteLine("Modifier un workspace");
                Console.ForegroundColor = ConsoleColor.DarkCyan;
                Console.Write("  [L] ");
                Console.ForegroundColor = ConsoleColor.White;
                Console.WriteLine("Lister les workspaces");
                Console.ForegroundColor = ConsoleColor.DarkCyan;
                Console.Write("  [Q] ");
                Console.ForegroundColor = ConsoleColor.White;
                Console.WriteLine("Quitter la gestion");
                Console.WriteLine();

                Console.ForegroundColor = ConsoleColor.Magenta;
                Console.Write("? ");
                Console.ForegroundColor = ConsoleColor.White;
                Console.Write("Action : ");
                Console.ResetColor();
                var action = Console.ReadLine()?.Trim().ToUpper();

                switch (action)
                {
                    case "A":
                        Console.ForegroundColor = ConsoleColor.Magenta;
                        Console.Write("? ");
                        Console.ForegroundColor = ConsoleColor.White;
                        Console.Write("Nom du workspace : ");
                        Console.ResetColor();
                        var newName = Console.ReadLine();

                        Console.ForegroundColor = ConsoleColor.Magenta;
                        Console.Write("? ");
                        Console.ForegroundColor = ConsoleColor.White;
                        Console.Write("ID du workspace : ");
                        Console.ResetColor();
                        var newId = Console.ReadLine();

                        if (!string.IsNullOrWhiteSpace(newName) && !string.IsNullOrWhiteSpace(newId))
                        {
                            if (WorkspaceManager.AddWorkspace(newName, newId))
                            {
                                Console.ForegroundColor = ConsoleColor.Green;
                                Console.WriteLine($"  ✓ Workspace \"{newName}\" ajouté");
                                Console.ResetColor();
                                workspaceList = WorkspaceManager.LoadWorkspaces();
                            }
                        }
                        break;

                    case "S":
                        Console.WriteLine();
                        Console.ForegroundColor = ConsoleColor.DarkGray;
                        Console.WriteLine("Workspaces actuels :");
                        for (int i = 0; i < workspaceList.Count; i++)
                        {
                            Console.WriteLine($"  [{i + 1}] {workspaceList[i].Name} ({workspaceList[i].Id})");
                        }
                        Console.ResetColor();
                        Console.WriteLine();
                        Console.ForegroundColor = ConsoleColor.Magenta;
                        Console.Write("? ");
                        Console.ForegroundColor = ConsoleColor.White;
                        Console.Write("Numéro à supprimer : ");
                        Console.ResetColor();
                        if (int.TryParse(Console.ReadLine(), out int delIndex) && delIndex >= 1 && delIndex <= workspaceList.Count)
                        {
                            var wsToDelete = workspaceList[delIndex - 1];
                            Console.ForegroundColor = ConsoleColor.Yellow;
                            Console.Write($"  Supprimer \"{wsToDelete.Name}\" ? [Y/n] ");
                            Console.ResetColor();
                            if (Console.ReadLine()?.Trim().ToUpper() == "Y")
                            {
                                if (WorkspaceManager.RemoveWorkspace(wsToDelete.Id))
                                {
                                    Console.ForegroundColor = ConsoleColor.Green;
                                    Console.WriteLine($"  ✓ Workspace supprimé");
                                    Console.ResetColor();
                                    workspaceList = WorkspaceManager.LoadWorkspaces();
                                }
                            }
                        }
                        break;

                    case "M":
                        Console.WriteLine();
                        Console.ForegroundColor = ConsoleColor.DarkGray;
                        Console.WriteLine("Workspaces actuels :");
                        for (int i = 0; i < workspaceList.Count; i++)
                        {
                            Console.WriteLine($"  [{i + 1}] {workspaceList[i].Name} ({workspaceList[i].Id})");
                        }
                        Console.ResetColor();
                        Console.WriteLine();
                        Console.ForegroundColor = ConsoleColor.Magenta;
                        Console.Write("? ");
                        Console.ForegroundColor = ConsoleColor.White;
                        Console.Write("Numéro à modifier : ");
                        Console.ResetColor();
                        if (int.TryParse(Console.ReadLine(), out int modIndex) && modIndex >= 1 && modIndex <= workspaceList.Count)
                        {
                            var wsToModify = workspaceList[modIndex - 1];
                            Console.ForegroundColor = ConsoleColor.Magenta;
                            Console.Write("? ");
                            Console.ForegroundColor = ConsoleColor.White;
                            Console.Write($"Nouveau nom [{wsToModify.Name}] : ");
                            Console.ResetColor();
                            var modName = Console.ReadLine();

                            Console.ForegroundColor = ConsoleColor.Magenta;
                            Console.Write("? ");
                            Console.ForegroundColor = ConsoleColor.White;
                            Console.Write($"Nouvel ID [{wsToModify.Id}] : ");
                            Console.ResetColor();
                            var modId = Console.ReadLine();

                            // Supprimer l'ancien et ajouter le nouveau
                            WorkspaceManager.RemoveWorkspace(wsToModify.Id);
                            WorkspaceManager.AddWorkspace(
                                string.IsNullOrWhiteSpace(modName) ? wsToModify.Name : modName,
                                string.IsNullOrWhiteSpace(modId) ? wsToModify.Id : modId
                            );
                            Console.ForegroundColor = ConsoleColor.Green;
                            Console.WriteLine($"  ✓ Workspace modifié");
                            Console.ResetColor();
                            workspaceList = WorkspaceManager.LoadWorkspaces();
                        }
                        break;

                    case "L":
                        Console.WriteLine();
                        Console.ForegroundColor = ConsoleColor.Cyan;
                        Console.WriteLine($"═══ {workspaceList.Count} workspace(s) ═══");
                        Console.ResetColor();
                        foreach (var ws in workspaceList)
                        {
                            Console.ForegroundColor = ConsoleColor.White;
                            Console.Write($"  • {ws.Name}");
                            Console.ForegroundColor = ConsoleColor.DarkGray;
                            Console.WriteLine($" ({ws.Id})");
                        }
                        Console.ResetColor();
                        break;

                    case "Q":
                    case "":
                    case null:
                        continueManaging = false;
                        break;

                    default:
                        Console.ForegroundColor = ConsoleColor.Yellow;
                        Console.WriteLine("  Action non reconnue");
                        Console.ResetColor();
                        break;
                }
            }

            Console.WriteLine();
            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.WriteLine("Appuyez sur une touche pour quitter...");
            Console.ResetColor();
            Console.ReadKey();
        }
    }
}
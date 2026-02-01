using System.Text.Json;

namespace GetServicesHubUser
{
    /// <summary>
    /// Gère les workspaces stockés dans %APPDATA%\ServicesHubExtractor\workspaces.json
    /// Peut être personnalisé via la variable d'environnement SERVICESHUB_WORKSPACES_PATH
    /// </summary>
    public static class WorkspaceManager
    {
        private static readonly string AppDataFolder;
        private static readonly string WorkspacesFilePath;

        static WorkspaceManager()
        {
            // Vérifier si une variable d'environnement personnalise le chemin
            var customPath = Environment.GetEnvironmentVariable("SERVICESHUB_WORKSPACES_PATH");
            if (!string.IsNullOrEmpty(customPath))
            {
                WorkspacesFilePath = customPath;
                AppDataFolder = Path.GetDirectoryName(customPath) ?? Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    "ServicesHubExtractor");
            }
            else
            {
                AppDataFolder = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    "ServicesHubExtractor");
                WorkspacesFilePath = Path.Combine(AppDataFolder, "workspaces.json");
            }
        }

        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            WriteIndented = true,
            PropertyNameCaseInsensitive = true
        };

        /// <summary>
        /// Charge les workspaces depuis le fichier local
        /// </summary>
        public static List<WorkspaceInfo> LoadWorkspaces()
        {
            EnsureDirectoryExists();

            if (!File.Exists(WorkspacesFilePath))
            {
                // Créer un fichier vide avec quelques exemples
                var defaultWorkspaces = new List<WorkspaceInfo>();
                SaveWorkspaces(defaultWorkspaces);
                return defaultWorkspaces;
            }

            try
            {
                string json = File.ReadAllText(WorkspacesFilePath);
                return JsonSerializer.Deserialize<List<WorkspaceInfo>>(json, JsonOptions) ?? new List<WorkspaceInfo>();
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"Erreur lors du chargement des workspaces : {ex.Message}");
                Console.ResetColor();
                return new List<WorkspaceInfo>();
            }
        }

        /// <summary>
        /// Sauvegarde les workspaces dans le fichier local
        /// </summary>
        public static void SaveWorkspaces(List<WorkspaceInfo> workspaces)
        {
            EnsureDirectoryExists();

            try
            {
                string json = JsonSerializer.Serialize(workspaces, JsonOptions);
                File.WriteAllText(WorkspacesFilePath, json);
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"Erreur lors de la sauvegarde des workspaces : {ex.Message}");
                Console.ResetColor();
            }
        }

        /// <summary>
        /// Ajoute un workspace à la liste
        /// </summary>
        public static bool AddWorkspace(string name, string id)
        {
            var workspaces = LoadWorkspaces();
            
            // Vérifier si l'ID existe déjà
            if (workspaces.Any(w => w.Id.Equals(id, StringComparison.OrdinalIgnoreCase)))
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine($"  ⚠ Ce workspace existe déjà");
                Console.ResetColor();
                return false;
            }

            workspaces.Add(new WorkspaceInfo(name, id));
            SaveWorkspaces(workspaces);
            return true;
        }

        /// <summary>
        /// Supprime un workspace de la liste
        /// </summary>
        public static bool RemoveWorkspace(string id)
        {
            var workspaces = LoadWorkspaces();
            var toRemove = workspaces.FirstOrDefault(w => w.Id.Equals(id, StringComparison.OrdinalIgnoreCase));
            
            if (toRemove != null)
            {
                workspaces.Remove(toRemove);
                SaveWorkspaces(workspaces);
                return true;
            }
            return false;
        }

        /// <summary>
        /// Retourne le chemin du fichier de workspaces
        /// </summary>
        public static string GetWorkspacesFilePath() => WorkspacesFilePath;

        /// <summary>
        /// Importe les workspaces depuis appsettings.json (migration initiale)
        /// </summary>
        public static int ImportFromAppSettings(List<WorkspaceInfo> appSettingsWorkspaces)
        {
            var existingWorkspaces = LoadWorkspaces();
            int imported = 0;

            foreach (var ws in appSettingsWorkspaces)
            {
                if (!existingWorkspaces.Any(w => w.Id.Equals(ws.Id, StringComparison.OrdinalIgnoreCase)))
                {
                    existingWorkspaces.Add(ws);
                    imported++;
                }
            }

            if (imported > 0)
            {
                SaveWorkspaces(existingWorkspaces);
            }

            return imported;
        }

        private static void EnsureDirectoryExists()
        {
            if (!Directory.Exists(AppDataFolder))
            {
                Directory.CreateDirectory(AppDataFolder);
            }
        }
    }
}

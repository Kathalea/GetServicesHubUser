namespace GetServicesHubUser
{
    /// <summary>
    /// Configuration principale de l'application, chargée depuis appsettings.json
    /// </summary>
    public class AppSettings
    {
        /// <summary>Configuration du composant WebView2</summary>
        public WebViewConfig WebView { get; set; } = null!;
        /// <summary>Configuration de Services Hub</summary>
        public ServicesHubConfig ServicesHub { get; set; } = null!;
        /// <summary>Configuration des URL de l'API</summary>
        public ApiConfig Api { get; set; } = null!;
        /// <summary>Liste des workspaces par défaut (migration initiale)</summary>
        public List<WorkspaceInfo> Workspaces { get; set; } = new();
        /// <summary>Configuration de sortie (CSV)</summary>
        public OutputConfig Output { get; set; } = null!;
    }

    /// <summary>
    /// Configuration du composant WebView2 pour l'authentification
    /// </summary>
    public class WebViewConfig
    {
        /// <summary>Titre de la fenêtre de connexion</summary>
        public string FormTitle { get; set; } = "";
        /// <summary>Dossier de stockage des données WebView2 (cookies, cache)</summary>
        public string UserDataFolder { get; set; } = "";
        /// <summary>Classe CSS de la div à attendre avant de lancer l'API</summary>
        public string DivClass { get; set; } = "";
    }

    /// <summary>
    /// Configuration de Services Hub
    /// </summary>
    public class ServicesHubConfig
    {
        /// <summary>URL de base de Services Hub</summary>
        public string BaseUrl { get; set; } = "";
        /// <summary>Nom du cookie d'authentification</summary>
        public string CookieName { get; set; } = "";
    }

    /// <summary>
    /// Configuration des endpoints API
    /// </summary>
    public class ApiConfig
    {
        /// <summary>Format d'URL de la page de connexion (utilise {0} pour l'ID workspace)</summary>
        public string LoginPageUrlFormat { get; set; } = "";
        /// <summary>Format d'URL de l'API utilisateurs (utilise {0} pour l'ID workspace)</summary>
        public string ApiUrlFormat { get; set; } = "";
    }

    /// <summary>
    /// Informations d'un workspace Services Hub
    /// </summary>
    public class WorkspaceInfo
    {
        /// <summary>Nom lisible du workspace</summary>
        public string Name { get; set; } = "";
        /// <summary>ID unique (GUID) du workspace</summary>
        public string Id { get; set; } = "";

        public WorkspaceInfo() { }
        public WorkspaceInfo(string name, string id)
        {
            Name = name;
            Id = id;
        }
    }

    /// <summary>
    /// Configuration de sortie pour l'export CSV
    /// </summary>
    public class OutputConfig
    {
        /// <summary>Dossier de destination (%DOWNLOADS% supporté)</summary>
        public string CsvFolderPath { get; set; } = "";
        /// <summary>Format du nom de fichier (utilise {0} pour le mois/année)</summary>
        public string CsvNameFormat { get; set; } = "";
    }
}

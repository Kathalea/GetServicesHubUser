using System.Text.Json;

namespace GetServicesHubUser
{
    /// <summary>
    /// Informations supplémentaires retournées par l'API Services Hub
    /// </summary>
    public class AdditionalInformation
    {
        /// <summary>Indique s'il y a eu une erreur lors de la récupération des groupes AAD</summary>
        public bool? AadGroupsErrorInfo { get; set; }
    }

    /// <summary>
    /// Structure racine de la réponse API Services Hub
    /// </summary>
    public class Root
    {
        /// <summary>Informations supplémentaires sur la requête</summary>
        public AdditionalInformation? additionalInformation { get; set; }
        /// <summary>Liste des utilisateurs du workspace</summary>
        public List<Value>? values { get; set; }
        /// <summary>Nombre total d'utilisateurs</summary>
        public int? totalCount { get; set; }
    }

    /// <summary>
    /// Représente un utilisateur Services Hub.
    /// Les propriétés correspondent aux champs JSON de l'API.
    /// </summary>
    public class Value
    {
        /// <summary>Identifiant unique de l'utilisateur</summary>
        public string? id { get; set; }
        /// <summary>Nom complet affiché</summary>
        public string? displayName { get; set; }
        /// <summary>Email ou UPN de l'utilisateur</summary>
        public string? accountName { get; set; }
        /// <summary>Liste des rôles attribués</summary>
        public List<string>? roles { get; set; }
        /// <summary>Rôle principal</summary>
        public string? role { get; set; }
        /// <summary>Contact support</summary>
        public bool isSupportContact { get; set; }
        /// <summary>Customer Success Manager</summary>
        public bool isCsm { get; set; }
        /// <summary>Contact support global</summary>
        public bool isGlobalSupportContact { get; set; }
        /// <summary>Contact en lecture seule</summary>
        public bool isReadOnlyContact { get; set; }
        /// <summary>Statut de l'utilisateur (Active, Pending, etc.)</summary>
        public string? status { get; set; }
        /// <summary>Type d'utilisateur</summary>
        public string? userType { get; set; }
        /// <summary>Dernière connexion</summary>
        public DateTime? lastLoggedIn { get; set; }
        /// <summary>Groupes AAD (structure variable selon les workspaces)</summary>
        public List<JsonElement>? aadGroups { get; set; }
        /// <summary>Groupes AAD de l'utilisateur (structure variable)</summary>
        public List<JsonElement>? userAADGroups { get; set; }
        /// <summary>Prénom</summary>
        public string? firstName { get; set; }
        /// <summary>Nom de famille</summary>
        public string? lastName { get; set; }
        /// <summary>Nom du workspace (ajouté par l'application)</summary>
        public string? WorkspaceName { get; set; }
    }
}

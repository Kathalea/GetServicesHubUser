# Services Hub User Extractor

Outil d'extraction des utilisateurs depuis Microsoft Services Hub. Permet d'exporter la liste des contacts de vos workspaces vers un fichier CSV.

## Fonctionnalités

- Extraction automatique des utilisateurs via WebView2 (authentification SSO)
- Support de multiples workspaces
- Filtrage automatique des contacts @microsoft.com
- Gestion personnalisée de la liste des workspaces
- Export CSV des résultats
- Retry automatique (3 tentatives) en cas d'erreur
- Logs d'erreurs détaillés

## Prérequis

- Windows 10/11
- .NET 8.0 ou supérieur
- Microsoft Edge WebView2 Runtime (généralement déjà installé)
- Accès à Services Hub avec un compte Microsoft

## Installation

### Option 1 : Télécharger l'exécutable

1. Téléchargez la dernière release depuis [Releases](../../releases)
2. Extrayez l'archive
3. **Copiez `appsettings.template.json` en `appsettings.json`**
4. Lancez `GetServicesHubUser.exe`

### Option 2 : Compiler depuis les sources

```powershell
git clone https://github.com/Kathalea/GetServicesHubUser.git
cd GetServicesHubUser

# Créer votre fichier de configuration
Copy-Item appsettings.template.json appsettings.json

dotnet build
dotnet run
```

> **Important** : Le fichier `appsettings.json` contient vos données personnelles et n'est pas versionné. Utilisez le template fourni pour créer le vôtre.

## Utilisation

### Menu principal

Au lancement, vous avez 3 options :

```
═══════════════════════════════════════════════════════
       Services Hub User Extractor
═══════════════════════════════════════════════════════

Que souhaitez-vous faire ?

  [1] Extraire tous les workspaces
  [2] Extraire des workspaces spécifiques
  [3] Gérer les workspaces

? Votre choix :
```

### Option 1 : Extraire tous les workspaces

Extrait les utilisateurs de tous les workspaces configurés dans votre liste.

### Option 2 : Extraire des workspaces spécifiques

Entrez les IDs des workspaces séparés par `;` :

```
? IDs : db7f4202-bc75-47ae-a6a9-1ad6ec518c5c;5e6f9931-58a0-4daa-a9a5-6154680348d4
```

> **Astuce** : Vous pouvez entrer un ID inconnu, il sera testé automatiquement. Si l'extraction réussit, vous pourrez l'ajouter à votre liste.

### Option 3 : Gérer les workspaces

```
═══ Gestion des workspaces ═══

  [A] Ajouter un workspace
  [S] Supprimer un workspace
  [M] Modifier un workspace
  [L] Lister les workspaces
  [Q] Quitter la gestion
```

## Configuration

### Fichier de workspaces

Les workspaces sont stockés dans :
```
%APPDATA%\ServicesHubExtractor\workspaces.json
```

Exemple de contenu :
```json
[
  {
    "Name": "Mon Workspace",
    "Id": "db7f4202-bc75-47ae-a6a9-1ad6ec518c5c"
  },
  {
    "Name": "Autre Workspace",
    "Id": "5e6f9931-58a0-4daa-a9a5-6154680348d4"
  }
]
```

### Trouver l'ID d'un workspace

1. Connectez-vous à [Services Hub](https://serviceshub.microsoft.com)
2. Allez dans votre workspace
3. Cliquez sur "Users" dans le menu
4. L'ID se trouve dans l'URL : `https://serviceshub.microsoft.com/users?workspaceId=VOTRE_ID_ICI`

### Configuration avancée (appsettings.json)

Le fichier `appsettings.json` contient la configuration de l'application :

```json
{
  "WebView": {
    "FormTitle": "Authentification requise - WebView2",
    "UserDataFolder": "Microsoft\\Edge\\User Data\\Default",
    "DivClass": "ms-DetailsRow"
  },
  "ServicesHub": {
    "BaseUrl": "https://serviceshub.microsoft.com",
    "CookieName": ".AspNet.Cookies"
  },
  "Api": {
    "LoginPageUrlFormat": "https://serviceshub.microsoft.com/users?workspaceId={0}",
    "ApiUrlFormat": "https://serviceshub.microsoft.com/api/WorkspaceUsers/GetV2?workspaceId={0}&locale=en-US&orderBy=displayName&desc=false&top=5000"
  },
  "Output": {
    "CsvFolderPath": "%DOWNLOADS%",
    "CsvNameFormat": "Extract_contacts_caih_last_update{0}"
  }
}
```

| Paramètre | Description |
|-----------|-------------|
| `Output.CsvFolderPath` | Dossier de sortie du CSV (`%DOWNLOADS%` = dossier Téléchargements) |
| `Output.CsvNameFormat` | Format du nom du fichier CSV (`{0}` = mois/année) |

## Variables d'environnement

Vous pouvez personnaliser certains comportements via des variables d'environnement :

### Définir le dossier de sortie

```powershell
# PowerShell - Session courante uniquement
$env:SERVICESHUB_OUTPUT_PATH = "C:\MesExports"

# PowerShell - Permanent (utilisateur)
[Environment]::SetEnvironmentVariable("SERVICESHUB_OUTPUT_PATH", "C:\MesExports", "User")

# CMD
setx SERVICESHUB_OUTPUT_PATH "C:\MesExports"
```

### Définir le chemin du fichier de workspaces

```powershell
# PowerShell - Permanent (utilisateur)
[Environment]::SetEnvironmentVariable("SERVICESHUB_WORKSPACES_PATH", "C:\Config\workspaces.json", "User")
```

> **Note** : Redémarrez l'application après avoir modifié les variables d'environnement.

## Fichiers générés

| Fichier | Emplacement | Description |
|---------|-------------|-------------|
| `workspaces.json` | `%APPDATA%\ServicesHubExtractor\` | Liste de vos workspaces |
| `Extract_contacts_*.csv` | Téléchargements | Export des utilisateurs |
| `ErrorLog_ServicesHub.txt` | Téléchargements | Log des erreurs (si applicable) |

## Format du CSV exporté

Le fichier CSV contient les colonnes suivantes :

| Colonne | Description |
|---------|-------------|
| `id` | Identifiant unique de l'utilisateur |
| `displayName` | Nom affiché |
| `accountName` | Email/UPN |
| `roles` | Rôles attribués |
| `role` | Rôle principal |
| `isSupportContact` | Est un contact support |
| `isCsm` | Est un CSM |
| `status` | Statut (Active, Pending, etc.) |
| `userType` | Type d'utilisateur |
| `lastLoggedIn` | Dernière connexion |
| `firstName` | Prénom |
| `lastName` | Nom |
| `WorkspaceName` | Nom du workspace source |

## Dépannage

### L'authentification ne fonctionne pas

1. Fermez toutes les fenêtres Edge
2. Supprimez le cache WebView2 :
   ```
   %LOCALAPPDATA%\Microsoft\Edge\User Data\Default
   ```
3. Relancez l'application

### Erreur "JSON désérialisation"

- L'API Services Hub peut parfois retourner des données vides
- L'application réessaie automatiquement 3 fois
- Vérifiez le fichier `ErrorLog_ServicesHub.txt` pour plus de détails

### Workspace non trouvé

- Vérifiez que l'ID est correct (format GUID)
- Assurez-vous d'avoir les droits d'accès au workspace

## Contribution

Les contributions sont les bienvenues ! N'hésitez pas à :
- Ouvrir une issue pour signaler un bug
- Proposer une Pull Request pour une amélioration

## Licence

MIT License - Voir [LICENSE](LICENSE)

## Auteur

Développé pour faciliter la gestion des contacts Services Hub.

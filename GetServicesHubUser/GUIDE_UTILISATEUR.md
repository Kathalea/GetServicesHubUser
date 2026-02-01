# Services Hub User Extractor - Guide Utilisateur

## Téléchargement

1. Téléchargez l'application : [ServicesHubExtractor-v1.0.zip](https://github.com/Kathalea/GetServicesHubUser/releases/download/v1.0/ServicesHubExtractor-v1.0.zip)

2. Extrayez le fichier ZIP dans un dossier de votre choix (ex: `C:\Outils\ServicesHubExtractor`)

3. Double-cliquez sur **GetServicesHubUser.exe** pour lancer l'application

---

## Première utilisation

Au premier lancement, une fenêtre de navigateur s'ouvrira pour vous authentifier avec votre compte Microsoft. Connectez-vous normalement.

---

## Menu principal

```
═══════════════════════════════════════════════════════
       Services Hub User Extractor
═══════════════════════════════════════════════════════

Que souhaitez-vous faire ?

  [1] Extraire tous les utilisateurs de Services Hub
  [2] Extraire les utilisateurs de workspaces spécifiques
  [3] Gérer les workspaces
  [Q] Quitter
```

### Option 1 : Extraire tous les workspaces
Lance l'extraction de tous les utilisateurs de tous vos workspaces configurés.

### Option 2 : Extraire des workspaces spécifiques
Permet de sélectionner uniquement certains workspaces par leur ID.

### Option 3 : Gérer les workspaces
Ajouter, supprimer ou modifier la liste de vos workspaces.

---

## Ajouter un workspace

1. Choisissez l'option **3** (Gérer les workspaces)
2. Choisissez **A** (Ajouter)
3. Entrez le nom du workspace (ex: "Mon Hôpital")
4. Entrez l'ID du workspace

### Comment trouver l'ID d'un workspace ?

1. Connectez-vous à [Services Hub](https://serviceshub.microsoft.com)
2. Sélectionnez votre workspace
3. Cliquez sur **Users** dans le menu de gauche
4. Regardez l'URL dans votre navigateur :
   ```
   https://serviceshub.microsoft.com/users?workspaceId=VOTRE_ID_ICI
   ```
5. Copiez l'ID (format : `xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx`)

---

## Résultat

Après l'extraction, un fichier CSV sera généré dans votre dossier **Téléchargements** :
```
Extract_contacts_MMYYYY.csv
```

Ce fichier contient :
- Nom, prénom, email
- Rôles et statut
- Dernière connexion
- Nom du workspace source

Vous pouvez ouvrir ce fichier avec Excel.

---

## Dépannage

### La fenêtre d'authentification ne s'affiche pas
- Fermez toutes les fenêtres Microsoft Edge
- Relancez l'application

### "Erreur de désérialisation JSON"
- L'application réessaie automatiquement 3 fois
- Si l'erreur persiste, vérifiez votre connexion internet

### L'extraction est très lente
- C'est normal pour les workspaces avec beaucoup d'utilisateurs
- Ne fermez pas la fenêtre pendant le traitement

---

## Contact

Pour toute question, contactez : [nora.droulin@microsoft.com]

#Set-ExecutionPolicy -Scope CurrentUser -ExecutionPolicy Restricted
#powershell -ExecutionPolicy Bypass -File "C:\Users\noelmors\OneDrive - Microsoft\repos\GetServicesHubUser\MergeCsvFile.ps1"

# Spécifiez le dossier contenant les fichiers CSV
$sourceFolder = "C:\Users\noelmors\OneDrive - Microsoft\repos\GetServicesHubUser"
# Génère le nom de fichier basé sur le mois et l'année actuels
$currentDate = Get-Date
$currentMonth = $currentDate.ToString("MM") # Mois à deux chiffres
$currentYear = $currentDate.ToString("yyyy") # Année à quatre chiffres
$destinationFileName = "Extract_AllServiceshubContact_${currentMonth}_${currentYear}.csv"
$destinationFile = Join-Path -Path $sourceFolder -ChildPath $destinationFileName

# Récupère tous les fichiers .csv dans le dossier source
$csvFiles = Get-ChildItem -Path $sourceFolder -Filter *.csv

# Vérifie s'il y a des fichiers CSV
if ($csvFiles.Count -eq 0) {
    Write-Host "Aucun fichier CSV trouvé dans le dossier spécifié."
    exit
}

# Crée le fichier de destination avec les en-têtes en UTF-8
"Mail,Display Name" | Out-File -FilePath $destinationFile -Encoding utf8

# Boucle à travers chaque fichier CSV
foreach ($file in $csvFiles) {
    # Lit le contenu du fichier d'entrée et le traite ligne par ligne
    Get-Content -Path $file.FullName -Encoding utf8 | Out-File -FilePath $destinationFile -Append -Encoding utf8
}

Write-Host "Tous les fichiers CSV ont été compilés dans '$destinationFile'."

using CsvHelper;
using System.Globalization;
using System.Text.Json;
using Microsoft.Extensions.Configuration;

namespace GetServicesHubUser
{
    public class Program
    {
        [STAThread]
        public static void Main(string[] args)
        {
            MainAsync(args).GetAwaiter().GetResult();
        }

        public static async Task MainAsync(string[] args)
        {
            // Chargement de la configuration à partir du fichier appsettings.json
            var config = new ConfigurationBuilder()
                .SetBasePath(AppContext.BaseDirectory)
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .Build();   
            AppSettings appSettings = config.Get<AppSettings>();

            var workspaceList = appSettings.Workspaces;

            var working = false;
            List<Value> resultAllup = [];


            Console.WriteLine("Quel mode voulez-vous démarrer l'application - 1: Manuel - 2 : Automatique");
            string? methode = Console.ReadLine();

            if (methode == "1")
            {
                working = true;
                //Méthode manuelle qui fonctionne

                foreach (var ws in workspaceList)
                {
                    string loginPageUrl = string.Format(appSettings.Api.LoginPageUrlFormat, ws.Id);
                    string apiUrl = string.Format(appSettings.Api.ApiUrlFormat, ws.Id);
                    Console.WriteLine("Veuillez copier/coller la valeur du cookie d'authentification (.AspNet.Cookies) pour :" + ws.Name);
                    Console.WriteLine(loginPageUrl);
                    Console.WriteLine(apiUrl);
                    string? authCookie = Console.ReadLine();
                    Console.WriteLine("");
                    Console.WriteLine("");
                    if (string.IsNullOrEmpty(authCookie))
                    {
                        Console.WriteLine("Cookie non fourni, passage au workspace suivant.");
                        continue;
                    }
                    Console.WriteLine("Appel API...");
                    var result = await UsersProcessor.LoadUser(apiUrl, authCookie, ws.Name);
                    foreach (var user in result.values ?? Enumerable.Empty<Value>())
                    {
                        resultAllup.Add(user);
                    }
                }
            }

            if (methode == "2")
            {
                foreach (var ws in workspaceList)
                {
                    //Méthode automatique qui des fois ne fonctionne pas
                    //var ws = workspaceList.First();
                    string loginPageUrl = string.Format(appSettings.Api.LoginPageUrlFormat, ws.Id);
                    string apiUrl = string.Format(appSettings.Api.ApiUrlFormat, ws.Id);
                    Console.WriteLine("  ");
                    Console.WriteLine("**********************************************************************************************");
                    Console.WriteLine("***");
                    Console.WriteLine("***   " + ws.Name + " - ID :" + ws.Id);
                    Console.WriteLine("***");
                    Console.WriteLine("**********************************************************************************************");


                    working = true;
                    string? automatiqueAuthCookie = await WebViewCookieHelper.GetAuthCookieAsync(loginPageUrl, appSettings.ServicesHub.CookieName);
                    
                    if (string.IsNullOrEmpty(automatiqueAuthCookie))
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine("Impossible de récupérer le cookie d'authentification.");
                        Console.ResetColor();
                        return;
                    }
                    else
                    {
                        Console.ForegroundColor = ConsoleColor.Green;
                        Console.WriteLine("### Connection réussit vers la page Users.");
                        Console.ResetColor();
                        string? apiResponse = await WebViewCookieHelper.GetApiResponseAsync(loginPageUrl, apiUrl, ws.Name);

                        // Désérialisation de la réponse JSON en objet Root
                        if (!string.IsNullOrEmpty(apiResponse))
                        {
                            // Retire le préfixe "STATUS:xxx|" s'il est présent
                            var json = apiResponse;
                            if (json.StartsWith("STATUS:"))
                            {
                                var idx = json.IndexOf('|');
                                if (idx >= 0) json = json.Substring(idx + 1);
                            }
                            try
                            {
                                var root = JsonSerializer.Deserialize<Root>(json);
                                var users = root?.values ?? Enumerable.Empty<Value>();
                                foreach (var user in users)
                                {
                                    resultAllup.Add(user);
                                }

                                Console.WriteLine($"Nombre d'utilisateurs : {users.Count()}");
                            }
                            catch (Exception ex)
                            {
                                Console.ForegroundColor = ConsoleColor.Red;
                                Console.WriteLine("Erreur lors de la désérialisation : " + ex.Message);
                                Console.ResetColor();
                            }
                        }
                        else
                        {
                            Console.WriteLine("Réponse vide, aucun utilisateur ajouté.");
                        }
                    }
                }
            }
            else
            {
                Console.WriteLine("J'ai rien compris merci de recommencer");
            }

            if (working)
            {
                Console.ForegroundColor = ConsoleColor.Magenta;
                Console.WriteLine("Voulez-vous générer un fichier CSV ? Taper Y pour oui");
                Console.ResetColor();
                var answer = Console.ReadLine();
                if(answer == "Y")
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
                    Console.WriteLine("Export terminé.");
                }
                Console.WriteLine("Appuyez sur une touche pour quitter.");
                Console.ReadKey();
            }
        }
    }
}
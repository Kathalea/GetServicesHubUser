using Microsoft.Web.WebView2.WinForms;
using Microsoft.Web.WebView2.Core;
using Microsoft.Extensions.Configuration;
using System.Security.Policy;
using System.Text.Json;

namespace GetServicesHubUser
{
    public class WebViewCookieHelper
    {
        private static readonly AppSettings appSettings;
        static WebViewCookieHelper()
        {
            var config = new ConfigurationBuilder()
                .SetBasePath(AppContext.BaseDirectory)
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .Build();
            appSettings = config.Get<AppSettings>();
        }

        /// <summary>
        /// Traite tous les workspaces avec une seule session WebView2 (une seule authentification)
        /// </summary>
        public static Task<List<string>> GetAllWorkspacesDataAsync(List<WorkspaceInfo> workspaces, string loginPageUrlFormat, string apiUrlFormat)
        {
            var tcs = new TaskCompletionSource<List<string>>();
            var results = new List<string>();
            var thread = new Thread(() =>
            {
                var form = new Form();
                form.Text = appSettings.WebView.FormTitle;
                form.WindowState = FormWindowState.Normal;
                form.Width = 1024;
                form.Height = 768;
                var webView = new WebView2 { Dock = DockStyle.Fill };
                form.Controls.Add(webView);

                int currentWorkspaceIndex = 0;
                bool isAuthenticated = false;
                bool isProcessingApi = false;

                form.Load += async (s, e) =>
                {
                    try
                    {
                        string userDataFolder = Path.Combine(
                            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                            appSettings.WebView.UserDataFolder);
                        var environment = await CoreWebView2Environment.CreateAsync(null, userDataFolder,
                            new CoreWebView2EnvironmentOptions { AllowSingleSignOnUsingOSPrimaryAccount = true });
                        await webView.EnsureCoreWebView2Async(environment);

                        // Naviguer vers le premier workspace pour l'authentification
                        var firstWs = workspaces[0];
                        string loginPageUrl = string.Format(loginPageUrlFormat, firstWs.Id);
                        Console.WriteLine();
                        Console.ForegroundColor = ConsoleColor.Yellow;
                        Console.Write("● ");
                        Console.ForegroundColor = ConsoleColor.White;
                        Console.WriteLine($"Authentification en cours sur {firstWs.Name}...");
                        Console.ResetColor();
                        webView.Source = new Uri(loginPageUrl);

                        webView.NavigationCompleted += async (sender, args) =>
                        {
                            if (isProcessingApi) return;

                            // Vérifier si on est authentifié
                            var cookies = await webView.CoreWebView2.CookieManager.GetCookiesAsync(appSettings.ServicesHub.BaseUrl);
                            var authCookie = cookies.FirstOrDefault(c => c.Name == appSettings.ServicesHub.CookieName);

                            if (authCookie != null && !isAuthenticated)
                            {
                                isAuthenticated = true;
                                Console.ForegroundColor = ConsoleColor.Green;
                                Console.WriteLine("✓ Authentification réussie !");
                                Console.ResetColor();
                                Console.WriteLine();
                                Console.ForegroundColor = ConsoleColor.Cyan;
                                Console.WriteLine($"═══ Traitement de {workspaces.Count} workspace(s) ═══");
                                Console.ResetColor();
                            }

                            if (isAuthenticated && !isProcessingApi)
                            {
                                // Attendre que la div soit présente puis lancer le fetch
                                string js = $@"
                                (function() {{
                                    function waitForDiv() {{
                                        var div = document.querySelector('div.{appSettings.WebView.DivClass}');
                                        if (div) {{
                                            window.chrome.webview.postMessage('DIV_PRESENT');
                                        }} else {{
                                            setTimeout(waitForDiv, 500);
                                        }}
                                    }}
                                    waitForDiv();
                                }})();";
                                await webView.CoreWebView2.ExecuteScriptAsync(js);
                            }
                        };

                        webView.CoreWebView2.WebMessageReceived += async (ss, ee) =>
                        {
                            var msg = ee.TryGetWebMessageAsString();
                            
                            if (msg == "DIV_PRESENT" && !isProcessingApi && currentWorkspaceIndex < workspaces.Count)
                            {
                                isProcessingApi = true;
                                var ws = workspaces[currentWorkspaceIndex];
                                string apiUrl = string.Format(apiUrlFormat, ws.Id);

                                Console.WriteLine();
                                Console.ForegroundColor = ConsoleColor.DarkCyan;
                                Console.Write($"[{currentWorkspaceIndex + 1}/{workspaces.Count}] ");
                                Console.ForegroundColor = ConsoleColor.White;
                                Console.Write(ws.Name);
                                Console.ForegroundColor = ConsoleColor.DarkGray;
                                Console.WriteLine($" ({ws.Id})");
                                Console.ForegroundColor = ConsoleColor.Gray;
                                Console.Write("    ▶ Fetch API...");
                                Console.ResetColor();

                                string jsFetch = $@"
(async function() {{
    const maxRetries = 3;
    const workspaceName = '{ws.Name.Replace("'", "\\'")}';
    const workspaceId = '{ws.Id}';
    let lastError = null;
    let lastData = null;
    
    for (let attempt = 1; attempt <= maxRetries; attempt++) {{
        try {{
            const response = await fetch('{apiUrl}', {{ credentials: 'include' }});
            const data = await response.text();
            lastData = data;
            
            try {{
                var obj = JSON.parse(data);
                if (obj.values && Array.isArray(obj.values)) {{
                    obj.values.forEach(u => u.WorkspaceName = workspaceName);
                }}
                const jsonData = JSON.stringify(obj);
                window.chrome.webview.postMessage('API_RESULT:' + response.status + '|' + jsonData);
                return;
            }} catch (parseError) {{
                lastError = parseError.message || parseError.toString();
                console.log('Tentative ' + attempt + '/' + maxRetries + ' - Erreur JSON: ' + lastError);
                if (attempt < maxRetries) {{
                    await new Promise(resolve => setTimeout(resolve, 1000));
                }}
            }}
        }} catch (fetchError) {{
            lastError = fetchError.message || fetchError.toString();
            console.log('Tentative ' + attempt + '/' + maxRetries + ' - Erreur fetch: ' + lastError);
            if (attempt < maxRetries) {{
                await new Promise(resolve => setTimeout(resolve, 1000));
            }}
        }}
    }}
    
    const errorObj = {{
        error: true,
        workspace: workspaceName,
        workspaceId: workspaceId,
        message: lastError,
        rawData: lastData ? lastData.substring(0, 500) : null,
        timestamp: new Date().toISOString()
    }};
    window.chrome.webview.postMessage('API_ERROR:' + JSON.stringify(errorObj));
}})();";
                                await webView.CoreWebView2.ExecuteScriptAsync(jsFetch);
                            }
                            else if (msg != null && msg.StartsWith("API_RESULT:"))
                            {
                                var ws = workspaces[currentWorkspaceIndex];
                                // Inclure le nom du workspace dans la réponse pour le debug
                                results.Add($"WORKSPACE:{ws.Name}|STATUS:" + msg.Substring("API_RESULT:".Length));
                                Console.ForegroundColor = ConsoleColor.Green;
                                Console.WriteLine(" ✓");
                                Console.ResetColor();

                                // Passer au workspace suivant
                                currentWorkspaceIndex++;
                                isProcessingApi = false;

                                if (currentWorkspaceIndex < workspaces.Count)
                                {
                                    var nextWs = workspaces[currentWorkspaceIndex];
                                    string nextLoginPageUrl = string.Format(loginPageUrlFormat, nextWs.Id);
                                    await Task.Delay(500); // Petit délai pour stabilité
                                    webView.Source = new Uri(nextLoginPageUrl);
                                }
                                else
                                {
                                    // Tous les workspaces traités
                                    Console.WriteLine();
                                    Console.ForegroundColor = ConsoleColor.Cyan;
                                    Console.WriteLine("═══ Tous les workspaces ont été traités ! ═══");
                                    Console.ResetColor();
                                    tcs.SetResult(results);
                                    form.Invoke(new Action(() => form.Close()));
                                }
                            }
                            else if (msg != null && msg.StartsWith("API_ERROR:"))
                            {
                                var ws = workspaces[currentWorkspaceIndex];
                                // Logger l'erreur
                                try
                                {
                                    var errorJson = msg.Substring("API_ERROR:".Length);
                                    string errorFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads");
                                    string errorFilePath = Path.Combine(errorFolder, "ErrorLog_ServicesHub.txt");
                                    string apiUrl = string.Format(apiUrlFormat, ws.Id);

                                    var errorInfo = JsonSerializer.Deserialize<Dictionary<string, object>>(errorJson);
                                    string errorContent = $@"
================================================================================
Date: {DateTime.Now:yyyy-MM-dd HH:mm:ss}
Workspace: {ws.Name}
Workspace ID: {ws.Id}
API URL: {apiUrl}
Message d'erreur: {errorInfo?.GetValueOrDefault("message", "Inconnu")}
Données brutes (500 premiers caractères):
{errorInfo?.GetValueOrDefault("rawData", "N/A")}
================================================================================
";
                                    File.AppendAllText(errorFilePath, errorContent);
                                    Console.ForegroundColor = ConsoleColor.Red;
                                    Console.WriteLine(" ✗");
                                    Console.ForegroundColor = ConsoleColor.DarkYellow;
                                    Console.WriteLine($"    └─ Erreur loggée: {errorFilePath}");
                                    Console.ResetColor();
                                }
                                catch (Exception ex)
                                {
                                    Console.WriteLine($"Impossible d'écrire dans le fichier de log: {ex.Message}");
                                }

                                // Continuer avec le workspace suivant malgré l'erreur
                                currentWorkspaceIndex++;
                                isProcessingApi = false;

                                if (currentWorkspaceIndex < workspaces.Count)
                                {
                                    var nextWs = workspaces[currentWorkspaceIndex];
                                    string nextLoginPageUrl = string.Format(loginPageUrlFormat, nextWs.Id);
                                    await Task.Delay(500);
                                    webView.Source = new Uri(nextLoginPageUrl);
                                }
                                else
                                {
                                    Console.ForegroundColor = ConsoleColor.Cyan;
                                    Console.WriteLine("### Tous les workspaces ont été traités !");
                                    Console.ResetColor();
                                    tcs.SetResult(results);
                                    form.Invoke(new Action(() => form.Close()));
                                }
                            }
                        };
                    }
                    catch (Exception ex)
                    {
                        tcs.SetException(ex);
                        form.Invoke(new Action(() => form.Close()));
                    }
                };

                Application.Run(form);
            });
            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();

            return tcs.Task;
        }

        public static Task<string?> GetAuthCookieAsync(string url, string cookieName)
        {
            var tcs = new TaskCompletionSource<string?>();
            var thread = new Thread(() =>
            {
                var form = new Form();
                form.Text = appSettings.WebView.FormTitle;
                var webView = new WebView2 { Dock = DockStyle.Fill };
                form.Controls.Add(webView);

                form.Load += async (s, e) =>
                {
                    try
                    {
                        string userDataFolder = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    appSettings.WebView.UserDataFolder
                    );
                        var environment = await CoreWebView2Environment.CreateAsync(
                         null, userDataFolder,
                         new Microsoft.Web.WebView2.Core.CoreWebView2EnvironmentOptions
                         {
                             AllowSingleSignOnUsingOSPrimaryAccount = true
                         });
                        await webView.EnsureCoreWebView2Async(environment);
                        webView.Source = new Uri(url);

                        webView.NavigationCompleted += async (sender, args) =>
                        {
                            if (webView.Source.Host.Contains(appSettings.ServicesHub.BaseUrl))
                            {
                                Console.WriteLine("GetAuthCookieAsync - Navigation vers la page d'authentification...");
                            }
                            else
                            {
                                Console.WriteLine("GetAuthCookieAsync - Connexion vers la page des utilisateurs de Serviceshub...");
                            }
                            {
                                var cookies = await webView.CoreWebView2.CookieManager.GetCookiesAsync(appSettings.ServicesHub.BaseUrl);
                                var authCookie = cookies.FirstOrDefault(c => c.Name == cookieName);
                                if (authCookie != null)
                                {
                                    tcs.SetResult(authCookie.Value);
                                    form.Invoke(new Action(() => form.Close()));
                                }
                            }
                        };
                    }
                    catch (Exception ex)
                    {
                        tcs.SetException(ex);
                        form.Invoke(new Action(() => form.Close()));
                    }
                };
                Application.Run(form);
            });
            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();

            return tcs.Task;
        
        }
        public static Task<string?> GetApiResponseAsync(string loginPageUrl, string apiUrl, string workspaceName, string workspaceId)
        {
            var tcs = new TaskCompletionSource<string?>();
            var thread = new Thread(() =>
            {
                var divClass = appSettings.WebView.DivClass;
                var form = new Form();
                form.Text = appSettings.WebView.FormTitle;
                var webView = new WebView2 { Dock = DockStyle.Fill };

                form.Controls.Add(webView);

                form.Load += async (s, e) =>
                {
                    try
                    {
                        string userDataFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), appSettings.WebView.UserDataFolder);
                        var environment = await CoreWebView2Environment.CreateAsync(null, userDataFolder, new CoreWebView2EnvironmentOptions
                        {
                            AllowSingleSignOnUsingOSPrimaryAccount = true
                        });

                        await webView.EnsureCoreWebView2Async(environment);

                        Console.WriteLine("GetApiResponseAsync - Connexion vers la page des utilisateurs de Serviceshub...");
                        webView.Source = new Uri(loginPageUrl);

                        webView.NavigationCompleted += async (sender, args) =>
                        {
                            var url = webView.Source.AbsoluteUri;
                            Console.ForegroundColor = ConsoleColor.Green;
                            Console.WriteLine("### GetApiResponseAsync : OK ");
                            Console.ResetColor();
                            Console.WriteLine("  ");
                            Console.WriteLine("Lancement du fetch JS sur l'API...");


                            // Injecte un JS qui attend la présence de la div
                            string js = $@"
                        (function() {{
                            function waitForDiv() {{
                                var div = document.querySelector('div.{appSettings.WebView.DivClass}');
                                if (div) {{
                                    window.chrome.webview.postMessage('DIV_PRESENT');
                                }} else {{
                                    setTimeout(waitForDiv, 500);
                                }}
                            }}
                            waitForDiv();
                        }})();
                    ";
                            await webView.CoreWebView2.ExecuteScriptAsync(js);
                        };
                        bool apiCalled = false;
                        webView.CoreWebView2.WebMessageReceived += async (ss, ee) =>
                        {
                            var msg = ee.TryGetWebMessageAsString();
                            if (msg == "DIV_PRESENT" && !apiCalled)
                            {
                                apiCalled = true;
                                // Appel API après détection de la div avec retry 3 fois si échec de désérialisation JSON
                                string jsFetch = $@"
(async function() {{
    const maxRetries = 3;
    const workspaceName = '{workspaceName.Replace("'", "\\'")}';
    let lastError = null;
    let lastData = null;
    
    for (let attempt = 1; attempt <= maxRetries; attempt++) {{
        try {{
            const response = await fetch('{apiUrl}', {{ credentials: 'include' }});
            const data = await response.text();
            lastData = data;
            
     
            try {{
                var obj = JSON.parse(data);
                if (obj.values && Array.isArray(obj.values)) {{
                    obj.values.forEach(u => u.WorkspaceName = workspaceName);
                }}
                const jsonData = JSON.stringify(obj);
                window.chrome.webview.postMessage('STATUS:' + response.status + '|' + jsonData);
                // DEBUG: Affiche la réponse API dans la fenêtre
                // document.body.innerHTML = '<h2>Réponse API :</h2><pre>' + jsonData.replace(/</g, '&lt;') + '</pre>';
                return; // Succès, on sort
            }} catch (parseError) {{
                lastError = parseError.message || parseError.toString();
                console.log('Tentative ' + attempt + '/' + maxRetries + ' - Erreur de désérialisation JSON: ' + lastError);
                if (attempt < maxRetries) {{
                    await new Promise(resolve => setTimeout(resolve, 1000)); // Attendre 1 seconde avant retry
                }}
            }}
        }} catch (fetchError) {{
            lastError = fetchError.message || fetchError.toString();
            console.log('Tentative ' + attempt + '/' + maxRetries + ' - Erreur fetch: ' + lastError);
            if (attempt < maxRetries) {{
                await new Promise(resolve => setTimeout(resolve, 1000));
            }}
        }}
    }}
    
    // Après 3 tentatives échouées, envoyer l'erreur
    const errorObj = {{
        error: true,
        workspace: workspaceName,
        workspaceId: '{workspaceId}',
        message: lastError,
        rawData: lastData ? lastData.substring(0, 500) : null,
        timestamp: new Date().toISOString()
    }};
    window.chrome.webview.postMessage('JSON_PARSE_ERROR:' + JSON.stringify(errorObj));
}})();
";
                                await webView.CoreWebView2.ExecuteScriptAsync(jsFetch);
                            }
                            else if (msg != null && msg.StartsWith("FETCH_ERROR:"))
                            {
                                tcs.SetResult(msg);
                            }
                            else if (msg != null && msg.StartsWith("JSON_PARSE_ERROR:"))
                            {
                                // Erreur de désérialisation JSON après 3 tentatives - append au fichier de log
                                try
                                {
                                    var errorJson = msg.Substring("JSON_PARSE_ERROR:".Length);
                                    var errorInfo = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object>>(errorJson);
                                    
                                    string errorFolder = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile) + "\\Downloads";
                                    string errorFileName = "ErrorLog_ServicesHub.txt";
                                    string errorFilePath = Path.Combine(errorFolder, errorFileName);
                                    
                                    string errorContent = $@"
================================================================================
Date: {DateTime.Now:yyyy-MM-dd HH:mm:ss}
Workspace: {workspaceName}
Workspace ID: {workspaceId}
API URL: {apiUrl}
Message d'erreur: {errorInfo?.GetValueOrDefault("message", "Inconnu")}
Données brutes (500 premiers caractères):
{errorInfo?.GetValueOrDefault("rawData", "N/A")}
================================================================================
";
                                    File.AppendAllText(errorFilePath, errorContent);
                                    Console.ForegroundColor = ConsoleColor.Yellow;
                                    Console.WriteLine($"Erreur JSON pour {workspaceName} (ID: {workspaceId}) - Log ajouté: {errorFilePath}");
                                    Console.ResetColor();
                                }
                                catch (Exception ex)
                                {
                                    Console.WriteLine($"Impossible d'écrire dans le fichier de log: {ex.Message}");
                                }
                                tcs.SetResult(msg);
                            }
                            else if (msg != null && msg != "DIV_PRESENT")
                            {
                                // Réponse API reçue
                                tcs.SetResult(msg);
                                //Laisse ouvert pour éviter d'avoir à se recconecter
                                // form.Invoke(new Action(() => form.Close()));
                            }
                            
                        };

                    }
                    catch (Exception ex)
                    {
                        tcs.SetException(ex);
                        //Laisse ouvert pour éviter d'avoir à se recconecter
                        // form.Invoke(new Action(() => form.Close()));
                    }
                };


                    

                Application.Run(form);
            });
            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();

            return tcs.Task;
        }

    }
}
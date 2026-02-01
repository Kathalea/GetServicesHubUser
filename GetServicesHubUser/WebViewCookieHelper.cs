using Microsoft.Web.WebView2.WinForms;
using Microsoft.Web.WebView2.Core;
using Microsoft.Extensions.Configuration;
using System.Security.Policy;

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
        public static Task<string?> GetApiResponseAsync(string loginPageUrl, string apiUrl, string workspaceName)
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
                                // Appel API après détection de la div
                                string jsFetch = $@"
    fetch('{apiUrl}', {{
        credentials: 'include'
    }})
    .then(r => r.text().then(data => {{
        try {{
            var obj = JSON.parse(data);
            if (obj.values && Array.isArray(obj.values)) {{
                obj.values.forEach(u => u.WorkspaceName = '{workspaceName.Replace("'", "\\'")}');
            }}
            data = JSON.stringify(obj);
        }} catch (e) {{}}
        window.chrome.webview.postMessage('STATUS:' + r.status + '|' + data);
        document.body.innerHTML = '<h2>Réponse API :</h2><pre>' + data.replace(/</g, '&lt;') + '</pre>';
    }}))
    .catch(e => {{
        window.chrome.webview.postMessage('FETCH_ERROR:' + e);
        document.body.innerHTML = '<h2>Erreur lors du fetch :</h2><pre>' + e + '</pre>';
    }});
";
                                await webView.CoreWebView2.ExecuteScriptAsync(jsFetch);
                            }
                            else if (msg != null && msg.StartsWith("FETCH_ERROR:"))
                            {
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
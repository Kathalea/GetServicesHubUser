using Microsoft.Extensions.Configuration;

namespace GetServicesHubUser
{
    public class UsersProcessor
    {
         private static readonly HttpClient httpClient = new HttpClient();
        public static async Task<Root> LoadUser(string apiUrl, string authCookie, string WorkspaceName)
        {
            var config = new ConfigurationBuilder()
                .SetBasePath(AppContext.BaseDirectory)
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .Build();   
            AppSettings appSettings = config.Get<AppSettings>();

            var request = new HttpRequestMessage(HttpMethod.Get, apiUrl);
            request.Headers.Add("Cookie", $"{appSettings.ServicesHub.CookieName}={authCookie}");

            using (HttpResponseMessage response = await httpClient.SendAsync(request))
            {
                if (response.IsSuccessStatusCode)
                {
                    
                    Root root = await response.Content.ReadAsAsync<Root>();
                    Console.WriteLine(root);
                    root.values?.ForEach(user => user.WorkspaceName = WorkspaceName);
                    return root;
                }
                else
                {
                    throw new Exception(response.ReasonPhrase);
                }
            }
        }

    }
}
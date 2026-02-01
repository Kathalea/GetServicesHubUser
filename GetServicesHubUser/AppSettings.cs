using System.ComponentModel.Composition.Primitives;

namespace GetServicesHubUser
{
    public class AppSettings
    {
        public WebViewConfig WebView { get; set; } = null!;
        public ServicesHubConfig ServicesHub { get; set; } = null!;
        public ApiConfig Api { get; set; } = null!;
        public List<WorkspaceInfo> Workspaces { get; set; } = new();
        public OutputConfig Output { get; set; } = null!;
    }

    public class WebViewConfig
    {
        public string FormTitle { get; set; }= "";
        public string UserDataFolder { get; set; }= "";
        public string DivClass { get; set; }= "";
    }

    public class ServicesHubConfig
    {
        public string BaseUrl { get; set; }= "";
        public string CookieName { get; set; }= "";
    }

    public class ApiConfig
    {
        public string LoginPageUrlFormat { get; set; }= "";
        public string ApiUrlFormat { get; set; }= "";
    }

    public class WorkspaceInfo
    {
        public string Name { get; set; }= "";
        public string Id { get; set; }= "";

        public WorkspaceInfo() { }
        public WorkspaceInfo(string name, string id)
        {
            Name = name;
            Id = id;
        }
    }
    public class OutputConfig
{
       public string CsvFolderPath { get; set; } = "";
    public string CsvNameFormat { get; set; } = "";
}

}

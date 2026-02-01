namespace GetServicesHubUser
{
    // Root myDeserializedClass = JsonConvert.DeserializeObject<Root>(myJsonResponse);
    public class AdditionalInformation
    {
        public bool? AadGroupsErrorInfo { get; set; }
    }

    public class Root
    {
        public AdditionalInformation? additionalInformation { get; set; }
        public List<Value>? values { get; set; }
        public int? totalCount { get; set; }
    }

    public class Value
    {
        public string id { get; set; }
        public string displayName { get; set; }
        public string accountName { get; set; }
        public List<string> roles { get; set; }
        public string role { get; set; }
        public bool isSupportContact { get; set; }
        public bool isCsm { get; set; }
        public bool isGlobalSupportContact { get; set; }
        public bool isReadOnlyContact { get; set; }
        public string status { get; set; }
        public string userType { get; set; }
        public DateTime? lastLoggedIn { get; set; }
        public List<string> aadGroups { get; set; }
        public List<string> userAADGroups { get; set; }
        public string firstName { get; set; }
        public string lastName { get; set; }
        public string WorkspaceName { get; set; }
    }


}

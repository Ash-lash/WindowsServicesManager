namespace WindowsServicesManager
{
    public class ServiceInfo
    {
        public string ServiceName { get; set; }
        public string DisplayName { get; set; }
        public string Status { get; set; }
        public string StartType { get; set; }
        public string LogOnAs { get; set; }
        public string PathName { get; set; }
        public string ServiceType { get; set; }
        public string CanPauseAndContinue { get; set; }
        public string CanStop { get; set; }
        public string ErrorControl { get; set; }
        public string Description { get; set; }
    }
}
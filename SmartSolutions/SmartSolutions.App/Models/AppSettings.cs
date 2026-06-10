namespace SmartSolutions.App.Models;

public class AppSettings
{
    public ConnectionStringsSection ConnectionStrings { get; set; } = new();
    public FirstRunData?             FirstRunData     { get; set; }

    public class ConnectionStringsSection
    {
        public string Default { get; set; } = "";
    }
}

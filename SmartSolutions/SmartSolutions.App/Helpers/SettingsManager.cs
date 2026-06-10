using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using SmartSolutions.App.Models;

namespace SmartSolutions.App.Helpers;

public static class SettingsManager
{
    private static readonly JsonSerializerOptions _json = new()
    {
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public static string SettingsFilePath =>
        Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "SmartSolutions",
            "appsettings.json");

    public static bool IsSetupRequired() => !File.Exists(SettingsFilePath);

    public static AppSettings Load()
    {
        if (!File.Exists(SettingsFilePath))
            return new AppSettings();

        var json = File.ReadAllText(SettingsFilePath);
        return JsonSerializer.Deserialize<AppSettings>(json, _json) ?? new AppSettings();
    }

    public static void SaveWithFirstRunData(string connectionString, FirstRunData data)
    {
        Write(new AppSettings
        {
            ConnectionStrings = new AppSettings.ConnectionStringsSection { Default = connectionString },
            FirstRunData = data
        });
    }

    public static void ClearFirstRunData()
    {
        var settings = Load();
        settings.FirstRunData = null;
        Write(settings);
    }

    private static void Write(AppSettings settings)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(SettingsFilePath)!);
        File.WriteAllText(SettingsFilePath, JsonSerializer.Serialize(settings, _json));
    }
}

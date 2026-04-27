using System.IO;
using System.Text.Json;

namespace Vimina.Core.Config;

public static class ConfigManager
{
    private static readonly string ConfigPath = Path.Combine(AppContext.BaseDirectory, "config.json");
    private static readonly string DataDir = Path.Combine(AppContext.BaseDirectory, "data");

    public static ViminaConfig Current { get; private set; } = new();

    public static void EnsureDirectories()
    {
        if (!Directory.Exists(DataDir))
            Directory.CreateDirectory(DataDir);
    }

    public static void Load()
    {
        EnsureDirectories();
        if (File.Exists(ConfigPath))
        {
            try
            {
                var json = File.ReadAllText(ConfigPath);
                var loaded = JsonSerializer.Deserialize<ViminaConfig>(json);
                if (loaded != null)
                    Current = loaded;
            }
            catch { }
        }
        else
        {
            Save();
        }
    }

    public static void Save()
    {
        EnsureDirectories();
        var json = JsonSerializer.Serialize(Current, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(ConfigPath, json);
    }

    public static string ScanResultPath => Path.Combine(DataDir, "scan_result.json");
    public static string LabelMapPath => Path.Combine(DataDir, "label_map.json");
}

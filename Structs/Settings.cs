using BepInEx;
using BepInEx.Configuration;
using System.IO;

namespace CrimsonSQL.Structs;

public readonly struct Settings
{
    public static ConfigEntry<string> DatabaseName { get; private set; }
    public static ConfigEntry<string> Host { get; private set; }
    public static ConfigEntry<int> Port { get; private set; }
    public static ConfigEntry<string> UserName { get; private set; }
    public static ConfigEntry<string> Password { get; private set; }

    public static bool MySQLConfigured { get; set; } = false;

    public static void InitConfig()
    {
        DatabaseName = InitConfigEntry("ServerConnection", "DatabaseName", "",
            "The name of your MySQL database.");
        Host = InitConfigEntry("ServerConnection", "Host", "",
            "The host address of your MySQL database.");
        Port = InitConfigEntry("ServerConnection", "Port", 3306,
            "The port of your database server.");
        UserName = InitConfigEntry("ServerConnection", "Username", "",
            "The login username for your database.");
        Password = InitConfigEntry("ServerConnection", "Password", "",
            "The login password for your database.");

        if (
            !string.IsNullOrEmpty(DatabaseName.Value)
            && !string.IsNullOrEmpty(Host.Value)
            && Port.Value != 0
            && !string.IsNullOrEmpty(UserName.Value)
            && !string.IsNullOrEmpty(Password.Value)
          ) MySQLConfigured = true;
    }

    static ConfigEntry<T> InitConfigEntry<T>(string section, string key, T defaultValue, string description)
    {
        // Bind the configuration entry and get its value
        var entry = Plugin.Instance.Config.Bind(section, key, defaultValue, description);

        // Check if the key exists in the configuration file and retrieve its current value
        var newFile = Path.Combine(Paths.ConfigPath, $"{MyPluginInfo.PLUGIN_GUID}.cfg");

        if (File.Exists(newFile))
        {
            var config = new ConfigFile(newFile, true);
            if (config.TryGetEntry(section, key, out ConfigEntry<T> existingEntry))
            {
                // If the entry exists, update the value to the existing value
                entry.Value = existingEntry.Value;
            }
        }
        return entry;
    }
}

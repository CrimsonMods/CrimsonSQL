using BepInEx;
using BepInEx.Logging;
using BepInEx.Unity.IL2CPP;
using CrimsonSQL.API;
using CrimsonSQL.Services;
using CrimsonSQL.Structs;
using HarmonyLib;

namespace CrimsonSQL;

[BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
public class Plugin : BasePlugin
{
    Harmony _harmony;
    internal static Plugin Instance { get; private set; }
    public static Harmony Harmony => Instance._harmony;
    public static ManualLogSource LogInstance => Instance.Log;
    public static Settings Settings;
    public static ISQLService SQLService {  get; private set; }
    

    public override void Load()
    {
        Instance = this;
        Settings = new Settings();
        Settings.InitConfig();

        _harmony = new Harmony(MyPluginInfo.PLUGIN_GUID);
        _harmony.PatchAll(System.Reflection.Assembly.GetExecutingAssembly());
        
        if (Settings.MySQLConfigured)
        {
            SQLService = new SQLService();
        }
    }

    public override bool Unload()
    {
        _harmony?.UnpatchSelf();
        return true;
    }
}

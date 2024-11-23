
# CrimsonSQL Documentation

CrimsonSQL is a MySQL database integration tool for V Rising BepInEx mods. It provides a simple interface to interact with MySQL databases from your mods.

## Setup

1. Add CrimsonSQL as a SoftDependency in your Plugin.cs:
```csharp
[BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
[BepInDependency("CrimsonSQL", BepInDependency.DependencyFlags.SoftDependency)]
internal class Plugin : BasePlugin
```

2. Create a way to check for CrimsonSQL, this can be in your main plugin.cs or some other utility script.
```csharp
public static bool SQLFound = false;
public override void Load()
{
    _harmony = Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly());

    foreach (var plugin in IL2CPPChainLoader.Instance.Plugins)
	{
	    var metadata = plugin.Value.Metadata;
		if (metadata.GUID.Equals("CrimsonSQL"))
		{
			// found it
            if(CrimsonSQL.Settings.MySQLConfigured == true)
            {
                SQLFound = true;
            }
			
			break;
		}
	}
}
```

3. In your mod's code, you can now use CrimsonSQL to interact with MySQL databases. Here's some examples:

- Creating Tables
```csharp 
var columns = new Dictionary<string, string>
{
    { "id", "INT PRIMARY KEY AUTO_INCREMENT" },
    { "player_name", "VARCHAR(255)" },
    { "score", "INT" }
};

CrimsonSQL.Plugin.SQLService.CreateTable("player_stats", columns);
```

- Inserting Data
```csharp
var values = new Dictionary<string, object>
{
    { "player_name", "Dracula" },
    { "score", 1000 }
};

CrimsonSQL.Plugin.SQLService.Insert("player_stats", values);
```

- Deleting Data
```csharp
var conditions = new Dictionary<string, object>
{
    { "player_name", "Dracula" }
};

CrimsonSQL.Plugin.SQLService.Delete("player_stats", conditions);
```

- Raw Queries
```csharp
// Execute custom SELECT queries
var query = "SELECT * FROM player_stats WHERE score > @minScore";
var parameters = new Dictionary<string, object>
{
    { "minScore", 500 }
};
var results = _sqlService.ExecuteQuery(query, parameters);

// Execute custom INSERT/UPDATE/DELETE queries
_sqlService.ExecuteNonQuery("UPDATE player_stats SET score = score + 100 WHERE player_name = @name", 
    new Dictionary<string, object> { { "name", "Dracula" } });
```

For examples in a live mod, check out the [CrimsonBanned](https://github.com/CrimsonMods/CrimsonBanned) respository. 
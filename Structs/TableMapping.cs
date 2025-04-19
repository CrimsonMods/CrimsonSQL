using System.Collections.Generic;
using System.IO;
using BepInEx;
using System.Text.Json;

namespace CrimsonSQL.Structs
{
    public class TableMapping
    {
        private static string mappingFilePath = Path.Combine(Paths.ConfigPath, "CrimsonSQL_TableMappings.json");
        private Dictionary<string, string> _tableMappings;

        public TableMapping()
        {
            _tableMappings = new Dictionary<string, string>();
            LoadMappings();
        }

        public string GetMappedTableName(string originalTableName)
        {
            if (_tableMappings.TryGetValue(originalTableName, out string mappedName))
            {
                return mappedName;
            }
            else
            {
                SetMapping(originalTableName, originalTableName);
                return originalTableName;
            }
        }

        public void SetMapping(string originalTableName, string mappedTableName)
        {
            _tableMappings[originalTableName] = mappedTableName;
            SaveMappings();
        }

        public void RemoveMapping(string originalTableName)
        {
            if (_tableMappings.ContainsKey(originalTableName))
            {
                _tableMappings.Remove(originalTableName);
                SaveMappings();
            }
        }

        public Dictionary<string, string> GetAllMappings()
        {
            return new Dictionary<string, string>(_tableMappings);
        }

        private void LoadMappings()
        {
            try
            {
                if (File.Exists(mappingFilePath))
                {
                    string json = File.ReadAllText(mappingFilePath);
                    _tableMappings = JsonSerializer.Deserialize<Dictionary<string, string>>(json)
                        ?? new Dictionary<string, string>();
                    Plugin.LogInstance.LogInfo($"Loaded {_tableMappings.Count} table mappings from configuration");
                    ApplyTableRenamesInDatabase();
                }
                else
                {
                    Plugin.LogInstance.LogInfo("No table mapping configuration found. Creating new file.");
                    SaveMappings();
                }
            }
            catch (System.Exception ex)
            {
                Plugin.LogInstance.LogError($"Error loading table mappings: {ex.Message}");
                _tableMappings = new Dictionary<string, string>();
            }
        }

        private void SaveMappings()
        {
            try
            {
                var options = new JsonSerializerOptions { WriteIndented = true };
                string json = JsonSerializer.Serialize(_tableMappings, options);
                File.WriteAllText(mappingFilePath, json);
            }
            catch (System.Exception ex)
            {
                Plugin.LogInstance.LogError($"Error saving table mappings: {ex.Message}");
            }
        }

        private void ApplyTableRenamesInDatabase()
        {
            // Skip if SQL service is not available or not configured
            if (Plugin.SQLService == null || !Settings.MySQLConfigured)
            {
                return;
            }

            try
            {
                // Get existing tables in the database
                var query = "SHOW TABLES;";
                var existingTablesResult = Plugin.SQLService.ExecuteQuery(query);

                if (existingTablesResult != null && existingTablesResult.Rows.Count > 0)
                {
                    var existingTables = new List<string>();
                    foreach (System.Data.DataRow row in existingTablesResult.Rows)
                    {
                        existingTables.Add(row[0].ToString());
                    }

                    // For each mapping where original table exists but mapped name doesn't match
                    foreach (var mapping in _tableMappings)
                    {
                        // If the source name exists in the database but is different from the target name
                        if (existingTables.Contains(mapping.Key) && mapping.Key != mapping.Value && !existingTables.Contains(mapping.Value))
                        {
                            RenameTableInDatabase(mapping.Key, mapping.Value);
                        }
                    }
                }
            }
            catch (System.Exception ex)
            {
                Plugin.LogInstance.LogError($"Error applying table renames in database: {ex.Message}");
            }
        }

        private void RenameTableInDatabase(string oldName, string newName)
        {
            try
            {
                if (string.IsNullOrEmpty(oldName) || string.IsNullOrEmpty(newName) || oldName == newName)
                {
                    return;
                }

                string checkQuery = $"SHOW TABLES LIKE '{oldName}';";
                var result = Plugin.SQLService.ExecuteQuery(checkQuery);

                if (result == null || result.Rows.Count == 0)
                {
                    Plugin.LogInstance.LogWarning($"Cannot rename table '{oldName}' to '{newName}': source table does not exist");
                    return;
                }

                string checkNewQuery = $"SHOW TABLES LIKE '{newName}';";
                var newResult = Plugin.SQLService.ExecuteQuery(checkNewQuery);

                if (newResult != null && newResult.Rows.Count > 0)
                {
                    Plugin.LogInstance.LogWarning($"Cannot rename table '{oldName}' to '{newName}': target table already exists");
                    return;
                }

                string renameQuery = $"RENAME TABLE `{oldName}` TO `{newName}`;";
                Plugin.SQLService.ExecuteNonQuery(renameQuery);
                Plugin.LogInstance.LogInfo($"Renamed table in database: '{oldName}' to '{newName}'");
            }
            catch (System.Exception ex)
            {
                Plugin.LogInstance.LogError($"Failed to rename table '{oldName}' to '{newName}': {ex.Message}");
            }
        }
    }
}
using CrimsonSQL.API;
using CrimsonSQL.Structs;
using CrimsonSQL.Utility;
using MySql.Data.MySqlClient;
using System.Collections.Generic;
using System.Data;
using System.Linq;

namespace CrimsonSQL.Services;

internal class SQLService : ISQLService
{
    private static string connectionString;

    public SQLService()
    {
        AssemblyResolver.Resolve();

        connectionString = $"Server={Settings.Host.Value};Database={Settings.DatabaseName.Value};User ID={Settings.UserName.Value};Password={Settings.Password.Value};CharSet=utf8mb4;Convert Zero Datetime=True;Allow Zero Datetime=True;";

        Connect();
    }

    public bool Connect()
    {
        try
        {
            using (var connection = new MySqlConnection(connectionString))
            {
                connection.Open();
                Plugin.LogInstance.LogInfo("Connected to MySQL database.");
                return true;
            }
        }
        catch (MySqlException e)
        {
            Settings.MySQLConfigured = false;
            Plugin.LogInstance.LogError("Failed to connect to MySQL database.");
            Plugin.LogInstance.LogError(e.Message);
            return false;
        }
    }

    public void CreateTable(string tableName, Dictionary<string, string> columns)
    {
        if (!Settings.MySQLConfigured) { Plugin.LogInstance.LogError("Attempted to use CrimsonSQL with a misconfigured SQL."); }
        var columnDefinitions = string.Join(", ", columns.Select(kvp => $"{kvp.Key} {kvp.Value}"));
        string query = $@"CREATE TABLE IF NOT EXISTS {tableName} ({columnDefinitions});";
        ExecuteNonQuery(query);
    }

    public void Insert(string tableName, Dictionary<string, object> values)
    {
        if (!Settings.MySQLConfigured) { Plugin.LogInstance.LogError("Attempted to use CrimsonSQL with a misconfigured SQL."); }
        var columns = string.Join(", ", values.Keys);
        var parameters = string.Join(", ", values.Keys.Select(k => $"@{k}"));

        string query = $"INSERT INTO {tableName} ({columns}) VALUES ({parameters})";

        ExecuteNonQuery(query, values);
    }

    public void Delete(string tableName, Dictionary<string, object> whereConditions)
    {
        if (!Settings.MySQLConfigured) { Plugin.LogInstance.LogError("Attempted to use CrimsonSQL with a misconfigured SQL."); }
        var where = string.Join(" AND ", whereConditions.Keys.Select(k => $"{k} = @{k}"));
        string query = $"DELETE FROM {tableName} WHERE {where}";

        ExecuteNonQuery(query, whereConditions);
    }

    public DataTable Select(string tableName, string[] columns = null, Dictionary<string, object> whereConditions = null)
    {
        if (!Settings.MySQLConfigured) { Plugin.LogInstance.LogError("Attempted to use CrimsonSQL with a misconfigured SQL."); }
        var selectedColumns = columns?.Any() == true ? string.Join(", ", columns) : "*";
        var query = $"SELECT {selectedColumns} FROM {tableName}";

        if (whereConditions?.Any() == true)
        {
            var where = string.Join(" AND ", whereConditions.Keys.Select(k => $"{k} = @{k}"));
            query += $" WHERE {where}";
        }

        return ExecuteQuery(query, whereConditions);
    }

    public DataTable ExecuteQuery(string query, Dictionary<string, object> parameters = null)
    {
        if (!Settings.MySQLConfigured) { Plugin.LogInstance.LogError("Attempted to use CrimsonSQL with a misconfigured SQL."); }
        using var connection = new MySqlConnection(connectionString);
        using var command = new MySqlCommand(query, connection);

        if (parameters != null)
        {
            foreach (var param in parameters)
            {
                command.Parameters.AddWithValue($"@{param.Key}", param.Value);
            }
        }

        connection.Open();
        using var reader = command.ExecuteReader();
        var results = new DataTable();
        results.Load(reader);
        return results;
    }

    public void ExecuteNonQuery(string query, Dictionary<string, object> parameters = null)
    {
        if (!Settings.MySQLConfigured) { Plugin.LogInstance.LogError("Attempted to use CrimsonSQL with a misconfigured SQL."); }
        using var connection = new MySqlConnection(connectionString);
        using var command = new MySqlCommand(query, connection);

        if (parameters != null)
        {
            foreach (var param in parameters)
            {
                command.Parameters.AddWithValue($"@{param.Key}", param.Value);
            }
        }

        connection.Open();
        command.ExecuteNonQuery();
    }
}

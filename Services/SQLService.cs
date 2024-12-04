using CrimsonSQL.API;
using CrimsonSQL.Structs;
using CrimsonSQL.Utility;
using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;

namespace CrimsonSQL.Services;

internal class SQLService : ISQLService
{
    private static string connectionString;
    private static bool reportConnection = true;
    public SQLService()
    {
        AssemblyResolver.Resolve();

        connectionString = $"Server={Settings.Host.Value};Database={Settings.DatabaseName.Value};User ID={Settings.UserName.Value};Password={Settings.Password.Value};{Settings.Parameters.Value}";

        Connect();
    }

    public bool Connect()
    {
        try
        {
            using (var connection = new MySqlConnection(connectionString))
            {
                connection.Open();
                if (reportConnection)
                {
                    reportConnection = false;
                    Plugin.LogInstance.LogInfo("Connected to MySQL database.");
                }
                return true;
            }
        }
        catch (MySqlException e)
        {
            Settings.MySQLConfigured = false;
            Plugin.LogInstance.LogError("Failed to connect to MySQL database. " +
                $"\nError Info:" +
                $"\nError Code: {e.ErrorCode} " +
                $"\nNumber: {e.Number} " +
                $"\nMessage: {e.Message}" +
                $"\nInner: {e.InnerException.Message}");

            reportConnection = true;
            return false;
        }
    }

    public void CreateTable(string tableName, Dictionary<string, string> columns)
    {
        if (!Settings.MySQLConfigured) { Plugin.LogInstance.LogError("Attempted to use CrimsonSQL with a misconfigured SQL."); return; }
        var columnDefinitions = string.Join(", ", columns.Select(kvp => $"{kvp.Key} {kvp.Value}"));
        string query = $@"CREATE TABLE IF NOT EXISTS {tableName} ({columnDefinitions});";
        ExecuteNonQuery(query);
    }

    public int Insert(string tableName, Dictionary<string, object> values, List<int> handledExceptions = null)
    {
        if (!Settings.MySQLConfigured) { Plugin.LogInstance.LogError("Attempted to use CrimsonSQL with a misconfigured SQL."); return -1; }
        var columns = string.Join(", ", values.Keys);
        var parameters = string.Join(", ", values.Keys.Select(k => $"@{k}"));

        string query = $"INSERT INTO {tableName} ({columns}) VALUES ({parameters}); SELECT LAST_INSERT_ID();";

        using var connection = new MySqlConnection(connectionString);
        using var command = new MySqlCommand(query, connection);

        if (values != null)
        {
            foreach (var param in values)
            {
                command.Parameters.AddWithValue($"@{param.Key}", param.Value);
            }
        }

        try
        {
            connection.Open();
            return Convert.ToInt32(command.ExecuteScalar());
        }
        catch (MySqlException e)
        {
            if (handledExceptions == null || !handledExceptions.Contains(e.Number))
            {
                Plugin.LogInstance.LogError("MySQL Exception occurred: " +
                $"\nError Code: {e.ErrorCode} " +
                $"\nNumber: {e.Number} " +
                $"\nMessage: {e.Message}" +
                $"\nInner: {e.InnerException?.Message}");
            }

            return -e.Number;
        }
    }

    public void Delete(string tableName, Dictionary<string, object> whereConditions)
    {
        if (!Settings.MySQLConfigured) { Plugin.LogInstance.LogError("Attempted to use CrimsonSQL with a misconfigured SQL."); return; }
        var where = string.Join(" AND ", whereConditions.Keys.Select(k => $"{k} = @{k}"));
        string query = $"DELETE FROM {tableName} WHERE {where}";

        ExecuteNonQuery(query, whereConditions);
    }

    public DataTable Select(string tableName, string[] columns = null, Dictionary<string, object> whereConditions = null)
    {
        if (!Settings.MySQLConfigured) { Plugin.LogInstance.LogError("Attempted to use CrimsonSQL with a misconfigured SQL."); return null; }
        var selectedColumns = columns?.Any() == true ? string.Join(", ", columns) : "*";
        var query = $"SELECT {selectedColumns} FROM {tableName}";

        if (whereConditions?.Any() == true)
        {
            var where = string.Join(" AND ", whereConditions.Keys.Select(k => $"{k} = @{k}"));
            query += $" WHERE {where}";
        }

        return ExecuteQuery(query, whereConditions);
    }

    public int Replace(string tableName, Dictionary<string, object> whereConditions, Dictionary<string, object> newValues)
    {
        if (!Settings.MySQLConfigured) { Plugin.LogInstance.LogError("Attempted to use CrimsonSQL with a misconfigured SQL."); return -1; }

        using var connection = new MySqlConnection(connectionString);
        connection.Open();
        using var transaction = connection.BeginTransaction();

        try
        {
            // Delete the existing row
            var where = string.Join(" AND ", whereConditions.Keys.Select(k => $"{k} = @{k}"));
            string deleteQuery = $"DELETE FROM {tableName} WHERE {where}";
            using (var deleteCommand = new MySqlCommand(deleteQuery, connection, transaction))
            {
                foreach (var param in whereConditions)
                {
                    deleteCommand.Parameters.AddWithValue($"@{param.Key}", param.Value);
                }
                deleteCommand.ExecuteNonQuery();
            }

            // Insert the new row and get ID
            var columns = string.Join(", ", newValues.Keys);
            var parameters = string.Join(", ", newValues.Keys.Select(k => $"@{k}"));
            string insertQuery = $"INSERT INTO {tableName} ({columns}) VALUES ({parameters}); SELECT LAST_INSERT_ID();";

            using (var insertCommand = new MySqlCommand(insertQuery, connection, transaction))
            {
                foreach (var param in newValues)
                {
                    insertCommand.Parameters.AddWithValue($"@{param.Key}", param.Value);
                }
                int newId = Convert.ToInt32(insertCommand.ExecuteScalar());
                transaction.Commit();
                return newId;
            }
        }
        catch (MySqlException e)
        {
            transaction.Rollback();
            Plugin.LogInstance.LogError("MySQL Exception occurred during Replace: " +
                $"\nError Code: {e.ErrorCode} " +
                $"\nNumber: {e.Number} " +
                $"\nMessage: {e.Message}" +
                $"\nInner: {e.InnerException?.Message}");
                
            return -e.Number;
        }
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

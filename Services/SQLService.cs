using CrimsonSQL.API;
using CrimsonSQL.Structs;
using MySqlConnector;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;

namespace CrimsonSQL.Services;

public class SQLService : ISQLService
{
    private static string connectionString;
    private static bool reportConnection = true;
    public SQLService()
    {
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
                    Settings.MySQLConfigured = true;
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
                $"\nInner: {e.InnerException?.Message}");

            reportConnection = true;
            return false;
        }
    }

    public void CreateTable(string tableName, Dictionary<string, string> columns)
    {
        if (!Settings.MySQLConfigured) { Plugin.LogInstance.LogError("Attempted to use CrimsonSQL with a misconfigured SQL."); return; }
        tableName = GetMappedTableName(tableName);
        var columnDefinitions = string.Join(", ", columns.Select(kvp => $"{kvp.Key} {kvp.Value}"));
        string query = $@"CREATE TABLE IF NOT EXISTS {tableName} ({columnDefinitions});";
        ExecuteNonQuery(query);
    }

    public void ClearTable(string tableName)
    {
        if (!Settings.MySQLConfigured) { Plugin.LogInstance.LogError("Attempted to use CrimsonSQL with a misconfigured SQL."); return; }
        tableName = GetMappedTableName(tableName);

        string query = $"TRUNCATE TABLE {tableName};";

        try
        {
            ExecuteNonQuery(query);
            Plugin.LogInstance.LogInfo($"Table '{tableName}' has been cleared successfully.");
        }
        catch (MySqlException e)
        {
            string deleteQuery = $"DELETE FROM {tableName};";
            ExecuteNonQuery(deleteQuery);
            Plugin.LogInstance.LogInfo($"Table '{tableName}' has been cleared using DELETE method.");
        }
    }

    #region Standard Operations

    public int Insert(string tableName, Dictionary<string, object> values, List<int> handledExceptions = null)
    {
        if (!Settings.MySQLConfigured) { Plugin.LogInstance.LogError("Attempted to use CrimsonSQL with a misconfigured SQL."); return -1; }
        var columns = string.Join(", ", values.Keys);
        var parameters = string.Join(", ", values.Keys.Select(k => $"@{k}"));
        tableName = GetMappedTableName(tableName);

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
        tableName = GetMappedTableName(tableName);
        var where = string.Join(" AND ", whereConditions.Keys.Select(k => $"{k} = @{k}"));
        string query = $"DELETE FROM {tableName} WHERE {where}";

        ExecuteNonQuery(query, whereConditions);
    }

    public DataTable Select(string tableName, string[] columns = null, Dictionary<string, object> whereConditions = null)
    {
        if (!Settings.MySQLConfigured) { Plugin.LogInstance.LogError("Attempted to use CrimsonSQL with a misconfigured SQL."); return null; }
        tableName = GetMappedTableName(tableName);
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
        tableName = GetMappedTableName(tableName);
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

    #endregion
    #region Execute

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

    #endregion

    #region Batch Operations

    public List<int> ExecuteBatch(List<BatchOperation> operations)
    {
        if (!Settings.MySQLConfigured)
        {
            Plugin.LogInstance.LogError("Attempted to use CrimsonSQL with a misconfigured SQL.");
            return operations.Select(o => -1).ToList();
        }

        foreach (var operation in operations)
        {
            operation.TableName = GetMappedTableName(operation.TableName);
        }

        using var connection = new MySqlConnection(connectionString);
        connection.Open();
        using var transaction = connection.BeginTransaction();

        try
        {
            List<int> results = new List<int>();

            foreach (var operation in operations)
            {
                int result = -1;

                switch (operation.OperationType)
                {
                    case BatchOperationType.Insert:
                        result = ExecuteInsertInBatch(connection, transaction, operation);
                        break;

                    case BatchOperationType.Delete:
                        ExecuteDeleteInBatch(connection, transaction, operation);
                        result = 0; // Success indicator
                        break;

                    case BatchOperationType.Update:
                        result = ExecuteUpdateInBatch(connection, transaction, operation);
                        break;

                    case BatchOperationType.Replace:
                        result = ExecuteReplaceInBatch(connection, transaction, operation);
                        break;

                    case BatchOperationType.CustomSQL:
                        result = ExecuteCustomSQLInBatch(connection, transaction, operation);
                        break;
                }

                operation.ResultId = result;
                results.Add(result);
            }

            transaction.Commit();
            return results;
        }
        catch (MySqlException e)
        {
            transaction.Rollback();
            Plugin.LogInstance.LogError("MySQL Exception occurred during batch operation: " +
                $"\nError Code: {e.ErrorCode} " +
                $"\nNumber: {e.Number} " +
                $"\nMessage: {e.Message}" +
                $"\nInner: {e.InnerException?.Message}");

            return operations.Select(o => -e.Number).ToList();
        }
    }

    private int ExecuteInsertInBatch(MySqlConnection connection, MySqlTransaction transaction, BatchOperation operation)
    {
        var columns = string.Join(", ", operation.Values.Keys);
        var parameters = string.Join(", ", operation.Values.Keys.Select(k => $"@{k}"));

        string query = $"INSERT INTO {operation.TableName} ({columns}) VALUES ({parameters}); SELECT LAST_INSERT_ID();";

        using var command = new MySqlCommand(query, connection, transaction);

        foreach (var param in operation.Values)
        {
            command.Parameters.AddWithValue($"@{param.Key}", param.Value);
        }

        try
        {
            return Convert.ToInt32(command.ExecuteScalar());
        }
        catch (MySqlException e)
        {
            if (operation.HandledExceptions == null || !operation.HandledExceptions.Contains(e.Number))
            {
                throw; // Re-throw to be caught by the outer try-catch
            }

            return -e.Number;
        }
    }

    private void ExecuteDeleteInBatch(MySqlConnection connection, MySqlTransaction transaction, BatchOperation operation)
    {
        var where = string.Join(" AND ", operation.WhereConditions.Keys.Select(k => $"{k} = @{k}"));
        string query = $"DELETE FROM {operation.TableName} WHERE {where}";

        using var command = new MySqlCommand(query, connection, transaction);

        foreach (var param in operation.WhereConditions)
        {
            command.Parameters.AddWithValue($"@{param.Key}", param.Value);
        }

        command.ExecuteNonQuery();
    }

    private int ExecuteUpdateInBatch(MySqlConnection connection, MySqlTransaction transaction, BatchOperation operation)
    {
        var setClause = string.Join(", ", operation.Values.Keys.Select(k => $"{k} = @{k}"));
        var whereClause = string.Join(" AND ", operation.WhereConditions.Keys.Select(k => $"{k} = @where_{k}"));

        string query = $"UPDATE {operation.TableName} SET {setClause} WHERE {whereClause}";

        using var command = new MySqlCommand(query, connection, transaction);

        foreach (var param in operation.Values)
        {
            command.Parameters.AddWithValue($"@{param.Key}", param.Value);
        }

        foreach (var param in operation.WhereConditions)
        {
            command.Parameters.AddWithValue($"@where_{param.Key}", param.Value);
        }

        return command.ExecuteNonQuery();
    }

    private int ExecuteReplaceInBatch(MySqlConnection connection, MySqlTransaction transaction, BatchOperation operation)
    {
        // Delete the existing row
        var where = string.Join(" AND ", operation.WhereConditions.Keys.Select(k => $"{k} = @{k}"));
        string deleteQuery = $"DELETE FROM {operation.TableName} WHERE {where}";

        using (var deleteCommand = new MySqlCommand(deleteQuery, connection, transaction))
        {
            foreach (var param in operation.WhereConditions)
            {
                deleteCommand.Parameters.AddWithValue($"@{param.Key}", param.Value);
            }
            deleteCommand.ExecuteNonQuery();
        }

        // Insert the new row and get ID
        var columns = string.Join(", ", operation.Values.Keys);
        var parameters = string.Join(", ", operation.Values.Keys.Select(k => $"@{k}"));
        string insertQuery = $"INSERT INTO {operation.TableName} ({columns}) VALUES ({parameters}); SELECT LAST_INSERT_ID();";

        using (var insertCommand = new MySqlCommand(insertQuery, connection, transaction))
        {
            foreach (var param in operation.Values)
            {
                insertCommand.Parameters.AddWithValue($"@{param.Key}", param.Value);
            }
            return Convert.ToInt32(insertCommand.ExecuteScalar());
        }
    }

    private int ExecuteCustomSQLInBatch(MySqlConnection connection, MySqlTransaction transaction, BatchOperation operation)
    {
        using var command = new MySqlCommand(operation.CustomQuery, connection, transaction);

        foreach (var param in operation.Values)
        {
            command.Parameters.AddWithValue($"@{param.Key}", param.Value);
        }

        if (operation.CustomQuery.Contains("SELECT LAST_INSERT_ID()"))
            return Convert.ToInt32(command.ExecuteScalar());
        else
            return command.ExecuteNonQuery();
    }

    #endregion
    #region Helper Methods

    private string GetMappedTableName(string originalTableName)
    {
        return Plugin.TableMapping.GetMappedTableName(originalTableName);
    }

    #endregion
}

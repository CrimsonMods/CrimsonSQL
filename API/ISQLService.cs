using System.Collections.Generic;
using System.Data;

namespace CrimsonSQL.API;

public interface ISQLService
{
    /// <summary>
    /// Attempts to establish a connection to the configured MySQL database
    /// </summary>
    /// <returns>True if connection successful, false otherwise</returns>
    bool Connect();

    /// <summary>
    /// Executes a non-query SQL command with optional parameters
    /// </summary>
    /// <param name="query">The SQL query to execute</param>
    /// <param name="parameters">Optional dictionary of parameters to use in the query</param>
    void ExecuteNonQuery(string query, Dictionary<string, object> parameters = null);

    /// <summary>
    /// Executes a SQL query and returns the results as a DataTable
    /// </summary>
    /// <param name="query">The SQL query to execute</param>
    /// <param name="parameters">Optional dictionary of parameters to use in the query</param>
    /// <returns>DataTable containing the query results</returns>
    DataTable ExecuteQuery(string query, Dictionary<string, object> parameters = null);

    /// <summary>
    /// Creates a new table in the database if it doesn't exist
    /// </summary>
    /// <param name="tableName">Name of the table to create</param>
    /// <param name="columns">Dictionary of column names and their SQL data types</param>
    void CreateTable(string tableName, Dictionary<string, string> columns);

    /// <summary>
    /// Inserts a new row into the specified table
    /// </summary>
    /// <param name="tableName">Name of the target table</param>
    /// <param name="values">Dictionary of column names and values to insert</param>
    /// <returns>The ID of the newly inserted row</returns>
    int Insert(string tableName, Dictionary<string, object> values);

    /// <summary>
    /// Deletes rows from the specified table that match the given conditions
    /// </summary>
    /// <param name="tableName">Name of the target table</param>
    /// <param name="whereConditions">Dictionary of column names and values to match for deletion</param>
    void Delete(string tableName, Dictionary<string, object> whereConditions);

    /// <summary>
    /// Selects rows from the specified table based on optional conditions
    /// </summary>
    /// <param name="tableName">Name of the target table</param>
    /// <param name="columns">Optional array of column names to select. Null selects all columns</param>
    /// <param name="whereConditions">Optional dictionary of column names and values to filter results</param>
    /// <returns>DataTable containing the selected rows</returns>
    DataTable Select(string tableName, string[] columns = null, Dictionary<string, object> whereConditions = null);

    /// <summary>
    /// Replaces existing rows in the table with new values using a transaction
    /// </summary>
    /// <param name="tableName">Name of the target table</param>
    /// <param name="whereConditions">Dictionary of column names and values to identify rows to replace</param>
    /// <param name="newValues">Dictionary of column names and new values to insert</param>
    /// <returns>True if replacement successful, false otherwise</returns>
    bool Replace(string tableName, Dictionary<string, object> whereConditions, Dictionary<string, object> newValues);
}
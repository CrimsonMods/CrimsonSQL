using System.Collections.Generic;
using System.Data;

namespace CrimsonSQL.API;

public interface ISQLService
{
    bool Connect();
    void ExecuteNonQuery(string query, Dictionary<string, object> parameters = null);
    DataTable ExecuteQuery(string query, Dictionary<string, object> parameters = null);
    void CreateTable(string tableName, Dictionary<string, string> columns);
    int Insert(string tableName, Dictionary<string, object> values);
    void Delete(string tableName, Dictionary<string, object> whereConditions);
    DataTable Select(string tableName, string[] columns = null, Dictionary<string, object> whereConditions = null);
}

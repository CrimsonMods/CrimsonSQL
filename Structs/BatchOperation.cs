using System.Collections.Generic;

namespace CrimsonSQL.Structs
{
    public enum BatchOperationType
    {
        Insert,
        Delete,
        Update,
        Replace,
        CustomSQL
    }

    public class BatchOperation
    {
        public BatchOperationType OperationType { get; set; }
        public string TableName { get; set; }
        public Dictionary<string, object> Values { get; set; }
        public Dictionary<string, object> WhereConditions { get; set; }
        public int ResultId { get; set; } = -1;
        public List<int> HandledExceptions { get; set; }
        public string CustomQuery { get; set; }

        // Constructor for Insert
        public static BatchOperation CreateInsert(string tableName, Dictionary<string, object> values, List<int> handledExceptions = null)
        {
            return new BatchOperation
            {
                OperationType = BatchOperationType.Insert,
                TableName = tableName,
                Values = values,
                HandledExceptions = handledExceptions
            };
        }

        // Constructor for Delete
        public static BatchOperation CreateDelete(string tableName, Dictionary<string, object> whereConditions)
        {
            return new BatchOperation
            {
                OperationType = BatchOperationType.Delete,
                TableName = tableName,
                WhereConditions = whereConditions
            };
        }

        // Constructor for Update
        public static BatchOperation CreateUpdate(string tableName, Dictionary<string, object> values, Dictionary<string, object> whereConditions)
        {
            return new BatchOperation
            {
                OperationType = BatchOperationType.Update,
                TableName = tableName,
                Values = values,
                WhereConditions = whereConditions
            };
        }

        // Constructor for Replace
        public static BatchOperation CreateReplace(string tableName, Dictionary<string, object> whereConditions, Dictionary<string, object> newValues)
        {
            return new BatchOperation
            {
                OperationType = BatchOperationType.Replace,
                TableName = tableName,
                Values = newValues,
                WhereConditions = whereConditions
            };
        }

        // Constructor for custom SQL
        public static BatchOperation CreateCustomSQL(string tableName, string customQuery, Dictionary<string, object> parameters)
        {
            return new BatchOperation
            {
                OperationType = BatchOperationType.CustomSQL,
                TableName = tableName,
                Values = parameters,
                CustomQuery = customQuery
            };
        }
    }
}
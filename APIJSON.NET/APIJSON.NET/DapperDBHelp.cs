namespace APIJSON.NET
{
    using Dapper;
    using System.Collections.Generic;
    using System.Data.SqlClient;
    public static class DapperDBHelp
    {
        public static dynamic QueryFirstOrDefault(string ConnectionString, string sql, object param)
        {
            using (var sqlConnection = new SqlConnection(ConnectionString))
            {
                sqlConnection.Open();
                return sqlConnection.QueryFirstOrDefault(sql, param);
            }
        }
        public static IEnumerable<dynamic> Query(string ConnectionString, string sql, object param)
        {
            using (var sqlConnection = new SqlConnection(ConnectionString))
            {
                sqlConnection.Open();
                return sqlConnection.Query(sql, param);
            }
        }
    }
}
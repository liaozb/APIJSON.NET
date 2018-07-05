namespace APIJSON.NET
{
    using Dapper;
    using Microsoft.Extensions.Options;
    using MySql.Data.MySqlClient;
    using System.Collections.Generic;
    using System.Data;
    using System.Data.SqlClient;
    public  class DapperHelper
    {
      
        private DapperOptions _options;

        public DapperHelper(IOptions<DapperOptions> options)
        {
            this._options = options.Value;
        }
        public IDbConnection Connection
        {
            get
            {
                if (!string.IsNullOrEmpty(_options.MySql))
                {
                    return new MySqlConnection(_options.MySql);
                }
                else
                {
                    return new SqlConnection(_options.SqlServer);
                }
            }
        }
     
        public  dynamic QueryFirstOrDefault(string sql, object param)
        {
            using (var sqlConnection = Connection)
            {
                sqlConnection.Open();
                return sqlConnection.QueryFirstOrDefault(sql, param);
            }
        }
        public  IEnumerable<dynamic> Query( string sql, object param)
        {
            using (var sqlConnection = Connection)
            {
                sqlConnection.Open();
                return sqlConnection.Query(sql, param);
            }
        }
    }
}
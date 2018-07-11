using Microsoft.Extensions.Options;
using SqlSugar;

namespace APIJSON.NET
{
    public class DbContext
    {
        public DbContext(IOptions<DbOptions> options)
        {
            Db = new SqlSugarClient(new ConnectionConfig()
            {
                ConnectionString = options.Value.ConnectionString,
                DbType = options.Value.DbType,
                IsAutoCloseConnection = true
            });
        }
        public SqlSugarClient Db;
    }
}

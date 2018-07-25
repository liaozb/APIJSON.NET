using APIJSON.NET.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using SqlSugar;
using System;
using System.Collections.Generic;

namespace APIJSON.NET
{
    public class DbContext
    {
        public DbContext(IConfiguration options)
        {
            Db = new SqlSugarClient(new ConnectionConfig()
            {
                ConnectionString = options.GetConnectionString("ConnectionString"),
                DbType = (DbType)Enum.Parse(typeof(SqlSugar.DbType), options.GetConnectionString("DbType")),
                IsAutoCloseConnection = true,
                InitKeyType= InitKeyType.Attribute
            });
        }
        public SqlSugarClient Db;
        public DbSet<Login> LoginDb { get { return new DbSet<Login>(Db); } }
    }
    public class DbSet<T> : SimpleClient<T> where T : class, new()
    {
        public DbSet(SqlSugarClient context) : base(context)
        {

        }
        public List<T> GetByIds(dynamic[] ids)
        {
            return Context.Queryable<T>().In(ids).ToList(); ;
        }
    }

}

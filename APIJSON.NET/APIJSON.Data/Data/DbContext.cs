﻿using APIJSON.Data.Models;
using Microsoft.Extensions.Configuration;
using SqlSugar;
using System;
using System.Collections.Generic;
using Volo.Abp.DependencyInjection;
namespace APIJSON.Data;

public class DbContext:ISingletonDependency
{
    public DbContext(IConfiguration options)
    {
        Db = new SqlSugarClient(new ConnectionConfig()
        {
            ConnectionString = options.GetConnectionString("ConnectionString"),
            DbType = (DbType)Enum.Parse(typeof(DbType), options.GetConnectionString("DbType")), InitKeyType= InitKeyType.Attribute,
            IsAutoCloseConnection = true
        });
        Db.Aop.OnLogExecuted = (sql, pars) => //SQL执行完事件
        {
             
        };
        Db.Aop.OnLogExecuting = (sql, pars) => //SQL执行前事件
        {

        };
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

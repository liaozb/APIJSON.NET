using APIJSON.Data;
using APIJSON.Data.Models;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Text;

namespace APIJSON.NET.Data;

public static class DbInit
{
    /// <summary>
    /// 初始化用户表和数据
    /// </summary>
    /// <param name="app"></param>
    public static void Initialize(IApplicationBuilder app)
    {
        using (var scope = app.ApplicationServices.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<DbContext>();

            db.Db.CodeFirst.InitTables(typeof(Login));
            if (!db.LoginDb.IsAny(it => it.userId > 0))
            {
                var ds = new List<Login>();

                for (int i = 1; i < 10; i++)
                {
                    var d = new Login();
                    d.userId = i;
                    d.userName = "admin" + i.ToString();
                    d.passWordSalt = Guid.NewGuid().ToString();
                    d.passWord = SimpleStringCipher.Instance.Encrypt("123456", null, Encoding.ASCII.GetBytes(d.passWordSalt));
                    d.roleCode = "role1";
                    ds.Add(d);
                }
                db.LoginDb.InsertRange(ds.ToArray());


            }
        }


    }
}

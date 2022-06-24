using APIJSON.NET.Data.Models;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using SqlSugar;
using System;
using System.Collections.Generic;
using System.Text;
namespace APIJSON.NET
{
    public static class DbInit
    {
        public static void Initialize(IApplicationBuilder app)
        {
            var db = app.ApplicationServices.GetRequiredService<DbContext>();

            db.Db.CodeFirst.InitTables(typeof(Login));
            if (!db.LoginDb.IsAny(it=>it.userId>0))
            {
                var ds = new List<Login>();

                for (int i = 1; i < 10; i++)
                {
                    var d = new Login();
                    d.userId = i;
                    d.userName = "admin"+i.ToString();
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

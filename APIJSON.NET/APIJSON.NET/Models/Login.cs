using SqlSugar;
using System;

namespace APIJSON.NET.Models
{
    public class Login
    {
        [SugarColumn(IsNullable = false, IsPrimaryKey = true)]
        public int userId { get; set; }
        [SugarColumn(Length =100,ColumnDescription ="用户名")]
        public string userName { get; set; }
        [SugarColumn(Length = 200, ColumnDescription = "密码")]
        public string passWord { get; set; }
        [SugarColumn(Length = 100, ColumnDescription = "密码盐")]
        public string passWordSalt { get; set; }
        [SugarColumn(Length = 100, ColumnDescription = "权限组")]
        public string roleCode { get; set; }
       
    }
}

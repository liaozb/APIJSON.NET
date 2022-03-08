using APIJSON.NET.Models;
using System;

namespace APIJSON.NET.Services
{
    public interface IIdentityService
    {
        /// <summary>
        /// 获取当前用户id
        /// </summary>
        /// <returns></returns>
        string GetUserIdentity();
        /// <summary>
        /// 获取当前用户权限组名称
        /// </summary>
        /// <returns></returns>
        string GetUserRoleName();
        /// <summary>
        /// 获取当前用户权限
        /// </summary>
        /// <returns></returns>
        Role GetRole();
        /// <summary>
        /// 获取当前表的可查询字段
        /// </summary>
        /// <param name="table"></param>
        /// <returns></returns>
        Tuple<bool, string> GetSelectRole(string table);


        bool ColIsRole(string col, string[] selectrole);
    }
}

using APIJSON.Data;
using APIJSON.Data.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Volo.Abp.DependencyInjection;

namespace APIJSON.NET.Services
{
    /// <summary>
    /// 
    /// </summary>
    public class IdentityService : IIdentityService,ITransientDependency
    {
        private IHttpContextAccessor _context;
        private List<Role> roles;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="context"></param>
        /// <param name="_roles"></param>
        public IdentityService(IHttpContextAccessor context, IOptions<List<Role>> _roles)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            roles = _roles.Value;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public string GetUserIdentity()
        {
            return _context.HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public string GetUserRoleName()
        {
            return _context.HttpContext.User.FindFirstValue(ClaimTypes.Role);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public Role GetRole()
        {
            var role = new Role();
            if (string.IsNullOrEmpty(GetUserRoleName()))//没登录默认取第一个
            {
                role = roles.FirstOrDefault();

            }
            else
            {
                role = roles.FirstOrDefault(it => it.Name.Equals(GetUserRoleName(), StringComparison.CurrentCultureIgnoreCase));
            }
            return role;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="table"></param>
        /// <returns></returns>
        public Tuple<bool, string> GetSelectRole(string table)
        {
            var role = GetRole();
            if (role == null || role.Select == null || role.Select.Table == null)
            {
                return Tuple.Create(false, $"appsettings.json权限配置不正确！");
            }
            string tablerole = role.Select.Table.FirstOrDefault(it => it == "*" || it.Equals(table, StringComparison.CurrentCultureIgnoreCase));

            if (string.IsNullOrEmpty(tablerole))
            {
                return Tuple.Create(false, $"表名{table}没权限查询！");
            }
            int index = Array.IndexOf(role.Select.Table, tablerole);
            string selectrole = role.Select.Column[index];
            return Tuple.Create(true, selectrole);
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="col"></param>
        /// <param name="selectrole"></param>
        /// <returns></returns>
        public bool ColIsRole(string col, string[] selectrole)
        {
            if (selectrole.Contains("*"))
            {
                return true;
            }
            else
            {
                if (col.Contains("(") && col.Contains(")"))
                {
                    Regex reg = new Regex(@"\(([^)]*)\)");
                    Match m = reg.Match(col);
                    if (selectrole.Contains(m.Result("$1"), StringComparer.CurrentCultureIgnoreCase))
                    {
                        return true;
                    }
                    else
                    {
                        return false;
                    }
                }
                else
                {
                    if (selectrole.Contains(col, StringComparer.CurrentCultureIgnoreCase))
                    {
                        return true;
                    }
                    else
                    {
                        return false;
                    }
                }
            }
        }
    }
}

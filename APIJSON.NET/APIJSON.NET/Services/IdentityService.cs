using APIJSON.NET.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace APIJSON.NET.Services
{
    public class IdentityService : IIdentityService
    {
        private IHttpContextAccessor _context;
        private List<Role> roles;

        public IdentityService(IHttpContextAccessor context,IOptions<List<Role>> _roles)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            roles = _roles.Value;
        }
        public string GetUserIdentity()
        {
            return _context.HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);
        }

        public string GetUserRoleName()
        {
            return _context.HttpContext.User.FindFirstValue(ClaimTypes.Role);
        }
        public Role GetRole()
        {
            var role = new Role();
            if (string.IsNullOrEmpty(GetUserRoleName()))
            {
                role = roles.FirstOrDefault();

            }
            else
            {
                role = roles.FirstOrDefault(it => it.Name.Equals(GetUserRoleName(), StringComparison.CurrentCultureIgnoreCase));
            }
            return role;
        }
    }
}

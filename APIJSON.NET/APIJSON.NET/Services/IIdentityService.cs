using APIJSON.NET.Models;

namespace APIJSON.NET.Services
{
    public interface IIdentityService
    {
        string GetUserIdentity();
        string GetUserRoleName();
        Role GetRole();
    }
}

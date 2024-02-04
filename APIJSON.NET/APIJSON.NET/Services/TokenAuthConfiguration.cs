using Microsoft.IdentityModel.Tokens;
using System;
using Volo.Abp.DependencyInjection;

namespace APIJSON.NET.Data.Models;

public class TokenAuthConfiguration:ISingletonDependency
{
    public SymmetricSecurityKey SecurityKey { get; set; }

    public string Issuer { get; set; }

    public string Audience { get; set; }

    public SigningCredentials SigningCredentials { get; set; }

    public TimeSpan Expiration { get; set; }
}

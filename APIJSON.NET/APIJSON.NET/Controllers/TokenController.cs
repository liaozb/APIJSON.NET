using System;
using System.Collections.Generic;
using System.Globalization;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Newtonsoft.Json.Linq;

namespace APIJSON.NET.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class TokenController : ControllerBase
    {
        private DbOptions _options;
        private readonly IOptions<TokenAuthConfiguration> _configuration;
        public TokenController(IOptions<DbOptions> options,  IOptions<TokenAuthConfiguration> configuration)
        {
            this._options = options.Value;
            _configuration = configuration;
        }
        [HttpPost("/token")]
        public IActionResult Create(string username, string password)
        {
            JObject ht = new JObject();
            ht.Add("code", "200");
            ht.Add("msg", "success");
            if (username!=password)
            {

            }

            var identity = new ClaimsIdentity();
            identity.AddClaim(new Claim(ClaimTypes.NameIdentifier, "1"));
            identity.AddClaim(new Claim(ClaimTypes.Name, "1"));
            identity.AddClaim(new Claim(ClaimTypes.Role, ""));
            identity.AddClaim(new Claim(JwtRegisteredClaimNames.Sub, username));
            identity.AddClaim(new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()));
            identity.AddClaim(new Claim(JwtRegisteredClaimNames.Iat, DateTimeOffset.Now.ToUnixTimeSeconds().ToString(), ClaimValueTypes.Integer64));
          
            var accessToken = CreateAccessToken(identity.Claims.ToList());

            var data = new AuthenticateResultModel()
            {
                AccessToken = accessToken,
                ExpireInSeconds = (int)_configuration.Value.Expiration.TotalSeconds
            };
    
            ht.Add("data", JToken.FromObject(data));
            return Ok(ht);
        }
        private string CreateAccessToken(IEnumerable<Claim> claims, TimeSpan? expiration = null)
        {
            var now = DateTime.UtcNow;

            var jwtSecurityToken = new JwtSecurityToken(
                issuer: _configuration.Value.Issuer,
                audience: _configuration.Value.Audience,
                claims: claims,
                notBefore: now,
                expires: now.Add(expiration ?? _configuration.Value.Expiration),
                signingCredentials: _configuration.Value.SigningCredentials
            );

            return new JwtSecurityTokenHandler().WriteToken(jwtSecurityToken);
        }
    }
    public class AuthenticateResultModel
    {
        public string AccessToken { get; set; }
     
        public int ExpireInSeconds { get; set; }
  

    }
}
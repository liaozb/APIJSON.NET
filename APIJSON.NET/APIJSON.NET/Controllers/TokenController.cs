using System;
using System.Collections.Generic;
using System.Globalization;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Newtonsoft.Json.Linq;

namespace APIJSON.NET.Controllers
{
    [Route("api/[controller]/[action]")]
    [ApiController]
    [Authorize]
    public class TokenController : ControllerBase
    {
        private DbContext db;
        private readonly IOptions<TokenAuthConfiguration> _configuration;
        public TokenController(DbContext _db, IOptions<TokenAuthConfiguration> configuration)
        {
            _configuration = configuration;
            db = _db;
        }
        [HttpPost("/token")]
        [AllowAnonymous]
        public IActionResult Create([FromBody]TokenInput input)
        {
            JObject ht = new JObject();
            ht.Add("code", "200");
            ht.Add("msg", "success");
            var us = db.LoginDb.GetSingle(it => it.userName == input.username);
            if (us==null)
            {
                ht["code"] = "201";
                ht["msg"] = "用户名或者密码错误！";
                return Ok(ht);
            }
            string str = SimpleStringCipher.Instance.Encrypt(input.password,null, Encoding.ASCII.GetBytes(us.passWordSalt));
            if (!us.passWord.Equals(str))
            {
                ht["code"]="201";
                ht["msg"]= "用户名或者密码错误！";
                return Ok(ht);
            }
            var identity = new ClaimsIdentity();
            identity.AddClaim(new Claim(ClaimTypes.NameIdentifier, us.userId.ToString(CultureInfo.InvariantCulture)));
            identity.AddClaim(new Claim(ClaimTypes.Name, us.userId.ToString(CultureInfo.InvariantCulture)));
            identity.AddClaim(new Claim(ClaimTypes.Role, us.roleCode.ToString(CultureInfo.InvariantCulture)));
            var claims = identity.Claims.ToList();

            claims.AddRange(new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub,us.userId.ToString(CultureInfo.InvariantCulture)),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new Claim(JwtRegisteredClaimNames.Iat, DateTimeOffset.Now.ToUnixTimeSeconds().ToString(), ClaimValueTypes.Integer64)
            });

            var accessToken = CreateAccessToken(claims);

            var data = new AuthenticateResultModel()
            {
                AccessToken = accessToken,
                ExpireInSeconds = (int)_configuration.Value.Expiration.TotalSeconds
            };
    
            ht.Add("data", JToken.FromObject(data));
            return Ok(ht);
        }
        [HttpGet] 
        public IActionResult GetRole()
        {
            return Ok(User.FindFirstValue(ClaimTypes.Role));
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
    public class TokenInput
    {
        public string username { get; set; }
        public string password { get; set; }
    }
    public class AuthenticateResultModel
    {
        public string AccessToken { get; set; }
     
        public int ExpireInSeconds { get; set; }
  

    }
}
using APIJSON.NET;
using APIJSON.NET.Models;
using APIJSON.NET.Services;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System;
using System.Collections.Generic;
using System.Net;
using System.Text;

const string _defaultCorsPolicyName = "localhost";
var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers().AddNewtonsoftJson(options =>
{
    options.SerializerSettings.ReferenceLoopHandling = Newtonsoft.Json.ReferenceLoopHandling.Ignore;
    options.SerializerSettings.DateFormatString = "yyyy-MM-dd HH:mm:ss";
});
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
 
builder.Services.Configure<List<Role>>(builder.Configuration.GetSection("RoleList"));
builder.Services.Configure<Dictionary<string, string>>(builder.Configuration.GetSection("tablempper"));
builder.Services.Configure<TokenAuthConfiguration>(tokenAuthConfig =>
{
    tokenAuthConfig.SecurityKey = new SymmetricSecurityKey(Encoding.ASCII.GetBytes(builder.Configuration["Authentication:JwtBearer:SecurityKey"]));
    tokenAuthConfig.Issuer = builder.Configuration["Authentication:JwtBearer:Issuer"];
    tokenAuthConfig.Audience = builder.Configuration["Authentication:JwtBearer:Audience"];
    tokenAuthConfig.SigningCredentials = new SigningCredentials(tokenAuthConfig.SecurityKey, SecurityAlgorithms.HmacSha256);
    tokenAuthConfig.Expiration = TimeSpan.FromDays(1);
});
AuthConfigurer.Configure(builder.Services, builder.Configuration);

var origins = builder.Configuration.GetSection("CorsUrls").Value.Split(",");
builder.Services.AddCors(options => options.AddPolicy(_defaultCorsPolicyName,
    builder =>
    builder.WithOrigins(origins)
      .AllowAnyHeader()
      .AllowAnyMethod().AllowCredentials()
      ));
 
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "APIJSON.NET", Version = "v1" });
});
builder.Services.AddSingleton<DbContext>();

builder.Services.AddSingleton<TokenAuthConfiguration>();
builder.Services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();
builder.Services.AddTransient<IIdentityService, IdentityService>();
builder.Services.AddTransient<ITableMapper, TableMapper>();
 

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "My API V1");

    });
}

app.UseHttpsRedirection();
app.UseDefaultFiles();
app.UseStaticFiles();
app.UseAuthorization();
app.UseCors(_defaultCorsPolicyName);
app.MapControllers();
app.UseJwtTokenMiddleware();
DbInit.Initialize(app);
app.Run();
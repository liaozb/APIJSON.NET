using APIJSON.Data;
using APIJSON.Data.Models;
using APIJSON.NET.Data;
using APIJSON.NET.Data.Models;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using Volo.Abp;
using Volo.Abp.AspNetCore.Mvc;
using Volo.Abp.AspNetCore.Serilog;
using Volo.Abp.Autofac;
using Volo.Abp.Modularity;
using Volo.Abp.Swashbuckle;

namespace APIJSON.NET;

[DependsOn(
    typeof(AbpAspNetCoreMvcModule),
    typeof(AbpAutofacModule),
    typeof(AbpAspNetCoreSerilogModule),
    typeof(AbpSwashbuckleModule),
    typeof(ApiJsonNetDataModule))]
public class AppModule : AbpModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        var configuration = context.Services.GetConfiguration();
        var hostingEnvironment = context.Services.GetHostingEnvironment();

        PreConfigure<JsonSerializerOptions>(options =>
        {
            options.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
        });

        ConfigureCors(context, configuration);
        ConfigureSwaggerServices(context, configuration);

        context.Services.Configure<List<Role>>(configuration.GetSection("RoleList"));
        context.Services.Configure<Dictionary<string, string>>(configuration.GetSection("tablempper"));
        context.Services.Configure<TokenAuthConfiguration>(tokenAuthConfig =>
        {
            tokenAuthConfig.SecurityKey = new SymmetricSecurityKey(Encoding.ASCII.GetBytes(configuration["Authentication:JwtBearer:SecurityKey"]));
            tokenAuthConfig.Issuer = configuration["Authentication:JwtBearer:Issuer"];
            tokenAuthConfig.Audience = configuration["Authentication:JwtBearer:Audience"];
            tokenAuthConfig.SigningCredentials = new SigningCredentials(tokenAuthConfig.SecurityKey, SecurityAlgorithms.HmacSha256);
            tokenAuthConfig.Expiration = TimeSpan.FromDays(1);
        });
        AuthConfigurer.Configure(context.Services,configuration);

        context.Services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();
      
    }
    private static void ConfigureSwaggerServices(ServiceConfigurationContext context, IConfiguration configuration)
    {
        context.Services.AddAbpSwaggerGen(
            options =>
            {
                options.SwaggerDoc("v1", new OpenApiInfo { Title = "APIJSON.NET API", Version = "v1" });
                options.DocInclusionPredicate((docName, description) => true);
                options.CustomSchemaIds(type => type.FullName);
            });
    }
    private void ConfigureCors(ServiceConfigurationContext context, IConfiguration configuration)
    {
        context.Services.AddCors(options =>
        {
            options.AddDefaultPolicy(builder =>
            {
                builder
                    .WithOrigins(configuration["CorsUrls"]?
                        .Split(",", StringSplitOptions.RemoveEmptyEntries)
                        .Select(o => o.RemovePostFix("/"))
                        .ToArray() ?? Array.Empty<string>())
                    .WithAbpExposedHeaders()
                    .SetIsOriginAllowedToAllowWildcardSubdomains()
                    .AllowAnyHeader()
                    .AllowAnyMethod()
                    .AllowCredentials();
            });
        });
    }
    public override void OnApplicationInitialization(ApplicationInitializationContext context)
    {
        var app = context.GetApplicationBuilder();
        var env = context.GetEnvironment();

        if (env.IsDevelopment())
        {
            app.UseDeveloperExceptionPage();
        }

        app.UseAbpRequestLocalization();
        app.UseCorrelationId();
        app.UseStaticFiles();
        app.UseRouting();
        app.UseCors();
        app.UseAuthentication();

   
        app.UseAuthorization();

        app.UseSwagger();
        app.UseAbpSwaggerUI(options =>
        {
            options.SwaggerEndpoint("/swagger/v1/swagger.json", "MyProjectName API");

        });
        app.UseAbpSerilogEnrichers();
        app.UseConfiguredEndpoints();
        app.UseJwtTokenMiddleware();
        DbInit.Initialize(app);
    }
}

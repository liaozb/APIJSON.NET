namespace APIJSON.NET
{
    using System;
    using System.Collections.Generic;
    using System.Text;
    using APIJSON.NET.Models;
    using APIJSON.NET.Services;
    using Microsoft.AspNetCore.Builder;
    using Microsoft.AspNetCore.Hosting;
    using Microsoft.AspNetCore.Http;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.IdentityModel.Tokens;
    using Microsoft.OpenApi.Models;
    using Swashbuckle.AspNetCore.Swagger;

    public class Startup
    {
        private const string _defaultCorsPolicyName = "localhost";
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
         
            services.Configure<List<Role>>(Configuration.GetSection("RoleList"));
            services.Configure<Dictionary<string,string>>(Configuration.GetSection("tablempper"));
            services.Configure<TokenAuthConfiguration>(tokenAuthConfig =>
            {
                tokenAuthConfig.SecurityKey = new SymmetricSecurityKey(Encoding.ASCII.GetBytes(Configuration["Authentication:JwtBearer:SecurityKey"]));
                tokenAuthConfig.Issuer = Configuration["Authentication:JwtBearer:Issuer"];
                tokenAuthConfig.Audience = Configuration["Authentication:JwtBearer:Audience"];
                tokenAuthConfig.SigningCredentials = new SigningCredentials(tokenAuthConfig.SecurityKey, SecurityAlgorithms.HmacSha256);
                tokenAuthConfig.Expiration = TimeSpan.FromDays(1);
            });
            AuthConfigurer.Configure(services, Configuration);

            var origins = Configuration.GetSection("CorsUrls").Value.Split(",");
            services.AddCors( options => options.AddPolicy( _defaultCorsPolicyName, 
                builder => 
                builder.WithOrigins(origins)
                  .AllowAnyHeader()
                  .AllowAnyMethod().AllowCredentials()
                  ));
            services.AddControllers()
                .AddNewtonsoftJson(options =>
                {
                    options.SerializerSettings.ReferenceLoopHandling = Newtonsoft.Json.ReferenceLoopHandling.Ignore;
                    options.SerializerSettings.DateFormatString = "yyyy-MM-dd HH:mm:ss";
                }); ;
            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo { Title = "APIJSON.NET", Version = "v1" });
            });
            services.AddSingleton<DbContext>();
            services.AddSingleton<SelectTable>();
            services.AddSingleton<TokenAuthConfiguration>();
            services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();
            services.AddTransient<IIdentityService, IdentityService>();
            services.AddTransient<ITableMapper, TableMapper>();

        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {

            app.UseAuthentication();

            app.UseRouting();
            app.UseStaticFiles();
            app.UseCors(_defaultCorsPolicyName);
            app.UseSwagger();
            app.UseSwaggerUI(c =>
            {
                c.SwaggerEndpoint("/swagger/v1/swagger.json", "My API V1");
               
            });
            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });
            app.UseJwtTokenMiddleware();
            DbInit.Initialize(app);
        }
    }
}

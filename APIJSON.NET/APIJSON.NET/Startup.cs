namespace APIJSON.NET
{
    using System;
    using System.Collections.Generic;
    using System.Text;
    using APIJSON.NET.Models;
    using Microsoft.AspNetCore.Builder;
    using Microsoft.AspNetCore.Hosting;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.IdentityModel.Tokens;
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

            services.Configure<DbOptions>(options =>
            {
                options.DbType = (SqlSugar.DbType)Enum.Parse(typeof(SqlSugar.DbType), Configuration.GetConnectionString("DbType"));
                options.ConnectionString = Configuration.GetConnectionString("ConnectionString");
            });
            services.Configure<List<Role>>(Configuration.GetSection("RoleList"));
            services.Configure<TokenAuthConfiguration>(tokenAuthConfig =>
            {
                tokenAuthConfig.SecurityKey = new SymmetricSecurityKey(Encoding.ASCII.GetBytes(Configuration["Authentication:JwtBearer:SecurityKey"]));
                tokenAuthConfig.Issuer = Configuration["Authentication:JwtBearer:Issuer"];
                tokenAuthConfig.Audience = Configuration["Authentication:JwtBearer:Audience"];
                tokenAuthConfig.SigningCredentials = new SigningCredentials(tokenAuthConfig.SecurityKey, SecurityAlgorithms.HmacSha256);
                tokenAuthConfig.Expiration = TimeSpan.FromDays(1);
            });
            AuthConfigurer.Configure(services, Configuration);

            services.AddCors( options => options.AddPolicy( _defaultCorsPolicyName, builder => builder.AllowAnyOrigin()
                  .AllowAnyHeader()
                  .AllowAnyMethod()
                  ));
            services.AddMvc().SetCompatibilityVersion(CompatibilityVersion.Version_2_1);
            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new Info { Title = "APIJSON.NET", Version = "v1" });
            });
            services.AddTransient<DbContext>();
            services.AddSingleton<JsonToSql>();
            services.AddSingleton<TokenAuthConfiguration>();

        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            app.UseMvc(routes =>
            {
                routes.MapRoute(
                    name: "default",
                    template: "{controller=Home}/{action=Index}/{id?}");
            });
            app.UseCors(_defaultCorsPolicyName);
            app.UseSwagger();
            app.UseSwaggerUI(c =>
            {
                c.SwaggerEndpoint("/swagger/v1/swagger.json", "My API V1");
            });
            app.UseAuthentication();
        }
    }
}

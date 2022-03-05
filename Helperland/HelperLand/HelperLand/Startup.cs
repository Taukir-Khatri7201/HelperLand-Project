using HelperLand.Security;
using HelperLand.Data;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using HelperLand.Utility;
using HelperLand.Models;

namespace HelperLand
{
    public class Startup
    {
        private readonly IConfiguration Configuration;

        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }
        // This method gets called by the runtime. Use this method to add services to the container.
        // For more information on how to configure your application, visit https://go.microsoft.com/fwlink/?LinkID=398940
        public void ConfigureServices(IServiceCollection services)
        {
            services.Configure<CookiePolicyOptions>(options =>
            {
                options.CheckConsentNeeded = context => false;
                options.MinimumSameSitePolicy = SameSiteMode.Strict;
                options.OnDeleteCookie = context =>
                {
                    context.Context.Request.HttpContext.Response.Redirect("/Home/Logout");
                };
            });
            services.AddSession(options => {
                options.IdleTimeout = TimeSpan.FromMinutes(30);
                options.Cookie.Name = "SessionCookie";
                options.Cookie.MaxAge = TimeSpan.FromMinutes(30);
            });
            services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme).AddCookie(options =>
            {
                options.Cookie.Name = "AuthenticationCookie";
                options.Cookie.MaxAge = TimeSpan.FromMinutes(30);
                options.ExpireTimeSpan = TimeSpan.FromMinutes(30);
                options.LoginPath = new PathString("/Home/Index");
                options.LogoutPath = new PathString("/Home/Logout");
                options.SlidingExpiration = true;
                options.AccessDeniedPath = new PathString("/Shared/AccessDenied");
            });
            services.ConfigureApplicationCookie(options =>
            {
                options.Cookie.Name = "ConfigureApplicationCookie";
                options.ExpireTimeSpan = TimeSpan.FromMinutes(30);
                options.Cookie.Name = "ApplicationCookie";
                options.LoginPath = new PathString("/Home/Index");
                options.LogoutPath = new PathString("/Home/Logout");
            });
            services.AddMvc(option => { option.EnableEndpointRouting = false; });
            services.AddDbContext<HelperlandDBContext>();
            services.AddSingleton<ICustomDataProtector, CustomDataProtector>();
            services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();
            services.AddDataProtection();
            var emailConfig = Configuration.GetSection("EmailConfiguration").Get<EmailConfiguration>();
            services.AddSingleton(emailConfig);
            services.AddScoped<IEmailSender, EmailSender>();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseStaticFiles();
            app.UseCookiePolicy();
            app.UseSession();
            app.UseAuthentication();
            app.UseAuthorization();
            app.UseMvcWithDefaultRoute();
        }
    }
}

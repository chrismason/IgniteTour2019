using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Ignite.API.Services;
using Ignite.Common.KeyVault;
using Ignite.Data.UXO.DataAccess;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Swashbuckle.AspNetCore.Swagger;
using Microsoft.EntityFrameworkCore;
using Ignite.API.Common.Settings;
using Ignite.Data.UXO.Queries;
using Ignite.Common.Authentication;
using System.Data.SqlClient;
using Ignite.API.Models;
using Ignite.Data.UXO.Documents;
using Microsoft.AspNetCore.Mvc.Formatters;
using System.Net.Http.Headers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.ApplicationInsights.Extensibility.PerfCounterCollector.QuickPulse;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.ApplicationInsights.Extensibility.Implementation.ApplicationId;
using Microsoft.ApplicationInsights.Channel;
using Microsoft.ApplicationInsights.WindowsServer.TelemetryChannel;

namespace Ignite.API
{
    public class Startup
    {
        private readonly ILogger _logger;
        private readonly ILoggerFactory _loggerFactory;

        public Startup(IConfiguration configuration, ILogger<Startup> logger, ILoggerFactory loggerFactory)
        {
            Configuration = configuration;
            _logger = logger;
            _loggerFactory = loggerFactory;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            if (Configuration.GetSection("Telemetry").GetValue<bool>("UseAppInsights"))
            {
                if (Configuration.GetSection("Telemetry").GetValue<bool>("UseAzureGov"))
                {
                    services.ConfigureTelemetryModule<QuickPulseTelemetryModule>((module, o) => module.QuickPulseServiceEndpoint="https://quickpulse.applicationinsights.us/QuickPulseService.svc");
                    services.AddSingleton<IApplicationIdProvider, ApplicationInsightsApplicationIdProvider>(_ => new ApplicationInsightsApplicationIdProvider() { ProfileQueryEndpoint = "https://dc.applicationinsights.us/api/profiles/{0}/appId" });
                    services.AddSingleton<ITelemetryChannel>(_ => new ServerTelemetryChannel() { EndpointAddress = "https://dc.applicationinsights.us/v2/track" });
                }
                services.AddApplicationInsightsTelemetry(Configuration.GetSection("Telemetry").GetValue<string>("InstrumentationKey"));
            }
            KeyVaultRepositoryFactory.Environment = CloudEnvironment.AzureGovernment;
            var sqlSettings = new SqlSettings();
            Configuration.GetSection("Sql").Bind(sqlSettings);
            var authenticationOptions = new AuthenticationOptions();
            Configuration.GetSection("AAD").Bind(authenticationOptions);
            services.AddSingleton(sqlSettings);
            services.AddSingleton(authenticationOptions);
            
            services.Configure<StorageSettings>(Configuration.GetSection("Storage"));
            services.AddSingleton(KeyVaultRepositoryFactory.GetRepository(_loggerFactory, Configuration));
            services.AddTransient<IUXOService, UXOService>();
            services.AddTransient<IUXODocumentService, UXODocumentService>();
            services.AddTransient<IUXODocumentGenerator, UXODocumentGenerator>();
            services.AddTransient<IAuthenticator, Authenticator>();

            services.AddDbContext<UXODbContext>(options => {
                options.UseSqlServer(sqlSettings.ConnectionString);
            });
            if (Configuration.GetSection("Sql").GetValue<bool>("UseKeyVault"))
            {
                _logger.LogInformation("Configuring SQL to use KeyVault");
                services.AddScoped<IUXODbContext>(serviceProvider => {
                    var context = serviceProvider.GetService<UXODbContext>();
                    var authenticator = serviceProvider.GetService<IAuthenticator>();
                    var connection = context.Database.GetDbConnection() as SqlConnection;
                    if (connection == null)
                    {
                        return null;
                    }
                    connection.AccessToken = authenticator.Authenticate().Result;
                    return context;
                });
            }
            else
            {
                _logger.LogInformation("Configuring SQL without KeyVault");
                services.AddScoped<IUXODbContext>(serviceProvider => {
                    var context = serviceProvider.GetService<UXODbContext>();
                    return context;
                });
            }
            services.AddScoped<IUXOItemQuery, UXOItemQuery>();

            services.AddCors(options =>
            {
                options.AddPolicy("AllowAllCORS",
                    builder => builder.AllowAnyOrigin()
                    .AllowAnyMethod()
                    .WithExposedHeaders(new string[] {"Content-Disposition"})
                    .AllowAnyHeader()
                    .AllowCredentials());
            });

            services.AddMvc().SetCompatibilityVersion(CompatibilityVersion.Version_2_2);
            
            services.AddAuthorization(options =>
            {
                if (Configuration.GetSection("Authentication").GetValue<bool>("Enabled"))
                {
                    options.DefaultPolicy = new AuthorizationPolicyBuilder()
                        .RequireAuthenticatedUser()
                        .Build();
                }
                else
                {
                    options.DefaultPolicy = new AuthorizationPolicyBuilder()
                        .RequireAssertion(_ => true)
                        .Build();
                }
            });

            if (Configuration.GetSection("Authentication").GetValue<bool>("Enabled"))
            {
                _logger.LogInformation("Configuring authentication");
                services.AddAuthentication(options =>
                {
                    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
                }).AddJwtBearer(options =>
                {
                    options.Authority = Configuration.GetSection("Authentication").GetValue<string>("JwtAuthority");
                    var audiences = Configuration.GetSection("Authentication").GetValue<string>("JwtAudience").Split(";").ToList();
                    options.TokenValidationParameters = new Microsoft.IdentityModel.Tokens.TokenValidationParameters
                    {
                        ValidateIssuer = true,
                        ValidAudiences = audiences
                    };
                });
            }

            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new Info { Title = "Ignite API", Version = "v1" });
                c.AddSecurityDefinition("Bearer", new ApiKeyScheme
                {
                    Description = "JWT Authorization header using the Bearer scheme. Example: \"Authorization: Bearer {token}\"",
                    Name = "Authorization",
                    In = "header",
                    Type = "apiKey"
                });
            });
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env, IUXODbContext dataContext)
        {
            if (env.IsDevelopment() || env.IsEnvironment("Integration"))
            {
                app.UseDeveloperExceptionPage();
                _logger.LogInformation("Enabling Swagger UI");
                app.UseSwagger();
                app.UseSwaggerUI(c =>
                {
                    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Ignite API V1");
                });
            }
            else
            {
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }

            app.UseCors("AllowAllCORS");

            if (Configuration.GetSection("Authentication").GetValue<bool>("Enabled"))
            {
                app.UseAuthentication();
            }

            app.UseHttpsRedirection();
            app.UseMvc();

            dataContext.Database.EnsureCreated();
        }
    }
}

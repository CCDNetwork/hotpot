using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Ccd.Server.AdministrativeRegions;
using Ccd.Server.Authentication;
using Ccd.Server.Beneficiaries;
using Ccd.Server.BeneficiaryAttributes;
using Ccd.Server.BeneficiaryData;
using Ccd.Server.Data;
using Ccd.Server.Deduplication;
using Ccd.Server.Email;
using Ccd.Server.Handbooks;
using Ccd.Server.Helpers;
using Ccd.Server.Notifications;
using Ccd.Server.Organizations;
using Ccd.Server.Referrals;
using Ccd.Server.Settings;
using Ccd.Server.Storage;
using Ccd.Server.Templates;
using Ccd.Server.Users;
using Dapper;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using Npgsql;
using File = System.IO.File;

namespace Ccd.Server;

public class Startup
{
    public Startup(IConfiguration configuration)
    {
        Configuration = configuration;
    }

    private IConfiguration Configuration { get; }

    public void ConfigureServices(IServiceCollection services)
    {
        DefaultTypeMap.MatchNamesWithUnderscores = true;
        NpgsqlConnection.GlobalTypeMapper.EnableDynamicJson();

        StaticConfiguration.Initialize(Configuration);
        var dbConnectionString = StaticConfiguration.DbConnectionString;

        services.AddCors();
        services.AddControllers();

        // configure jwt authentication
        var key = Encoding.ASCII.GetBytes(StaticConfiguration.AppSettingsSecret);

        services
            .AddAuthentication(x =>
            {
                x.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                x.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(x =>
            {
                x.RequireHttpsMetadata = false;
                x.SaveToken = true;
                x.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(key),
                    ValidateIssuer = false,
                    ValidateAudience = false
                };
            });

        services.AddAuthorization(options =>
        {
            options.AddPolicy(
                "AuthPolicy",
                policy =>
                {
                    policy.AuthenticationSchemes.Add(JwtBearerDefaults.AuthenticationScheme);
                    policy.RequireAuthenticatedUser();
                }
            );

            options.DefaultPolicy = options.GetPolicy("AuthPolicy");
        });

        services.AddSwaggerGen(c =>
        {
            c.SwaggerDoc(
                "v1",
                new OpenApiInfo
                {
                    Version = "v1",
                    Title = "INIT Ccd Server API",
                    Description = "INIT Ccd Server API",
                    TermsOfService = new Uri("https://init.hr")
                }
            );

            c.MapType<TimeSpan>(
                () => new OpenApiSchema { Type = "string", Example = new OpenApiString("00:00:00") }
            );

            c.AddSecurityDefinition(
                "apiKey",
                new OpenApiSecurityScheme
                {
                    Name = "apiKey",
                    Scheme = "apiKey",
                    In = ParameterLocation.Query,
                    Type = SecuritySchemeType.ApiKey
                }
            );

            c.AddSecurityRequirement(
                new OpenApiSecurityRequirement
                {
                    {
                        new OpenApiSecurityScheme
                        {
                            Reference = new OpenApiReference
                            {
                                Type = ReferenceType.SecurityScheme,
                                Id = "Bearer"
                            }
                        },
                        new List<string>()
                    },
                    {
                        new OpenApiSecurityScheme
                        {
                            Reference = new OpenApiReference
                            {
                                Type = ReferenceType.SecurityScheme,
                                Id = "apiKey"
                            }
                        },
                        new List<string>()
                    }
                }
            );

            c.CustomSchemaIds(type => type.ToString());

            // uncomment to generate Swagger descriptions from summary comments
            // var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
            // var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
            // c.IncludeXmlComments(xmlPath);
        });

        services.AddScoped<EmailManagerService>();
        services.AddScoped<SendGridService>();
        services.AddScoped<OrganizationService>();
        services.AddScoped<UserService>();
        services.AddScoped<AuthenticationService>();
        services.AddScoped<DeduplicationService>();
        services.AddScoped<BookingService>();
        services.AddScoped<BeneficiaryAttributeService>();
        services.AddScoped<BeneficiaryAttributeGroupService>();
        services.AddScoped<BeneficaryService>();
        services.AddScoped<BeneficaryDataService>();
        services.AddScoped<ReferralService>();
        services.AddScoped<ExportService>();
        services.AddScoped<TemplateService>();
        services.AddScoped<HandbookService>();
        services.AddScoped<SettingsService>();
        services.AddScoped<AdministrativeRegionService>();
        services.AddScoped<IStorageService, StorageService>();
        services.AddScoped<INotificationService, NotificationService>();

        // configure DI for application services
        services.AddDbContext<CcdContext>(options => options.UseNpgsql(dbConnectionString));
        services.AddHttpContextAccessor();
        services.AddAutoMapper(typeof(Mappings.Mappings));
        services.AddScoped<DbUserTrackingService>();
        services.AddSingleton<DateTimeProvider>();

        SqlMapper.AddTypeHandler(new DateTimeHandler());
        SqlMapper.AddTypeHandler(new JsonHandler<List<Guid>>());
        SqlMapper.AddTypeHandler(new JsonHandler<List<string>>());
    }

    // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
    public void Configure(
        IApplicationBuilder app,
        IWebHostEnvironment env,
        CcdContext ccdContext
    )
    {
        if (env.IsDevelopment()) app.UseDeveloperExceptionPage();

        app.UseExceptionHandler("/error");

        app.UseRouting();

        // global cors policy
        app.UseCors(x => x.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader());

        app.UseAuthentication();
        app.UseAuthorization();
        app.UsePermissionLevel();

        app.UseEndpoints(endpoints =>
        {
            endpoints.MapGet("/", context => context.Response.WriteAsync("Ccd API"));
            endpoints.MapGet("/health", context => context.Response.WriteAsync("ok"));
            endpoints.MapGet(
                "/version",
                context =>
                    context.Response.WriteAsync(
                        File.Exists("version.txt")
                            ? File.ReadAllText("version.txt")
                            : "non-production"
                    )
            );
        });

        app.UseEndpoints(endpoints => { endpoints.MapControllers(); });

        app.UseSwagger();
        app.UseSwaggerUI(c => { c.SwaggerEndpoint("/swagger/v1/swagger.json", "CcdServerTests Server"); });

        if (ccdContext.Database.GetPendingMigrations().Any())
        {
            Console.WriteLine("Applying DB migrations");
            ccdContext.Database.Migrate();
        }
    }
}
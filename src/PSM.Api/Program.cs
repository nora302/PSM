using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using PdfSharp.Fonts;
using PSM.Application.Interfaces;
using PSM.Infrastructure.Data;
using PSM.Infrastructure.Identity;
using PSM.Infrastructure.SpeechToText;

var builder = WebApplication.CreateBuilder(args);

// --------------------------------------------------
// PDFsharp / MigraDoc Fonts
// --------------------------------------------------

// Für lokale Entwicklung unter Windows.
// PDFsharp 6.2 benötigt einen expliziten Font Resolver.
GlobalFontSettings.UseWindowsFontsUnderWindows = true;

// --------------------------------------------------
// PostgreSQL
// --------------------------------------------------

var connectionString = builder.Configuration
    .GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException(
        "Connection string 'DefaultConnection' wurde nicht gefunden.");

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(connectionString));

// --------------------------------------------------
// ASP.NET Core Identity
// --------------------------------------------------

builder.Services
    .AddIdentity<Benutzer, IdentityRole>(options =>
    {
        // Nur für lokale Tests
        options.Password.RequiredLength = 4;
        options.Password.RequireDigit = true;
        options.Password.RequireUppercase = false;
        options.Password.RequireLowercase = false;
        options.Password.RequireNonAlphanumeric = false;

        options.User.RequireUniqueEmail = false;
    })
    .AddEntityFrameworkStores<AppDbContext>()
    .AddDefaultTokenProviders();

// --------------------------------------------------
// JWT
// --------------------------------------------------

var jwtKey = builder.Configuration["Jwt:Key"]
    ?? throw new InvalidOperationException(
        "JWT Key wurde nicht konfiguriert.");

builder.Services
    .AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme =
            JwtBearerDefaults.AuthenticationScheme;

        options.DefaultChallengeScheme =
            JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters =
            new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,

                IssuerSigningKey =
                    new SymmetricSecurityKey(
                        Encoding.UTF8.GetBytes(jwtKey)),

                ValidateIssuer = false,
                ValidateAudience = false,
                ValidateLifetime = true,

                ClockSkew = TimeSpan.Zero
            };
    });

// --------------------------------------------------
// Authorization
// --------------------------------------------------

builder.Services.AddAuthorization();

// --------------------------------------------------
// Azure Speech-to-Text
// --------------------------------------------------

builder.Services.AddScoped<
    ISpeechToTextService,
    AzureSpeechToTextService>();

// --------------------------------------------------
// Controller
// --------------------------------------------------

builder.Services.AddControllers();

// --------------------------------------------------
// Swagger + JWT Authorize
// --------------------------------------------------

builder.Services.AddEndpointsApiExplorer();

builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc(
        "v1",
        new OpenApiInfo
        {
            Title = "PSM API",
            Version = "v1"
        });

    options.AddSecurityDefinition(
        "Bearer",
        new OpenApiSecurityScheme
        {
            Name = "Authorization",
            Type = SecuritySchemeType.Http,
            Scheme = "bearer",
            BearerFormat = "JWT",
            In = ParameterLocation.Header,
            Description = "JWT Token eingeben."
        });

    options.AddSecurityRequirement(
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
                Array.Empty<string>()
            }
        });
});

var app = builder.Build();

// --------------------------------------------------
// Rollen + Test-Administrator
// --------------------------------------------------

using (var scope = app.Services.CreateScope())
{
    var roleManager =
        scope.ServiceProvider
            .GetRequiredService<RoleManager<IdentityRole>>();

    var userManager =
        scope.ServiceProvider
            .GetRequiredService<UserManager<Benutzer>>();

    string[] rollen =
    {
        "Administrator",
        "Pflegekraft",
        "Hauswirtschaftskraft",
        "Kuechenmitarbeiter"
    };

    foreach (var rolle in rollen)
    {
        if (!await roleManager.RoleExistsAsync(rolle))
        {
            await roleManager.CreateAsync(
                new IdentityRole(rolle));
        }
    }

    // Nur für lokale Entwicklung / Tests
    const string adminBenutzername = "H.Aidouni";
    const string adminPasswort = "1234";

    var admin =
        await userManager.FindByNameAsync(
            adminBenutzername);

    if (admin == null)
    {
        admin = new Benutzer
        {
            UserName = adminBenutzername,
            Vorname = "H.",
            Nachname = "Aidouni",
            StandortId = null,
            IstAktiv = true
        };

        var result =
            await userManager.CreateAsync(
                admin,
                adminPasswort);

        if (!result.Succeeded)
        {
            var errors = string.Join(
                ", ",
                result.Errors.Select(
                    e => e.Description));

            throw new InvalidOperationException(
                $"Administrator konnte nicht erstellt werden: {errors}");
        }

        await userManager.AddToRoleAsync(
            admin,
            "Administrator");
    }
}

// --------------------------------------------------
// Swagger
// --------------------------------------------------

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// --------------------------------------------------
// HTTP Pipeline
// --------------------------------------------------

// Für lokale HTTP-Entwicklung vorerst deaktiviert.
// In Produktion wieder HTTPS aktivieren.
// app.UseHttpsRedirection();

app.UseAuthentication();

app.UseAuthorization();

app.MapControllers();

app.Run();
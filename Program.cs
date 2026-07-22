using DentalCollegeManagementSystem_AAU.Data;
using DentalCollegeManagementSystem_AAU.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// =====================================================
// MVC
// =====================================================

builder.Services.AddControllersWithViews();

// =====================================================
// SQL SERVER DATABASE
// =====================================================

string patientConnectionString =
    builder.Configuration.GetConnectionString("Patient")
    ?? throw new InvalidOperationException(
        "ConnectionStrings:Patient is missing.");

builder.Services.AddDbContext<AppDbContext>(
    options =>
    {
        options.UseSqlServer(
            patientConnectionString,
            sqlOptions =>
            {
                sqlOptions.CommandTimeout(90);

                sqlOptions.EnableRetryOnFailure(
                    maxRetryCount: 3,
                    maxRetryDelay: TimeSpan.FromSeconds(10),
                    errorNumbersToAdd: null);
            });
    });

// =====================================================
// SESSION
// =====================================================

builder.Services.AddDistributedMemoryCache();

builder.Services.AddSession(
    options =>
    {
        options.IdleTimeout =
            TimeSpan.FromMinutes(30);

        options.Cookie.Name =
            ".DentalCollege.Session";

        options.Cookie.HttpOnly = true;

        options.Cookie.IsEssential = true;

        options.Cookie.SameSite =
            SameSiteMode.Lax;

        options.Cookie.SecurePolicy =
            CookieSecurePolicy.SameAsRequest;
    });

// =====================================================
// ACTIVE DIRECTORY
// =====================================================

builder.Services.AddScoped<ActiveDirectoryValidator>(
    serviceProvider =>
    {
        IConfiguration configuration =
            serviceProvider.GetRequiredService<IConfiguration>();

        string activeDirectoryServer =
            configuration["ActiveDirectory:Server"]
            ?? configuration["ActiveDirectory:Domain"]
            ?? throw new InvalidOperationException(
                "ActiveDirectory:Server or ActiveDirectory:Domain "
                + "is missing from appsettings.json.");

        return new ActiveDirectoryValidator(
            activeDirectoryServer);
    });

// =====================================================
// APPLICATION SERVICES
// =====================================================

builder.Services.AddScoped<JwtTokenService>();

builder.Services.AddSignalR();

// =====================================================
// JWT SETTINGS
// =====================================================

string jwtKey =
    builder.Configuration["Jwt:Key"]
    ?? throw new InvalidOperationException(
        "Jwt:Key is missing.");

string jwtIssuer =
    builder.Configuration["Jwt:Issuer"]
    ?? throw new InvalidOperationException(
        "Jwt:Issuer is missing.");

string jwtAudience =
    builder.Configuration["Jwt:Audience"]
    ?? throw new InvalidOperationException(
        "Jwt:Audience is missing.");

if (Encoding.UTF8.GetByteCount(jwtKey) < 32)
{
    throw new InvalidOperationException(
        "Jwt:Key must be at least 32 bytes long.");
}

// =====================================================
// AUTHENTICATION
// =====================================================

builder.Services
    .AddAuthentication(
        options =>
        {
            options.DefaultAuthenticateScheme =
                JwtBearerDefaults.AuthenticationScheme;

            options.DefaultChallengeScheme =
                JwtBearerDefaults.AuthenticationScheme;
        })
    .AddJwtBearer(
        options =>
        {
            options.RequireHttpsMetadata = false;

            options.SaveToken = true;

            options.TokenValidationParameters =
                new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,

                    ValidIssuer = jwtIssuer,
                    ValidAudience = jwtAudience,

                    IssuerSigningKey =
                        new SymmetricSecurityKey(
                            Encoding.UTF8.GetBytes(jwtKey)),

                    ClockSkew =
                        TimeSpan.FromMinutes(1)
                };

            options.Events =
                new JwtBearerEvents
                {
                    OnMessageReceived =
                        context =>
                        {
                            string? token =
                                context.HttpContext
                                    .Session
                                    .GetString("AccessToken");

                            if (!string.IsNullOrWhiteSpace(token))
                            {
                                context.Token = token;
                            }

                            return Task.CompletedTask;
                        },

                    OnAuthenticationFailed =
                        context =>
                        {
                            Console.WriteLine(
                                "JWT authentication failed: "
                                + context.Exception.Message);

                            return Task.CompletedTask;
                        },

                    OnChallenge =
                        context =>
                        {
                            Console.WriteLine(
                                "JWT challenge occurred.");

                            return Task.CompletedTask;
                        }
                };
        });

builder.Services.AddAuthorization();

// =====================================================
// STARTUP INFORMATION
// =====================================================

string activeDirectoryDomain =
    builder.Configuration["ActiveDirectory:Domain"]
    ?? "(missing)";

string activeDirectoryServer =
    builder.Configuration["ActiveDirectory:Server"]
    ?? "(missing)";

Console.WriteLine(
    "==========================================");

Console.WriteLine(
    $"ENVIRONMENT: {builder.Environment.EnvironmentName}");

Console.WriteLine(
    $"ACTIVE DIRECTORY DOMAIN: {activeDirectoryDomain}");

Console.WriteLine(
    $"ACTIVE DIRECTORY SERVER: {activeDirectoryServer}");

Console.WriteLine(
    "==========================================");

// =====================================================
// BUILD APPLICATION
// =====================================================

var app = builder.Build();

// =====================================================
// ERROR HANDLING
// =====================================================

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");

    app.UseHsts();
}
else
{
    app.UseDeveloperExceptionPage();
}

// =====================================================
// HTTP PIPELINE
// =====================================================

app.UseHttpsRedirection();

app.UseStaticFiles();

app.UseRouting();

/*
 * Session must run before Authentication because
 * the JWT token is read from Session.
 */
app.UseSession();

app.UseAuthentication();

app.UseAuthorization();

// =====================================================
// SIGNALR
// =====================================================

app.MapHub<NotificationHub>(
    "/notificationHub");

// =====================================================
// ROUTES
// =====================================================

app.MapControllerRoute(
    name: "areas",
    pattern:
        "{area:exists}/{controller=Users}"
        + "/{action=PendingUsers}/{id?}");

app.MapControllerRoute(
    name: "default",
    pattern:
        "{controller=Account}"
        + "/{action=Login}/{id?}");

app.Run();
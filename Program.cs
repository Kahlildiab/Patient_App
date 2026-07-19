using DentalCollegeManagementSystem_AAU.Data;
using DentalCollegeManagementSystem_AAU.Options;
using DentalCollegeManagementSystem_AAU.Services;
using DentalCollegeManagementSystem_AAU.Services.StudentTransfer;
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
// DATABASE
// =====================================================

string connectionString =
    builder.Configuration.GetConnectionString("Patient")
    ?? throw new InvalidOperationException(
        "ConnectionStrings:Patient is missing.");

builder.Services.AddDbContext<AppDbContext>(
    options =>
    {
        options.UseSqlServer(
            connectionString,
            sqlOptions =>
            {
                sqlOptions.CommandTimeout(90);
                sqlOptions.EnableRetryOnFailure(
                    maxRetryCount: 3,
                    maxRetryDelay:
                        TimeSpan.FromSeconds(10),
                    errorNumbersToAdd: null);
            });
    });

// =====================================================
// SESSION
// =====================================================

builder.Services.AddSession(
    options =>
    {
        options.IdleTimeout =
            TimeSpan.FromMinutes(30);

        options.Cookie.HttpOnly = true;
        options.Cookie.IsEssential = true;

        options.Cookie.SameSite =
            SameSiteMode.Lax;

        options.Cookie.SecurePolicy =
            CookieSecurePolicy.SameAsRequest;
    });

// =====================================================
// APPLICATION SERVICES
// =====================================================

builder.Services.AddScoped<ActiveDirectoryValidator>(
    serviceProvider =>
    {
        IConfiguration configuration =
            serviceProvider
                .GetRequiredService<IConfiguration>();

        string ldapPath =
            configuration["ActiveDirectory:Server"]
            ?? configuration["ActiveDirectory:Domain"]
            ?? throw new InvalidOperationException(
                "ActiveDirectory:Server or "
                + "ActiveDirectory:Domain is missing "
                + "from appsettings.json.");

        return new ActiveDirectoryValidator(
            ldapPath);
    });

builder.Services.AddScoped<JwtTokenService>();

builder.Services.AddSignalR();

// =====================================================
// STUDENT TRANSFER SERVICE
// =====================================================

builder.Services.Configure<StudentTransferOptions>(
    builder.Configuration.GetSection(
        StudentTransferOptions.SectionName));

builder.Services.AddScoped<
    IStudentTransferService,
    StudentTransferService>();

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
                JwtBearerDefaults
                    .AuthenticationScheme;

            options.DefaultChallengeScheme =
                JwtBearerDefaults
                    .AuthenticationScheme;
        })
    .AddJwtBearer(
        options =>
        {
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
                            Encoding.UTF8.GetBytes(
                                jwtKey)),

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
                                    .GetString(
                                        "AccessToken");

                            if (!string.IsNullOrWhiteSpace(
                                    token))
                            {
                                context.Token = token;
                            }

                            return Task.CompletedTask;
                        },

                    OnAuthenticationFailed =
                        context =>
                        {
                            Console.WriteLine(
                                "JWT authentication "
                                + "failed: "
                                + context.Exception
                                    .Message);

                            return Task.CompletedTask;
                        }
                };
        });

builder.Services.AddAuthorization();

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

// =====================================================
// HTTP PIPELINE
// =====================================================

app.UseHttpsRedirection();

app.UseStaticFiles();

app.UseRouting();

/*
 * Session يجب أن تكون قبل Authentication
 * لأن التوكن تتم قراءته من Session.
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
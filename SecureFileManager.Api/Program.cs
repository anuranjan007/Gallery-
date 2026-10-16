using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using SecureFileManager.Infrastructure.Data;
using SecureFileManager.Infrastructure.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Configuration.AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                  .AddJsonFile("appsettings.Development.json", optional: true)
                  .AddEnvironmentVariables();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme.",
        Name = "Authorization",
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.Http,
        Scheme = "bearer"
    });
    options.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
    {
        {
            new Microsoft.OpenApi.Models.OpenApiSecurityScheme { Reference = new Microsoft.OpenApi.Models.OpenApiReference { Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme, Id = "Bearer" } },
            new string[] { }
        }
    });
});

// DbContext: support SQLite dev/local fallback and SQL Server in production
var connectionString = builder.Configuration.GetValue<string>("ConnectionStrings:DefaultConnection")
                       ?? "DataSource=AppData/securefilemanager.db"; // default to local sqlite

if (connectionString.Contains("DataSource=") || connectionString.EndsWith(".db", StringComparison.OrdinalIgnoreCase))
{
    builder.Services.AddDbContext<AppDbContext>(opts => opts.UseSqlite(connectionString));
}
else
{
    builder.Services.AddDbContext<AppDbContext>(opts => opts.UseSqlServer(connectionString));
}

// Identity helpers
builder.Services.AddScoped<IPasswordHasher<SecureFileManager.Domain.Entities.AdminUser>, PasswordHasher<SecureFileManager.Domain.Entities.AdminUser>>();

// App services
builder.Services.AddScoped<IAdminService, AdminService>();
builder.Services.AddScoped<IFileService, FileService>();
builder.Services.AddScoped<IFolderService, FolderService>();
builder.Services.AddScoped<IAuditLogService, AuditLogService>();
builder.Services.AddScoped<IAdminOtpService, AdminOtpService>();

builder.Services.AddScoped<FileSystemStorageService>();
builder.Services.AddScoped<EncryptionService>();

// Email service: prefer SMTP, fallback to file in dev
var smtpHost = builder.Configuration.GetValue<string>("Smtp:Host");
if (!string.IsNullOrEmpty(smtpHost)) builder.Services.AddScoped<IEmailService, SmtpEmailService>();
else builder.Services.AddScoped<IEmailService, FileEmailService>();

// Authentication (JWT)
var jwtKey = builder.Configuration.GetValue<string>("Jwt:SigningKey") ?? "development-signing-key-please-replace";
var signingKey = new Microsoft.IdentityModel.Tokens.SymmetricSecurityKey(System.Text.Encoding.UTF8.GetBytes(jwtKey));

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = Microsoft.AspNetCore.Authentication.JwtBearer.JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = Microsoft.AspNetCore.Authentication.JwtBearer.JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new Microsoft.IdentityModel.Tokens.TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = builder.Configuration.GetValue<string>("Jwt:Issuer") ?? "SecureFileManager",
        ValidAudience = builder.Configuration.GetValue<string>("Jwt:Audience") ?? "SecureFileManagerClient",
        IssuerSigningKey = signingKey
    };
});

var app = builder.Build();

app.UseApiExceptionHandling();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

// If using SQLite (local dev) ensure DB is created automatically so you can test without running migrations
using (var scope = app.Services.CreateScope())
{
    var env = scope.ServiceProvider.GetRequiredService<IWebHostEnvironment>();
    var cfg = scope.ServiceProvider.GetRequiredService<IConfiguration>();
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

    // If the provider is SQLite, attempt EnsureCreated to auto-create schema for quick local testing.
    try
    {
        var providerName = db.Database.ProviderName ?? string.Empty;
        if (providerName.Contains("Sqlite", StringComparison.OrdinalIgnoreCase))
        {
            db.Database.EnsureCreated();
        }
    }
    catch { /* ignore creation errors here; migrations are preferred for production */ }

    // Provision admin if configured (dev only)
    var username = cfg.GetValue<string>("InitialAdmin:Username");
    var password = cfg.GetValue<string>("InitialAdmin:Password");
    if (!string.IsNullOrEmpty(username) && !string.IsNullOrEmpty(password))
    {
        var hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher<SecureFileManager.Domain.Entities.AdminUser>>();
        if (!db.AdminUsers.Any())
        {
            var admin = new SecureFileManager.Domain.Entities.AdminUser
            {
                Id = Guid.NewGuid(),
                Username = username,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            admin.PasswordHash = hasher.HashPassword(admin, password);
            db.AdminUsers.Add(admin);
            db.SaveChanges();
        }
    }
}

app.Run();

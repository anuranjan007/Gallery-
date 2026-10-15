using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using SecureFileManager.Infrastructure.Data;
using SecureFileManager.Infrastructure.Services;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Configuration
builder.Configuration.AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                  .AddJsonFile("appsettings.Development.json", optional: true)
                  .AddEnvironmentVariables();

// Services
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// DbContext
var connectionString = builder.Configuration.GetValue<string>("ConnectionStrings:DefaultConnection") ?? "Server=(localdb)\\mssqllocaldb;Database=SecureFileManagerDb;Trusted_Connection=True;";
builder.Services.AddDbContext<AppDbContext>(opts => opts.UseSqlServer(connectionString));

// Application services
builder.Services.AddScoped<FileSystemStorageService>();
builder.Services.AddScoped<EncryptionService>();

// Authentication (JWT skeleton)
var jwtKey = builder.Configuration.GetValue<string>("Jwt:SigningKey") ?? "development-signing-key-please-replace";
var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
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

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();

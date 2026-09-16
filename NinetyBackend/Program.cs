using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using NinetyBackend.Infrastructure.Authentication;
using NinetyBackend.Infrastructure.Database;
using NinetyBackend.Infrastructure.Email;
using NinetyBackend.Modules.Auth.Repositories;
using NinetyBackend.Modules.Auth.Services;

var builder = WebApplication.CreateBuilder(args);

// 1. Add Environment Variables Configuration
builder.Configuration.AddEnvironmentVariables();

// 2. Database Context Configuration (Supabase PostgreSQL via Npgsql)
var connectionString = builder.Configuration["SUPABASE_CONNECTION_STRING"]
    ?? builder.Configuration.GetConnectionString("DefaultConnection")
    ?? "Host=localhost;Database=ninety_db;Username=postgres;Password=postgres";

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(connectionString));

// 3. Configure JwtOptions
builder.Services.Configure<JwtOptions>(options =>
{
    options.Issuer = builder.Configuration["JWT_ISSUER"] ?? "NinetyBackend";
    options.Audience = builder.Configuration["JWT_AUDIENCE"] ?? "NinetyBackendClient";
    options.SecretKey = builder.Configuration["JWT_SECRET_KEY"] ?? "SuperSecretDefaultKeyMustBeAtLeast32BytesLongForSecurity!";
    options.AccessTokenMinutes = int.TryParse(builder.Configuration["JWT_ACCESS_TOKEN_MINUTES"], out var m) ? m : 15;
    options.RefreshTokenDays = int.TryParse(builder.Configuration["JWT_REFRESH_TOKEN_DAYS"], out var d) ? d : 30;
});

// 4. Register Repositories & Services in DI
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IOtpRepository, OtpRepository>();
builder.Services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();
builder.Services.AddScoped<IRoleRepository, RoleRepository>();

builder.Services.AddScoped<IEmailService, EmailService>();
builder.Services.AddScoped<IJwtService, JwtService>();
builder.Services.AddScoped<IOtpService, OtpService>();
builder.Services.AddScoped<IOAuthService, OAuthService>();
builder.Services.AddScoped<IAuthorizationService, AuthorizationService>();
builder.Services.AddScoped<IAuthService, AuthService>();

// 5. Configure Authentication (JWT + Google OAuth)
var jwtSecretKey = builder.Configuration["JWT_SECRET_KEY"]
    ?? "SuperSecretDefaultKeyMustBeAtLeast32BytesLongForSecurity!";

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
        ValidIssuer = builder.Configuration["JWT_ISSUER"] ?? "NinetyBackend",
        ValidAudience = builder.Configuration["JWT_AUDIENCE"] ?? "NinetyBackendClient",
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecretKey))
    };

    options.Events = new JwtBearerEvents
    {
        OnMessageReceived = context =>
        {
            if (context.Request.Cookies.TryGetValue("accessToken", out var token))
            {
                context.Token = token;
            }
            return Task.CompletedTask;
        }
    };
})
.AddGoogle(googleOptions =>
{
    googleOptions.ClientId = builder.Configuration["GOOGLE_CLIENT_ID"] ?? "dummy-google-client-id";
    googleOptions.ClientSecret = builder.Configuration["GOOGLE_CLIENT_SECRET"] ?? "dummy-google-client-secret";
});

// 6. Controllers & OpenAPI
builder.Services.AddControllers();
builder.Services.AddOpenApi();

var app = builder.Build();

// 7. Pipeline Configuration
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();

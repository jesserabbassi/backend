using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using HealthChecks.UI.Client;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using NinetyBackend.Infrastructure.Authentication;
using NinetyBackend.Infrastructure.Configuration;
using NinetyBackend.Infrastructure.Database;
using NinetyBackend.Infrastructure.Email;
using NinetyBackend.Infrastructure.SignalR;
using NinetyBackend.Modules.Auth.Repositories;
using NinetyBackend.Modules.Auth.Services;
using NinetyBackend.Modules.Stations.Repositories;
using NinetyBackend.Modules.Stations.Services;
using Swashbuckle.AspNetCore.Swagger;
using Swashbuckle.AspNetCore.SwaggerUI;

// 1. Parse .env file before builder setup
var envData = DotEnv.Load(".env");

var builder = WebApplication.CreateBuilder(args);


// 2. Load .env data and expand %VAR% / ${VAR} placeholders from appsettings.json
builder.Configuration.AddInMemoryCollection(envData);
builder.Configuration.AddEnvironmentVariables();

var expandedPlaceholders = DotEnv.ExpandPlaceholders(builder.Configuration, envData);
builder.Configuration.AddInMemoryCollection(expandedPlaceholders);

// 3. Database Context Configuration (Supabase PostgreSQL via Npgsql)
var connectionString =
    builder.Configuration.GetConnectionString("DefaultConnection");

Console.WriteLine(
    $"Connection string configured: {!string.IsNullOrWhiteSpace(connectionString)}"
);

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(connectionString));

// 4. Configure JwtOptions from environment / appsettings
builder.Services.Configure<JwtOptions>(options =>
{
    var issuer = builder.Configuration["JWT_ISSUER"] ?? builder.Configuration["Jwt:Issuer"];
    options.Issuer = string.IsNullOrEmpty(issuer) || issuer.StartsWith('%') ? "NinetyBackend" : issuer;

    var audience = builder.Configuration["JWT_AUDIENCE"] ?? builder.Configuration["Jwt:Audience"];
    options.Audience = string.IsNullOrEmpty(audience) || audience.StartsWith('%') ? "NinetyBackendClient" : audience;

    var secretKey = builder.Configuration["JWT_SECRET_KEY"] ?? builder.Configuration["Jwt:SecretKey"];
    options.SecretKey = string.IsNullOrEmpty(secretKey) || secretKey.StartsWith('%')
        ? "SuperSecretDefaultKeyMustBeAtLeast32BytesLongForSecurity!"
        : secretKey;

    var accessMin = builder.Configuration["JWT_ACCESS_TOKEN_MINUTES"] ?? builder.Configuration["Jwt:AccessTokenMinutes"];
    options.AccessTokenMinutes = int.TryParse(accessMin, out var m) ? m : 15;

    var refreshDays = builder.Configuration["JWT_REFRESH_TOKEN_DAYS"] ?? builder.Configuration["Jwt:RefreshTokenDays"];
    options.RefreshTokenDays = int.TryParse(refreshDays, out var d) ? d : 30;
});

// 5. Register Repositories & Services in DI Container
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IOtpRepository, OtpRepository>();
builder.Services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();
builder.Services.AddScoped<IRoleRepository, RoleRepository>();
builder.Services.AddScoped<IStationRepository, StationRepository>();
builder.Services.AddScoped<IAgentRepository, AgentRepository>();

builder.Services.AddScoped<IEmailService, EmailService>();
builder.Services.AddScoped<IJwtService, JwtService>();
builder.Services.AddScoped<IOtpService, OtpService>();
builder.Services.AddScoped<IOAuthService, OAuthService>();
builder.Services.AddScoped<IAuthorizationService, AuthorizationService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IStationService, StationService>();
builder.Services.AddScoped<IAgentService, AgentService>();

// 6. Configure Authentication (JWT Bearer + Google OAuth)
var rawSecretKey = builder.Configuration["JWT_SECRET_KEY"] ?? builder.Configuration["Jwt:SecretKey"];
var jwtSecretKey = string.IsNullOrEmpty(rawSecretKey) || rawSecretKey.StartsWith('%')
    ? "SuperSecretDefaultKeyMustBeAtLeast32BytesLongForSecurity!"
    : rawSecretKey;

var rawIssuer = builder.Configuration["JWT_ISSUER"] ?? builder.Configuration["Jwt:Issuer"];
var jwtIssuer = string.IsNullOrEmpty(rawIssuer) || rawIssuer.StartsWith('%') ? "NinetyBackend" : rawIssuer;

var rawAudience = builder.Configuration["JWT_AUDIENCE"] ?? builder.Configuration["Jwt:Audience"];
var jwtAudience = string.IsNullOrEmpty(rawAudience) || rawAudience.StartsWith('%') ? "NinetyBackendClient" : rawAudience;

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
        ValidIssuer = jwtIssuer,
        ValidAudience = jwtAudience,
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
    var clientId = builder.Configuration["GOOGLE_CLIENT_ID"] ?? builder.Configuration["Authentication:Google:ClientId"];
    googleOptions.ClientId = string.IsNullOrEmpty(clientId) || clientId.StartsWith('%') ? "dummy-google-client-id" : clientId;

    var clientSecret = builder.Configuration["GOOGLE_CLIENT_SECRET"] ?? builder.Configuration["Authentication:Google:ClientSecret"];
    googleOptions.ClientSecret = string.IsNullOrEmpty(clientSecret) || clientSecret.StartsWith('%') ? "dummy-google-client-secret" : clientSecret;
});

// 7. Add Controllers & Swagger & SignalR
builder.Services.AddControllers();
builder.Services.AddSignalR();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddHealthChecks();

// Avant Build()
builder.Services.AddSwaggerGen(options =>
{
    options.DocumentFilter<HealthCheckDocumentFilter>();
});

var app = builder.Build();

// 8. Health Endpoint
app.MapHealthChecks("/health", new HealthCheckOptions
{
    ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
});
// 9. Pipeline Configuration
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapHub<StationHub>("/hubs/stations");

app.Run();

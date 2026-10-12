using System.Text;
using System.Threading.RateLimiting;
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
using NinetyBackend.Modules.Auth.Authorization;
using NinetyBackend.Modules.Customers.Repositories;
using NinetyBackend.Modules.Customers.Services;
using NinetyBackend.Modules.Games.Repositories;
using NinetyBackend.Modules.Games.Services;
using NinetyBackend.Modules.MonitoringAlerts.Repositories;
using NinetyBackend.Modules.MonitoringAlerts.Services;
using NinetyBackend.Modules.RemoteControl.Repositories;
using NinetyBackend.Modules.RemoteControl.Services;
using NinetyBackend.Modules.Reservations.Repositories;
using NinetyBackend.Modules.Reservations.Services;
using NinetyBackend.Modules.Stations.Repositories;
using NinetyBackend.Modules.Stations.Services;
using NinetyBackend.Modules.Stations.Models;
using NinetyBackend.Modules.Wallets.Repositories;
using NinetyBackend.Modules.Wallets.Services;
using Swashbuckle.AspNetCore.Swagger;
using Swashbuckle.AspNetCore.SwaggerUI;

// 1. Parse .env file before builder setup
var envData = DotEnv.Load(".env");

var builder = WebApplication.CreateBuilder(args);
var isProduction = builder.Environment.IsProduction();


// 2. Load .env data and expand %VAR% / ${VAR} placeholders from appsettings.json
builder.Configuration.AddInMemoryCollection(envData);
builder.Configuration.AddEnvironmentVariables();

var expandedPlaceholders = DotEnv.ExpandPlaceholders(builder.Configuration, envData);
builder.Configuration.AddInMemoryCollection(expandedPlaceholders);

// 3. Database Context Configuration (Supabase PostgreSQL via Npgsql)
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? builder.Configuration["NINETY_DATABASE_CONNECTION_STRING"];

if (string.IsNullOrWhiteSpace(connectionString))
    throw new InvalidOperationException("Database connection string is required.");

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
    if (isProduction && (string.IsNullOrEmpty(secretKey) || secretKey.StartsWith('%') || secretKey.Length < 32))
        throw new InvalidOperationException("JWT_SECRET_KEY must be configured with at least 32 characters in production.");
    options.SecretKey = string.IsNullOrEmpty(secretKey) || secretKey.StartsWith('%')
        ? "DevelopmentOnlySecretKeyMustNotBeUsedInProduction!"
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
builder.Services.AddScoped<ICustomerRepository, CustomerRepository>();
builder.Services.AddScoped<IGameRepository, GameRepository>();
builder.Services.AddScoped<IStationRepository, StationRepository>();
builder.Services.AddScoped<IReservationRepository, ReservationRepository>();
builder.Services.AddScoped<IAgentRepository, AgentRepository>();
builder.Services.AddScoped<IAgentRefreshTokenRepository, AgentRefreshTokenRepository>();
builder.Services.AddScoped<ITelemetryRepository, TelemetryRepository>();
builder.Services.AddScoped<IAlertRepository, AlertRepository>();
builder.Services.AddScoped<IWalletRepository, WalletRepository>();
builder.Services.AddScoped<ICommandRepository, CommandRepository>();
builder.Services.AddScoped<IAgentConnectionService, InMemoryAgentConnectionService>();

builder.Services.AddScoped<IEmailService, EmailService>();
builder.Services.AddScoped<IJwtService, JwtService>();
builder.Services.AddScoped<IOtpService, OtpService>();
builder.Services.AddScoped<IOAuthService, OAuthService>();
builder.Services.AddScoped<IAuthorizationService, AuthorizationService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<ICustomerService, CustomerService>();
builder.Services.AddScoped<IGameCatalogService, GameCatalogService>();
builder.Services.AddScoped<IStationService, StationService>();
builder.Services.AddScoped<IAgentService, AgentService>();
builder.Services.AddScoped<IMonitoringService, MonitoringService>();
builder.Services.AddScoped<IAlertService, AlertService>();
builder.Services.AddScoped<IWalletService, WalletService>();
builder.Services.AddScoped<ITransactionService, TransactionService>();
builder.Services.AddScoped<IPaymentService, PaymentService>();
builder.Services.AddSingleton<IAgentConnectionManager, AgentConnectionManager>();
builder.Services.AddScoped<IRemoteControlService, RemoteControlService>();
builder.Services.AddScoped<IAvailabilityService, AvailabilityService>();
builder.Services.AddScoped<IReservationService, ReservationService>();

// 6. Configure Authentication (JWT Bearer + Google OAuth)
var rawSecretKey = builder.Configuration["JWT_SECRET_KEY"] ?? builder.Configuration["Jwt:SecretKey"];
if (isProduction && (string.IsNullOrEmpty(rawSecretKey) || rawSecretKey.StartsWith('%') || rawSecretKey.Length < 32))
    throw new InvalidOperationException("JWT_SECRET_KEY must be configured with at least 32 characters in production.");
var jwtSecretKey = string.IsNullOrEmpty(rawSecretKey) || rawSecretKey.StartsWith('%')
    ? "DevelopmentOnlySecretKeyMustNotBeUsedInProduction!"
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
    options.MapInboundClaims = false;
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
            else if (context.HttpContext.Request.Path.StartsWithSegments("/hubs/agents") &&
                     context.Request.Query.TryGetValue("access_token", out var queryToken))
            {
                context.Token = queryToken;
            }
            return Task.CompletedTask;
        },
        OnTokenValidated = async context =>
        {
            var agentId = context.Principal?.FindFirst("agent_id")?.Value;
            if (!Guid.TryParse(agentId, out var id))
                return;

            var repository = context.HttpContext.RequestServices.GetRequiredService<IAgentRepository>();
            var agent = await repository.GetByIdAsync(id);
            if (agent is null || agent.Status is AgentStatus.Pending or AgentStatus.Rejected or AgentStatus.Revoked)
                context.Fail("Agent is not active.");
        }
    };
})
.AddGoogle(googleOptions =>
{
    var clientId = builder.Configuration["NINETY_GOOGLE_CLIENT_ID"] ?? builder.Configuration["GOOGLE_CLIENT_ID"] ?? builder.Configuration["Authentication:Google:ClientId"];
    if (isProduction && (string.IsNullOrWhiteSpace(clientId) || clientId.StartsWith('%')))
        throw new InvalidOperationException("Google OAuth client ID is required when Google authentication is enabled in production.");
    googleOptions.ClientId = string.IsNullOrEmpty(clientId) || clientId.StartsWith('%') ? "dummy-google-client-id" : clientId;

    var clientSecret = builder.Configuration["NINETY_GOOGLE_CLIENT_SECRET"] ?? builder.Configuration["GOOGLE_CLIENT_SECRET"] ?? builder.Configuration["Authentication:Google:ClientSecret"];
    if (isProduction && (string.IsNullOrWhiteSpace(clientSecret) || clientSecret.StartsWith('%')))
        throw new InvalidOperationException("Google OAuth client secret is required when Google authentication is enabled in production.");
    googleOptions.ClientSecret = string.IsNullOrEmpty(clientSecret) || clientSecret.StartsWith('%') ? "dummy-google-client-secret" : clientSecret;
});

builder.Services.AddAuthorization(options =>
{
    foreach (var permission in RbacDefinitions.Permissions)
    {
        options.AddPolicy(permission, policy =>
            policy.RequireAuthenticatedUser().RequireClaim("permission", permission));
    }
});

// 7. Add Controllers & Swagger & SignalR
builder.Services.AddControllers();
builder.Services.AddSignalR(options =>
{
    options.EnableDetailedErrors = builder.Environment.IsDevelopment();
});
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddHealthChecks();
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
        RateLimitPartition.GetFixedWindowLimiter(
            context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 120,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0
            }));
});

// Avant Build()
builder.Services.AddSwaggerGen(options =>
{
    options.DocumentFilter<HealthCheckDocumentFilter>();
});

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler();
    app.UseHsts();
}

using (var scope = app.Services.CreateScope())
{
    await RbacSeeder.SeedAsync(scope.ServiceProvider.GetRequiredService<ApplicationDbContext>());
}

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
app.UseRateLimiter();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapHub<StationHub>("/hubs/stations");
app.MapHub<AgentHub>("/hubs/agents");

app.Run();

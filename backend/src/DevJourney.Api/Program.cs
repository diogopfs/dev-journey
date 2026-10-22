using System.Text;
using System.Text.Json.Serialization;
using System.Threading.RateLimiting;
using DevJourney.Api.Infrastructure;
using DevJourney.Api.Security;
using DevJourney.Application;
using DevJourney.Application.Abstractions.Security;
using DevJourney.Domain.Users;
using DevJourney.Infrastructure;
using DevJourney.Infrastructure.Observability;
using DevJourney.Infrastructure.Persistence;
using DevJourney.Infrastructure.Security;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.IdentityModel.Tokens;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

// ---------- Observabilidade (OpenTelemetry: traces, métricas e logs) ----------
builder.AddObservability("devjourney-api");

// ---------- Camadas (Clean Architecture) ----------
builder.Services
    .AddApplication()
    .AddInfrastructure(builder.Configuration);

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUser, CurrentUser>();
builder.Services.AddSingleton<AuthMetrics>();

// ---------- Autenticação JWT Bearer ----------
var jwt = builder.Configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>()
    ?? throw new InvalidOperationException("Seção 'Jwt' não configurada.");

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.MapInboundClaims = false; // mantém os nomes padrão das claims (sub, email, role...)
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwt.Issuer,
            ValidateAudience = true,
            ValidAudience = jwt.Audience,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.SigningKey)),
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromSeconds(30),
            NameClaimType = "name",
            RoleClaimType = "role"
        };
    });

// ---------- Autorização (políticas) ----------
builder.Services.AddAuthorizationBuilder()
    .AddPolicy(AuthPolicies.AdminOnly, policy => policy.RequireRole(nameof(UserRole.Admin)));

// ---------- Rate limiting: limita tentativas nos endpoints de autenticação (anti força bruta) ----------
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddPolicy(AuthPolicies.AuthRateLimit, context =>
        RateLimitPartition.GetFixedWindowLimiter(
            context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = builder.Configuration.GetValue("RateLimiting:AuthPermitPerMinute", 20),
                Window = TimeSpan.FromMinutes(1)
            }));
});

// ---------- CORS: apenas as origens do front-end configuradas ----------
var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];
builder.Services.AddCors(options => options.AddDefaultPolicy(policy =>
    policy.WithOrigins(allowedOrigins).AllowAnyHeader().AllowAnyMethod().AllowCredentials()));

// ---------- Tratamento de erros padronizado (Problem Details - RFC 9457) ----------
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();

// Enums trafegam como texto ("Admin") em vez de número (1): contrato mais legível.
builder.Services.AddControllers()
    .AddJsonOptions(o => o.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));
builder.Services.AddOpenApi(options => options.AddDocumentTransformer<BearerSecuritySchemeTransformer>());

var app = builder.Build();

if (app.Configuration.GetValue("Database:MigrateOnStartup", true))
    await app.Services.InitializeDatabaseAsync();

app.UseExceptionHandler();
app.UseStatusCodePages();
app.UseSecurityHeaders();

if (app.Environment.IsDevelopment())
{
    // Documentação interativa: /scalar (OpenAPI em /openapi/v1.json)
    app.MapOpenApi();
    app.MapScalarApiReference(options => options.WithTitle("DevJourney API"));
}
else
{
    app.UseHsts();
}

app.UseCors();
app.UseAuthentication();
app.UseAuthorization();
app.UseRateLimiter();

app.MapControllers();

// Liveness: o processo está de pé? Readiness: as dependências (Postgres, Mongo, RabbitMQ) respondem?
app.MapHealthChecks("/health/live", new HealthCheckOptions { Predicate = _ => false });
app.MapHealthChecks("/health/ready", new HealthCheckOptions { Predicate = c => c.Tags.Contains("ready") });

app.Run();

// Necessário para testes de integração com WebApplicationFactory (evolução futura).
public partial class Program;

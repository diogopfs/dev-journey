using DevJourney.Application.Abstractions.Audit;
using DevJourney.Application.Abstractions.Messaging;
using DevJourney.Application.Abstractions.Persistence;
using DevJourney.Application.Abstractions.Security;
using DevJourney.Infrastructure.HealthChecks;
using DevJourney.Infrastructure.Messaging;
using DevJourney.Infrastructure.NoSql;
using DevJourney.Infrastructure.Persistence;
using DevJourney.Infrastructure.Persistence.Repositories;
using DevJourney.Infrastructure.Security;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using MongoDB.Driver;

namespace DevJourney.Infrastructure;

/// <summary>
/// Composition root da infraestrutura. Cada bloco é um método separado para que cada aplicação
/// registre só o que precisa (a API usa tudo; o Worker usa apenas mensageria + MongoDB).
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration) =>
        services
            .AddPersistence(configuration)
            .AddSecurity(configuration)
            .AddMessaging(configuration)
            .AddMongo(configuration);

    public static IServiceCollection AddPersistence(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("Postgres")
            ?? throw new InvalidOperationException("ConnectionStrings:Postgres não configurada.");

        services.AddDbContext<AppDbContext>(options => options.UseNpgsql(connectionString));
        services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<AppDbContext>());
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();

        services.AddHealthChecks().AddDbContextCheck<AppDbContext>("postgres", tags: ["ready"]);
        return services;
    }

    public static IServiceCollection AddSecurity(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<JwtOptions>()
            .Bind(configuration.GetSection(JwtOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddSingleton<ITokenService, JwtTokenService>();
        services.AddSingleton<IPasswordHasher, PasswordHasher>();
        return services;
    }

    public static IServiceCollection AddMessaging(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<RabbitMqOptions>(configuration.GetSection(RabbitMqOptions.SectionName));
        services.AddSingleton<RabbitMqConnection>();
        services.AddSingleton<IEventPublisher, RabbitMqEventPublisher>();

        services.AddHealthChecks().AddCheck<RabbitMqHealthCheck>("rabbitmq", tags: ["ready"]);
        return services;
    }

    public static IServiceCollection AddMongo(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<MongoOptions>(configuration.GetSection(MongoOptions.SectionName));

        // MongoClient é thread-safe e deve ser singleton (ele gerencia o pool de conexões).
        services.AddSingleton<IMongoClient>(sp => new MongoClient(sp.GetRequiredService<IOptions<MongoOptions>>().Value.ConnectionString));
        services.AddSingleton(sp => sp.GetRequiredService<IMongoClient>()
            .GetDatabase(sp.GetRequiredService<IOptions<MongoOptions>>().Value.Database));

        services.AddSingleton<MongoAuditLogStore>();
        services.AddSingleton<IAuditLogWriter>(sp => sp.GetRequiredService<MongoAuditLogStore>());
        services.AddSingleton<IAuditLogReader>(sp => sp.GetRequiredService<MongoAuditLogStore>());

        services.AddHealthChecks().AddCheck<MongoHealthCheck>("mongodb", tags: ["ready"]);
        return services;
    }
}

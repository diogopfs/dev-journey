using DevJourney.Infrastructure;
using DevJourney.Infrastructure.NoSql;
using DevJourney.Infrastructure.Observability;
using DevJourney.Worker.Consumers;

// O Worker é um processo separado da API: escala de forma independente
// e, se cair, as mensagens ficam guardadas na fila até ele voltar.
var builder = Host.CreateApplicationBuilder(args);

builder.AddObservability("devjourney-worker");

builder.Services.AddSingleton(TimeProvider.System);
builder.Services
    .AddMessaging(builder.Configuration)
    .AddMongo(builder.Configuration);

builder.Services.AddHostedService<AuditLogConsumer>();
builder.Services.AddHostedService<WelcomeEmailConsumer>();

var host = builder.Build();

await host.Services.InitializeMongoAsync();

host.Run();

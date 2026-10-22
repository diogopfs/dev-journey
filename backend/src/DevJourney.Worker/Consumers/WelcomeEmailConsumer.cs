using System.Text.Json;
using DevJourney.Application.Events;
using DevJourney.Infrastructure.Messaging;
using DevJourney.Worker.Messaging;

namespace DevJourney.Worker.Consumers;

/// <summary>
/// Recebe apenas "user.created" e simula o envio de um e-mail de boas-vindas.
/// Repare: o MESMO evento chega a duas filas diferentes (auditoria e e-mail) — padrão publish/subscribe.
/// A API não sabe que este consumidor existe; podemos adicionar novos consumidores sem alterar a API
/// (Open/Closed Principle aplicado à arquitetura).
/// </summary>
public sealed class WelcomeEmailConsumer(RabbitMqConnection connection, ILogger<WelcomeEmailConsumer> logger)
    : RabbitMqConsumer(connection, logger)
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    protected override string QueueName => "devjourney.welcome-email";
    protected override string[] RoutingKeys => [UserCreatedEvent.Type];

    protected override async Task HandleAsync(ConsumedMessage message, CancellationToken ct)
    {
        var @event = JsonSerializer.Deserialize<UserCreatedEvent>(message.Body, JsonOptions)
            ?? throw new InvalidOperationException("Mensagem inválida.");

        // Aqui entraria um serviço real de e-mail (SMTP, SendGrid, Amazon SES...).
        await Task.Delay(TimeSpan.FromMilliseconds(200), ct);
        logger.LogInformation("📧 E-mail de boas-vindas enviado para {Name} <{Email}>", @event.Name, @event.Email);
    }
}

using System.Diagnostics;
using System.Text.Json;
using DevJourney.Application.Abstractions.Messaging;
using DevJourney.Application.Events;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;

namespace DevJourney.Infrastructure.Messaging;

/// <summary>
/// Publica eventos de integração na exchange "devjourney.events" usando o tipo do evento como routing key.
/// O publisher NÃO conhece os consumidores: quem tiver interesse cria sua fila e faz o "bind" (desacoplamento).
/// </summary>
internal sealed class RabbitMqEventPublisher(RabbitMqConnection connection, ILogger<RabbitMqEventPublisher> logger)
    : IEventPublisher
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public async Task PublishAsync<TEvent>(TEvent @event, CancellationToken ct = default) where TEvent : IntegrationEvent
    {
        using var activity = MessagingTelemetry.ActivitySource.StartActivity($"{@event.EventType} publish", ActivityKind.Producer);
        activity?.SetTag("messaging.system", "rabbitmq");
        activity?.SetTag("messaging.destination.name", RabbitMqTopology.EventsExchange);
        activity?.SetTag("messaging.rabbitmq.destination.routing_key", @event.EventType);
        activity?.SetTag("messaging.message.id", @event.EventId.ToString());

        try
        {
            var conn = await connection.GetConnectionAsync(ct);

            // Canais são baratos e NÃO são thread-safe; aqui abrimos um por publicação para simplificar.
            // Publisher confirms: o broker confirma o recebimento antes de seguirmos.
            await using var channel = await conn.CreateChannelAsync(
                new CreateChannelOptions(publisherConfirmationsEnabled: true, publisherConfirmationTrackingEnabled: true),
                ct);

            await channel.ExchangeDeclareAsync(RabbitMqTopology.EventsExchange, ExchangeType.Topic, durable: true, cancellationToken: ct);

            var headers = new Dictionary<string, object?>();
            MessagingTelemetry.Inject(activity, headers);

            var properties = new BasicProperties
            {
                MessageId = @event.EventId.ToString(),
                Type = @event.EventType,
                ContentType = "application/json",
                DeliveryMode = DeliveryModes.Persistent, // mensagem sobrevive a reinício do broker
                Timestamp = new AmqpTimestamp(@event.OccurredAt.ToUnixTimeSeconds()),
                Headers = headers
            };

            // Serializa pelo tipo concreto para incluir todas as propriedades do evento.
            var body = JsonSerializer.SerializeToUtf8Bytes(@event, @event.GetType(), JsonOptions);

            await channel.BasicPublishAsync(
                RabbitMqTopology.EventsExchange, @event.EventType, mandatory: false, properties, body, ct);

            MessagingTelemetry.MessagesPublished.Add(1, new KeyValuePair<string, object?>("event_type", @event.EventType));
            logger.LogInformation("Evento {EventType} {EventId} publicado", @event.EventType, @event.EventId);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            // Decisão didática: falha no broker não derruba a requisição (o dado já foi salvo no banco).
            // O preço é poder "perder" o evento. A solução robusta é o padrão Transactional Outbox (próximo passo!).
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            logger.LogError(ex, "Falha ao publicar evento {EventType} {EventId}", @event.EventType, @event.EventId);
        }
    }
}

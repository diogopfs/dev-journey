using DevJourney.Application.Events;

namespace DevJourney.Application.Abstractions.Messaging;

/// <summary>
/// Publica eventos de integração em um message broker (RabbitMQ na implementação atual).
/// Trocar de broker (Kafka, Azure Service Bus...) significa apenas criar outra implementação.
/// </summary>
public interface IEventPublisher
{
    Task PublishAsync<TEvent>(TEvent @event, CancellationToken ct = default) where TEvent : IntegrationEvent;
}

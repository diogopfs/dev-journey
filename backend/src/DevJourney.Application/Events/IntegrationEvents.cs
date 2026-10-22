using System.Text.Json.Serialization;

namespace DevJourney.Application.Events;

/// <summary>
/// Evento de integração: um fato que já aconteceu ("UserCreated") e que interessa a outros serviços.
/// <see cref="EventType"/> é usado como routing key no RabbitMQ (ex.: "user.created").
/// </summary>
public abstract record IntegrationEvent
{
    public Guid EventId { get; init; } = Guid.CreateVersion7();
    public DateTimeOffset OccurredAt { get; init; } = DateTimeOffset.UtcNow;

    [JsonIgnore]
    public abstract string EventType { get; }
}

public sealed record UserCreatedEvent(Guid UserId, string Name, string Email, string Role, Guid? PerformedBy)
    : IntegrationEvent
{
    public const string Type = "user.created";
    public override string EventType => Type;
}

public sealed record UserUpdatedEvent(Guid UserId, string Name, string Email, string Role, bool IsActive, Guid? PerformedBy)
    : IntegrationEvent
{
    public const string Type = "user.updated";
    public override string EventType => Type;
}

public sealed record UserDeletedEvent(Guid UserId, string Email, Guid? PerformedBy) : IntegrationEvent
{
    public const string Type = "user.deleted";
    public override string EventType => Type;
}

public sealed record UserLoggedInEvent(Guid UserId, string Email) : IntegrationEvent
{
    public const string Type = "user.logged-in";
    public override string EventType => Type;
}

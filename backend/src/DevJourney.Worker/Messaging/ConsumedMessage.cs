namespace DevJourney.Worker.Messaging;

public sealed record ConsumedMessage(string RoutingKey, string? MessageId, string Body);

namespace DevJourney.Infrastructure.Messaging;

public sealed class RabbitMqOptions
{
    public const string SectionName = "RabbitMq";

    /// <summary>
    /// URI completa, ex.: amqps://user:pass@host/vhost (formato fornecido por serviços gratuitos como o CloudAMQP).
    /// Se informada, tem prioridade sobre HostName/Port/UserName/Password.
    /// </summary>
    public string? Uri { get; init; }

    public string HostName { get; init; } = "localhost";
    public int Port { get; init; } = 5672;
    public string UserName { get; init; } = "guest";
    public string Password { get; init; } = "guest";
    public string VirtualHost { get; init; } = "/";
}

/// <summary>
/// Nomes da topologia do RabbitMQ (exchanges e filas) centralizados em um só lugar.
/// </summary>
public static class RabbitMqTopology
{
    /// <summary>Exchange do tipo "topic": roteia mensagens por padrão de routing key (ex.: "user.*").</summary>
    public const string EventsExchange = "devjourney.events";

    /// <summary>Dead Letter Exchange: recebe mensagens que falharam no processamento.</summary>
    public const string DeadLetterExchange = "devjourney.events.dlx";
}

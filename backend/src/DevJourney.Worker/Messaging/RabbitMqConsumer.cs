using System.Diagnostics;
using System.Text;
using DevJourney.Infrastructure.Messaging;
using OpenTelemetry;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace DevJourney.Worker.Messaging;

/// <summary>
/// Classe base (Template Method) para consumidores RabbitMQ. Cada consumidor concreto só define:
/// - o nome da sua fila,
/// - quais routing keys quer receber,
/// - o que fazer com a mensagem (HandleAsync).
///
/// Conceitos demonstrados:
/// - Exchange "topic" + bindings por padrão de routing key ("user.*", "user.created");
/// - Fila durável com Dead Letter Exchange (mensagens com erro vão para a fila "&lt;fila&gt;.dlq");
/// - ACK manual: a mensagem só sai da fila depois de processada com sucesso;
/// - Prefetch (QoS): quantas mensagens o consumidor recebe por vez.
/// </summary>
public abstract class RabbitMqConsumer(RabbitMqConnection connection, ILogger logger) : BackgroundService
{
    private IChannel? _channel;

    protected abstract string QueueName { get; }
    protected abstract string[] RoutingKeys { get; }

    protected abstract Task HandleAsync(ConsumedMessage message, CancellationToken ct);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Retenta a conexão até o broker estar disponível (útil quando tudo sobe junto no docker compose).
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await StartConsumingAsync(stoppingToken);
                return;
            }
            catch (Exception ex) when (!stoppingToken.IsCancellationRequested)
            {
                logger.LogWarning(ex, "Falha ao conectar consumidor {Queue}. Nova tentativa em 5s...", QueueName);
                await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
            }
        }
    }

    private async Task StartConsumingAsync(CancellationToken ct)
    {
        var conn = await connection.GetConnectionAsync(ct);
        _channel = await conn.CreateChannelAsync(cancellationToken: ct);

        await DeclareTopologyAsync(_channel, ct);
        await _channel.BasicQosAsync(prefetchSize: 0, prefetchCount: 10, global: false, ct);

        var consumer = new AsyncEventingBasicConsumer(_channel);
        consumer.ReceivedAsync += (_, args) => OnMessageAsync(args, ct);

        await _channel.BasicConsumeAsync(QueueName, autoAck: false, consumer, ct);
        logger.LogInformation("Consumindo fila {Queue} (bindings: {RoutingKeys})", QueueName, string.Join(", ", RoutingKeys));
    }

    private async Task DeclareTopologyAsync(IChannel channel, CancellationToken ct)
    {
        var deadLetterQueue = $"{QueueName}.dlq";

        await channel.ExchangeDeclareAsync(RabbitMqTopology.EventsExchange, ExchangeType.Topic, durable: true, cancellationToken: ct);
        await channel.ExchangeDeclareAsync(RabbitMqTopology.DeadLetterExchange, ExchangeType.Direct, durable: true, cancellationToken: ct);

        // Fila de "mensagens mortas" para análise/reprocessamento manual.
        await channel.QueueDeclareAsync(deadLetterQueue, durable: true, exclusive: false, autoDelete: false, cancellationToken: ct);
        await channel.QueueBindAsync(deadLetterQueue, RabbitMqTopology.DeadLetterExchange, routingKey: QueueName, cancellationToken: ct);

        await channel.QueueDeclareAsync(QueueName, durable: true, exclusive: false, autoDelete: false,
            arguments: new Dictionary<string, object?>
            {
                ["x-dead-letter-exchange"] = RabbitMqTopology.DeadLetterExchange,
                ["x-dead-letter-routing-key"] = QueueName
            },
            cancellationToken: ct);

        foreach (var routingKey in RoutingKeys)
            await channel.QueueBindAsync(QueueName, RabbitMqTopology.EventsExchange, routingKey, cancellationToken: ct);
    }

    private async Task OnMessageAsync(BasicDeliverEventArgs args, CancellationToken ct)
    {
        // Continua o trace iniciado pela API (contexto propagado nos headers da mensagem).
        var parent = MessagingTelemetry.Extract(args.BasicProperties.Headers);
        Baggage.Current = parent.Baggage;

        using var activity = MessagingTelemetry.ActivitySource.StartActivity(
            $"{QueueName} process", ActivityKind.Consumer, parent.ActivityContext);
        activity?.SetTag("messaging.system", "rabbitmq");
        activity?.SetTag("messaging.destination.name", QueueName);
        activity?.SetTag("messaging.rabbitmq.destination.routing_key", args.RoutingKey);
        activity?.SetTag("messaging.message.id", args.BasicProperties.MessageId);

        var tags = new KeyValuePair<string, object?>[] { new("queue", QueueName), new("event_type", args.RoutingKey) };

        try
        {
            var message = new ConsumedMessage(args.RoutingKey, args.BasicProperties.MessageId, Encoding.UTF8.GetString(args.Body.Span));
            await HandleAsync(message, ct);

            await _channel!.BasicAckAsync(args.DeliveryTag, multiple: false, ct);
            MessagingTelemetry.MessagesConsumed.Add(1, tags);
        }
        catch (Exception ex)
        {
            // requeue: false -> a mensagem vai para a Dead Letter Queue em vez de ficar em loop infinito.
            logger.LogError(ex, "Erro ao processar mensagem {MessageId} da fila {Queue}", args.BasicProperties.MessageId, QueueName);
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            await _channel!.BasicNackAsync(args.DeliveryTag, multiple: false, requeue: false, ct);
            MessagingTelemetry.MessagesFailed.Add(1, tags);
        }
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        if (_channel is not null)
        {
            await _channel.CloseAsync(cancellationToken);
            _channel.Dispose();
        }
        await base.StopAsync(cancellationToken);
    }
}

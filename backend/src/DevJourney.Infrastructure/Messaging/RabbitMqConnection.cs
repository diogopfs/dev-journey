using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;

namespace DevJourney.Infrastructure.Messaging;

/// <summary>
/// Mantém UMA conexão TCP com o RabbitMQ por aplicação (conexões são caras; canais são leves).
/// A conexão é criada sob demanda e recriada automaticamente pelo client em caso de queda.
/// </summary>
public sealed class RabbitMqConnection(IOptions<RabbitMqOptions> options, ILogger<RabbitMqConnection> logger)
    : IAsyncDisposable
{
    private readonly SemaphoreSlim _lock = new(1, 1);
    private IConnection? _connection;

    public bool IsConnected => _connection is { IsOpen: true };

    public async Task<IConnection> GetConnectionAsync(CancellationToken ct = default)
    {
        if (_connection is { IsOpen: true }) return _connection;

        await _lock.WaitAsync(ct);
        try
        {
            if (_connection is { IsOpen: true }) return _connection;

            var factory = CreateFactory(options.Value);
            _connection = await factory.CreateConnectionAsync(clientProvidedName: AppDomain.CurrentDomain.FriendlyName, ct);
            logger.LogInformation("Conectado ao RabbitMQ em {Host}", factory.HostName);
            return _connection;
        }
        finally
        {
            _lock.Release();
        }
    }

    private static ConnectionFactory CreateFactory(RabbitMqOptions o)
    {
        var factory = new ConnectionFactory { AutomaticRecoveryEnabled = true };

        if (!string.IsNullOrWhiteSpace(o.Uri))
        {
            factory.Uri = new Uri(o.Uri);
        }
        else
        {
            factory.HostName = o.HostName;
            factory.Port = o.Port;
            factory.UserName = o.UserName;
            factory.Password = o.Password;
            factory.VirtualHost = o.VirtualHost;
        }

        return factory;
    }

    public async ValueTask DisposeAsync()
    {
        if (_connection is not null)
        {
            await _connection.CloseAsync();
            _connection.Dispose();
        }
        _lock.Dispose();
    }
}

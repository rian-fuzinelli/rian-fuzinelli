using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Collections.Concurrent;
using System.Text;

// RpcClient encapsula a lógica de chamada RPC
// Permite fazer chamadas assíncronas com suporte a múltiplas requisições simultâneas
public class FibonacciRpcClient : IDisposable
{
    private readonly IConnection _connection;
    private readonly IModel _channel;
    private readonly string _replyQueueName;
    private readonly EventingBasicConsumer _consumer;

    // Dicionário que mapeia CorrelationId -> TaskCompletionSource
    // Permite aguardar a resposta de forma assíncrona
    private readonly ConcurrentDictionary<string, TaskCompletionSource<string>> _pendingCalls;

    public FibonacciRpcClient()
    {
        _pendingCalls = new ConcurrentDictionary<string, TaskCompletionSource<string>>();

        var factory = new ConnectionFactory
        {
            HostName = "localhost",
            Port = 5672,
            UserName = "guest",
            Password = "guest"
        };

        _connection = factory.CreateConnection();
        _channel = _connection.CreateModel();

        // Usa o Direct Reply-To do RabbitMQ
        // Não cria fila temporária — usa o mecanismo nativo do broker
        _replyQueueName = "amq.rabbitmq.reply-to";

        _consumer = new EventingBasicConsumer(_channel);
        _consumer.Received += OnResposta;

        // Assina a fila de respostas (autoAck obrigatório para amq.rabbitmq.reply-to)
        _channel.BasicConsume(
            queue: _replyQueueName,
            autoAck: true,
            consumer: _consumer
        );
    }

    private void OnResposta(object? model, BasicDeliverEventArgs eventArgs)
    {
        var correlationId = eventArgs.BasicProperties.CorrelationId;

        // Verifica se esta resposta era esperada
        if (_pendingCalls.TryRemove(correlationId, out var tcs))
        {
            var resposta = Encoding.UTF8.GetString(eventArgs.Body.ToArray());
            tcs.SetResult(resposta);  // Desbloqueia o await no cliente
        }
    }

    public Task<string> ChamarAsync(int n)
    {
        // Gera um ID único para esta chamada
        var correlationId = Guid.NewGuid().ToString();

        // TaskCompletionSource permite aguardar a resposta de forma assíncrona
        var tcs = new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);
        _pendingCalls[correlationId] = tcs;

        var propriedades = _channel.CreateBasicProperties();
        propriedades.CorrelationId = correlationId;       // ID para correlacionar a resposta
        propriedades.ReplyTo = _replyQueueName;           // Onde o servidor deve responder

        var body = Encoding.UTF8.GetBytes(n.ToString());

        _channel.BasicPublish(
            exchange: "",
            routingKey: "rpc_queue",   // Fila do servidor RPC
            basicProperties: propriedades,
            body: body
        );

        return tcs.Task;
    }

    public void Dispose()
    {
        _channel.Dispose();
        _connection.Dispose();
    }
}

// Programa principal
Console.WriteLine("[*] Calculando números de Fibonacci via RPC...\n");

using var client = new FibonacciRpcClient();

var numeros = new[] { 5, 10, 20, 30, 35 };

foreach (var n in numeros)
{
    var correlationId = Guid.NewGuid().ToString()[..8];
    var stopwatch = System.Diagnostics.Stopwatch.StartNew();

    Console.WriteLine($"[→] Enviando requisição: Fibonacci({n})");
    var resultado = await client.ChamarAsync(n);

    stopwatch.Stop();
    Console.WriteLine($"[←] Fibonacci({n}) = {resultado}  [{stopwatch.ElapsedMilliseconds}ms]\n");
}

Console.WriteLine("[✓] Todas as chamadas RPC concluídas.");

using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;

// Argumentos: nome_do_consumer + routing_keys
// Ex: dotnet run alertas error
// Ex: dotnet run geral info warning
// Ex: dotnet run todos info warning error debug
if (args.Length < 2)
{
    Console.WriteLine("Uso: dotnet run <nome_consumer> <routing_key1> [routing_key2] ...");
    Console.WriteLine("Exemplos:");
    Console.WriteLine("  dotnet run alertas error");
    Console.WriteLine("  dotnet run geral info warning");
    Console.WriteLine("  dotnet run todos info warning error debug");
    return;
}

var nomeConsumer = args[0];
var routingKeys = args[1..]; // Todas as routing keys a partir do índice 1

var factory = new ConnectionFactory
{
    HostName = "localhost",
    Port = 5672,
    UserName = "guest",
    Password = "guest"
};

using var connection = factory.CreateConnection();
using var channel = connection.CreateModel();

// Declara o exchange (idempotente)
channel.ExchangeDeclare(
    exchange: "direct_logs",
    type: ExchangeType.Direct
);

// Cria uma fila temporária para este consumer
var queueName = channel.QueueDeclare(
    queue: "",
    durable: false,
    exclusive: true,
    autoDelete: true,
    arguments: null
).QueueName;

// Cria um binding para CADA routing key solicitada
// Permite que um consumer receba múltiplos tipos de mensagens
foreach (var routingKey in routingKeys)
{
    channel.QueueBind(
        queue: queueName,
        exchange: "direct_logs",
        routingKey: routingKey
    );
    Console.WriteLine($"[i] Binding criado: direct_logs --[{routingKey}]--> {queueName}");
}

Console.WriteLine($"\n[*] Consumer [{nomeConsumer}] aguardando: {string.Join(", ", routingKeys)}");
Console.WriteLine("[*] CTRL+C para sair.\n");

var consumer = new EventingBasicConsumer(channel);

consumer.Received += (model, eventArgs) =>
{
    var body = eventArgs.Body.ToArray();
    var mensagem = Encoding.UTF8.GetString(body);
    var routingKey = eventArgs.RoutingKey; // Routing key da mensagem recebida

    Console.WriteLine($"[x] [{nomeConsumer}] Recebido ({routingKey}): {mensagem}");
};

channel.BasicConsume(
    queue: queueName,
    autoAck: true,
    consumer: consumer
);

var cts = new CancellationTokenSource();
Console.CancelKeyPress += (_, e) =>
{
    e.Cancel = true;
    cts.Cancel();
};

try
{
    await Task.Delay(Timeout.Infinite, cts.Token);
}
catch (OperationCanceledException)
{
    Console.WriteLine($"\n[i] Consumer [{nomeConsumer}] encerrado.");
}

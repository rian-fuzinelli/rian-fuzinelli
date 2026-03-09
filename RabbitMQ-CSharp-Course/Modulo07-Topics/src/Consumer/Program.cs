using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;

// Argumentos: <nome_consumer> <binding_pattern1> [binding_pattern2] ...
// Exemplos:
//   dotnet run alertas "*.error"
//   dotnet run pagamentos "pagamentos.#"
//   dotnet run auditoria "#"
//   dotnet run auth-monitor "auth.*" "auth.#"
if (args.Length < 2)
{
    Console.WriteLine("Uso: dotnet run <nome> <pattern1> [pattern2] ...");
    Console.WriteLine("\nExemplos:");
    Console.WriteLine("  dotnet run alertas \"*.error\"");
    Console.WriteLine("  dotnet run pagamentos \"pagamentos.#\"");
    Console.WriteLine("  dotnet run auditoria \"#\"");
    return;
}

var nomeConsumer = args[0];
var patterns = args[1..];

var factory = new ConnectionFactory
{
    HostName = "localhost",
    Port = 5672,
    UserName = "guest",
    Password = "guest"
};

using var connection = factory.CreateConnection();
using var channel = connection.CreateModel();

channel.ExchangeDeclare(
    exchange: "topic_logs",
    type: ExchangeType.Topic
);

// Fila temporária para este consumer
var queueName = channel.QueueDeclare(
    queue: "",
    durable: false,
    exclusive: true,
    autoDelete: true,
    arguments: null
).QueueName;

// Cria bindings para cada padrão especificado
foreach (var pattern in patterns)
{
    channel.QueueBind(
        queue: queueName,
        exchange: "topic_logs",
        routingKey: pattern   // Ex: "*.error", "pagamentos.#", "#"
    );
    Console.WriteLine($"[i] Binding: topic_logs --[{pattern}]--> {queueName}");
}

Console.WriteLine($"\n[*] Consumer [{nomeConsumer}] aguardando padrões: {string.Join(", ", patterns)}");
Console.WriteLine("[*] CTRL+C para sair.\n");

var consumer = new EventingBasicConsumer(channel);

consumer.Received += (model, eventArgs) =>
{
    var body = eventArgs.Body.ToArray();
    var mensagem = Encoding.UTF8.GetString(body);
    var routingKey = eventArgs.RoutingKey;

    // Destaca qual padrão fez o match
    var patternMatch = patterns.FirstOrDefault(p => MatchesPattern(routingKey, p)) ?? "?";

    Console.WriteLine($"[x] [{nomeConsumer}] (key: {routingKey} | pattern: {patternMatch})");
    Console.WriteLine($"    Mensagem: {mensagem}");
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

// Verifica se uma routing key corresponde a um pattern de Topic Exchange
// * = exatamente uma palavra, # = zero ou mais palavras
static bool MatchesPattern(string routingKey, string pattern)
{
    var keyParts = routingKey.Split('.');
    var patternParts = pattern.Split('.');
    return MatchParts(keyParts, 0, patternParts, 0);
}

static bool MatchParts(string[] key, int ki, string[] pattern, int pi)
{
    if (pi == pattern.Length && ki == key.Length) return true;
    if (pi == pattern.Length) return false;

    if (pattern[pi] == "#")
    {
        // # pode corresponder a 0 ou mais palavras
        for (int i = ki; i <= key.Length; i++)
        {
            if (MatchParts(key, i, pattern, pi + 1)) return true;
        }
        return false;
    }

    if (ki == key.Length) return false;

    if (pattern[pi] == "*" || pattern[pi] == key[ki])
    {
        return MatchParts(key, ki + 1, pattern, pi + 1);
    }

    return false;
}

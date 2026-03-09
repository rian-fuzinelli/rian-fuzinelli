using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;

// Tipo de subscriber: "console" ou "file"
var tipo = args.Length > 0 ? args[0] : "console";
var logFile = $"logs_{tipo}_{DateTime.Now:yyyyMMdd_HHmmss}.txt";

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
    exchange: "logs",
    type: ExchangeType.Fanout
);

// Cria uma fila temporária e exclusiva para este subscriber
// - Nome gerado pelo broker (ex: amq.gen-XYZ...)
// - Exclusiva: pertence a esta conexão apenas
// - Auto-deletável: removida quando a conexão fechar
var queueName = channel.QueueDeclare(
    queue: "",          // Nome vazio = broker gera um nome único
    durable: false,     // Não persiste após restart
    exclusive: true,    // Exclusiva desta conexão
    autoDelete: true,   // Deletada quando a conexão fecha
    arguments: null
).QueueName;

Console.WriteLine($"[i] Fila temporária criada: {queueName}");

// Binding: conecta a fila ao exchange
// No Fanout, routingKey é ignorada mas é obrigatório passar ""
channel.QueueBind(
    queue: queueName,
    exchange: "logs",
    routingKey: ""   // Ignorado no Fanout
);

Console.WriteLine($"[*] Subscriber [{tipo}] aguardando logs. CTRL+C para sair.\n");

var consumer = new EventingBasicConsumer(channel);

consumer.Received += (model, eventArgs) =>
{
    var body = eventArgs.Body.ToArray();
    var mensagem = Encoding.UTF8.GetString(body);

    if (tipo == "file")
    {
        // Subscriber "file": salva em arquivo
        File.AppendAllText(logFile, mensagem + Environment.NewLine);
        Console.WriteLine($"[x] Log salvo em arquivo: {mensagem}");
    }
    else
    {
        // Subscriber "console": exibe no terminal
        Console.WriteLine($"[x] Log recebido: {mensagem}");
    }
};

// autoAck: true — confirmação automática (adequado para logs não-críticos)
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
    if (tipo == "file")
        Console.WriteLine($"\n[i] Logs salvos em: {logFile}");
    Console.WriteLine($"[i] Subscriber [{tipo}] encerrado.");
}

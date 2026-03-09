using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;

var factory = new ConnectionFactory
{
    HostName = "localhost",
    Port = 5672,
    UserName = "guest",
    Password = "guest",
    AutomaticRecoveryEnabled = true,
    NetworkRecoveryInterval = TimeSpan.FromSeconds(10),
    RequestedHeartbeat = TimeSpan.FromSeconds(60)
};

using var connection = factory.CreateConnection();

connection.ConnectionShutdown += (_, args) =>
    Console.WriteLine($"\n[!] Conexão encerrada: {args.ReplyText}");
connection.RecoverySucceeded += (_, _) =>
    Console.WriteLine("[✓] Conexão recuperada!");

using var channel = connection.CreateModel();

Console.WriteLine("═══════════════════════════════════════════════════════");
Console.WriteLine("  Consumer Avançado: Priority Queue + Headers Exchange");
Console.WriteLine("═══════════════════════════════════════════════════════\n");

// ══════════════════════════════════════════════════════════════
// CONSUMER DA PRIORITY QUEUE
// ══════════════════════════════════════════════════════════════

var priorityQueueArgs = new Dictionary<string, object>
{
    ["x-max-priority"] = 10
};

channel.QueueDeclare(
    queue: "priority_queue",
    durable: true,
    exclusive: false,
    autoDelete: false,
    arguments: priorityQueueArgs
);

// Prefetch baixo para garantir que mensagens de alta prioridade sejam processadas primeiro
channel.BasicQos(prefetchSize: 0, prefetchCount: 3, global: false);

var priorityConsumer = new EventingBasicConsumer(channel);
priorityConsumer.Received += (model, eventArgs) =>
{
    var body = eventArgs.Body.ToArray();
    var mensagem = Encoding.UTF8.GetString(body);
    var prioridade = eventArgs.BasicProperties.Priority;

    var prioLabel = prioridade >= 7 ? "🔴 HIGH" : prioridade >= 4 ? "🟡 MED " : "🟢 LOW ";
    Console.WriteLine($"[PRIORITY] {prioLabel} (p={prioridade}): {mensagem}");

    // Simula processamento mais rápido para alta prioridade
    var delay = prioridade >= 7 ? 100 : prioridade >= 4 ? 300 : 500;
    Thread.Sleep(delay);

    channel.BasicAck(deliveryTag: eventArgs.DeliveryTag, multiple: false);
};

channel.BasicConsume(queue: "priority_queue", autoAck: false, consumer: priorityConsumer);

// ══════════════════════════════════════════════════════════════
// CONSUMERS DO HEADERS EXCHANGE
// ══════════════════════════════════════════════════════════════

using var headersConnection = factory.CreateConnection();
using var headersChannel = headersConnection.CreateModel();

// Declara exchanges e filas (idempotente)
headersChannel.ExchangeDeclare("headers_exchange", ExchangeType.Headers, durable: false);
headersChannel.QueueDeclare("pdf_reports", durable: false, exclusive: false, autoDelete: false);
headersChannel.QueueDeclare("zip_images", durable: false, exclusive: false, autoDelete: false);

headersChannel.QueueBind("pdf_reports", "headers_exchange", "",
    arguments: new Dictionary<string, object>
    {
        ["x-match"] = "all", ["format"] = "pdf", ["type"] = "report"
    });

headersChannel.QueueBind("zip_images", "headers_exchange", "",
    arguments: new Dictionary<string, object>
    {
        ["x-match"] = "any", ["format"] = "zip", ["type"] = "image"
    });

// Consumer da fila de PDFs (match: all — format=pdf AND type=report)
var pdfConsumer = new EventingBasicConsumer(headersChannel);
pdfConsumer.Received += (model, eventArgs) =>
{
    var mensagem = Encoding.UTF8.GetString(eventArgs.Body.ToArray());
    Console.WriteLine($"[PDF-REPORTS] 📄 Recebido: {mensagem}");
    headersChannel.BasicAck(eventArgs.DeliveryTag, false);
};

// Consumer da fila de imagens/ZIP (match: any — format=zip OR type=image)
var zipConsumer = new EventingBasicConsumer(headersChannel);
zipConsumer.Received += (model, eventArgs) =>
{
    var mensagem = Encoding.UTF8.GetString(eventArgs.Body.ToArray());
    Console.WriteLine($"[ZIP-IMAGES]  🗜 Recebido: {mensagem}");
    headersChannel.BasicAck(eventArgs.DeliveryTag, false);
};

headersChannel.BasicConsume(queue: "pdf_reports", autoAck: false, consumer: pdfConsumer);
headersChannel.BasicConsume(queue: "zip_images", autoAck: false, consumer: zipConsumer);

Console.WriteLine("[*] Aguardando mensagens. CTRL+C para sair.\n");

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
    Console.WriteLine("\n[i] Consumer encerrado.");
}

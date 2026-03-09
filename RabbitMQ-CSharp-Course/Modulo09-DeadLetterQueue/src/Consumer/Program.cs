using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;
using System.Text.Json;

var factory = new ConnectionFactory
{
    HostName = "localhost",
    Port = 5672,
    UserName = "guest",
    Password = "guest"
};

using var connection = factory.CreateConnection();
using var channel = connection.CreateModel();

// ══════════════════════════════════════════════════════════════
// CONSUMER DA FILA PRINCIPAL
// ══════════════════════════════════════════════════════════════

// Declara as filas (idempotente)
channel.ExchangeDeclare("dead_letter_exchange", ExchangeType.Direct, durable: true);
channel.QueueDeclare("dead_letter_queue", durable: true, exclusive: false, autoDelete: false);
channel.QueueBind("dead_letter_queue", "dead_letter_exchange", "dead");

var mainQueueArgs = new Dictionary<string, object>
{
    ["x-dead-letter-exchange"] = "dead_letter_exchange",
    ["x-dead-letter-routing-key"] = "dead",
    ["x-message-ttl"] = 15000,
    ["x-max-length"] = 50
};
channel.QueueDeclare("main_queue", durable: true, exclusive: false, autoDelete: false,
    arguments: mainQueueArgs);

channel.BasicQos(prefetchSize: 0, prefetchCount: 1, global: false);

Console.WriteLine("═══════════════════════════════════════════════════");
Console.WriteLine("  Consumer Principal + DLQ Monitor");
Console.WriteLine("═══════════════════════════════════════════════════");
Console.WriteLine("[*] Aguardando mensagens. CTRL+C para sair.\n");

// Consumer da fila principal
var mainConsumer = new EventingBasicConsumer(channel);
mainConsumer.Received += (model, eventArgs) =>
{
    var body = eventArgs.Body.ToArray();
    var mensagem = Encoding.UTF8.GetString(body);
    var messageId = eventArgs.BasicProperties.MessageId ?? "?";

    Console.WriteLine($"[MAIN] Processando pedido [{messageId}]: {mensagem}");

    try
    {
        // Simula falha para pedidos com "erro" na descrição
        if (mensagem.Contains("erro"))
        {
            Console.WriteLine($"[MAIN] ✗ Falha ao processar pedido [{messageId}]");
            Console.WriteLine($"[MAIN] → Enviando para Dead Letter Queue (NACK sem requeue)");

            // NACK com requeue: false → mensagem vai para a DLQ
            channel.BasicNack(
                deliveryTag: eventArgs.DeliveryTag,
                multiple: false,
                requeue: false    // false = não coloca de volta na fila → vai para DLQ
            );
        }
        else
        {
            // Simula processamento bem-sucedido
            Thread.Sleep(500);
            Console.WriteLine($"[MAIN] ✓ Pedido [{messageId}] processado com sucesso!\n");

            channel.BasicAck(
                deliveryTag: eventArgs.DeliveryTag,
                multiple: false
            );
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"[MAIN] ✗ Exceção ao processar [{messageId}]: {ex.Message}");
        channel.BasicNack(eventArgs.DeliveryTag, false, requeue: false);
    }
};

channel.BasicConsume(queue: "main_queue", autoAck: false, consumer: mainConsumer);

// ══════════════════════════════════════════════════════════════
// CONSUMER DA DEAD LETTER QUEUE (Monitoramento)
// ══════════════════════════════════════════════════════════════

// Canal separado para a DLQ
using var dlqConnection = factory.CreateConnection();
using var dlqChannel = dlqConnection.CreateModel();

var dlqConsumer = new EventingBasicConsumer(dlqChannel);
dlqConsumer.Received += (model, eventArgs) =>
{
    var body = eventArgs.Body.ToArray();
    var mensagem = Encoding.UTF8.GetString(body);
    var messageId = eventArgs.BasicProperties.MessageId ?? "?";

    // Headers adicionados pelo RabbitMQ com informações sobre o dead lettering
    var headers = eventArgs.BasicProperties.Headers;
    var deathReason = "unknown";

    if (headers != null && headers.TryGetValue("x-death", out var deathInfo))
    {
        var deaths = deathInfo as List<object>;
        var firstDeath = deaths?.FirstOrDefault() as Dictionary<string, object>;
        if (firstDeath != null && firstDeath.TryGetValue("reason", out var reason))
        {
            deathReason = Encoding.UTF8.GetString((byte[])reason);
        }
    }

    Console.WriteLine($"\n[DLQ] ⚠ Mensagem morta recebida!");
    Console.WriteLine($"[DLQ]   ID: {messageId}");
    Console.WriteLine($"[DLQ]   Razão: {deathReason}");
    Console.WriteLine($"[DLQ]   Conteúdo: {mensagem}");
    Console.WriteLine($"[DLQ]   → Mensagem registrada para análise\n");

    // Na DLQ, pode-se:
    // 1. Registrar em banco de dados para auditoria
    // 2. Enviar alerta para o time de operações
    // 3. Reprocessar manualmente após investigação

    dlqChannel.BasicAck(deliveryTag: eventArgs.DeliveryTag, multiple: false);
};

dlqChannel.BasicConsume(queue: "dead_letter_queue", autoAck: false, consumer: dlqConsumer);

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

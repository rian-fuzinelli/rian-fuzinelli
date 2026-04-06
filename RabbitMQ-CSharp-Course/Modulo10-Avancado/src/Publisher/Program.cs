using RabbitMQ.Client;
using System.Collections.Concurrent;
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

// Monitora eventos de reconexão
connection.ConnectionShutdown += (_, args) =>
    Console.WriteLine($"[!] Conexão encerrada: {args.ReplyText}");
connection.RecoverySucceeded += (_, _) =>
    Console.WriteLine("[✓] Conexão recuperada!");

using var channel = connection.CreateModel();

// ══════════════════════════════════════════════════════════════
// PUBLISHER CONFIRMS
// ══════════════════════════════════════════════════════════════

// Ativa o modo Publisher Confirms no canal
channel.ConfirmSelect();

// Rastreia mensagens pendentes de confirmação: SeqNo → Mensagem
var pendingConfirms = new ConcurrentDictionary<ulong, string>();
var confirmedCount = 0;
var nackCount = 0;

// Handler de ACK: broker confirmou o recebimento
channel.BasicAcks += (sender, args) =>
{
    if (args.Multiple)
    {
        // Confirma TODAS as mensagens com SeqNo <= args.DeliveryTag
        var confirmed = pendingConfirms.Keys.Where(k => k <= args.DeliveryTag).ToList();
        foreach (var key in confirmed)
        {
            if (pendingConfirms.TryRemove(key, out var msg))
            {
                Interlocked.Increment(ref confirmedCount);
                Console.WriteLine($"[✓] Confirmado (multiple) SeqNo={key}: {msg}");
            }
        }
    }
    else
    {
        if (pendingConfirms.TryRemove(args.DeliveryTag, out var msg))
        {
            Interlocked.Increment(ref confirmedCount);
            Console.WriteLine($"[✓] Confirmado SeqNo={args.DeliveryTag}: {msg}");
        }
    }
};

// Handler de NACK: broker rejeitou a mensagem
channel.BasicNacks += (sender, args) =>
{
    if (pendingConfirms.TryRemove(args.DeliveryTag, out var msg))
    {
        Interlocked.Increment(ref nackCount);
        Console.WriteLine($"[✗] NACK SeqNo={args.DeliveryTag}: {msg} — reenviar!");
        // Em produção: adicionar lógica de retry aqui
    }
};

// ══════════════════════════════════════════════════════════════
// PRIORITY QUEUE
// ══════════════════════════════════════════════════════════════

var priorityQueueArgs = new Dictionary<string, object>
{
    ["x-max-priority"] = 10  // Suporte a prioridades de 1 a 10
};

channel.QueueDeclare(
    queue: "priority_queue",
    durable: true,
    exclusive: false,
    autoDelete: false,
    arguments: priorityQueueArgs
);

// ══════════════════════════════════════════════════════════════
// HEADERS EXCHANGE
// ══════════════════════════════════════════════════════════════

channel.ExchangeDeclare(
    exchange: "headers_exchange",
    type: ExchangeType.Headers,
    durable: false,
    autoDelete: false
);

// Fila para relatórios PDF
channel.QueueDeclare("pdf_reports", durable: false, exclusive: false, autoDelete: false);
channel.QueueBind("pdf_reports", "headers_exchange", routingKey: "",
    arguments: new Dictionary<string, object>
    {
        ["x-match"] = "all",   // TODOS os headers devem corresponder
        ["format"] = "pdf",
        ["type"] = "report"
    });

// Fila para imagens ZIP
channel.QueueDeclare("zip_images", durable: false, exclusive: false, autoDelete: false);
channel.QueueBind("zip_images", "headers_exchange", routingKey: "",
    arguments: new Dictionary<string, object>
    {
        ["x-match"] = "any",   // QUALQUER header deve corresponder
        ["format"] = "zip",
        ["type"] = "image"
    });

Console.WriteLine("═══════════════════════════════════════════════════════");
Console.WriteLine("  Publisher Avançado: Confirms + Priority + Headers");
Console.WriteLine("═══════════════════════════════════════════════════════\n");

// ══════════════════════════════════════════════════════════════
// PUBLICANDO COM PUBLISHER CONFIRMS + PRIORIDADES
// ══════════════════════════════════════════════════════════════

Console.WriteLine("[*] Publicando mensagens com Publisher Confirms e Prioridades...\n");

var pedidos = new[]
{
    (priority: (byte)1, msg: "Pedido LOW:  relatório mensal agendado"),
    (priority: (byte)5, msg: "Pedido MED:  processamento de dados do cliente"),
    (priority: (byte)9, msg: "Pedido HIGH: pagamento urgente - pedido #999"),
    (priority: (byte)3, msg: "Pedido LOW:  limpeza de cache"),
    (priority: (byte)8, msg: "Pedido HIGH: alerta de segurança detectado"),
    (priority: (byte)2, msg: "Pedido LOW:  envio de newsletter"),
    (priority: (byte)7, msg: "Pedido HIGH: estoque crítico - produto #123")
};

foreach (var (priority, msg) in pedidos)
{
    var seqNo = channel.NextPublishSeqNo;
    pendingConfirms[seqNo] = msg;

    var properties = channel.CreateBasicProperties();
    properties.Persistent = true;
    properties.Priority = priority;

    channel.BasicPublish(
        exchange: "",
        routingKey: "priority_queue",
        basicProperties: properties,
        body: Encoding.UTF8.GetBytes(msg)
    );

    Console.WriteLine($"[→] SeqNo={seqNo} Prioridade={priority}: {msg}");
    Thread.Sleep(200);
}

// Aguarda todas as confirmações (timeout de 5 segundos)
Console.WriteLine("\n[*] Aguardando confirmações do broker...");
channel.WaitForConfirmsOrDie(TimeSpan.FromSeconds(5));

Console.WriteLine($"\n[✓] Publicações concluídas: {confirmedCount} confirmadas, {nackCount} rejeitadas");

// ══════════════════════════════════════════════════════════════
// PUBLICANDO COM HEADERS EXCHANGE
// ══════════════════════════════════════════════════════════════

Console.WriteLine("\n[*] Publicando com Headers Exchange...\n");

var documentos = new[]
{
    (headers: new Dictionary<string, object> { ["format"] = "pdf", ["type"] = "report" },
     msg: "Relatório Financeiro Q4 2024.pdf"),
    (headers: new Dictionary<string, object> { ["format"] = "xlsx", ["type"] = "report" },
     msg: "Dados de Vendas Janeiro 2025.xlsx"),
    (headers: new Dictionary<string, object> { ["format"] = "jpg", ["type"] = "image" },
     msg: "Foto de perfil usuario_123.jpg"),
    (headers: new Dictionary<string, object> { ["format"] = "zip", ["type"] = "backup" },
     msg: "Backup dados 2025-01-15.zip"),
    (headers: new Dictionary<string, object> { ["format"] = "pdf", ["type"] = "invoice" },
     msg: "Nota Fiscal #5678.pdf"),
};

foreach (var (hdrs, msg) in documentos)
{
    var properties = channel.CreateBasicProperties();
    properties.Headers = hdrs;

    channel.BasicPublish(
        exchange: "headers_exchange",
        routingKey: "",     // Ignorado no Headers Exchange
        basicProperties: properties,
        body: Encoding.UTF8.GetBytes(msg)
    );

    var headersStr = string.Join(", ", hdrs.Select(h => $"{h.Key}={h.Value}"));
    Console.WriteLine($"[→] Headers({headersStr}): {msg}");
    Thread.Sleep(200);
}

Console.WriteLine("\n[✓] Todos os documentos publicados via Headers Exchange.");

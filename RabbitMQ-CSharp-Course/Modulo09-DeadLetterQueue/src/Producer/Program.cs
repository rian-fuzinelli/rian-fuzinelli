using RabbitMQ.Client;
using System.Text;

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
// CONFIGURAÇÃO DA DEAD LETTER QUEUE
// ══════════════════════════════════════════════════════════════

// 1. Declara o Dead Letter Exchange
channel.ExchangeDeclare(
    exchange: "dead_letter_exchange",
    type: ExchangeType.Direct,
    durable: true,
    autoDelete: false,
    arguments: null
);

// 2. Declara a Dead Letter Queue (onde mensagens mortas chegam)
channel.QueueDeclare(
    queue: "dead_letter_queue",
    durable: true,
    exclusive: false,
    autoDelete: false,
    arguments: null
);

// 3. Binding: DLX → DLQ
channel.QueueBind(
    queue: "dead_letter_queue",
    exchange: "dead_letter_exchange",
    routingKey: "dead"
);

// ══════════════════════════════════════════════════════════════
// CONFIGURAÇÃO DA FILA PRINCIPAL COM DLQ
// ══════════════════════════════════════════════════════════════

var mainQueueArgs = new Dictionary<string, object>
{
    // Configuração da DLQ: mensagens mortas vão para este exchange
    ["x-dead-letter-exchange"] = "dead_letter_exchange",

    // Routing key usada ao enviar para o DLX
    ["x-dead-letter-routing-key"] = "dead",

    // TTL: mensagens expiram após 15 segundos se não consumidas
    ["x-message-ttl"] = 15000,

    // Limite de mensagens na fila (mensagens excedentes são dead lettered)
    ["x-max-length"] = 50
};

channel.QueueDeclare(
    queue: "main_queue",
    durable: true,
    exclusive: false,
    autoDelete: false,
    arguments: mainQueueArgs
);

Console.WriteLine("[✓] Filas configuradas:");
Console.WriteLine("    main_queue       → fila principal (DLQ ativo, TTL=15s, max=50)");
Console.WriteLine("    dead_letter_queue → dead letter queue");
Console.WriteLine();

// ══════════════════════════════════════════════════════════════
// PUBLICANDO MENSAGENS
// ══════════════════════════════════════════════════════════════

var pedidos = new[]
{
    ("P001", "Pedido normal — será processado com sucesso"),
    ("P002", "Pedido com erro — será rejeitado pelo consumer"),
    ("P003", "Pedido normal — será processado com sucesso"),
    ("P004", "Pedido com erro — será rejeitado pelo consumer"),
    ("P005", "Pedido com TTL curto — expirará em 3 segundos"),
    ("P006", "Pedido normal — será processado com sucesso"),
    ("P007", "Pedido com erro — será rejeitado pelo consumer"),
};

Console.WriteLine("[*] Publicando pedidos...\n");

foreach (var (id, descricao) in pedidos)
{
    var mensagem = $"{{\"id\":\"{id}\",\"descricao\":\"{descricao}\"}}";
    var body = Encoding.UTF8.GetBytes(mensagem);

    var properties = channel.CreateBasicProperties();
    properties.Persistent = true;
    properties.MessageId = id;

    // Para P005, define TTL individual curto (3 segundos)
    if (id == "P005")
    {
        properties.Expiration = "3000";  // 3 segundos (como string, em ms)
        Console.WriteLine($"[x] Publicando [{id}] com TTL=3s: {descricao}");
    }
    else
    {
        Console.WriteLine($"[x] Publicando [{id}]: {descricao}");
    }

    channel.BasicPublish(
        exchange: "",
        routingKey: "main_queue",
        basicProperties: properties,
        body: body
    );

    Thread.Sleep(300);
}

Console.WriteLine("\n[✓] Pedidos publicados.");
Console.WriteLine("[i] Inicie o Consumer para ver processamento e dead lettering.");

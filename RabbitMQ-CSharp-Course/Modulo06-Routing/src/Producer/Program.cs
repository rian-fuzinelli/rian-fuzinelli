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

// Declara o exchange do tipo Direct
// Direct: roteia para filas cujo binding key == routing key da mensagem
channel.ExchangeDeclare(
    exchange: "direct_logs",
    type: ExchangeType.Direct,
    durable: false,
    autoDelete: false,
    arguments: null
);

// Logs de diferentes severidades para simular um sistema real
var logs = new[]
{
    ("info",    "[INFO]    Usuário 'joao@email.com' realizou login"),
    ("info",    "[INFO]    Pedido #1001 criado com sucesso"),
    ("warning", "[WARNING] Tempo de resposta da API acima de 500ms"),
    ("error",   "[ERROR]   Falha ao conectar ao banco de dados PostgreSQL"),
    ("info",    "[INFO]    Cache atualizado com sucesso"),
    ("warning", "[WARNING] Uso de CPU acima de 80%"),
    ("error",   "[ERROR]   Pagamento rejeitado para pedido #1002"),
    ("debug",   "[DEBUG]   Query executada em 12ms: SELECT * FROM users"),
    ("warning", "[WARNING] Tentativa de login inválida para 'admin'"),
    ("error",   "[ERROR]   Serviço de email indisponível")
};

Console.WriteLine("[*] Publicando logs com Direct Exchange...\n");

foreach (var (nivel, mensagem) in logs)
{
    var body = Encoding.UTF8.GetBytes(mensagem);

    // Cada mensagem é roteada para filas com binding key igual ao nível
    channel.BasicPublish(
        exchange: "direct_logs",
        routingKey: nivel,   // "info", "warning", "error" ou "debug"
        basicProperties: null,
        body: body
    );

    Console.WriteLine($"[x] Publicado [{nivel}]: {mensagem}");
    Thread.Sleep(300);
}

Console.WriteLine("\n[✓] Todos os logs publicados.");
Console.WriteLine("[i] Consumers com bindings correspondentes receberam as mensagens.");

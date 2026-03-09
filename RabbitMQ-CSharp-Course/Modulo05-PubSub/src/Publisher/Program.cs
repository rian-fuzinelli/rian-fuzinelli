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

// Declara o exchange do tipo Fanout
// Fanout: envia para TODAS as filas vinculadas, ignora routing key
channel.ExchangeDeclare(
    exchange: "logs",
    type: ExchangeType.Fanout,
    durable: false,    // Exchange não persiste após restart
    autoDelete: false,
    arguments: null
);

var niveis = new[] { "INFO", "WARNING", "ERROR", "DEBUG" };
var mensagens = new[]
{
    "Usuário fez login com sucesso",
    "Tentativa de acesso com credenciais inválidas",
    "Falha ao conectar ao banco de dados",
    "Cache expirado, recarregando dados",
    "Novo pedido criado: #12345",
    "Pagamento processado com sucesso",
    "Estoque abaixo do mínimo para produto #789"
};

var random = new Random();
var total = 7;

Console.WriteLine("[*] Publicando logs no exchange 'logs' (Fanout)...\n");

for (int i = 0; i < total; i++)
{
    var nivel = niveis[random.Next(niveis.Length)];
    var mensagem = mensagens[i % mensagens.Length];
    var log = $"[{nivel}] {DateTime.Now:HH:mm:ss} - {mensagem}";
    var body = Encoding.UTF8.GetBytes(log);

    // No Fanout, routingKey é ignorada — a mensagem vai para TODAS as filas vinculadas
    channel.BasicPublish(
        exchange: "logs",
        routingKey: "",   // Ignorado no Fanout
        basicProperties: null,
        body: body
    );

    Console.WriteLine($"[x] Publicado: {log}");
    Thread.Sleep(500);
}

Console.WriteLine($"\n[✓] {total} logs publicados.");
Console.WriteLine("[i] Todos os Subscribers vinculados ao exchange 'logs' receberam cada mensagem.");

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

// Declara exchange do tipo Topic
channel.ExchangeDeclare(
    exchange: "topic_logs",
    type: ExchangeType.Topic,
    durable: false,
    autoDelete: false,
    arguments: null
);

// Eventos com routing keys hierárquicas no formato: <servico>.<entidade>.<acao> ou <servico>.<nivel>
var eventos = new[]
{
    // (routing_key, mensagem)
    ("pagamentos.order.created",   "Pedido #1001 criado - R$ 259,90"),
    ("pagamentos.order.error",     "Falha ao processar pagamento do pedido #1002"),
    ("auth.user.login",            "Usuário 'joao@email.com' logou com sucesso"),
    ("auth.user.error",            "Tentativa de login inválida para 'admin'"),
    ("estoque.produto.atualizado", "Estoque do produto #789 atualizado: 50 unidades"),
    ("estoque.produto.error",      "Produto #790 sem estoque disponível"),
    ("pagamentos.pix.confirmado",  "PIX confirmado - Pedido #1003 - R$ 89,90"),
    ("auth.token.expirado",        "Token JWT expirado para usuário #456"),
    ("email.notificacao.enviada",  "Email de confirmação enviado para 'maria@email.com'"),
    ("email.notificacao.error",    "Falha ao enviar email: servidor SMTP indisponível")
};

Console.WriteLine("[*] Publicando eventos com Topic Exchange...\n");

foreach (var (routingKey, mensagem) in eventos)
{
    var body = Encoding.UTF8.GetBytes(mensagem);

    channel.BasicPublish(
        exchange: "topic_logs",
        routingKey: routingKey,  // Ex: "pagamentos.order.error"
        basicProperties: null,
        body: body
    );

    Console.WriteLine($"[x] [{routingKey}]: {mensagem}");
    Thread.Sleep(400);
}

Console.WriteLine("\n[✓] Todos os eventos publicados.");
Console.WriteLine("\n[i] Padrões de binding ativos:");
Console.WriteLine("  *.error      → recebe: pagamentos.order.error, auth.user.error, etc.");
Console.WriteLine("  pagamentos.# → recebe: pagamentos.order.created, pagamentos.pix.confirmado, etc.");
Console.WriteLine("  #            → recebe: todos os eventos");

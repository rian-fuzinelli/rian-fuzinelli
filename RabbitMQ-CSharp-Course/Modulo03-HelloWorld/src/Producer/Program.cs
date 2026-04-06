using RabbitMQ.Client;
using System.Text;

// Configuração da fábrica de conexão
var factory = new ConnectionFactory
{
    HostName = "localhost",
    Port = 5672,
    UserName = "guest",
    Password = "guest"
};

// Criação da conexão e do canal
// IConnection: representa a conexão TCP (pesada, 1 por aplicação)
// IModel: representa um canal virtual (leve, 1 por thread)
using var connection = factory.CreateConnection();
using var channel = connection.CreateModel();

// Declara a fila (idempotente — cria se não existir)
channel.QueueDeclare(
    queue: "hello",
    durable: false,     // Não persiste após reinicialização do broker
    exclusive: false,   // Pode ser usada por múltiplas conexões
    autoDelete: false,  // Não deleta quando não há consumers
    arguments: null
);

const int totalMensagens = 5;

for (int i = 1; i <= totalMensagens; i++)
{
    var mensagem = $"Hello World! (#{i})";
    var body = Encoding.UTF8.GetBytes(mensagem);

    // Publica a mensagem no Default Exchange
    // No Default Exchange, a routingKey é o nome da fila de destino
    channel.BasicPublish(
        exchange: "",           // "" = Default Exchange
        routingKey: "hello",    // Nome da fila
        basicProperties: null,  // Sem propriedades adicionais
        body: body
    );

    Console.WriteLine($"[x] Enviando mensagem: {mensagem}");
    Thread.Sleep(500); // Pequena pausa para demonstração
}

Console.WriteLine($"\n[✓] {totalMensagens} mensagens enviadas com sucesso.");

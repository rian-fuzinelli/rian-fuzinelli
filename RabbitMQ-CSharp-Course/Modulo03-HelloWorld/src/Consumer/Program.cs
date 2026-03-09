using RabbitMQ.Client;
using RabbitMQ.Client.Events;
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

// Declara a mesma fila que o producer usa
// É uma boa prática declarar no consumer também (idempotente)
// Garante que a fila existe mesmo que o consumer inicie antes do producer
channel.QueueDeclare(
    queue: "hello",
    durable: false,
    exclusive: false,
    autoDelete: false,
    arguments: null
);

Console.WriteLine("[*] Aguardando mensagens. Pressione CTRL+C para sair.\n");

// EventingBasicConsumer: consumer baseado em eventos
// O callback é chamado quando uma mensagem chega
var consumer = new EventingBasicConsumer(channel);

consumer.Received += (model, eventArgs) =>
{
    // Corpo da mensagem como array de bytes
    var body = eventArgs.Body.ToArray();

    // Converte os bytes para string
    var mensagem = Encoding.UTF8.GetString(body);

    Console.WriteLine($"[x] Recebido: {mensagem}");

    // Com autoAck: true, o RabbitMQ remove a mensagem
    // automaticamente ao entregá-la ao consumer
};

// Registra o consumer na fila
channel.BasicConsume(
    queue: "hello",
    autoAck: true,   // Confirmação automática — mensagem removida ao ser entregue
    consumer: consumer
);

// Mantém o processo em execução aguardando mensagens
Console.ReadLine();

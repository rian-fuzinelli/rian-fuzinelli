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

// Fila durável: sobrevive ao restart do broker
channel.QueueDeclare(
    queue: "task_queue",
    durable: true,      // Persiste após reinicialização
    exclusive: false,
    autoDelete: false,
    arguments: null
);

// Propriedades da mensagem com persistência em disco
var properties = channel.CreateBasicProperties();
properties.Persistent = true;  // Garante que a mensagem seja salva em disco

var random = new Random();
var totalTarefas = 10;

Console.WriteLine($"[*] Publicando {totalTarefas} tarefas na fila task_queue...\n");

for (int i = 1; i <= totalTarefas; i++)
{
    // Simula tarefas com diferentes tempos de processamento (1 a 5 segundos)
    var tempoProcessamento = random.Next(1, 6);
    var mensagem = $"Tarefa #{i} (processamento: {tempoProcessamento}s)";
    var body = Encoding.UTF8.GetBytes(mensagem);

    channel.BasicPublish(
        exchange: "",
        routingKey: "task_queue",
        basicProperties: properties,   // Mensagem persistente
        body: body
    );

    Console.WriteLine($"[x] Enviado: {mensagem}");
    Thread.Sleep(200);
}

Console.WriteLine($"\n[✓] {totalTarefas} tarefas publicadas com sucesso.");
Console.WriteLine("[i] Inicie múltiplos Workers para ver a distribuição.");

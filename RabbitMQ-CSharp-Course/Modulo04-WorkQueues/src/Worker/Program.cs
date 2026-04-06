using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;
using System.Text.RegularExpressions;

// Identificador do worker (passado como argumento ou gerado automaticamente)
var workerId = args.Length > 0 ? args[0] : $"worker-{Guid.NewGuid().ToString()[..8]}";

var factory = new ConnectionFactory
{
    HostName = "localhost",
    Port = 5672,
    UserName = "guest",
    Password = "guest"
};

using var connection = factory.CreateConnection();
using var channel = connection.CreateModel();

// Declara a mesma fila durável
channel.QueueDeclare(
    queue: "task_queue",
    durable: true,
    exclusive: false,
    autoDelete: false,
    arguments: null
);

// Fair Dispatch: só recebe 1 mensagem por vez
// O worker só recebe nova tarefa após confirmar a anterior (BasicAck)
// Isso evita sobrecarregar um worker com tarefas enquanto outro está ocioso
channel.BasicQos(
    prefetchSize: 0,    // Sem limite de tamanho (0 = sem limite)
    prefetchCount: 1,   // Máximo 1 mensagem não-confirmada por vez
    global: false       // false = por consumer | true = por canal
);

Console.WriteLine($"[*] Worker [{workerId}] aguardando tarefas. CTRL+C para sair.\n");

var consumer = new EventingBasicConsumer(channel);

consumer.Received += (model, eventArgs) =>
{
    var body = eventArgs.Body.ToArray();
    var mensagem = Encoding.UTF8.GetString(body);

    Console.WriteLine($"[x] [{workerId}] Recebido: {mensagem}");

    // Simula processamento baseado no tempo indicado na mensagem
    // Ex: "Tarefa #1 (processamento: 3s)" => dorme 3 segundos
    var match = Regex.Match(mensagem, @"processamento: (\d+)s");
    var segundos = match.Success ? int.Parse(match.Groups[1].Value) : 1;

    Thread.Sleep(segundos * 1000);  // Simula trabalho demorado

    Console.WriteLine($"[✓] [{workerId}] Tarefa concluída: {mensagem}");

    // ACK manual: confirma que a mensagem foi processada com sucesso
    // O RabbitMQ só remove a mensagem da fila após receber o ACK
    // Se o worker morrer sem enviar ACK, a mensagem volta para a fila
    channel.BasicAck(
        deliveryTag: eventArgs.DeliveryTag,
        multiple: false  // false = confirma apenas esta mensagem
                         // true  = confirma esta e todas as anteriores
    );
};

channel.BasicConsume(
    queue: "task_queue",
    autoAck: false,   // ACK manual — não confirma automaticamente
    consumer: consumer
);

// Mantém o worker em execução
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
    Console.WriteLine($"\n[i] Worker [{workerId}] encerrando...");
}

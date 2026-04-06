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

// Declara a fila RPC onde o servidor aguarda requisições
channel.QueueDeclare(
    queue: "rpc_queue",
    durable: false,
    exclusive: false,
    autoDelete: false,
    arguments: null
);

// Prefetch: processa uma requisição por vez
channel.BasicQos(prefetchSize: 0, prefetchCount: 1, global: false);

Console.WriteLine("[*] Servidor RPC aguardando requisições de Fibonacci...\n");

var consumer = new EventingBasicConsumer(channel);

consumer.Received += (model, eventArgs) =>
{
    var body = eventArgs.Body.ToArray();
    var requisicao = Encoding.UTF8.GetString(body);
    var props = eventArgs.BasicProperties;

    // Verifica se a requisição é válida (número para calcular Fibonacci)
    if (!int.TryParse(requisicao, out var n) || n < 0)
    {
        Console.WriteLine($"[!] Requisição inválida: '{requisicao}'");
        channel.BasicAck(eventArgs.DeliveryTag, false);
        return;
    }

    Console.WriteLine($"[.] Calculando Fibonacci({n})...");

    var resultado = CalcularFibonacci(n);

    Console.WriteLine($"[✓] Fibonacci({n}) = {resultado} — respondendo para {props.ReplyTo}");

    // Prepara as propriedades da resposta
    // O CorrelationId é ecoado de volta para que o cliente correlacione a resposta
    var replyProps = channel.CreateBasicProperties();
    replyProps.CorrelationId = props.CorrelationId;  // Ecoa o CorrelationId do cliente

    var resposta = resultado.ToString();
    var respostaBody = Encoding.UTF8.GetBytes(resposta);

    // Envia a resposta para a fila indicada em ReplyTo
    channel.BasicPublish(
        exchange: "",
        routingKey: props.ReplyTo,     // Fila de resposta do cliente
        basicProperties: replyProps,   // Com o mesmo CorrelationId
        body: respostaBody
    );

    // Confirma o recebimento da requisição
    channel.BasicAck(deliveryTag: eventArgs.DeliveryTag, multiple: false);
};

channel.BasicConsume(
    queue: "rpc_queue",
    autoAck: false,   // Manual ACK — confirma após processar
    consumer: consumer
);

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
    Console.WriteLine("\n[i] Servidor RPC encerrado.");
}

// Calcula Fibonacci de forma recursiva (intencional para demonstrar latência)
static long CalcularFibonacci(int n)
{
    if (n <= 1) return n;
    return CalcularFibonacci(n - 1) + CalcularFibonacci(n - 2);
}

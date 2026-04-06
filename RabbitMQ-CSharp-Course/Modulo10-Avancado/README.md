# Módulo 10 — Tópicos Avançados

## 🎯 Objetivos

- Publisher Confirms (confirmação de publicação)
- Priority Queues (filas com prioridade)
- Headers Exchange
- Consumer Cancellation Notification
- Connection Recovery (reconexão automática)
- Prefetch e controle de fluxo
- Boas práticas para produção

---

## 10.1 Publisher Confirms

Por padrão, `BasicPublish` é assíncrono — o producer não sabe se a mensagem chegou ao broker. **Publisher Confirms** é o mecanismo que garante que a mensagem foi recebida e persistida pelo RabbitMQ.

### Modo síncrono (simples, mas lento)

```csharp
// Ativa o modo de confirmações no canal
channel.ConfirmSelect();

channel.BasicPublish(exchange: "", routingKey: "minha_fila", body: body);

// Aguarda confirmação — bloqueia até o broker confirmar ou rejeitar
channel.WaitForConfirmsOrDie(timeout: TimeSpan.FromSeconds(5));
```

### Modo assíncrono com handlers (recomendado para produção)

```csharp
channel.ConfirmSelect();

// Handler chamado quando o broker confirma uma ou mais mensagens
channel.BasicAcks += (sender, args) =>
{
    Console.WriteLine($"[✓] Mensagem {args.DeliveryTag} confirmada. Multiple: {args.Multiple}");
};

// Handler chamado quando o broker rejeita uma mensagem
channel.BasicNacks += (sender, args) =>
{
    Console.WriteLine($"[✗] Mensagem {args.DeliveryTag} rejeitada! Reenviar...");
    // Lógica de reenvio aqui
};
```

### Publish Confirms com rastreamento

Para produção, rastreie quais mensagens foram confirmadas:

```csharp
var pendingConfirms = new ConcurrentDictionary<ulong, string>();

channel.ConfirmSelect();

// Adiciona antes de publicar
var sequenceNumber = channel.NextPublishSeqNo;
pendingConfirms[sequenceNumber] = mensagem;

channel.BasicPublish(...);

channel.BasicAcks += (sender, args) =>
{
    if (args.Multiple)
    {
        // Confirma todas as mensagens com DeliveryTag <= args.DeliveryTag
        var confirmed = pendingConfirms.Keys.Where(k => k <= args.DeliveryTag);
        foreach (var key in confirmed) pendingConfirms.TryRemove(key, out _);
    }
    else
    {
        pendingConfirms.TryRemove(args.DeliveryTag, out _);
    }
};
```

---

## 10.2 Priority Queues (Filas com Prioridade)

As filas com prioridade permitem que mensagens de maior prioridade sejam consumidas antes das de menor prioridade.

```csharp
// Cria fila com suporte a prioridades (1 a 10)
var args = new Dictionary<string, object>
{
    ["x-max-priority"] = 10  // Nível máximo de prioridade
};

channel.QueueDeclare(
    queue: "priority_queue",
    durable: true,
    arguments: args
);

// Publica com prioridade específica
var properties = channel.CreateBasicProperties();
properties.Priority = 9;  // Alta prioridade (1-10)

channel.BasicPublish(
    exchange: "",
    routingKey: "priority_queue",
    basicProperties: properties,
    body: body
);
```

**Importante:** Prioridade só funciona se houver mensagens acumuladas na fila. Se a fila estiver vazia, todas as mensagens são entregues na ordem de chegada independente da prioridade.

---

## 10.3 Headers Exchange

O Headers Exchange usa os **headers da mensagem** para roteamento, ignorando completamente a routing key.

```csharp
channel.ExchangeDeclare(
    exchange: "headers_exchange",
    type: ExchangeType.Headers
);

// Binding com headers específicos
// x-match: "all" = TODOS os headers devem corresponder
// x-match: "any" = QUALQUER header deve corresponder
var bindingArgs = new Dictionary<string, object>
{
    ["x-match"] = "all",    // Todos os headers devem bater
    ["format"] = "pdf",
    ["type"] = "report"
};

channel.QueueBind(
    queue: "pdf_reports",
    exchange: "headers_exchange",
    routingKey: "",          // Ignorado no Headers Exchange
    arguments: bindingArgs
);

// Publicando com headers
var properties = channel.CreateBasicProperties();
properties.Headers = new Dictionary<string, object>
{
    ["format"] = "pdf",
    ["type"] = "report",
    ["department"] = "finance"
};

channel.BasicPublish(
    exchange: "headers_exchange",
    routingKey: "",
    basicProperties: properties,
    body: body
);
```

---

## 10.4 Controle de Fluxo com Prefetch

O `BasicQos` controla quantas mensagens não confirmadas um consumer pode ter:

```csharp
// Global: false = por consumer | true = por canal (todos os consumers no canal)
channel.BasicQos(
    prefetchSize: 0,         // 0 = sem limite de tamanho
    prefetchCount: 10,       // Máximo 10 mensagens em voo por consumer
    global: false
);
```

### Estratégias de Prefetch:

| Cenário | Prefetch Count | Motivo |
|---|---|---|
| Processamento rápido | 20-50 | Reduz latência de rede |
| Processamento lento | 1-5 | Distribui carga de forma justa |
| Processamento crítico | 1 | Garante at-least-once processing |
| Alta throughput | 100-500 | Maximiza throughput |

---

## 10.5 Reconexão Automática

```csharp
var factory = new ConnectionFactory
{
    HostName = "localhost",
    
    // Reconexão automática em caso de falha de rede
    AutomaticRecoveryEnabled = true,
    
    // Intervalo entre tentativas de reconexão
    NetworkRecoveryInterval = TimeSpan.FromSeconds(10),
    
    // Timeout de reconexão
    RequestedConnectionTimeout = TimeSpan.FromSeconds(30),
    
    // Heartbeat para detectar conexões mortas
    RequestedHeartbeat = TimeSpan.FromSeconds(60)
};

var connection = factory.CreateConnection();

// Monitora eventos de recuperação
connection.ConnectionShutdown += (sender, args) =>
    Console.WriteLine($"[!] Conexão encerrada: {args.ReplyText}");

connection.RecoverySucceeded += (sender, args) =>
    Console.WriteLine("[✓] Conexão recuperada com sucesso!");

connection.ConnectionRecoveryError += (sender, args) =>
    Console.WriteLine($"[✗] Erro na recuperação: {args.Exception.Message}");
```

---

## 10.6 Boas Práticas para Produção

### Conexão e Canal

```
✅ 1 IConnection por processo/aplicação
✅ 1 IModel (channel) por thread
✅ Reusar channels para publicação (thread-safe com lock)
✅ Ativar AutomaticRecoveryEnabled = true
✅ Configurar RequestedHeartbeat
❌ Não compartilhar channels entre threads sem sincronização
❌ Não criar/destruir channels a cada publicação
```

### Mensagens

```
✅ Mensagens críticas: Persistent = true + durable queue
✅ Sempre usar manual ACK para processamento crítico
✅ Implementar DLQ para mensagens que falham
✅ Definir TTL para evitar acúmulo de mensagens antigas
✅ Usar CorrelationId para rastreamento distribuído
✅ Serializar com JSON/Protobuf/MessagePack
❌ Não enviar objetos grandes (> 128KB): use storage externo + referência
```

### Observabilidade

```
✅ Expor métricas: taxa de publicação, consumo, mensagens em fila
✅ Monitorar filas com muitas mensagens acumuladas
✅ Alertas para DLQ com mensagens novas
✅ Distributed tracing com OpenTelemetry
✅ Logs estruturados com CorrelationId
```

---

## 10.7 Executando o Exemplo

```bash
# Terminal 1 — Consumer com publisher confirms e priority queue
cd Modulo10-Avancado/src/Consumer
dotnet run

# Terminal 2 — Publisher com confirms e prioridades
cd Modulo10-Avancado/src/Publisher
dotnet run
```

---

## ✅ Resumo Geral do Curso

| Módulo | Padrão | Exchange | Uso |
|---|---|---|---|
| 03 | Hello World | Default | Introdução |
| 04 | Work Queue | Default | Processamento paralelo |
| 05 | Pub/Sub | Fanout | Broadcast |
| 06 | Routing | Direct | Roteamento seletivo |
| 07 | Topics | Topic | Roteamento por padrão |
| 08 | RPC | Default | Chamada remota |
| 09 | DLQ | Direct | Tratamento de erros |
| 10 | Avançado | Headers | Casos especiais |

---

## 🎓 Conclusão

Parabéns! Você completou o curso de RabbitMQ com C#. Você agora domina:

- ✅ Todos os tipos de Exchange
- ✅ Patterns essenciais de mensageria
- ✅ Garantias de entrega (ACK, NACK, Publisher Confirms)
- ✅ Tratamento de erros com DLQ e TTL
- ✅ Técnicas avançadas para produção

### Próximos Passos

- 📖 [RabbitMQ Official Documentation](https://www.rabbitmq.com/documentation.html)
- 🔧 Explore o plugin **shovel** para federação entre brokers
- 🏗️ Estude **Clustering** e **Quorum Queues** para alta disponibilidade
- 🔍 Integre com **OpenTelemetry** para distributed tracing
- 🚀 Explore **MassTransit** — framework de alto nível para RabbitMQ em .NET

---

*Desenvolvido por [Rian Fuzinelli](https://linkedin.com/in/rian-fuzinelli)*

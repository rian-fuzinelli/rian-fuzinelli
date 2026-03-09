# Módulo 05 — Pub/Sub com Fanout Exchange

## 🎯 Objetivos

- Entender o padrão Publish/Subscribe
- Implementar um Fanout Exchange
- Usar filas temporárias (exclusive queues)
- Entender a diferença entre Work Queue e Pub/Sub

---

## 5.1 O Padrão Publish/Subscribe

```
                              ┌──► [Queue 1] ──► [Subscriber 1]
[Publisher] ──► [logs] ──────►│
                              └──► [Queue 2] ──► [Subscriber 2]
```

No padrão Pub/Sub, **cada subscriber recebe uma cópia da mensagem**. Isso é diferente do Work Queue, onde cada mensagem é processada por apenas um worker.

**Casos de uso:**
- Logs distribuídos para múltiplos sistemas
- Notificações em tempo real
- Invalidação de cache em múltiplos servidores
- Sincronização de dados entre sistemas
- Eventos de negócio publicados para múltiplos consumidores

---

## 5.2 Fanout Exchange

O **Fanout Exchange** ignora completamente a routing key e envia a mensagem para **todas as filas vinculadas a ele**:

```
[Publisher] ──► [fanout exchange "logs"] ──► [queue_abc] ──► [Subscriber 1]
                                         ──► [queue_xyz] ──► [Subscriber 2]
                                         ──► [queue_123] ──► [Subscriber 3]
```

```csharp
// Declara o exchange do tipo fanout
channel.ExchangeDeclare(
    exchange: "logs",
    type: ExchangeType.Fanout
);

// Publica sem routing key (ignorada no fanout)
channel.BasicPublish(
    exchange: "logs",   // Nome do exchange
    routingKey: "",     // Ignorado no Fanout
    basicProperties: null,
    body: body
);
```

---

## 5.3 Filas Temporárias (Exclusive Queues)

No padrão Pub/Sub, cada subscriber geralmente quer:
1. Receber **todas** as mensagens (não dividir com outros)
2. Receber apenas mensagens **enquanto estiver conectado**
3. Usar uma fila **dedicada** e **temporária**

```csharp
// Cria uma fila com nome gerado pelo broker, exclusiva e auto-deletável
var queueName = channel.QueueDeclare().QueueName;
// queueName = algo como "amq.gen-JzTY20BRgKO-HjmUJj0wLg"
```

Esta fila:
- Tem nome aleatório gerado pelo RabbitMQ
- É exclusiva desta conexão
- É deletada quando a conexão fecha

---

## 5.4 Bindings

Um **Binding** é a ligação entre um Exchange e uma Queue:

```csharp
channel.QueueBind(
    queue: queueName,   // Nome da fila
    exchange: "logs",   // Nome do exchange
    routingKey: ""      // Ignorado no Fanout
);
```

---

## 5.5 Diferença: Work Queue vs Pub/Sub

| Aspecto | Work Queue | Pub/Sub (Fanout) |
|---|---|---|
| **Exchange** | Default (nenhum) | Fanout |
| **Distribuição** | Round-robin entre workers | Cópia para cada subscriber |
| **Uso** | Processamento paralelo de tarefas | Broadcast de eventos |
| **Fila** | Única e persistente | Uma por subscriber |
| **Mensagem recebida por** | Apenas 1 worker | Todos os subscribers |

---

## 5.6 Executando o Exemplo

```bash
# Terminal 1 — Subscriber 1 (log para console)
cd Modulo05-PubSub/src/Subscriber
dotnet run console

# Terminal 2 — Subscriber 2 (log para arquivo)
cd Modulo05-PubSub/src/Subscriber
dotnet run file

# Terminal 3 — Publisher
cd Modulo05-PubSub/src/Publisher
dotnet run
```

---

## ✅ Resumo do Módulo

| Conceito | Implementação |
|---|---|
| **Fanout Exchange** | `ExchangeDeclare(type: ExchangeType.Fanout)` |
| **Fila temporária** | `channel.QueueDeclare().QueueName` |
| **Binding** | `channel.QueueBind(queue, exchange, routingKey)` |
| **Broadcast** | `BasicPublish(exchange: "logs", routingKey: "")` |

---

## ➡️ Próximo Módulo

[Módulo 06 — Routing com Direct Exchange →](../Modulo06-Routing/README.md)

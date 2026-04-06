# Módulo 09 — Dead Letter Queue & Message TTL

## 🎯 Objetivos

- Entender o conceito de Dead Letter Queue (DLQ)
- Configurar uma DLQ para capturar mensagens rejeitadas ou expiradas
- Implementar TTL (Time-To-Live) de mensagens e filas
- Criar filas de espera (delay queues) usando TTL + DLQ
- Implementar padrão de retry com backoff exponencial

---

## 9.1 O que é uma Dead Letter Queue?

Uma **Dead Letter Queue (DLQ)** é uma fila especial que recebe mensagens que não puderam ser processadas por uma fila principal. Uma mensagem é "dead lettered" quando:

1. **Rejeitada** com `BasicNack/BasicReject` e `requeue: false`
2. **Expirada** pelo TTL (Time-To-Live) da mensagem ou da fila
3. **Fila cheia** (limite de mensagens atingido com `x-max-length`)

```
[Producer] ──► [main_queue] ──► [Consumer]
                    │                │
              (TTL expirado)    (NACK + requeue: false)
                    │                │
                    ▼                ▼
               [dead_letter_exchange]
                         │
                         ▼
                    [dlq_queue] ──► [DLQ Consumer / Auditoria]
```

---

## 9.2 Configurando uma DLQ

### 1. Criar o Dead Letter Exchange

```csharp
// Exchange que recebe mensagens mortas
channel.ExchangeDeclare(
    exchange: "dead_letter_exchange",
    type: ExchangeType.Direct
);

// Fila que armazena as mensagens mortas
channel.QueueDeclare(
    queue: "dead_letter_queue",
    durable: true,
    exclusive: false,
    autoDelete: false,
    arguments: null
);

channel.QueueBind(
    queue: "dead_letter_queue",
    exchange: "dead_letter_exchange",
    routingKey: "dead"
);
```

### 2. Configurar a fila principal para usar a DLQ

```csharp
var arguments = new Dictionary<string, object>
{
    // Exchange para onde as mensagens mortas serão enviadas
    ["x-dead-letter-exchange"] = "dead_letter_exchange",
    
    // Routing key usada ao enviar para a DLQ (opcional)
    ["x-dead-letter-routing-key"] = "dead",
    
    // TTL: mensagens expiram após 10 segundos
    ["x-message-ttl"] = 10000,
    
    // Limite de mensagens na fila (opcional)
    ["x-max-length"] = 100
};

channel.QueueDeclare(
    queue: "main_queue",
    durable: true,
    exclusive: false,
    autoDelete: false,
    arguments: arguments   // Argumentos especiais que ativam a DLQ
);
```

---

## 9.3 TTL (Time-To-Live)

### TTL de mensagem individual

```csharp
var properties = channel.CreateBasicProperties();
properties.Expiration = "5000";  // 5 segundos (em milissegundos, como string!)

channel.BasicPublish(
    exchange: "",
    routingKey: "main_queue",
    basicProperties: properties,
    body: body
);
```

### TTL de fila (todas as mensagens)

```csharp
var arguments = new Dictionary<string, object>
{
    ["x-message-ttl"] = 5000  // 5 segundos em milissegundos
};

channel.QueueDeclare(
    queue: "minha_fila",
    arguments: arguments
);
```

**Nota:** Se ambos TTL da mensagem e TTL da fila estiverem configurados, o menor valor prevalece.

---

## 9.4 Delay Queue (Fila de Atraso)

Um padrão popular usando DLQ é a **Delay Queue** — mensagens ficam em uma fila temporária por um tempo determinado antes de serem processadas:

```
[Producer] ──► [delay_queue (TTL=5s)] ──[expirada]──► [DLX] ──► [main_queue] ──► [Consumer]
```

Isso é útil para:
- Retry com backoff exponencial
- Agendamento de tarefas
- Rate limiting

---

## 9.5 Padrão de Retry com Backoff Exponencial

```
Tentativa 1: falha ──► DLQ com TTL=2s  ──► fila principal ──► Tentativa 2
Tentativa 2: falha ──► DLQ com TTL=4s  ──► fila principal ──► Tentativa 3
Tentativa 3: falha ──► DLQ com TTL=8s  ──► fila principal ──► Tentativa 4
Tentativa 4: falha ──► DLQ permanente (sem mais retries)
```

Implementado com headers para rastrear o número de tentativas:

```csharp
// Incrementa o contador de tentativas no header
var tentativas = (int)(properties.Headers?.GetValueOrDefault("x-retry-count") ?? 0);
var novosTtl = (int)Math.Pow(2, tentativas) * 1000; // 1s, 2s, 4s, 8s...
```

---

## 9.6 Executando o Exemplo

```bash
# Terminal 1 — Consumer com processamento que falha ocasionalmente
cd Modulo09-DeadLetterQueue/src/Consumer
dotnet run

# Terminal 2 — Producer
cd Modulo09-DeadLetterQueue/src/Producer
dotnet run
```

---

## ✅ Resumo do Módulo

| Conceito | Argumento / Configuração |
|---|---|
| **Dead Letter Exchange** | `x-dead-letter-exchange` |
| **DLQ Routing Key** | `x-dead-letter-routing-key` |
| **TTL da fila** | `x-message-ttl` (em ms) |
| **TTL da mensagem** | `properties.Expiration` (em ms, como string) |
| **Limite de mensagens** | `x-max-length` |
| **Rejeitar sem requeue** | `BasicNack(requeue: false)` |

---

## ➡️ Próximo Módulo

[Módulo 10 — Tópicos Avançados →](../Modulo10-Avancado/README.md)

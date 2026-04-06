# Módulo 04 — Work Queues (Filas de Trabalho)

## 🎯 Objetivos

- Entender o padrão Work Queue (Task Queue)
- Implementar distribuição de tarefas entre múltiplos workers
- Configurar Message Acknowledgement manual
- Garantir que tarefas não sejam perdidas em caso de falha
- Usar Prefetch Count para distribuição justa de carga

---

## 4.1 O Padrão Work Queue

```
                                    ┌──► [Worker 1]
[Producer] ──► [task_queue] ──────►│
                                    └──► [Worker 2]
```

O padrão Work Queue (ou Task Queue) distribui tarefas entre múltiplos workers. Cada tarefa é processada por **exatamente um worker**. Isso é ideal para tarefas demoradas que precisam ser paralelizadas.

**Casos de uso:**
- Processamento de imagens/vídeos
- Envio de emails/notificações
- Geração de relatórios
- Tarefas de importação/exportação

---

## 4.2 Round-Robin Dispatch

Por padrão, o RabbitMQ distribui mensagens em **round-robin** entre os workers:

```
Mensagem 1 ──► Worker 1
Mensagem 2 ──► Worker 2
Mensagem 3 ──► Worker 1
Mensagem 4 ──► Worker 2
```

---

## 4.3 Message Acknowledgement (ACK)

### O problema sem ACK manual

```
Worker 1 recebe tarefa longa ──► Worker 1 morre no meio do processamento
                                        ──► Tarefa PERDIDA! ❌
```

### A solução: Manual ACK

Com `autoAck: false`, o consumer confirma manualmente após o processamento:

```csharp
channel.BasicConsume(
    queue: "task_queue",
    autoAck: false,  // ACK manual
    consumer: consumer
);

// Após processar com sucesso:
channel.BasicAck(
    deliveryTag: eventArgs.DeliveryTag,
    multiple: false
);

// Em caso de erro (reenvia para a fila):
channel.BasicNack(
    deliveryTag: eventArgs.DeliveryTag,
    multiple: false,
    requeue: true
);
```

### Ciclo de vida com ACK manual

```
1. Worker recebe mensagem ──► Status: Unacked
2. Worker processa...
3a. Sucesso ──► BasicAck ──► Mensagem removida da fila ✅
3b. Falha   ──► BasicNack(requeue:true) ──► Mensagem volta para a fila 🔄
3c. Worker morre sem ACK ──► Mensagem volta automaticamente para a fila 🔄
```

---

## 4.4 Message Durability (Durabilidade)

### O problema sem durabilidade

```
RabbitMQ reinicia ──► Todas as filas e mensagens em memória são PERDIDAS! ❌
```

### A solução: Filas e Mensagens duráveis

```csharp
// 1. Fila durável
channel.QueueDeclare(
    queue: "task_queue",
    durable: true,    // Fila sobrevive ao restart do broker
    exclusive: false,
    autoDelete: false,
    arguments: null
);

// 2. Mensagem persistente
var properties = channel.CreateBasicProperties();
properties.Persistent = true;  // Mensagem salva em disco

channel.BasicPublish(
    exchange: "",
    routingKey: "task_queue",
    basicProperties: properties,  // Propriedades com Persistent = true
    body: body
);
```

**Importante:** Durabilidade não é 100% garantida — há uma janela pequena em que o RabbitMQ aceitou a mensagem mas ainda não salvou em disco. Para garantia total, use **Publisher Confirms** (visto no Módulo 10).

---

## 4.5 Fair Dispatch com Prefetch Count

### O problema do round-robin simples

```
Worker 1: recebe tarefas leves (2 dots) ──► Fica ocioso
Worker 2: recebe tarefas pesadas (8 dots) ──► Fica sobrecarregado
```

O round-robin puro não considera a carga atual de cada worker.

### A solução: BasicQos com Prefetch Count

```csharp
// Limita a 1 mensagem não confirmada por worker
channel.BasicQos(
    prefetchSize: 0,   // Sem limite de tamanho
    prefetchCount: 1,  // Máximo 1 mensagem por vez
    global: false      // Aplicado por consumer (não por canal)
);
```

Com `prefetchCount: 1`:
- Worker só recebe nova mensagem após confirmar a anterior
- Workers rápidos processam mais tarefas
- Workers lentos recebem menos tarefas

```
Worker 1 (rápido): ████░░ ────► recebe nova tarefa ──► ████░░ ────► ...
Worker 2 (lento):  ████████████████ ──────────────────────────────────► ...
```

---

## 4.6 Executando o Exemplo

```bash
# Terminal 1 — Worker 1
cd Modulo04-WorkQueues/src/Worker
dotnet run worker1

# Terminal 2 — Worker 2
cd Modulo04-WorkQueues/src/Worker
dotnet run worker2

# Terminal 3 — Producer
cd Modulo04-WorkQueues/src/Producer
dotnet run
```

**Saída esperada no Worker 1:**
```
[*] Worker worker1 aguardando tarefas...
[x] Recebido: Tarefa #1 (processamento: 1s)
[✓] Tarefa #1 concluída.
[x] Recebido: Tarefa #3 (processamento: 3s)
[✓] Tarefa #3 concluída.
```

**Saída esperada no Worker 2:**
```
[*] Worker worker2 aguardando tarefas...
[x] Recebido: Tarefa #2 (processamento: 2s)
[✓] Tarefa #2 concluída.
[x] Recebido: Tarefa #4 (processamento: 1s)
[✓] Tarefa #4 concluída.
```

---

## ✅ Resumo do Módulo

| Conceito | Configuração |
|---|---|
| **Round-Robin** | Padrão, automático |
| **Manual ACK** | `autoAck: false` + `BasicAck()` |
| **Durabilidade de fila** | `durable: true` em QueueDeclare |
| **Mensagem persistente** | `Persistent = true` em BasicProperties |
| **Fair Dispatch** | `BasicQos(prefetchCount: 1)` |

---

## ➡️ Próximo Módulo

[Módulo 05 — Pub/Sub com Fanout Exchange →](../Modulo05-PubSub/README.md)

# Módulo 08 — Padrão RPC (Remote Procedure Call)

## 🎯 Objetivos

- Entender o padrão RPC sobre RabbitMQ
- Implementar um servidor RPC em C#
- Implementar um cliente RPC em C#
- Usar `replyTo` e `correlationId` para correlacionar requisições e respostas
- Entender os trade-offs do RPC assíncrono

---

## 8.1 O Padrão RPC

O padrão RPC permite que um cliente chame uma função em um servidor remoto e **aguarde a resposta**, de forma similar a uma chamada de função local, mas de forma desacoplada via mensageria.

```
[RPC Client] ──► [rpc_queue] ──► [RPC Server]
[RPC Client] ◄── [reply_queue] ◄── [RPC Server]
```

**Casos de uso:**
- Validação de dados em microsserviços
- Consultas assíncronas com resposta
- Cálculos distribuídos
- Processamento com retorno de resultado

---

## 8.2 Propriedades Essenciais

### ReplyTo

A propriedade `ReplyTo` contém o nome da fila onde o servidor deve enviar a resposta:

```csharp
var properties = channel.CreateBasicProperties();
properties.ReplyTo = "amq.rabbitmq.reply-to"; // Ou nome de fila temporária
```

### CorrelationId

O `CorrelationId` é um identificador único que permite ao cliente correlacionar respostas com requisições quando há múltiplas requisições em voo:

```csharp
var correlationId = Guid.NewGuid().ToString();
properties.CorrelationId = correlationId;

// O servidor ecoa o mesmo CorrelationId na resposta
// O cliente filtra respostas pelo CorrelationId esperado
```

---

## 8.3 Direct Reply-To (Otimização)

O RabbitMQ oferece um mecanismo especial chamado **Direct Reply-To** que evita a necessidade de criar uma fila de resposta temporária:

```csharp
// Em vez de criar uma fila temporária, use "amq.rabbitmq.reply-to"
properties.ReplyTo = "amq.rabbitmq.reply-to";

// O consumer deve usar autoAck: true para a fila de resposta
channel.BasicConsume(
    queue: "amq.rabbitmq.reply-to",
    autoAck: true,
    consumer: replyConsumer
);
```

---

## 8.4 Fluxo de uma Chamada RPC

```
1. Cliente gera um CorrelationId único
2. Cliente cria uma fila de resposta (ou usa amq.rabbitmq.reply-to)
3. Cliente envia mensagem para rpc_queue com:
   - CorrelationId
   - ReplyTo = nome da fila de resposta
4. Servidor recebe da rpc_queue
5. Servidor processa a requisição
6. Servidor envia a resposta para a fila ReplyTo com mesmo CorrelationId
7. Cliente recebe da fila de resposta
8. Cliente verifica se o CorrelationId corresponde
9. Cliente retorna o resultado
```

---

## 8.5 Executando o Exemplo

```bash
# Terminal 1 — RPC Server (inicie primeiro)
cd Modulo08-RPC/src/RpcServer
dotnet run

# Terminal 2 — RPC Client
cd Modulo08-RPC/src/RpcClient
dotnet run
```

**Saída esperada no RPC Server:**
```
[*] Servidor RPC aguardando requisições de Fibonacci...
[.] Calculando Fibonacci(10)...
[✓] Fibonacci(10) = 55 — enviado para amq.rabbitmq.reply-to
[.] Calculando Fibonacci(30)...
[✓] Fibonacci(30) = 832040 — enviado para amq.rabbitmq.reply-to
```

**Saída esperada no RPC Client:**
```
[*] Calculando Fibonacci via RPC...
[x] Fibonacci(10) = 55  [CorrelationId: abc123...]
[x] Fibonacci(30) = 832040  [CorrelationId: def456...]
```

---

## 8.6 Considerações e Trade-offs

### Vantagens do RPC sobre RabbitMQ:
- ✅ Desacoplamento do servidor
- ✅ Escalabilidade (múltiplos servidores RPC)
- ✅ Resiliência (filas como buffer)
- ✅ Balanceamento de carga automático

### Desvantagens:
- ⚠️ Latência adicionada pelo broker
- ⚠️ Complexidade maior que HTTP/gRPC direto
- ⚠️ Difícil de depurar (rastreamento distribuído)

### Alternativas mais adequadas:
Para comunicação síncrona simples, considere usar **gRPC** ou **HTTP/REST** diretamente. Reserve o RPC sobre RabbitMQ para casos onde o desacoplamento é essencial.

---

## ✅ Resumo do Módulo

| Conceito | Implementação |
|---|---|
| **ReplyTo** | `properties.ReplyTo = "amq.rabbitmq.reply-to"` |
| **CorrelationId** | `properties.CorrelationId = Guid.NewGuid().ToString()` |
| **Direct Reply-To** | Fila especial `amq.rabbitmq.reply-to` |
| **Correlação** | Filtrar respostas pelo `CorrelationId` |

---

## ➡️ Próximo Módulo

[Módulo 09 — Dead Letter Queue & TTL →](../Modulo09-DeadLetterQueue/README.md)

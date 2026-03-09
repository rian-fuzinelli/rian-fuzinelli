# Módulo 03 — Hello World: Producer & Consumer

## 🎯 Objetivos

- Implementar o primeiro producer em C#
- Implementar o primeiro consumer em C#
- Entender o Exchange padrão (Default Exchange)
- Executar e observar a comunicação entre producer e consumer

---

## 3.1 O Padrão Hello World

```
[Producer] ──► [Default Exchange] ──► [hello] ──► [Consumer]
```

O **Default Exchange** é um exchange especial que já existe no RabbitMQ. Quando você publica uma mensagem com uma routing key igual ao nome da fila, ela é roteada diretamente para essa fila.

---

## 3.2 Producer

O producer envia mensagens para a fila `hello`.

**Código:** [`src/Producer/Program.cs`](./src/Producer/Program.cs)

---

## 3.3 Consumer

O consumer recebe e processa mensagens da fila `hello`.

**Código:** [`src/Consumer/Program.cs`](./src/Consumer/Program.cs)

---

## 3.4 Conceitos Explicados

### QueueDeclare — Idempotente

```csharp
channel.QueueDeclare(
    queue: "hello",
    durable: false,     // Não sobrevive ao restart do broker
    exclusive: false,   // Pode ser usada por múltiplas conexões
    autoDelete: false,  // Não é deletada quando consumers se desconectam
    arguments: null     // Sem argumentos adicionais
);
```

A declaração de fila é **idempotente**: se a fila já existir com os mesmos parâmetros, não faz nada. Se existir com parâmetros diferentes, lança exceção.

### BasicPublish — Publicando mensagens

```csharp
channel.BasicPublish(
    exchange: "",           // "" = Default Exchange
    routingKey: "hello",    // Nome da fila no Default Exchange
    basicProperties: null,  // Sem propriedades adicionais
    body: body              // Corpo da mensagem em bytes
);
```

### BasicConsume — Consumindo mensagens

```csharp
channel.BasicConsume(
    queue: "hello",
    autoAck: true,          // Confirma automaticamente ao receber
    consumer: consumer
);
```

**`autoAck: true`**: O RabbitMQ remove a mensagem da fila assim que a entrega ao consumer, sem esperar confirmação de processamento.

---

## 3.5 Executando o Exemplo

```bash
# Terminal 1 — Consumer (inicie primeiro)
cd Modulo03-HelloWorld/src/Consumer
dotnet run

# Terminal 2 — Producer
cd Modulo03-HelloWorld/src/Producer
dotnet run
```

**Saída esperada no Consumer:**
```
[*] Aguardando mensagens. Pressione CTRL+C para sair.
[x] Recebido: Hello World!
[x] Recebido: Hello World!
[x] Recebido: Hello World!
```

**Saída esperada no Producer:**
```
[x] Enviando mensagem: Hello World! (#1)
[x] Enviando mensagem: Hello World! (#2)
[x] Enviando mensagem: Hello World! (#3)
[x] 3 mensagens enviadas com sucesso.
```

---

## 3.6 Observando no Management UI

1. Acesse http://localhost:15672
2. Vá em **Queues** → clique em `hello`
3. Observe o gráfico de **mensagens publicadas** e **consumidas**
4. Veja a seção **Consumers** para ver os consumers conectados

---

## ✅ Resumo do Módulo

- ✅ Criou seu primeiro producer em C#
- ✅ Criou seu primeiro consumer em C#
- ✅ Entendeu o Default Exchange e routing key
- ✅ Usou `QueueDeclare`, `BasicPublish` e `BasicConsume`

---

## ➡️ Próximo Módulo

[Módulo 04 — Work Queues →](../Modulo04-WorkQueues/README.md)

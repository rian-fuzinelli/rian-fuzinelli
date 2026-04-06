# Módulo 06 — Routing com Direct Exchange

## 🎯 Objetivos

- Entender o roteamento com Direct Exchange
- Implementar múltiplos consumers com routing keys diferentes
- Filtrar mensagens por severidade ou categoria
- Entender múltiplos bindings no mesmo exchange

---

## 6.1 O Problema com Fanout

O Fanout envia tudo para todos. Mas e se você quiser:
- Consumer A receba apenas logs de `ERROR`
- Consumer B receba `INFO` e `WARNING`
- Consumer C receba tudo

O **Direct Exchange** resolve isso com routing keys.

---

## 6.2 Direct Exchange

```
                                  ┌─[binding: "error"]──► [Queue 1] ──► [Consumer 1: Alertas]
[Publisher] ──► [direct_logs] ───►│
                                  └─[binding: "info"]───► [Queue 2] ──► [Consumer 2: Geral]
                                  └─[binding: "warning"]─► [Queue 2] ──► [Consumer 2: Geral]
```

O **Direct Exchange** roteia mensagens para filas cujo **binding key** é **igual** à **routing key** da mensagem.

```csharp
// Declara exchange do tipo Direct
channel.ExchangeDeclare(
    exchange: "direct_logs",
    type: ExchangeType.Direct
);

// Publica com routing key específica
channel.BasicPublish(
    exchange: "direct_logs",
    routingKey: "error",   // Só chega em filas com binding "error"
    basicProperties: null,
    body: body
);
```

---

## 6.3 Múltiplos Bindings

Uma fila pode ter múltiplos bindings para o mesmo exchange:

```csharp
// Fila "geral" recebe info E warning
channel.QueueBind(queue: "geral", exchange: "direct_logs", routingKey: "info");
channel.QueueBind(queue: "geral", exchange: "direct_logs", routingKey: "warning");

// Fila "alertas" recebe apenas error
channel.QueueBind(queue: "alertas", exchange: "direct_logs", routingKey: "error");
```

---

## 6.4 Executando o Exemplo

```bash
# Terminal 1 — Consumer de alertas (apenas ERROR)
cd Modulo06-Routing/src/Consumer
dotnet run alertas error

# Terminal 2 — Consumer geral (INFO e WARNING)
cd Modulo06-Routing/src/Consumer
dotnet run geral info warning

# Terminal 3 — Producer
cd Modulo06-Routing/src/Producer
dotnet run
```

**Saída esperada no Consumer de alertas:**
```
[*] Consumer [alertas] aguardando: error
[x] [alertas] Recebido (error): [ERROR] 14:30:05 - Falha na conexão com banco
[x] [alertas] Recebido (error): [ERROR] 14:30:07 - Timeout na requisição
```

**Saída esperada no Consumer geral:**
```
[*] Consumer [geral] aguardando: info, warning
[x] [geral] Recebido (info): [INFO] 14:30:04 - Usuário logado
[x] [geral] Recebido (warning): [WARNING] 14:30:06 - CPU acima de 80%
```

---

## 6.5 Comparação: Default vs Fanout vs Direct

| Exchange | Roteamento | Uso |
|---|---|---|
| **Default** | Nome da fila = routing key | Simples, sem exchange explícito |
| **Fanout** | Todos (ignora routing key) | Broadcast |
| **Direct** | Routing key exata | Roteamento seletivo |

---

## ✅ Resumo do Módulo

| Conceito | Implementação |
|---|---|
| **Direct Exchange** | `ExchangeDeclare(type: ExchangeType.Direct)` |
| **Binding com key** | `QueueBind(queue, exchange, routingKey: "error")` |
| **Publicar com key** | `BasicPublish(exchange, routingKey: "error", ...)` |
| **Múltiplos bindings** | Vários `QueueBind()` na mesma fila |

---

## ➡️ Próximo Módulo

[Módulo 07 — Topics Exchange →](../Modulo07-Topics/README.md)

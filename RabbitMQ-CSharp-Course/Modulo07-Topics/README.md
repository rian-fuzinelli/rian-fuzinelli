# Módulo 07 — Topics Exchange

## 🎯 Objetivos

- Entender o roteamento por padrões com Topics Exchange
- Usar os wildcards `*` e `#`
- Implementar roteamento flexível e hierárquico
- Comparar Direct, Fanout e Topic exchanges

---

## 7.1 Limitação do Direct Exchange

O Direct Exchange exige correspondência **exata** da routing key. Mas e se você precisar de:
- Receber logs de `ERROR` de **qualquer** serviço
- Receber **todos** os logs do serviço `pagamentos`
- Receber logs de `WARNING` e `ERROR` do módulo `auth`

O **Topic Exchange** resolve isso com routing keys hierárquicas e wildcards.

---

## 7.2 Topic Exchange

O Topic Exchange usa routing keys no formato de **palavras separadas por ponto**:

```
<palavra>.<palavra>.<palavra>
```

Exemplos:
- `pagamentos.order.created`
- `auth.user.login`
- `estoque.produto.atualizado`
- `error.pagamentos.timeout`

### Wildcards

| Wildcard | Significado | Exemplo |
|---|---|---|
| `*` | Exatamente uma palavra | `pagamentos.*.created` |
| `#` | Zero ou mais palavras | `pagamentos.#` |

### Exemplos de correspondência

| Routing Key | Pattern `pagamentos.*` | Pattern `*.order.*` | Pattern `#.error` | Pattern `pagamentos.#` |
|---|---|---|---|---|
| `pagamentos.created` | ✅ | ❌ | ❌ | ✅ |
| `pagamentos.order.created` | ❌ | ✅ | ❌ | ✅ |
| `auth.order.created` | ❌ | ✅ | ❌ | ❌ |
| `pagamentos.timeout.error` | ❌ | ❌ | ✅ | ✅ |

---

## 7.3 Arquitetura do Exemplo

```
                                ┌─[*.error]──────► [Queue Erros]    ──► Consumer Alertas
[Publisher] ──► [topic_logs] ──►│
                                └─[pagamentos.#]──► [Queue Pagamentos] ──► Consumer Pagamentos
                                └─[#]─────────────► [Queue Todos]      ──► Consumer Auditoria
```

---

## 7.4 Declaração e Uso

```csharp
// Declara exchange do tipo Topic
channel.ExchangeDeclare(
    exchange: "topic_logs",
    type: ExchangeType.Topic
);

// Binding com wildcard *: exatamente uma palavra
channel.QueueBind(queue: "erros", exchange: "topic_logs", routingKey: "*.error");

// Binding com wildcard #: zero ou mais palavras
channel.QueueBind(queue: "pagamentos", exchange: "topic_logs", routingKey: "pagamentos.#");

// Binding # sozinho: recebe TUDO (equivalente ao Fanout)
channel.QueueBind(queue: "auditoria", exchange: "topic_logs", routingKey: "#");

// Publicando com routing key hierárquica
channel.BasicPublish(
    exchange: "topic_logs",
    routingKey: "pagamentos.order.error",  // Corresponde a *.error E pagamentos.# E #
    basicProperties: null,
    body: body
);
```

---

## 7.5 Executando o Exemplo

```bash
# Terminal 1 — Consumer de erros (*.error)
cd Modulo07-Topics/src/Consumer
dotnet run alertas "*.error"

# Terminal 2 — Consumer de pagamentos (pagamentos.#)
cd Modulo07-Topics/src/Consumer
dotnet run pagamentos "pagamentos.#"

# Terminal 3 — Consumer de auditoria (# — tudo)
cd Modulo07-Topics/src/Consumer
dotnet run auditoria "#"

# Terminal 4 — Producer
cd Modulo07-Topics/src/Producer
dotnet run
```

---

## 7.6 Comparação dos Tipos de Exchange

| Exchange | Routing Key | Wildcards | Uso típico |
|---|---|---|---|
| **Default** | Nome exato da fila | Não | Hello World |
| **Direct** | Palavra exata | Não | Roteamento simples |
| **Fanout** | Ignorada | N/A | Broadcast |
| **Topic** | Padrão com pontos | `*` e `#` | Roteamento flexível |
| **Headers** | Headers da mensagem | Não | Roteamento por metadados |

---

## ✅ Resumo do Módulo

| Wildcard | Regra |
|---|---|
| `*` | Substitui **exatamente 1** palavra |
| `#` | Substitui **0 ou mais** palavras |
| `#` sozinho | Corresponde a **qualquer** routing key |
| `palavra.*` | Começa com `palavra.` + 1 palavra |
| `palavra.#` | Começa com `palavra.` + qualquer coisa |

---

## ➡️ Próximo Módulo

[Módulo 08 — Padrão RPC →](../Modulo08-RPC/README.md)

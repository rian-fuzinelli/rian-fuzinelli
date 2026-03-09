# Módulo 01 — Introdução ao RabbitMQ

## 🎯 Objetivos

- Compreender o que é mensageria e por que usá-la
- Entender a arquitetura do RabbitMQ
- Conhecer o protocolo AMQP
- Dominar os conceitos fundamentais: Exchange, Queue, Binding, Routing Key

---

## 1.1 O Problema que a Mensageria Resolve

### Comunicação Síncrona — O problema

Em sistemas distribuídos tradicionais, os serviços se comunicam de forma **síncrona**:

```
[Serviço A] ──HTTP──► [Serviço B] ──HTTP──► [Serviço C]
```

**Problemas desta abordagem:**

| Problema | Impacto |
|---|---|
| **Acoplamento temporal** | Se B estiver fora do ar, A falha |
| **Acoplamento espacial** | A precisa saber o endereço de B |
| **Escalabilidade limitada** | Difícil distribuir carga entre múltiplas instâncias de B |
| **Resiliência fraca** | Uma falha em cascata derruba toda a cadeia |

### Comunicação Assíncrona — A solução

Com um Message Broker:

```
[Serviço A] ──► [Message Broker] ──► [Serviço B]
                        │
                        └──────────► [Serviço C]
```

**Benefícios:**

- ✅ **Desacoplamento**: A não precisa saber de B
- ✅ **Resiliência**: Mensagens ficam na fila se B estiver offline
- ✅ **Escalabilidade**: Múltiplas instâncias de B consomem em paralelo
- ✅ **Elasticidade**: Picos de carga são absorvidos pela fila

---

## 1.2 O que é RabbitMQ?

RabbitMQ é um **message broker** open-source, escrito em Erlang, que implementa o protocolo **AMQP 0-9-1** (Advanced Message Queuing Protocol).

### Características principais:

- 🔄 **Multi-protocolo**: AMQP, MQTT, STOMP
- 🌐 **Multi-plataforma**: Clients para Java, .NET, Python, Go, etc.
- 🔌 **Plugável**: Sistema de plugins extensível
- 🖥️ **Management UI**: Interface web para monitoramento
- 🔒 **Segurança**: TLS, autenticação, autorização por virtual host
- 🏗️ **Clustering**: Alta disponibilidade e escalabilidade horizontal

---

## 1.3 Protocolo AMQP 0-9-1

O **AMQP (Advanced Message Queuing Protocol)** é um protocolo binário, de camada de aplicação, que define:

- Como mensagens são criadas, transferidas e confirmadas
- Como exchanges e queues se comportam
- Como bindings funcionam

### Modelo de entrega:

```
Publisher ──► Exchange ──► Queue ──► Consumer
              (routing)   (storage)  (processing)
```

---

## 1.4 Componentes Fundamentais

### Exchange

O Exchange é o **roteador** do RabbitMQ. Recebe mensagens dos publishers e as distribui para as filas corretas com base em regras de roteamento.

#### Tipos de Exchange:

| Tipo | Comportamento | Uso |
|---|---|---|
| **Direct** | Roteia pela routing key exata | Roteamento direto e específico |
| **Fanout** | Roteia para todas as filas vinculadas | Broadcast / Pub-Sub |
| **Topic** | Roteia por padrões com wildcards (`*`, `#`) | Roteamento flexível |
| **Headers** | Roteia pelos headers da mensagem | Roteamento complexo |
| **Default** | Exchange padrão, roteia pelo nome da fila | Hello World básico |

### Queue (Fila)

A Queue é o **buffer** que armazena mensagens até serem consumidas.

#### Propriedades importantes:

| Propriedade | Descrição |
|---|---|
| **Durable** | Sobrevive a reinicializações do broker |
| **Exclusive** | Usada por apenas uma conexão |
| **Auto-delete** | Deletada quando o último consumer se desconecta |
| **Arguments** | Configurações adicionais (TTL, DLQ, etc.) |

### Binding

O Binding é a **regra** que conecta um Exchange a uma Queue. Pode incluir uma **Binding Key** usada para filtrar mensagens.

```
Exchange ──[binding key]──► Queue
```

### Routing Key

A Routing Key é um **atributo da mensagem** que o Exchange usa para decidir para qual fila enviá-la.

### Virtual Host (vhost)

Um vhost é um **namespace isolado** dentro do RabbitMQ. Permite que múltiplas aplicações compartilhem o mesmo broker com isolamento total.

---

## 1.5 Ciclo de Vida de uma Mensagem

```
1. Publisher cria uma mensagem
       │
       ▼
2. Publisher publica na Exchange com uma Routing Key
       │
       ▼
3. Exchange avalia a Routing Key contra os Bindings
       │
       ▼
4. Exchange roteia a mensagem para a(s) Queue(s) correspondente(s)
       │
       ▼
5. Mensagem fica na Queue até ser consumida
       │
       ▼
6. Consumer recebe a mensagem
       │
       ▼
7. Consumer processa e envia ACK (confirmação) ou NACK (rejeição)
       │
       ▼
8. RabbitMQ remove a mensagem da Queue (em caso de ACK)
```

---

## 1.6 Garantias de Entrega

RabbitMQ oferece diferentes garantias de entrega:

| Garantia | Descrição | Configuração |
|---|---|---|
| **At most once** | Mensagem entregue no máximo uma vez (pode perder) | Auto-ack ativo |
| **At least once** | Mensagem entregue ao menos uma vez (pode duplicar) | Manual ack + persistência |
| **Exactly once** | Entregue exatamente uma vez | Idempotência no consumer |

---

## 1.7 Casos de Uso Reais

### E-commerce

```
[Order Service] ──► [order.created] ──► Exchange
                                            ├──► [email-queue]      ──► Email Service
                                            ├──► [inventory-queue]  ──► Inventory Service
                                            └──► [payment-queue]    ──► Payment Service
```

### Processamento de Imagens

```
[Upload Service] ──► [image.uploaded] ──► Queue ──► [Resize Worker 1]
                                                 ──► [Resize Worker 2]
                                                 ──► [Resize Worker 3]
```

### Notificações

```
[App Service] ──► [notification.push]  ──► Push Queue  ──► Push Worker
              ──► [notification.email] ──► Email Queue ──► Email Worker
              ──► [notification.sms]   ──► SMS Queue   ──► SMS Worker
```

---

## 1.8 RabbitMQ vs Kafka

| Aspecto | RabbitMQ | Kafka |
|---|---|---|
| **Paradigma** | Message Queue (push) | Event Streaming (pull) |
| **Retenção** | Mensagem removida após consumo | Log imutável, retenção configurável |
| **Throughput** | Médio-alto (~50k msg/s) | Muito alto (~1M msg/s) |
| **Latência** | Muito baixa (< 1ms) | Baixa (ms) |
| **Roteamento** | Muito flexível (Exchange types) | Simples (por tópico/partição) |
| **Casos de uso** | Task queues, RPC, Pub/Sub | Event sourcing, stream processing |

---

## ✅ Resumo do Módulo

Neste módulo você aprendeu:

1. **Por que usar mensageria**: Desacoplamento, resiliência, escalabilidade
2. **O que é RabbitMQ**: Message broker AMQP open-source
3. **Componentes**: Exchange, Queue, Binding, Routing Key, vhost
4. **Tipos de Exchange**: Direct, Fanout, Topic, Headers
5. **Garantias de entrega**: At-most-once, At-least-once
6. **Casos de uso**: E-commerce, processamento assíncrono, notificações

---

## ➡️ Próximo Módulo

[Módulo 02 — Configuração do Ambiente →](../Modulo02-Configuracao/README.md)

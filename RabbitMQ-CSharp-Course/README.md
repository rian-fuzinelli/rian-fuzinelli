# 🐇 Curso Completo de RabbitMQ com C# — Do Zero ao Avançado

> Um guia prático e progressivo para dominar RabbitMQ no ecossistema .NET/C#.

---

## 📋 Sumário

| Módulo | Tópico | Nível |
|--------|--------|-------|
| [01](./Modulo01-Introducao/README.md) | Introdução ao RabbitMQ | Iniciante |
| [02](./Modulo02-Configuracao/README.md) | Configuração do Ambiente | Iniciante |
| [03](./Modulo03-HelloWorld/README.md) | Hello World — Producer & Consumer | Iniciante |
| [04](./Modulo04-WorkQueues/README.md) | Work Queues — Filas de Trabalho | Iniciante |
| [05](./Modulo05-PubSub/README.md) | Pub/Sub com Fanout Exchange | Intermediário |
| [06](./Modulo06-Routing/README.md) | Routing com Direct Exchange | Intermediário |
| [07](./Modulo07-Topics/README.md) | Topics Exchange | Intermediário |
| [08](./Modulo08-RPC/README.md) | Padrão RPC (Remote Procedure Call) | Intermediário |
| [09](./Modulo09-DeadLetterQueue/README.md) | Dead Letter Queue & Message TTL | Avançado |
| [10](./Modulo10-Avancado/README.md) | Tópicos Avançados | Avançado |

---

## 🎯 Objetivos do Curso

Ao concluir este curso, você será capaz de:

- ✅ Entender os fundamentos de message brokers e filas de mensagens
- ✅ Instalar e configurar RabbitMQ com Docker
- ✅ Implementar os padrões essenciais de mensageria em C#
- ✅ Utilizar todos os tipos de Exchange (Direct, Fanout, Topic, Headers)
- ✅ Garantir entrega de mensagens com acknowledgements e durabilidade
- ✅ Implementar Dead Letter Queues e TTL de mensagens
- ✅ Construir sistemas resilientes com prefetch e confirmações de publicação
- ✅ Criar filas com prioridade e implementar o padrão RPC
- ✅ Aplicar RabbitMQ em cenários reais de microsserviços

---

## 🛠️ Pré-requisitos

- Conhecimento básico de C# e .NET
- Docker instalado na máquina
- .NET 8 SDK instalado
- Editor de código (Visual Studio, VS Code ou Rider)

---

## 📦 Tecnologias Utilizadas

| Tecnologia | Versão | Finalidade |
|---|---|---|
| .NET | 8.0 | Runtime e SDK |
| RabbitMQ.Client | 6.8.1 | Client oficial do RabbitMQ para .NET |
| RabbitMQ | 3.13 | Message Broker |
| Docker | Latest | Execução do RabbitMQ |

---

## 🚀 Como Começar

```bash
# 1. Clone o repositório
git clone https://github.com/rian-fuzinelli/rian-fuzinelli.git
cd rian-fuzinelli/RabbitMQ-CSharp-Course

# 2. Suba o RabbitMQ com Docker
docker run -d \
  --name rabbitmq \
  -p 5672:5672 \
  -p 15672:15672 \
  rabbitmq:3.13-management

# 3. Acesse o Management UI
# http://localhost:15672
# Usuário: guest | Senha: guest
```

---

## 📐 Conceitos Fundamentais

### O que é um Message Broker?

Um **Message Broker** é um intermediário de software que permite que aplicações se comuniquem de forma **assíncrona**, **desacoplada** e **confiável**. Em vez de um serviço chamar outro diretamente, ele envia uma mensagem a um broker, que a armazena e entrega ao destinatário.

```
[Producer] --> [Message Broker] --> [Consumer]
```

### Por que usar RabbitMQ?

| Característica | Benefício |
|---|---|
| **Desacoplamento** | Producers e Consumers independentes |
| **Resiliência** | Mensagens persistidas em disco |
| **Escalabilidade** | Múltiplos consumers em paralelo |
| **Flexibilidade** | Vários padrões de roteamento |
| **Confiabilidade** | Confirmações de entrega (ACK/NACK) |

---

## 🗺️ Arquitetura do RabbitMQ

```
┌─────────────────────────────────────────────────────────────┐
│                         RabbitMQ Broker                     │
│                                                             │
│  [Producer] ──► [Exchange] ──► [Binding] ──► [Queue] ──► [Consumer]
│                    │                           │            │
│              (Direct/Fanout/              (Durable/        │
│               Topic/Headers)              Transient)       │
└─────────────────────────────────────────────────────────────┘
```

### Componentes principais:

- **Producer**: Aplicação que envia mensagens
- **Exchange**: Recebe mensagens do producer e as roteia para filas
- **Binding**: Regra que conecta um Exchange a uma Queue
- **Queue**: Buffer que armazena mensagens até serem consumidas
- **Consumer**: Aplicação que recebe e processa mensagens

---

## 📚 Estrutura de Cada Módulo

Cada módulo contém:

```
ModuloXX-Nome/
├── README.md          # Teoria, conceitos e explicações
└── src/
    ├── Producer/      # Projeto C# do produtor
    │   ├── Program.cs
    │   └── Producer.csproj
    └── Consumer/      # Projeto C# do consumidor
        ├── Program.cs
        └── Consumer.csproj
```

---

*Desenvolvido por [Rian Fuzinelli](https://linkedin.com/in/rian-fuzinelli) — Backend Software Developer*

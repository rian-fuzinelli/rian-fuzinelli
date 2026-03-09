# Módulo 02 — Configuração do Ambiente

## 🎯 Objetivos

- Instalar e executar o RabbitMQ com Docker
- Explorar o Management UI
- Criar e configurar um projeto .NET com RabbitMQ.Client
- Criar uma classe de conexão reutilizável

---

## 2.1 Instalando o RabbitMQ com Docker

A forma mais simples de executar o RabbitMQ em desenvolvimento é via Docker.

### Opção 1 — Docker simples

```bash
docker run -d \
  --name rabbitmq \
  -p 5672:5672 \
  -p 15672:15672 \
  rabbitmq:3.13-management
```

### Opção 2 — Docker Compose (recomendado)

Crie um arquivo `docker-compose.yml`:

```yaml
version: '3.8'

services:
  rabbitmq:
    image: rabbitmq:3.13-management
    container_name: rabbitmq
    ports:
      - "5672:5672"    # AMQP protocol
      - "15672:15672"  # Management UI
    environment:
      RABBITMQ_DEFAULT_USER: guest
      RABBITMQ_DEFAULT_PASS: guest
      RABBITMQ_DEFAULT_VHOST: /
    volumes:
      - rabbitmq_data:/var/lib/rabbitmq
    healthcheck:
      test: ["CMD", "rabbitmq-diagnostics", "ping"]
      interval: 10s
      timeout: 5s
      retries: 5

volumes:
  rabbitmq_data:
```

```bash
docker-compose up -d
```

### Verificando se está rodando

```bash
docker ps
# Deve mostrar o container rabbitmq em execução

docker logs rabbitmq
# Deve mostrar "Server startup complete"
```

---

## 2.2 Management UI

Acesse: **http://localhost:15672**

| Campo | Valor |
|---|---|
| Usuário | guest |
| Senha | guest |

### O que você pode fazer no Management UI:

- 👁️ Monitorar filas, exchanges e conexões em tempo real
- 📊 Ver métricas de throughput e latência
- ✉️ Publicar mensagens manualmente para testes
- 🔧 Criar/deletar filas, exchanges e bindings
- 👥 Gerenciar usuários e permissões
- 🌐 Gerenciar virtual hosts

---

## 2.3 Configurando o Projeto .NET

### Criando os projetos

```bash
# Criar solução
dotnet new sln -n RabbitMQCourse

# Criar projeto Producer
dotnet new console -n Producer -o src/Producer
dotnet sln add src/Producer/Producer.csproj

# Criar projeto Consumer
dotnet new console -n Consumer -o src/Consumer
dotnet sln add src/Consumer/Consumer.csproj

# Adicionar o pacote RabbitMQ.Client em ambos
dotnet add src/Producer/Producer.csproj package RabbitMQ.Client --version 6.8.1
dotnet add src/Consumer/Consumer.csproj package RabbitMQ.Client --version 6.8.1
```

### Estrutura do .csproj

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net8.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="RabbitMQ.Client" Version="6.8.1" />
  </ItemGroup>
</Project>
```

---

## 2.4 Configurações de Conexão

### Parâmetros de conexão

| Parâmetro | Padrão | Descrição |
|---|---|---|
| **HostName** | localhost | Endereço do broker |
| **Port** | 5672 | Porta AMQP |
| **UserName** | guest | Usuário |
| **Password** | guest | Senha |
| **VirtualHost** | / | Virtual host |
| **RequestedHeartbeat** | 60s | Intervalo de heartbeat |
| **AutomaticRecoveryEnabled** | true | Reconexão automática |
| **NetworkRecoveryInterval** | 10s | Intervalo de reconexão |

### Boas práticas de conexão

```csharp
var factory = new ConnectionFactory
{
    HostName = "localhost",
    Port = 5672,
    UserName = "guest",
    Password = "guest",
    VirtualHost = "/",
    
    // Reconexão automática em caso de falha de rede
    AutomaticRecoveryEnabled = true,
    NetworkRecoveryInterval = TimeSpan.FromSeconds(10),
    
    // Heartbeat para detectar conexões mortas
    RequestedHeartbeat = TimeSpan.FromSeconds(60),
    
    // Dispatch assíncrono de consumers (recomendado)
    DispatchConsumersAsync = true
};
```

---

## 2.5 Modelo de Objetos do RabbitMQ.Client

```
ConnectionFactory
    └── IConnection         (1 por aplicação)
            └── IModel      (1 por thread/operação)
                   ├── QueueDeclare()
                   ├── ExchangeDeclare()
                   ├── QueueBind()
                   ├── BasicPublish()
                   └── BasicConsume()
```

### Importante:

- **IConnection**: Representa a conexão TCP com o broker. Pesada, crie apenas uma por processo.
- **IModel (Channel)**: Representa um canal virtual multiplexado na conexão. Leve, mas **não é thread-safe**.
- Crie um canal por thread ou operação concorrente.

---

## 2.6 Variáveis de Ambiente e Configuração

Em produção, nunca hardcode credenciais. Use variáveis de ambiente:

```csharp
var factory = new ConnectionFactory
{
    HostName = Environment.GetEnvironmentVariable("RABBITMQ_HOST") ?? "localhost",
    Port = int.Parse(Environment.GetEnvironmentVariable("RABBITMQ_PORT") ?? "5672"),
    UserName = Environment.GetEnvironmentVariable("RABBITMQ_USER") ?? "guest",
    Password = Environment.GetEnvironmentVariable("RABBITMQ_PASS") ?? "guest",
    VirtualHost = Environment.GetEnvironmentVariable("RABBITMQ_VHOST") ?? "/"
};
```

Ou use `appsettings.json` com o padrão do .NET:

```json
{
  "RabbitMQ": {
    "HostName": "localhost",
    "Port": 5672,
    "UserName": "guest",
    "Password": "guest",
    "VirtualHost": "/"
  }
}
```

---

## 2.7 Comandos Úteis do Docker

```bash
# Ver status do container
docker ps

# Ver logs em tempo real
docker logs -f rabbitmq

# Parar o RabbitMQ
docker stop rabbitmq

# Iniciar novamente
docker start rabbitmq

# Acessar o container
docker exec -it rabbitmq bash

# Listar filas via CLI
docker exec rabbitmq rabbitmqctl list_queues

# Listar exchanges via CLI
docker exec rabbitmq rabbitmqctl list_exchanges

# Listar usuários via CLI
docker exec rabbitmq rabbitmqctl list_users

# Criar usuário via CLI
docker exec rabbitmq rabbitmqctl add_user myuser mypassword
docker exec rabbitmq rabbitmqctl set_user_tags myuser administrator
docker exec rabbitmq rabbitmqctl set_permissions -p / myuser ".*" ".*" ".*"
```

---

## ✅ Resumo do Módulo

Neste módulo você configurou:

1. **RabbitMQ** executando em Docker com Management UI
2. **Projeto .NET 8** com o pacote RabbitMQ.Client
3. **ConnectionFactory** com as melhores práticas de conexão
4. Entendeu a hierarquia **Connection → Channel**

---

## ➡️ Próximo Módulo

[Módulo 03 — Hello World →](../Modulo03-HelloWorld/README.md)

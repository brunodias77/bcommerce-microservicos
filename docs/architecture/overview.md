# Visão Geral da Arquitetura

O projeto **Bcommerce** é uma plataforma de e-commerce construída sobre uma arquitetura de microsserviços, utilizando tecnologias modernas e padrões robustos para garantir escalabilidade, resiliência e manutenibilidade.

## Tecnologias Principais

*   **Plataforma**: .NET 8
*   **Banco de Dados**: PostgreSQL (um database por serviço)
*   **Mensageria**: RabbitMQ (com MassTransit)
*   **Cache**: Redis
*   **Observabilidade**: OpenTelemetry, Serilog, Seq, Jaeger
*   **Containerização**: Docker & Docker Compose

## Princípios Arquiteturais

### Clean Architecture
Cada microsserviço segue a **Clean Architecture** (ou Onion Architecture), dividindo as responsabilidades em camadas concêntricas:

1.  **Domain**: O núcleo do sistema. Contém Entidades, Value Objects, Agregados, Eventos de Domínio e Interfaces de Repositório. Não possui dependências externas.
2.  **Application**: Casos de uso da aplicação (CQRS com MediatR). Orquestra o fluxo de dados entre o mundo externo e o Domínio.
3.  **Infrastructure**: Implementação de interfaces (EF Core, Gateways de Pagamento, Serviços de Email, MassTransit).
4.  **API**: A camada de entrada (Controllers, Minimal APIs, Consumers).

### Domain-Driven Design (DDD)
Adotamos DDD tático para modelagem da complexidade do negócio:
*   **Agregados**: Garantem a consistência transacional imediata.
*   **Entidades e Value Objects**: Modelagem rica de comportamento.
*   **Eventos de Domínio**: Desacoplamento entre partes do mesmo domínio (Side Effects).

### Event-Driven Architecture (EDA)
A comunicação entre microsserviços é predominantemente assíncrona, baseada em eventos (Integration Events), garantindo baixo acoplamento.

## Building Blocks
O projeto utiliza um conjunto de bibliotecas compartilhadas (`Bcommerce.BuildingBlocks`) para padronizar implementações transversais (Cross-Cutting Concerns) como:
*   Log e Monitoramento
*   Autenticação e Autorização
*   Configuração do MassTransit e Outbox
*   Tratamento de Exceções Global
*   Abstrações de Domínio e Data

## Fluxo de Desenvolvimento
1.  Definição do contrato (API/Eventos).
2.  Modelagem do Domínio.
3.  Implementação dos Casos de Uso (Application).
4.  Persistência e Infraestrutura.
5.  Exposição via API.

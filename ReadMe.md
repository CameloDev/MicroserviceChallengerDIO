# Desafio Técnico - Microserviços DIO

## Descrição do Desafio
Desenvolver uma aplicação com arquitetura de microserviços para gerenciamento de estoque de produtos e vendas em uma plataforma de e-commerce. A comunicação entre os microserviços ocorre via **RabbitMQ**, e a autenticação é realizada via **JWT**.

O sistema é composto por dois microserviços principais:  

- **Gestão de Estoque**: cadastra produtos e controla o estoque.  
- **Gestão de Vendas**: realiza pedidos, valida estoque e notifica o serviço de estoque sobre vendas.

## Tecnologias Utilizadas
- .NET Core (C#)  
- Entity Framework Core  
- RESTful API  
- RabbitMQ (para comunicação assíncrona)  
- JWT (para autenticação)  
- Banco de dados relacional (MySQL)  

## Arquitetura do Sistema
### Microserviço 1: Gestão de Estoque
- Cadastro de produtos (nome, descrição, preço e quantidade)  
- Consulta de produtos e estoque disponível  
- Atualização automática de estoque após venda (integração com microserviço de vendas)  

### Microserviço 2: Gestão de Vendas
- Criação de pedidos com validação do estoque  
- Consulta de pedidos e seus status  
- Notificação ao microserviço de estoque via RabbitMQ  

### API Gateway
- Ponto de entrada único para todas as requisições  
- Redireciona chamadas para o microserviço apropriado  

### Comunicação Assíncrona
- RabbitMQ é usado para enviar notificações de vendas e atualizar o estoque  

### Autenticação
- JWT garante que apenas usuários autenticados possam interagir com os microserviços  

## Funcionalidades Implementadas Até o Momento
- Microserviço de **Vendas**:  
  - Criação de pedidos e persistência no banco de dados  
  - Notificação ao microserviço de estoque via RabbitMQ  
  - Listagem de pedidos com itens incluídos (`Include` no EF Core)  

- Microserviço de **Estoque** (em desenvolvimento):  
  - Estrutura básica para cadastro e consulta de produtos  

- Configuração de **RabbitMQ** para comunicação assíncrona entre serviços  
- Configuração de **Entity Framework** com MySQL  
- Configuração de autenticação via **JWT**  

## Estrutura do Projeto
- `VendasService/Models`: entidades `Pedido` e `PedidoItem`  
- `VendasService/Data`: `VendasDbContext`  
- `VendasService/Services`: `VendaService` e `RabbitMQService`  
- `VendasService/Controllers`: endpoints para criar e consultar pedidos  

## Próximos Passos
- Finalizar microserviço de Estoque  
- Implementar API Gateway para centralizar requisições  
- Criar testes unitários para as funcionalidades principais  
- Implementar monitoramento e logs  
- Preparar para escalabilidade, adicionando novos microserviços (ex: pagamento, envio)  

## Observações
O projeto é parte de um **desafio técnico da DIO** e visa demonstrar conhecimento em **microserviços, comunicação assíncrona, autenticação segura e boas práticas de desenvolvimento em .NET Core**.

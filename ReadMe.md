# 📦 Desafio Técnico DIO — Arquitetura de Microserviços com .NET Core

Este projeto faz parte de um **desafio técnico** da DIO e consiste no desenvolvimento de uma aplicação com arquitetura de **microserviços** para gerenciamento de estoque de produtos e vendas em uma plataforma de e-commerce.

---

## 🏗 Arquitetura do Sistema

O sistema é composto por dois microserviços principais:

1. **Gestão de Estoque**
   - Cadastro de produtos (nome, descrição, preço e quantidade).
   - Consulta de produtos e estoque disponível.
   - Atualização automática de estoque após venda (via RabbitMQ).

2. **Gestão de Vendas**
   - Criação de pedidos com validação de estoque.
   - Consulta de pedidos e seus status.
   - Notificação ao microserviço de estoque via RabbitMQ.

### 🔌 Comunicação Assíncrona
- **RabbitMQ** é utilizado para enviar notificações de vendas e atualizar o estoque.

### 🔐 Autenticação
- A autenticação será implementada com **JWT**, garantindo que apenas usuários autenticados possam interagir com os microserviços.

### 🌐 API Gateway
- Será utilizado o **Ocelot** para centralizar as requisições, com documentação via **SwaggerForOcelot**.

---

## 🛠 Tecnologias Utilizadas
- [.NET Core (C#)](https://dotnet.microsoft.com/)
- [Entity Framework Core](https://docs.microsoft.com/en-us/ef/core/)
- [RESTful API](https://restfulapi.net/)
- [RabbitMQ](https://www.rabbitmq.com/) (comunicação assíncrona)
- [JWT](https://jwt.io/) (autenticação)
- [MySQL](https://www.mysql.com/) (banco relacional)
- [Ocelot](https://ocelot.readthedocs.io/en/latest/) (API Gateway)
- [Swagger](https://swagger.io/) e [SwaggerForOcelot](https://github.com/Burgyn/MMLib.SwaggerForOcelot)

---

## 📌 Status do Projeto

### Estado atual
- [x] Comunicação básica via RabbitMQ recebendo evento `venda_realizada` no microserviço de estoque.
- [x] Estrutura inicial dos microserviços de vendas e estoque.
- [x] Swagger configurado para documentação.
- [x] Ocelot configurado (SwaggerForOcelot ativo).
- [ ] JWT implementado.
- [ ] API Gateway finalizado (roteamento completo via Ocelot).
- [ ] Microserviço de Estoque finalizado.
- [ ] Fluxo de atualização automática de estoque concluído.

### Próximos passos
- [ ] Implementar autenticação JWT em ambos os microserviços.
- [ ] Configurar API Gateway (Ocelot) para rotear corretamente as requisições entre os serviços.
- [ ] Finalizar microserviço de Estoque:
  - [ ] CRUD completo de produtos.
  - [ ] Integração com o evento `venda_realizada` para atualização do estoque.
- [ ] Criar testes unitários para validação das funcionalidades principais.
- [ ] Adicionar logs e monitoramento básico.
- [ ] Revisar documentação no README.

---

## 📂 Estrutura do Projeto

```text
VendasService/
├── Models/
│   ├── Pedido.cs
│   ├── PedidoItem.cs
├── Data/
│   ├── VendasDbContext.cs
├── Services/
│   ├── VendaService.cs
│   ├── RabbitMQService.cs
├── Controllers/
│   ├── PedidoController.cs
```
## 🚀 Como Executar

### Pré-requisitos
- .NET SDK 8+
- MySQL
- RabbitMQ
- Ocelot

### Passos
1. Clonar o repositório:
  ```bash
  git clone https://github.com/camelodev/MicroserviceChallengerDIO.git
  ```
2. Configurar o banco de dados MySQL no `appsettings.json` de cada microserviço.
3. Iniciar o RabbitMQ.
4. Restaurar as dependências:
  ```bash
  dotnet restore
  ```
5. Executar os microserviços:
  ```bash
  dotnet run --project VendasService
  dotnet run --project EstoqueService
  ```

---

## 📜 Licença
Este projeto foi desenvolvido como parte de um desafio técnico da **DIO** e tem fins educacionais.

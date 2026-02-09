# Gestão de Pedidos Vizabel

Aplicação web ASP.NET Core MVC com .NET 8.0, usando SQL Server, Bootstrap para UI e jQuery para interatividade.

## Tecnologias Utilizadas

- **.NET 8.0** - Framework principal
- **ASP.NET Core MVC** - Arquitetura do projeto
- **Bootstrap 5.3.2** - Framework CSS para interface responsiva
- **jQuery 3.7.1** - Biblioteca JavaScript para interatividade
- **Entity Framework Core 8.0** - ORM para acesso a dados
- **SQL Server** - Banco de dados

## Configuração do Banco de Dados

A conexão com o SQL Server está configurada no arquivo `appsettings.json`:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=191.252.221.249;Database=VIZABEL;User Id=sa;Password=@3Xsojfb7;TrustServerCertificate=True;"
  }
}
```

## Estrutura do Projeto

```
GestaoPedidosVizabel/
├── Controllers/          # Controladores MVC
├── Data/                 # DbContext e configurações de dados
├── Models/              # Modelos de dados
├── Views/               # Views Razor
│   ├── Home/           # Views da página inicial
│   └── Shared/         # Layouts compartilhados
└── wwwroot/            # Arquivos estáticos (CSS, JS, imagens)
```

## Funcionalidades

- ✅ Menu lateral responsivo
- ✅ Layout moderno com Bootstrap
- ✅ Conexão configurada com SQL Server
- ✅ Estrutura base para gestão de pedidos

## Como Executar

1. Certifique-se de ter o .NET 8.0 SDK instalado
2. Restaure as dependências:
   ```bash
   dotnet restore
   ```
3. Restaure as bibliotecas front-end:
   ```bash
   libman restore
   ```
4. Execute o projeto:
   ```bash
   dotnet run
   ```
5. Acesse no navegador: `https://localhost:5001` ou `http://localhost:5000`

## Próximos Passos

- Implementar CRUD de Pedidos
- Implementar CRUD de Clientes
- Implementar CRUD de Produtos
- Adicionar autenticação e autorização
- Implementar relatórios

## Desenvolvido por

Sistema desenvolvido para Gestão de Pedidos Vizabel.


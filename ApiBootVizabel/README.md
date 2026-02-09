# ApiMensageria

API para envio de mensagens desenvolvida em .NET.

## Pré-requisitos

- .NET 8.0 SDK ou superior

## Executar

Na pasta raiz do repositório (onde está `ApiMensageria.sln`):

```bash
dotnet run --project ApiMensageria
```

Ou, para compilar e rodar só a API e evitar erros do projeto de testes no IDE, use sempre o projeto explícito. Se aparecer erro de *atributos duplicados*, limpe e rode de novo:

```bash
dotnet clean
dotnet run --project ApiMensageria
```

A API estará disponível em:

- HTTP: `http://localhost:5000`
- HTTPS: `https://localhost:5001`

## Endpoints

### POST /enviarmensagem

Envia uma mensagem para um contato.

**Body:**

```json
{
  "idContato": "010101asdfasd101",
  "mensagem": "20asdf20asdfa20"
}
```

**Resposta de Sucesso (200):**

```json
{
  "idContato": "010101asdfasd101",
  "mensagem": "20asdf20asdfa20",
  "erro": false
}
```

**Resposta de Erro (400):**

```json
{
  "idContato": "010101asdfasd101",
  "mensagem": "20asdf20asdfa20",
  "erro": true
}
```

## Swagger

Quando executado em modo Development, o Swagger estará disponível em:

- `https://localhost:5001/swagger`

## Testes unitários

Os testes ficam no projeto `ApiMensageria.Tests` (xUnit + Moq), fora da solution, para que `dotnet run` compile só a API.

**Erros "Xunit" ou "Moq" não podem ser encontrados**  
Aparecem quando o IDE compila o projeto de testes e os pacotes NuGet ainda não foram restaurados. Faça uma das opções:

1. **Restaurar pacotes** (com internet estável) e depois rodar os testes:
   ```bash
   dotnet restore ApiMensageria.Tests/ApiMensageria.Tests.csproj
   dotnet test ApiMensageria.Tests/ApiMensageria.Tests.csproj
   ```
2. **Compilar só a API** (não compila o projeto de testes):
   ```bash
   dotnet run --project ApiMensageria
   ```
3. **Ocultar o projeto de testes do Cursor** (para o IDE parar de compilá-lo e os erros sumirem):
   - Crie na **raiz** do repositório (mesma pasta do `ApiMensageria.sln`) um arquivo chamado **`.cursorignore`**.
   - Dentro do arquivo, coloque apenas esta linha:
   ```text
   ApiMensageria.Tests/
   ```
   - Salve e recarregue o Cursor se necessário. O projeto de testes deixa de ser incluído no build.

**Executar os testes** (na pasta raiz do repositório, após `dotnet restore`):

```bash
dotnet restore ApiMensageria.Tests/ApiMensageria.Tests.csproj
dotnet test ApiMensageria.Tests/ApiMensageria.Tests.csproj
```

**Executar com cobertura (coverlet):**

```bash
dotnet test --collect:"XPlat Code Coverage"
```

Os testes cobrem:
- **MensagemController**: validação de request (campos vazios/null), resposta de sucesso, exceções e `Pedidofinalizado`.
- **FluxoAtendimentoService**: estado inicial (menu), encerramento com "0", opções 1 e 2 do menu, opção inválida e isolamento entre contatos.

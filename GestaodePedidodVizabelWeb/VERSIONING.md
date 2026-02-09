# Guia de Versionamento

Este documento explica como versionar o projeto Gestão de Pedidos Vizabel.

## Sistema de Versionamento

O projeto utiliza **Semantic Versioning (SemVer)** no formato `MAJOR.MINOR.PATCH`:
- **MAJOR** (1.0.0): Mudanças incompatíveis na API
- **MINOR** (0.1.0): Adição de funcionalidades de forma compatível
- **PATCH** (0.0.1): Correções de bugs compatíveis

## Arquivos de Versão

### 1. GestaoPedidosVizabel.csproj
Contém as propriedades de versão do assembly:
```xml
<Version>1.0.0</Version>
<AssemblyVersion>1.0.0.0</AssemblyVersion>
<FileVersion>1.0.0.0</FileVersion>
```

### 2. CHANGELOG.md
Documenta todas as mudanças por versão.

### 3. VERSION.md
Contém informações sobre a versão atual e histórico.

## Como Versionar

### Passo 1: Atualizar a Versão no .csproj

Para uma correção de bug (PATCH):
```xml
<Version>1.0.1</Version>
<AssemblyVersion>1.0.1.0</AssemblyVersion>
<FileVersion>1.0.1.0</FileVersion>
```

Para uma nova funcionalidade (MINOR):
```xml
<Version>1.1.0</Version>
<AssemblyVersion>1.1.0.0</AssemblyVersion>
<FileVersion>1.1.0.0</FileVersion>
```

Para uma mudança incompatível (MAJOR):
```xml
<Version>2.0.0</Version>
<AssemblyVersion>2.0.0.0</AssemblyVersion>
<FileVersion>2.0.0.0</FileVersion>
```

### Passo 2: Atualizar o CHANGELOG.md

Adicione uma nova seção no topo do arquivo:
```markdown
## [1.0.1] - 2024-12-16

### Corrigido
- Correção de bug na validação de CPF

### Alterado
- Melhoria na performance do carregamento de clientes
```

### Passo 3: Atualizar o VERSION.md

Atualize a versão atual e adicione ao histórico.

### Passo 4: Criar Tag Git

```bash
# Criar tag anotada
git tag -a v1.0.1 -m "Versão 1.0.1 - Correção de bugs"

# Enviar tag para o repositório remoto
git push origin v1.0.1

# Ou enviar todas as tags
git push origin --tags
```

### Passo 5: Commit das Mudanças

```bash
git add .
git commit -m "Bump version to 1.0.1"
git push
```

## Exibir Versão na Aplicação (Opcional)

Para exibir a versão na interface, você pode criar um helper ou usar:

```csharp
// Em Program.cs ou um Controller
var version = typeof(Program).Assembly.GetName().Version;
ViewBag.Version = version?.ToString();
```

E no layout:
```html
<footer>
    <small>Versão @ViewBag.Version</small>
</footer>
```

## Comandos Úteis

### Ver versão atual
```bash
dotnet build /p:Version=1.0.0
```

### Listar tags Git
```bash
git tag -l
```

### Ver detalhes de uma tag
```bash
git show v1.0.0
```

### Deletar tag (se necessário)
```bash
git tag -d v1.0.1
git push origin :refs/tags/v1.0.1
```

## Exemplo de Workflow Completo

```bash
# 1. Fazer as alterações no código
# 2. Atualizar versão no .csproj para 1.0.1
# 3. Atualizar CHANGELOG.md
# 4. Commit
git add .
git commit -m "Correção: ajuste na validação de CPF"

# 5. Criar tag
git tag -a v1.0.1 -m "Versão 1.0.1"

# 6. Push
git push
git push origin v1.0.1
```





















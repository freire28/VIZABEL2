# Versão do Projeto

## Versão Atual: 1.0.0

### Formato de Versionamento
Este projeto utiliza [Semantic Versioning](https://semver.org/lang/pt-BR/) (SemVer):
- **MAJOR.MINOR.PATCH** (ex: 1.0.0)
  - **MAJOR**: Mudanças incompatíveis na API
  - **MINOR**: Adição de funcionalidades de forma compatível
  - **PATCH**: Correções de bugs compatíveis

### Histórico de Versões

#### 1.0.0 (2024-12-15)
- Versão inicial do projeto
- Sistema de cadastro de clientes
- Layout responsivo implementado
- Validação de CPF/CNPJ

### Como Atualizar a Versão

1. **No arquivo `.csproj`**:
   ```xml
   <Version>1.0.1</Version>
   <AssemblyVersion>1.0.1.0</AssemblyVersion>
   <FileVersion>1.0.1.0</FileVersion>
   ```

2. **No arquivo `CHANGELOG.md`**:
   - Adicione uma nova seção com a nova versão
   - Documente as mudanças

3. **Criar uma tag Git**:
   ```bash
   git tag -a v1.0.1 -m "Versão 1.0.1"
   git push origin v1.0.1
   ```





















using ApiMensageria.Models;
using System.Linq;
using System.Text.RegularExpressions;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Jpeg;


namespace ApiMensageria.Services;

public class FluxoAtendimentoService : IFluxoAtendimentoService
{
    private readonly Dictionary<string, ContatoFluxo> _contatos = new();
    private readonly object _lock = new();
    private readonly IServiceProvider _serviceProvider;

    public FluxoAtendimentoService(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public async Task<ProcessarMensagemResponse> ProcessarMensagemAsync(string idContato, string mensagem, string nomecontato)
    {
        ContatoFluxo contato;
        
        lock (_lock)
        {
            // Obtém ou cria o contato
            if (!_contatos.TryGetValue(idContato, out contato!))
            {
                contato = new ContatoFluxo
                {
                    IdContato = idContato,
                    Estado = EstadoFluxo.Inicial
                };
                _contatos[idContato] = contato;
            }

            contato.UltimaInteracao = DateTime.Now;
        }

        // Processa a mensagem baseado no estado atual (fora do lock para permitir async)
        return await ProcessarPorEstadoAsync(contato, mensagem, nomecontato);
    }

    // Método auxiliar para criar resposta
    private ProcessarMensagemResponse CriarResposta(string mensagem, bool pedidoFinalizado = false)
    {
        return new ProcessarMensagemResponse
        {
            Mensagem = mensagem,
            PedidoFinalizado = pedidoFinalizado
        };
    }

    private async Task<ProcessarMensagemResponse> ProcessarPorEstadoAsync(ContatoFluxo contato, string mensagem, string nomeContato)
    {
        // Verifica se a mensagem é "0" para encerrar atendimento em qualquer momento
        if (mensagem.Trim() == "0")
        {
            contato.Estado = EstadoFluxo.Encerrado;
            return CriarResposta($"👋 Atendimento encerrado, {contato.Nome ?? "Usuário"}!\n\nObrigado por utilizar o Boot de Pedidos Vizabel!");
        }

        switch (contato.Estado)
        {
            case EstadoFluxo.Inicial:
                // Primeira mensagem - extrai nome e mostra menu
                contato.Nome = nomeContato;
                contato.Estado = EstadoFluxo.AguardandoOpcao;
                return CriarResposta(GerarMenuInicial(contato.Nome));

            case EstadoFluxo.AguardandoOpcao:
                return CriarResposta(ProcessarOpcaoMenu(contato, mensagem));

            case EstadoFluxo.SelecionandoCliente:
                return await ProcessarSelecaoClienteAsync(contato, mensagem);

            case EstadoFluxo.ConfirmandoCliente:
                return CriarResposta(ProcessarConfirmacaoCliente(contato, mensagem));

            case EstadoFluxo.CadastrandoClienteNome:
                return CriarResposta(ProcessarCadastroClienteNome(contato, mensagem));

            case EstadoFluxo.CadastrandoClienteCpfCnpj:
                return CriarResposta(ProcessarCadastroClienteCpfCnpj(contato, mensagem));

            case EstadoFluxo.CadastrandoClienteTelefone:
                return CriarResposta(ProcessarCadastroClienteTelefone(contato, mensagem));

            case EstadoFluxo.CadastrandoClienteEndereco:
                return await ProcessarCadastroClienteEnderecoAsync(contato, mensagem);

            case EstadoFluxo.CadastrandoClienteTemplate:
                return CriarResposta(ProcessarCadastroClienteTemplate(contato, mensagem));

            case EstadoFluxo.AguardandoTemplatePreenchido:
                return await ProcessarTemplatePreenchidoAsync(contato, mensagem);

            case EstadoFluxo.BuscandoProduto:
                return await ProcessarBuscaProdutoAsync(contato, mensagem);

            case EstadoFluxo.SelecionandoTamanho:
                return await ProcessarSelecaoTamanhoAsync(contato, mensagem);

            case EstadoFluxo.PerguntandoInserirImagem:
                return CriarResposta(ProcessarPerguntandoInserirImagem(contato, mensagem));

            case EstadoFluxo.AguardandoImagem:
                return await ProcessarAguardandoImagemAsync(contato, mensagem);

            case EstadoFluxo.PerguntandoMaisProduto:
                return CriarResposta(ProcessarPerguntandoMaisProduto(contato, mensagem));

            case EstadoFluxo.FinalizandoPedido:
                return await ProcessarFinalizandoPedidoAsync(contato, mensagem);

            case EstadoFluxo.SelecionandoFormaPagamento:
                return await ProcessarSelecaoFormaPagamentoAsync(contato, mensagem);

            case EstadoFluxo.PerguntandoEmitirNfe:
                return await ProcessarEmitirNfeAsync(contato, mensagem);

            case EstadoFluxo.FazendoPedido:
                return CriarResposta(ProcessarFazendoPedido(contato, mensagem));

            case EstadoFluxo.VerificandoPedido:
                return CriarResposta(ProcessarVerificandoPedido(contato, mensagem));

            case EstadoFluxo.VerificandoPedidoTipoBusca:
                return CriarResposta(ProcessarVerificandoPedidoTipoBusca(contato, mensagem));

            case EstadoFluxo.VerificandoPedidoPorCodigo:
                return await ProcessarVerificandoPedidoPorCodigoAsync(contato, mensagem);

            case EstadoFluxo.VerificandoPedidoPorCliente:
                return CriarResposta(ProcessarVerificandoPedidoPorCliente(contato, mensagem));

            case EstadoFluxo.VerificandoPedidoCpfCnpj:
                return await ProcessarVerificandoPedidoCpfCnpjAsync(contato, mensagem);

            case EstadoFluxo.VerificandoPedidoSelecionarCliente:
                return await ProcessarVerificandoPedidoSelecionarClienteAsync(contato, mensagem);

            case EstadoFluxo.VerificandoPedidoListarPedidos:
                return await ProcessarVerificandoPedidoListarPedidosAsync(contato, mensagem);

            case EstadoFluxo.VerificandoPedidoSelecionarPedido:
                return await ProcessarVerificandoPedidoSelecionarPedidoAsync(contato, mensagem);

            case EstadoFluxo.Encerrado:
                // Se encerrado, reinicia o fluxo
                contato.Estado = EstadoFluxo.Inicial;
                contato.Nome = nomeContato;
                contato.Estado = EstadoFluxo.AguardandoOpcao;
                return CriarResposta(GerarMenuInicial(contato.Nome));

            default:
                contato.Estado = EstadoFluxo.Inicial;
                return CriarResposta(GerarMenuInicial(contato.Nome ?? "Usuário"));
        }
    }

    private string ProcessarOpcaoMenu(ContatoFluxo contato, string mensagem)
    {
        var opcao = mensagem.Trim();

        switch (opcao)
        {
            case "1":
                contato.Estado = EstadoFluxo.SelecionandoCliente;
                return GerarMenuSelecaoCliente();

            case "2":
                contato.Estado = EstadoFluxo.VerificandoPedidoTipoBusca;
                return "🔍 Você escolheu: Verificar Andamento de Pedido\n\n" +
                       "Como deseja buscar o pedido?\n\n" +
                       "1️⃣ Buscar pedido pelo Código\n" +
                       "2️⃣ Buscar pelo nome do Cliente\n\n" +
                       "0️⃣ Encerrar";

            case "0":
                contato.Estado = EstadoFluxo.Encerrado;
                return $"👋 Atendimento encerrado, {contato.Nome}!\n\nObrigado por utilizar o Boot de Pedidos Vizabel!";

            default:
                return $"❌ Opção inválida. Por favor, escolha uma opção válida:\n\n{GerarMenuInicial(contato.Nome ?? "Usuário")}";
        }
    }

    private async Task<ProcessarMensagemResponse> ProcessarSelecaoClienteAsync(ContatoFluxo contato, string mensagem)
    {
        var opcao = mensagem.Trim();

        // 0 sempre encerra
        if (opcao == "0")
        {
            contato.Estado = EstadoFluxo.Encerrado;
            return CriarResposta($"👋 Atendimento encerrado, {contato.Nome}!\n\nObrigado por utilizar o Boot de Pedidos Vizabel!");
        }

        // Quando há lista de clientes exibida, priorizar seleção por número (1, 2, 3...) da lista
        if (contato.ClientesEncontrados != null && contato.ClientesEncontrados.Count > 0 && int.TryParse(opcao, out int numeroCliente))
        {
            var indice = numeroCliente - 1;
            if (indice >= 0 && indice < contato.ClientesEncontrados.Count)
            {
                var clienteSelecionado = contato.ClientesEncontrados[indice];
                contato.ClienteSelecionadoId = clienteSelecionado.IdCliente;
                contato.ClienteEncontradoNome = clienteSelecionado.NomeExibicao;
                contato.ClienteEncontradoCpfCnpj = clienteSelecionado.CpfCnpj;
                contato.Estado = EstadoFluxo.ConfirmandoCliente;

                var cpfCnpjFormatado = !string.IsNullOrEmpty(clienteSelecionado.CpfCnpj)
                    ? FormatarCpfCnpj(clienteSelecionado.CpfCnpj)
                    : "Não informado";

                return CriarResposta($"✅ Cliente encontrado!\n\n" +
                       $"Nome: {clienteSelecionado.NomeExibicao}\n" +
                       $"CPF/CNPJ: {cpfCnpjFormatado}\n\n" +
                       $"Digite um número:\n\n" +
                       $"1️⃣ Confirmar o cliente\n" +
                       $"2️⃣ Pesquisar Novamente\n\n" +
                       $"0️⃣ Encerrar");
            }
        }

        // Menu 1 e 2 (Novo Cliente / Template) só quando NÃO há lista ou quando o número não é da lista
        switch (opcao)
        {
            case "1":
                // Novo Cliente
                contato.ClientesEncontrados = null; // Limpa lista anterior
                contato.UsandoTemplate = false;
                contato.ClienteNome = null;
                contato.ClienteCpfCnpj = null;
                contato.ClienteTelefone = null;
                contato.ClienteEndereco = null;
                contato.Estado = EstadoFluxo.CadastrandoClienteNome;
                return CriarResposta("👤 Novo Cliente\n\nPor favor, informe o Nome ou Razão Social do cliente:");

            case "2":
                // Novo Cliente usando Template - envia o template para o usuário preencher
                contato.ClientesEncontrados = null; // Limpa lista anterior
                contato.UsandoTemplate = true;
                contato.ClienteNome = null;
                contato.ClienteCpfCnpj = null;
                contato.ClienteTelefone = null;
                contato.ClienteEndereco = null;
                contato.Estado = EstadoFluxo.AguardandoTemplatePreenchido;
                return CriarResposta("*Cadastro de Cliente usando Template*!\n" +
                       "Copie o template abaixo incluindo a palavra [TEMPLATE] e coloque os dados depois dos dois pontos *(:)* \n\n" +
                       "[TEMPLATE]\n" +
                       "[CPF_CNPJ]:\n" +
                       "[NOME]:\n" +
                       "[ENDERECO]:\n" +
                       "[CEP]:\n" +
                       "[CIDADE]:\n\n" +
                       "0️⃣ Encerrar");
        }

        // Se não for opção do menu nem seleção da lista, busca no banco de dados
        return await BuscarClienteNoBancoAsync(contato, mensagem);
    }

    private async Task<ProcessarMensagemResponse> BuscarClienteNoBancoAsync(ContatoFluxo contato, string filtro)
    {
        using var scope = _serviceProvider.CreateScope();
        var clienteService = scope.ServiceProvider.GetRequiredService<IClienteService>();
        
        var clientes = await clienteService.BuscarClientesAsync(filtro);

        if (clientes == null || clientes.Count == 0)
        {
            return CriarResposta($"❌ Nenhum cliente encontrado com: {filtro}\n\n{GerarMenuSelecaoCliente()}");
        }

        if (clientes.Count == 1)
        {
            // Cliente único encontrado, mostra opções de confirmação
            var cliente = clientes[0];
            contato.ClienteSelecionadoId = cliente.IdCliente;
            contato.ClienteEncontradoNome = cliente.NomeExibicao;
            contato.ClienteEncontradoCpfCnpj = cliente.CpfCnpj;
            contato.Estado = EstadoFluxo.ConfirmandoCliente;
            
            var cpfCnpjFormatado = !string.IsNullOrEmpty(cliente.CpfCnpj) 
                ? FormatarCpfCnpj(cliente.CpfCnpj) 
                : "Não informado";
            
            return CriarResposta($"✅ Cliente encontrado!\n\n" +
                   $"Nome: {cliente.NomeExibicao}\n" +
                   $"CPF/CNPJ: {cpfCnpjFormatado}\n\n" +
                   $"Digite um número:\n\n" +
                   $"1️⃣ Confirmar o cliente\n" +
                   $"2️⃣ Pesquisar Novamente\n\n" +
                   $"0️⃣ Encerrar");
        }

        // Múltiplos clientes encontrados - armazena a lista para seleção posterior
        contato.ClientesEncontrados = clientes.Take(20).ToList();
        var listaClientes = string.Join("\n", contato.ClientesEncontrados.Select((c, i) => $"{i + 1}. {c.NomeExibicao} - {c.CpfCnpj}"));
        return CriarResposta($"🔍 Foram encontrados {clientes.Count} cliente(s):\n\n{listaClientes}\n\nDigite o número do cliente desejado ou faça uma nova busca:");
    }

    private string ProcessarConfirmacaoCliente(ContatoFluxo contato, string mensagem)
    {
        var opcao = mensagem.Trim();

        switch (opcao)
        {
            case "1":
                // Confirma o cliente e já solicita descrição do produto
                // Inicializa a lista de produtos selecionados
                contato.ProdutosSelecionados = new List<ProdutoSelecionado>();
                contato.Estado = EstadoFluxo.BuscandoProduto;
                return $"✅ Cliente confirmado: {contato.ClienteEncontradoNome}\n\nAgora Vamos Cadastrar os Produtos!\n\nDigite a descrição do produto:\n\n";
            
            case "2":
                // Volta para pesquisar cliente novamente
                contato.ClienteSelecionadoId = null;
                contato.ClienteEncontradoNome = null;
                contato.ClienteEncontradoCpfCnpj = null;
                contato.Estado = EstadoFluxo.SelecionandoCliente;
                return GerarMenuSelecaoCliente();
            
            case "0":
                contato.Estado = EstadoFluxo.Encerrado;
                return $"👋 Atendimento encerrado, {contato.Nome}!\n\nObrigado por utilizar o Boot de Pedidos Vizabel!";
            
            default:
                return $"❌ Opção inválida. Por favor, escolha uma opção válida:\n\n" +
                       $"✅ Cliente encontrado: {contato.ClienteEncontradoNome}\n\n" +
                       $"Digite um número:\n\n" +
                       $"1️⃣ Confirmar o cliente\n" +
                       $"2️⃣ Pesquisar Novamente\n\n" +
                       $"0️⃣ Encerrar";
        }
    }

    private async Task<ProcessarMensagemResponse> ProcessarBuscaProdutoAsync(ContatoFluxo contato, string mensagem)
    {
        var opcao = mensagem.Trim();
        
        if (opcao == "0")
        {
            contato.Estado = EstadoFluxo.Encerrado;
            return CriarResposta($"👋 Atendimento encerrado, {contato.Nome}!\n\nObrigado por utilizar o Boot de Pedidos Vizabel!");
        }

        // Verifica PRIMEIRO se há produtos armazenados e se é uma seleção numérica
        if (contato.ProdutosEncontrados != null && contato.ProdutosEncontrados.Count > 0)
        {
            // Se há produtos armazenados, qualquer número é uma seleção de produto
            if (int.TryParse(opcao, out int numeroProduto))
            {
                var indice = numeroProduto - 1;
                if (indice >= 0 && indice < contato.ProdutosEncontrados.Count)
                {
                    using var scope = _serviceProvider.CreateScope();
                    var produtoService = scope.ServiceProvider.GetRequiredService<IProdutoService>();
                    
                    var produtoSelecionado = contato.ProdutosEncontrados[indice];
                    contato.ProdutoSelecionadoId = produtoSelecionado.IdProduto;
                    contato.GradeSelecionadaId = produtoSelecionado.IdGrade;
                    
                    // Busca os tamanhos disponíveis
                    if (contato.GradeSelecionadaId.HasValue && produtoSelecionado.IdGrade.HasValue)
                    {
                        var tamanhos = await produtoService.BuscarTamanhosDisponiveisAsync(
                            produtoSelecionado.IdProduto, 
                            produtoSelecionado.IdGrade.Value);

                        if (tamanhos != null && tamanhos.Count > 0)
                        {
                            // Armazena os tamanhos para seleção posterior
                            contato.TamanhosDisponiveis = tamanhos;
                            contato.Estado = EstadoFluxo.SelecionandoTamanho;

                            // Tamanhos em uma única linha separados por |
                            var listaTamanhos = string.Join(" | ", tamanhos.Select(t => t.Tamanho));

                            return CriarResposta($"✅ Produto selecionado: {produtoSelecionado.Descricao}\n\n" +
                                   $"📏 Digite o(s) tamanho(s) com a quantidade no formato:\n" +
                                   $"👉 P10 M10 G10 XG20\n\n" +
                                   $"Onde a letra é o tamanho e o número é a quantidade.\n" +
                                   $"Para múltiplos tamanhos, separe por espaço.\n\n" +
                                   $"Tamanhos disponíveis: {listaTamanhos}\n\n" +
                                   $"0️⃣ Encerrar");
                        }
                        else
                        {
                            // Produto sem tamanhos disponíveis - pede apenas quantidade
                            contato.TamanhosDisponiveis = null;
                            contato.ProdutosEncontrados = null;
                            contato.Estado = EstadoFluxo.SelecionandoTamanho;
                            return CriarResposta($"✅ Produto selecionado: {produtoSelecionado.Descricao}\n\n" +
                                   $"Este produto não possui tamanhos disponíveis.\n\n" +
                                   $"Digite apenas a quantidade do produto (ex: 10):\n\n" +
                                   $"0️⃣ Encerrar");
                        }
                    }
                    else
                    {
                        contato.ProdutosEncontrados = null;
                        return CriarResposta($"✅ Produto selecionado: {produtoSelecionado.Descricao}\n\n" +
                               $"❌ Produto sem grade associada.\n\n" +
                               $"Digite a descrição de outro produto:\n\n");
                    }
                }
                else
                {
                    // Número inválido, mas há produtos armazenados - mostra erro e lista novamente
                    var listaProdutosErro = string.Join("\n", contato.ProdutosEncontrados.Select((p, i) => 
                        $"{i + 1}. {p.Descricao} {(p.FabricacaoTerceirizada ? "(Terceirizado)" : "")}"));
                    return CriarResposta($"❌ Número inválido. Por favor, escolha um produto da lista:\n\n{listaProdutosErro}\n\n" +
                           $"Digite o número do produto desejado:\n\n" +
                           $"0️⃣ Encerrar");
                }
            }
        }

        // Se não houver produtos armazenados, verifica se é "1" para buscar novamente
        if (opcao == "1" && (contato.ProdutosEncontrados == null || contato.ProdutosEncontrados.Count == 0))
        {
            // Buscar novamente (quando não encontrou produtos anteriormente)
            contato.ProdutosEncontrados = null;
            return CriarResposta("Agora Vamos Cadastrar os Produtos!\n\nDigite a descrição do produto:\n\n");
        }

        // Se não for número e não for "1", faz nova busca de produtos
        using var scopeBusca = _serviceProvider.CreateScope();
        var produtoServiceBusca = scopeBusca.ServiceProvider.GetRequiredService<IProdutoService>();
        
        var produtos = await produtoServiceBusca.BuscarProdutosAsync(mensagem);

        if (produtos == null || produtos.Count == 0)
        {
            // Não encontrou produtos - oferece opções
            return CriarResposta($"❌ Nenhum produto encontrado com a descrição: {mensagem}\n\n" +
                   $"Digite um número:\n\n" +
                   $"1️⃣ Buscar Novamente\n" +
                   $"0️⃣ Encerrar");
        }

        // Armazena a lista de produtos encontrados para seleção posterior
        contato.ProdutosEncontrados = produtos.Take(20).ToList();

        // Produtos encontrados - lista os resultados
        var listaProdutos = string.Join("\n",
            contato.ProdutosEncontrados.Select((p, i) =>
                $"[ {i + 1} ] - {p.Descricao} {(p.FabricacaoTerceirizada ? "(Terceirizado)" : "")}"
            )
        );

        return CriarResposta($"✅ Foram encontrados {produtos.Count} produto(s):\n\n{listaProdutos}\n\n" +
               $"Digite o número do produto desejado ou faça uma nova busca:");
    }

    private Task<ProcessarMensagemResponse> ProcessarSelecaoTamanhoAsync(ContatoFluxo contato, string mensagem)
    {
        var opcao = mensagem.Trim();

        if (opcao == "0")
        {
            contato.Estado = EstadoFluxo.Encerrado;
            return Task.FromResult(CriarResposta($"👋 Atendimento encerrado, {contato.Nome}!\n\nObrigado por utilizar o Boot de Pedidos Vizabel!"));
        }

        // Se não há tamanhos disponíveis, aceita apenas número (quantidade)
        if (contato.TamanhosDisponiveis == null || contato.TamanhosDisponiveis.Count == 0)
        {
            if (int.TryParse(opcao, out int quantidade) && quantidade > 0)
            {
                // Salva o produto na lista
                SalvarProdutoSelecionado(contato, quantidade: quantidade);
                
                // Limpa dados temporários
                contato.QuantidadeProduto = null;
                contato.TamanhosSelecionados = null;
                contato.ProdutosEncontrados = null;
                contato.TamanhosDisponiveis = null;
                
                // Pergunta se deseja inserir imagem
                contato.Estado = EstadoFluxo.PerguntandoInserirImagem;
                return Task.FromResult(CriarResposta($"✅ Quantidade registrada: {quantidade}\n\n" +
                       $"Deseja inserir a imagem de layout do produto?\n\n" +
                       $"[ 1 ] Sim\n" +
                       $"[ 2 ] Não\n\n" +
                       $"0️⃣ Encerrar"));
            }
            else
            {
                return Task.FromResult(CriarResposta($"❌ Quantidade inválida. Por favor, digite apenas um número (ex: 10):\n\n" +
                       $"0️⃣ Encerrar"));
            }
        }

        // Se há tamanhos disponíveis, valida o formato e processa
        if (!ValidaTamanhos(opcao))
        {
            var listaTamanhos = string.Join(" | ", contato.TamanhosDisponiveis.Select(t => t.Tamanho));
            return Task.FromResult(CriarResposta($"❌ Formato inválido. Por favor, digite no formato:\n" +
                   $"👉 P10 M10 G10 XG20\n\n" +
                   $"Onde a letra é o tamanho e o número é a quantidade.\n" +
                   $"Para múltiplos tamanhos, separe por espaço.\n\n" +
                   $"Tamanhos disponíveis: {listaTamanhos}\n\n" +
                   $"0️⃣ Encerrar"));
        }

        // Processa os tamanhos e quantidades
        var tamanhosSelecionados = ProcessarTamanhosQuantidades(opcao, contato.TamanhosDisponiveis);
        
        if (tamanhosSelecionados == null || tamanhosSelecionados.Count == 0)
        {
            var listaTamanhos = string.Join(" | ", contato.TamanhosDisponiveis.Select(t => t.Tamanho));
            return Task.FromResult(CriarResposta($"❌ Um ou mais tamanhos informados não estão disponíveis.\n\n" +
                   $"Tamanhos disponíveis: {listaTamanhos}\n\n" +
                   $"Digite novamente no formato P10 M10 G10:\n\n" +
                   $"0️⃣ Encerrar"));
        }

        // Salva o produto na lista
        SalvarProdutoSelecionado(contato, tamanhosQuantidades: tamanhosSelecionados);
        
        // Limpa dados temporários
        contato.TamanhosSelecionados = null;
        contato.QuantidadeProduto = null;
        contato.ProdutosEncontrados = null;
        contato.TamanhosDisponiveis = null;
        
        // Pergunta se deseja inserir imagem
        contato.Estado = EstadoFluxo.PerguntandoInserirImagem;
        var resumo = string.Join(", ", tamanhosSelecionados.Select(tq => $"{tq.Tamanho}{tq.Quantidade}"));
        return Task.FromResult(CriarResposta($"✅ Tamanhos e quantidades registrados: {resumo}\n\n" +
               $"Deseja inserir a imagem de layout do produto?\n\n" +
               $"[ 1 ] Sim\n" +
               $"[ 2 ] Não\n\n" +
               $"0️⃣ Encerrar"));
    }

    private void SalvarProdutoSelecionado(ContatoFluxo contato, List<TamanhoQuantidade>? tamanhosQuantidades = null, int? quantidade = null)
    {
        if (contato.ProdutosSelecionados == null)
        {
            contato.ProdutosSelecionados = new List<ProdutoSelecionado>();
        }

        // Busca a descrição do produto
        string? descricao = null;
        if (contato.ProdutosEncontrados != null && contato.ProdutoSelecionadoId.HasValue)
        {
            var produto = contato.ProdutosEncontrados.FirstOrDefault(p => p.IdProduto == contato.ProdutoSelecionadoId.Value);
            descricao = produto?.Descricao;
        }

        var produtoSelecionado = new ProdutoSelecionado
        {
            IdProduto = contato.ProdutoSelecionadoId ?? 0,
            IdGrade = contato.GradeSelecionadaId,
            Descricao = descricao,
            TamanhosQuantidades = tamanhosQuantidades,
            Quantidade = quantidade
        };

        contato.ProdutosSelecionados.Add(produtoSelecionado);
    }

    private string ProcessarPerguntandoInserirImagem(ContatoFluxo contato, string mensagem)
    {
        // Normaliza a opção: aceita "2", "[ 2 ]", "2 ]", etc. (extrai o primeiro dígito)
        var opcao = mensagem.Trim();
        var primeiroDigito = opcao.Length > 0 && char.IsDigit(opcao[0])
            ? opcao[0].ToString()
            : (opcao.Length > 0 ? new string(opcao.Where(char.IsDigit).Take(1).ToArray()) : "");

        if (opcao == "0" || primeiroDigito == "0")
        {
            contato.Estado = EstadoFluxo.Encerrado;
            return $"👋 Atendimento encerrado, {contato.Nome}!\n\nObrigado por utilizar o Boot de Pedidos Vizabel!";
        }

        if (opcao == "1" || primeiroDigito == "1")
        {
            // Usuário quer inserir imagem - aguarda o envio
            contato.Estado = EstadoFluxo.AguardandoImagem;
            return "📷 Por favor, envie a imagem de layout do produto:\n\n" +
                   "0️⃣ Encerrar";
        }
        if (opcao == "2" || primeiroDigito == "2")
        {
            // Usuário não quer inserir imagem - segue para perguntar se deseja incluir mais produtos
            contato.Estado = EstadoFluxo.PerguntandoMaisProduto;
            return "Deseja incluir mais um produto?\n\n" +
                   $"1️⃣ Sim\n" +
                   $"2️⃣ Finalizar pedido\n\n" +
                   $"0️⃣ Encerrar";
        }

        return $"❌ Opção inválida. Por favor, escolha uma opção:\n\n" +
               $"Deseja inserir a imagem de layout do produto?\n\n" +
               $"[ 1 ] Sim\n" +
               $"[ 2 ] Não\n\n" +
               $"0️⃣ Encerrar";
    }

    private async Task<ProcessarMensagemResponse> ProcessarAguardandoImagemAsync(ContatoFluxo contato, string mensagem)
    {
        if (mensagem.Trim() == "0")
        {
            contato.Estado = EstadoFluxo.Encerrado;
            return CriarResposta($"👋 Atendimento encerrado, {contato.Nome}!\n\nObrigado por utilizar o Boot de Pedidos Vizabel!");
        }

        // Verifica se a mensagem é uma imagem em base64
        // Normalmente, imagens do WhatsApp vêm com um prefixo como "data:image/jpeg;base64,"
        string base64Image = mensagem.Trim();
        
        // Remove o prefixo se existir
        if (base64Image.Contains(","))
        {
            base64Image = base64Image.Substring(base64Image.IndexOf(",") + 1);
        }

        try
        {
            // Converte base64 para byte array
            byte[] imageBytes = Convert.FromBase64String(base64Image);
            
            // Armazena temporariamente a imagem no último produto selecionado
            if (contato.ProdutosSelecionados != null && contato.ProdutosSelecionados.Count > 0)
            {
                var ultimoProduto = contato.ProdutosSelecionados[contato.ProdutosSelecionados.Count - 1];
                // Armazena a imagem base64 temporariamente no produto (será processada ao finalizar o pedido)
                ultimoProduto.ImagemBase64 = base64Image;
            }

            // Segue para perguntar se deseja incluir mais produtos
            contato.Estado = EstadoFluxo.PerguntandoMaisProduto;
            return CriarResposta("✅ Imagem recebida com sucesso!\n\n" +
                   "Deseja incluir mais um produto?\n\n" +
                   $"1️⃣ Sim\n" +
                   $"2️⃣ Finalizar pedido\n\n" +
                   $"0️⃣ Encerrar");
        }
        catch (Exception ex)
        {
            Console.WriteLine("=== ERRO AO PROCESSAR IMAGEM ===");
            Console.WriteLine(ex.ToString());
            return CriarResposta("❌ Erro ao processar a imagem. Por favor, envie a imagem novamente ou digite [ 2 ] para pular:\n\n" +
                   "0️⃣ Encerrar");
        }
    }

    private string ProcessarPerguntandoMaisProduto(ContatoFluxo contato, string mensagem)
    {
        var opcao = mensagem.Trim();

        switch (opcao)
        {
            case "1":
                // Incluir mais um produto - volta para buscar produto
                contato.ProdutoSelecionadoId = null;
                contato.GradeSelecionadaId = null;
                contato.Estado = EstadoFluxo.BuscandoProduto;
                return "Agora Vamos Cadastrar os Produtos!\n\nDigite a descrição do produto:\n\n";

            case "2":
                // Finalizar pedido
                contato.Estado = EstadoFluxo.FinalizandoPedido;
                return GerarResumoPedido(contato) + "\n\nDeseja finalizar o pedido?\n\n" +
                       $"1️⃣ Sim, finalizar pedido\n" +
                       $"2️⃣ Voltar para incluir mais produtos\n\n" +
                       $"0️⃣ Encerrar";

            case "0":
                contato.Estado = EstadoFluxo.Encerrado;
                return $"👋 Atendimento encerrado, {contato.Nome}!\n\nObrigado por utilizar o Boot de Pedidos Vizabel!";

            default:
                return $"❌ Opção inválida. Por favor, escolha uma opção válida:\n\n" +
                       $"Deseja incluir mais um produto?\n\n" +
                       $"1️⃣ Sim\n" +
                       $"2️⃣ Finalizar pedido\n\n" +
                       $"0️⃣ Encerrar";
        }
    }

    private async Task<ProcessarMensagemResponse> ProcessarFinalizandoPedidoAsync(ContatoFluxo contato, string mensagem)
    {
        var opcao = mensagem.Trim();

        switch (opcao)
        {
            case "1":
                // Finaliza o pedido - chama seleção de forma de pagamento
                contato.Estado = EstadoFluxo.SelecionandoFormaPagamento;
                return await BuscarFormasPagamentoAsync(contato);

            case "2":
                // Volta para incluir mais produtos
                contato.Estado = EstadoFluxo.PerguntandoMaisProduto;
                return CriarResposta($"Deseja incluir mais um produto?\n\n" +
                       $"1️⃣ Sim\n" +
                       $"2️⃣ Finalizar pedido\n\n" +
                       $"0️⃣ Encerrar");

            case "0":
                contato.Estado = EstadoFluxo.Encerrado;
                return CriarResposta($"👋 Atendimento encerrado, {contato.Nome}!\n\nObrigado por utilizar o Boot de Pedidos Vizabel!");

            default:
                return CriarResposta($"❌ Opção inválida. Por favor, escolha uma opção válida:\n\n" +
                       GerarResumoPedido(contato) + "\n\nDeseja finalizar o pedido?\n\n" +
                       $"1️⃣ Sim, finalizar pedido\n" +
                       $"2️⃣ Voltar para incluir mais produtos\n\n" +
                       $"0️⃣ Encerrar");
        }
    }

    private async Task<ProcessarMensagemResponse> BuscarFormasPagamentoAsync(ContatoFluxo contato)
    {
        using var scope = _serviceProvider.CreateScope();
        var formaPagamentoService = scope.ServiceProvider.GetRequiredService<IFormaPagamentoService>();
        
        var formasPagamento = await formaPagamentoService.BuscarFormasPagamentoAsync();

        if (formasPagamento == null || formasPagamento.Count == 0)
        {
            // Se não houver formas de pagamento, pergunta sobre NFe diretamente
            contato.Estado = EstadoFluxo.PerguntandoEmitirNfe;
            return CriarResposta($"⚠️ Nenhuma forma de pagamento disponível.\n\n" +
                   $"📄 *Emitir NFe?*\n\n" +
                   $"[ 1 ] Sim\n" +
                   $"[ 2 ] Não\n\n" +
                   $"0️⃣ Encerrar");
        }

        // Armazena as formas de pagamento para seleção
        contato.FormasPagamentoDisponiveis = formasPagamento;

        // Lista as formas de pagamento com índice
        var listaFormasPagamento = string.Join("\n", formasPagamento.Select((fp, i) => 
            $"[ {i + 1} ]. {fp.Descricao}"));        
        return CriarResposta($"💳 *Selecione a Forma de Pagamento:*\n\n{listaFormasPagamento}\n\n" +
               $"Digite o número da forma de pagamento desejada:\n\n" +
               $"0️⃣ Encerrar");
    }

    private async Task<ProcessarMensagemResponse> ProcessarSelecaoFormaPagamentoAsync(ContatoFluxo contato, string mensagem)
    {
        var opcao = mensagem.Trim();

        if (opcao == "0")
        {
            contato.Estado = EstadoFluxo.Encerrado;
            return CriarResposta($"👋 Atendimento encerrado, {contato.Nome}!\n\nObrigado por utilizar o Boot de Pedidos Vizabel!");
        }

        // Verifica se é uma seleção numérica
        if (int.TryParse(opcao, out int numeroFormaPagamento) && 
            contato.FormasPagamentoDisponiveis != null && 
            contato.FormasPagamentoDisponiveis.Count > 0)
        {
            var indice = numeroFormaPagamento - 1;
            if (indice >= 0 && indice < contato.FormasPagamentoDisponiveis.Count)
            {
                var formaPagamentoSelecionada = contato.FormasPagamentoDisponiveis[indice];
                contato.FormaPagamentoSelecionadaId = formaPagamentoSelecionada.IdFormaPagamento;
                contato.FormaPagamentoSelecionadaDescricao = formaPagamentoSelecionada.Descricao;

                // Pergunta sobre emitir NFe
                contato.Estado = EstadoFluxo.PerguntandoEmitirNfe;
                return CriarResposta($"✅ Forma de pagamento selecionada: {formaPagamentoSelecionada.Descricao}\n\n" +
                       $"📄 *Emitir NFe?*\n\n" +
                       $"[ 1 ] Sim\n" +
                       $"[ 2 ] Não\n\n" +
                       $"0️⃣ Encerrar");
            }
            else
            {
                // Número inválido
                var listaFormasPagamento = string.Join("\n", contato.FormasPagamentoDisponiveis.Select((fp, i) => 
                    $"{i + 1}. {fp.Descricao}"));
                return CriarResposta($"❌ Número inválido. Por favor, escolha uma forma de pagamento da lista:\n\n{listaFormasPagamento}\n\n" +
                       $"Digite o número da forma de pagamento desejada:\n\n" +
                       $"0️⃣ Encerrar");
            }
        }

        // Opção inválida
        if (contato.FormasPagamentoDisponiveis != null && contato.FormasPagamentoDisponiveis.Count > 0)
        {
            var listaFormasPagamento = string.Join("\n", contato.FormasPagamentoDisponiveis.Select((fp, i) => 
                $"{i + 1}. {fp.Descricao}"));
            return CriarResposta($"❌ Opção inválida. Por favor, escolha uma forma de pagamento da lista:\n\n{listaFormasPagamento}\n\n" +
                   $"Digite o número da forma de pagamento desejada:\n\n" +
                   $"0️⃣ Encerrar");
        }

        return await BuscarFormasPagamentoAsync(contato);
    }

    private async Task<ProcessarMensagemResponse> ProcessarEmitirNfeAsync(ContatoFluxo contato, string mensagem)
    {
        var opcao = mensagem.Trim();

        if (opcao == "0")
        {
            contato.Estado = EstadoFluxo.Encerrado;
            return CriarResposta($"👋 Atendimento encerrado, {contato.Nome}!\n\nObrigado por utilizar o Boot de Pedidos Vizabel!");
        }

        if (opcao == "1")
        {
            contato.EmitirNfe = true;
        }
        else if (opcao == "2")
        {
            contato.EmitirNfe = false;
        }
        else
        {
            return CriarResposta($"❌ Opção inválida. Por favor, escolha uma opção:\n\n" +
                   $"📄 *Emitir NFe?*\n\n" +
                   $"[ 1 ] Sim\n" +
                   $"[ 2 ] Não\n\n" +
                   $"0️⃣ Encerrar");
        }

        // Busca a forma de pagamento selecionada
        FormaPagamento? formaPagamento = null;
        if (contato.FormaPagamentoSelecionadaId.HasValue && contato.FormasPagamentoDisponiveis != null)
        {
            formaPagamento = contato.FormasPagamentoDisponiveis
                .FirstOrDefault(fp => fp.IdFormaPagamento == contato.FormaPagamentoSelecionadaId.Value);
        }

        // Finaliza o pedido com a forma de pagamento e NFe
        return await FinalizarPedidoAsync(contato, formaPagamento);
    }

    private async Task<ProcessarMensagemResponse> FinalizarPedidoAsync(ContatoFluxo contato, FormaPagamento? formaPagamento)
    {
        try
        {
            // Validações
            if (!contato.ClienteSelecionadoId.HasValue)
            {
                return CriarResposta("❌ Erro: Cliente não selecionado. Por favor, selecione um cliente.");
            }

            if (contato.ProdutosSelecionados == null || contato.ProdutosSelecionados.Count == 0)
            {
                return CriarResposta("❌ Erro: Nenhum produto selecionado. Por favor, adicione produtos ao pedido.");
            }

            using var scope = _serviceProvider.CreateScope();
            var pedidoService = scope.ServiceProvider.GetRequiredService<IPedidoService>();
            var configuracaoService = scope.ServiceProvider.GetRequiredService<IConfiguracaoService>();
            var produtoService = scope.ServiceProvider.GetRequiredService<IProdutoService>();

            // Busca o próximo código de pedido
            var codPedido = await pedidoService.BuscarProximoCodPedidoAsync();

            // Busca o status do pedido nas configurações
            var idStatusPedido = 1; // Valor padrão
            var configuracaoStatus = await configuracaoService.BuscarConfiguracaoPorChaveAsync("STATUS_PEDIDO_BOOT");
            if (configuracaoStatus != null && !string.IsNullOrEmpty(configuracaoStatus.Valor))
            {
                if (int.TryParse(configuracaoStatus.Valor, out int statusId))
                {
                    idStatusPedido = statusId;
                }
            }

            // Calcula a data de entrega baseada no maior prazo de entrega dos produtos
            var dataAtual = DateTime.Now;
            var diasEntrega = 30; // Valor padrão: 30 dias
            
            if (contato.ProdutosSelecionados != null && contato.ProdutosSelecionados.Count > 0)
            {
                // Busca os IDs dos produtos selecionados
                var idsProdutos = contato.ProdutosSelecionados
                    .Select(p => p.IdProduto)
                    .Distinct()
                    .ToList();

                // Busca os prazos de entrega dos produtos
                var prazosEntrega = await produtoService.BuscarPrazosEntregaAsync(idsProdutos);
                
                // Encontra o maior prazo de entrega (ignorando valores null)
                var prazosValidos = prazosEntrega.Where(p => p.HasValue).Select(p => p.Value).ToList();
                
                if (prazosValidos.Count > 0)
                {
                    diasEntrega = prazosValidos.Max();
                }
            }

            var dataEntrega = dataAtual.AddDays(diasEntrega);

            // Cria o objeto Pedido
            var pedido = new Pedido
            {
                CodPedido = codPedido,
                IdCliente = contato.ClienteSelecionadoId.Value,
                DataPedido = dataAtual,
                DataEntrega = dataEntrega,
                IdStatusPedido = idStatusPedido, 
                IdVendedor = null,
                Ativo = true,
                Observacoes = null,
                IdFormaPagamento = formaPagamento?.IdFormaPagamento,
                EmitirNfe = contato.EmitirNfe
            };

            // Cria a lista de PedidoProduto a partir dos produtos selecionados
            var pedidoProdutos = new List<PedidoProduto>();
            foreach (var produtoSelecionado in contato.ProdutosSelecionados)
            {
                var pedidoProduto = new PedidoProduto
                {
                    IdProduto = produtoSelecionado.IdProduto,
                    IdGrade = produtoSelecionado.IdGrade,
                    Quantidade = produtoSelecionado.Quantidade,
                    IdEtapaProducao = null,
                    IdFuncionarioResponsavel = null,
                    ValorVenda = null
                };

                // Se houver tamanhos, calcula a quantidade total
                if (produtoSelecionado.TamanhosQuantidades != null && produtoSelecionado.TamanhosQuantidades.Count > 0)
                {
                    pedidoProduto.Quantidade = produtoSelecionado.TamanhosQuantidades.Sum(tq => tq.Quantidade);
                }

                pedidoProdutos.Add(pedidoProduto);
            }

            // Insere o pedido no banco de dados
            var idPedido = await pedidoService.InserirPedidoAsync(pedido, pedidoProdutos, contato.ProdutosSelecionados);

            // Processa as imagens dos produtos, se houver
            if (contato.ProdutosSelecionados != null)
            {
                // Busca os produtos do pedido para obter os IDs
                var resumoPedido = await pedidoService.BuscarResumoPedidoAsync(idPedido);
                
                if (resumoPedido != null && resumoPedido.Produtos != null)
                {
                    // Itera pelos produtos selecionados e verifica se há imagem
                    for (int i = 0; i < contato.ProdutosSelecionados.Count && i < resumoPedido.Produtos.Count; i++)
                    {
                        var produtoSelecionado = contato.ProdutosSelecionados[i];
                        var produtoPedido = resumoPedido.Produtos[i];
                        
                        if (!string.IsNullOrEmpty(produtoSelecionado.ImagemBase64))
                        {
                            try
                            {
                                // Remove o prefixo se existir
                                string base64Image = produtoSelecionado.ImagemBase64;
                                if (base64Image.Contains(","))
                                {
                                    base64Image = base64Image.Substring(base64Image.IndexOf(",") + 1);
                                }
                                
                                // Converte base64 para JPEG e depois para byte array
                                byte[] imageBytes = ConverterBase64ParaJpegBytes(base64Image);
                                
                                // Salva a imagem no banco de dados
                                await pedidoService.InserirImagemPedidoAsync(produtoPedido.IdPedidoProduto, imageBytes);
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine($"=== ERRO AO SALVAR IMAGEM DO PRODUTO {produtoSelecionado.IdProduto} ===");
                                Console.WriteLine(ex.ToString());
                                // Continua o processo mesmo se houver erro ao salvar a imagem
                            }
                        }
                    }
                }
            }

            var resumoFinal = GerarResumoPedido(contato);
            resumoFinal += $"\n📦 Código do Pedido: {codPedido}\n";
            
            if (formaPagamento != null)
            {
                resumoFinal += $"💳 Forma de Pagamento: {formaPagamento.Descricao}\n";
            }

            // Limpa os dados do pedido
            contato.ProdutosSelecionados = null;
            contato.ProdutoSelecionadoId = null;
            contato.GradeSelecionadaId = null;
            contato.ClienteSelecionadoId = null;
            contato.ClienteEncontradoNome = null;
            contato.ClienteEncontradoCpfCnpj = null;
            contato.FormasPagamentoDisponiveis = null;
            contato.FormaPagamentoSelecionadaId = null;
            contato.FormaPagamentoSelecionadaDescricao = null;
            contato.EmitirNfe = null;
            
            contato.Estado = EstadoFluxo.Inicial;
            //return $"✅ Pedido finalizado com sucesso!\n\n{resumoFinal}\n\n{GerarMenuInicial(contato.Nome ?? "Usuário")}";
            return CriarResposta($"✅ Pedido finalizado com sucesso!\n\n{resumoFinal}", pedidoFinalizado: true);
        }
        catch (Exception ex)
        {
            Console.WriteLine("=== ERRO AO FINALIZAR PEDIDO ===");
            Console.WriteLine(ex.ToString());
            return CriarResposta($"❌ Erro ao finalizar o pedido: {ex.Message}\n\nPor favor, tente novamente.");
        }
    }

    private string GerarResumoPedido(ContatoFluxo contato)
    {
        var resumo = $"📋 *RESUMO DO PEDIDO*\n\n";
        
        if (!string.IsNullOrEmpty(contato.ClienteEncontradoNome))
        {
            resumo += $"👤 Cliente: {contato.ClienteEncontradoNome}\n";
            if (!string.IsNullOrEmpty(contato.ClienteEncontradoCpfCnpj))
            {
                resumo += $"📄 CPF/CNPJ: {FormatarCpfCnpj(contato.ClienteEncontradoCpfCnpj)}\n";
            }
            resumo += "\n";
        }

        if (contato.ProdutosSelecionados != null && contato.ProdutosSelecionados.Count > 0)
        {
            resumo += $"🛍️ *PRODUTOS:*\n\n";
            for (int i = 0; i < contato.ProdutosSelecionados.Count; i++)
            {
                var produto = contato.ProdutosSelecionados[i];
                resumo += $"{i + 1}. {produto.Descricao ?? $"Produto ID: {produto.IdProduto}"}\n";
                
                if (produto.TamanhosQuantidades != null && produto.TamanhosQuantidades.Count > 0)
                {
                    var tamanhos = string.Join(", ", produto.TamanhosQuantidades.Select(tq => $"{tq.Tamanho}: {tq.Quantidade}"));
                    resumo += $"   Tamanhos: {tamanhos}\n";
                }
                else if (produto.Quantidade.HasValue)
                {
                    resumo += $"   Quantidade: {produto.Quantidade.Value}\n";
                }
                resumo += "\n";
            }
        }

        return resumo;
    }

    private string ProcessarFazendoPedido(ContatoFluxo contato, string mensagem)
    {
        // Aqui você pode processar o pedido
        // Por enquanto, apenas confirma e volta ao menu
        contato.Estado = EstadoFluxo.AguardandoOpcao;
        return $"✅ Pedido registrado com sucesso!\n\nDetalhes: {mensagem}\n\n{GerarMenuInicial(contato.Nome ?? "Usuário")}";
    }

    private string ProcessarVerificandoPedido(ContatoFluxo contato, string mensagem)
    {
        // Aqui você pode buscar o pedido
        // Por enquanto, apenas simula a busca e volta ao menu
        contato.Estado = EstadoFluxo.AguardandoOpcao;
        return $"🔍 Pedido #{mensagem}:\nStatus: Em processamento\n\n{GerarMenuInicial(contato.Nome ?? "Usuário")}";
    }

    private string GerarMenuInicial(string nome)
    {
        return $@"👨🏼‍💼Olá {nome}!

Você está no boot de Pedidos Vizabel!

Digite um número:

1️⃣ Fazer um Pedido
2️⃣ Verificar Andamento de Pedido

0️⃣ Encerrar";
    }

    private string GerarMenuSelecaoCliente()
    {
        return @"Vamos Selecionar o Cliente!

Digite o Nome, CPF ou CNPJ do cliente:
1️⃣ Novo Cliente
2️⃣ Novo Cliente usando Template

0️⃣ Encerrar";
    }

    private string ProcessarCadastroClienteNome(ContatoFluxo contato, string mensagem)
    {
        if (string.IsNullOrWhiteSpace(mensagem) || mensagem.Trim().Length < 3)
        {
            return "❌ Nome inválido. Por favor, informe um nome com pelo menos 3 caracteres:";
        }

        contato.ClienteNome = mensagem.Trim();
        contato.Estado = EstadoFluxo.CadastrandoClienteCpfCnpj;
        return $"✅ Nome registrado: {contato.ClienteNome}\n\nPor favor, informe o CPF ou CNPJ do cliente:";
    }

    private string ProcessarCadastroClienteCpfCnpj(ContatoFluxo contato, string mensagem)
    {
        var cpfCnpj = RemoverCaracteresEspeciais(mensagem.Trim());

        if (string.IsNullOrWhiteSpace(cpfCnpj))
        {
            return "❌ CPF/CNPJ inválido. Por favor, informe um CPF ou CNPJ válido:";
        }

        // Validação básica de CPF (11 dígitos) ou CNPJ (14 dígitos)
        if (cpfCnpj.Length != 11 && cpfCnpj.Length != 14)
        {
            return "❌ CPF deve ter 11 dígitos ou CNPJ deve ter 14 dígitos. Por favor, informe novamente:";
        }

        contato.ClienteCpfCnpj = cpfCnpj;
        contato.Estado = EstadoFluxo.CadastrandoClienteTelefone;
        return $"✅ CPF/CNPJ registrado: {FormatarCpfCnpj(cpfCnpj)}\n\nPor favor, informe o telefone do cliente (com DDD):";
    }

    private string ProcessarCadastroClienteTelefone(ContatoFluxo contato, string mensagem)
    {
        var telefone = RemoverCaracteresEspeciais(mensagem.Trim());

        if (string.IsNullOrWhiteSpace(telefone) || telefone.Length < 10)
        {
            return "❌ Telefone inválido. Por favor, informe um telefone válido com DDD (ex: 11987654321):";
        }

        contato.ClienteTelefone = telefone;
        contato.Estado = EstadoFluxo.CadastrandoClienteEndereco;
        return $"✅ Telefone registrado: {FormatarTelefone(telefone)}\n\nPor favor, informe o endereço do cliente:";
    }

    private async Task<ProcessarMensagemResponse> ProcessarCadastroClienteEnderecoComTemplateAsync(ContatoFluxo contato, string endereco, string? cep, string? cidade)
    {
        try
        {
            // Cria o objeto Cliente para inserção com dados do template
            var cliente = new Cliente
            {
                NomeRazaoCompleto = contato.ClienteNome ?? string.Empty,
                TipoPessoa = contato.ClienteCpfCnpj?.Length == 14, // 14 dígitos = PJ (true), 11 dígitos = PF (false)
                FantasiaCompleto = null,
                CpfCnpjCompleto = contato.ClienteCpfCnpj,
                EnderecoLogradouro = endereco,
                EnderecoNumero = null,
                EnderecoComplemento = null,
                EnderecoBairro = null,
                EnderecoCidade = cidade,
                EnderecoCep = cep,
                Email = null,
                Ativo = true,
                CodCliente = null, // Será gerado automaticamente
                Contato = null,
                Fone = contato.ClienteTelefone,
                Whatsapp = contato.ClienteTelefone,
                FoneContato = null,
                RgIe = null,
                EnderecoUf = null,
                EnderecoIbge = null
            };

            using var scope = _serviceProvider.CreateScope();
            var clienteService = scope.ServiceProvider.GetRequiredService<IClienteService>();
            
            // Insere o cliente no banco de dados
            var idCliente = await clienteService.InserirClienteAsync(cliente);
            
            // Armazena o ID do cliente criado
            contato.ClienteSelecionadoId = idCliente;
            contato.ClienteEncontradoNome = contato.ClienteNome;
            contato.ClienteEncontradoCpfCnpj = contato.ClienteCpfCnpj;
            
            // Limpa dados temporários de cadastro
            contato.ClienteNome = null;
            contato.ClienteCpfCnpj = null;
            contato.ClienteTelefone = null;
            contato.ClienteEndereco = null;
            contato.UsandoTemplate = false;
            
            // Vai para seleção de produtos
            contato.ProdutosSelecionados = new List<ProdutoSelecionado>();
            contato.Estado = EstadoFluxo.BuscandoProduto;
            
            return CriarResposta($"✅ Cliente cadastrado com sucesso usando template!\n\n" +
                   $"Nome: {cliente.NomeRazaoCompleto}\n" +
                   $"CPF/CNPJ: {FormatarCpfCnpj(cliente.CpfCnpjCompleto ?? "")}\n" +
                   $"Endereço: {cliente.EnderecoLogradouro}\n" +
                   $"CEP: {cliente.EnderecoCep ?? "Não informado"}\n" +
                   $"Cidade: {cliente.EnderecoCidade ?? "Não informado"}\n\n" +
                   $"Agora Vamos Cadastrar os Produtos!\n\n" +
                   $"Digite a descrição do produto:\n\n");
        }
        catch (Exception ex)
        {
            Console.WriteLine("=== ERRO AO CADASTRAR CLIENTE COM TEMPLATE ===");
            Console.WriteLine(ex.ToString());
            return CriarResposta($"❌ Erro ao cadastrar cliente: {ex.Message}\n\nPor favor, tente novamente.");
        }
    }

    private async Task<ProcessarMensagemResponse> ProcessarCadastroClienteEnderecoAsync(ContatoFluxo contato, string mensagem)
    {
        if (string.IsNullOrWhiteSpace(mensagem) || mensagem.Trim().Length < 5)
        {
            return CriarResposta("❌ Endereço inválido. Por favor, informe um endereço válido:");
        }

        contato.ClienteEndereco = mensagem.Trim();
        
        try
        {
            // Cria o objeto Cliente para inserção
            var cliente = new Cliente
            {
                NomeRazaoCompleto = contato.ClienteNome ?? string.Empty,
                TipoPessoa = contato.ClienteCpfCnpj?.Length == 14, // 14 dígitos = PJ (true), 11 dígitos = PF (false)
                FantasiaCompleto = null,
                CpfCnpjCompleto = contato.ClienteCpfCnpj,
                EnderecoLogradouro = contato.ClienteEndereco,
                EnderecoNumero = null,
                EnderecoComplemento = null,
                EnderecoBairro = null,
                EnderecoCidade = null,
                EnderecoCep = null,
                Email = null,
                Ativo = true,
                CodCliente = null, // Será gerado automaticamente
                Contato = null,
                Fone = contato.ClienteTelefone,
                Whatsapp = contato.ClienteTelefone,
                FoneContato = null,
                RgIe = null,
                EnderecoUf = null,
                EnderecoIbge = null
            };

            using var scope = _serviceProvider.CreateScope();
            var clienteService = scope.ServiceProvider.GetRequiredService<IClienteService>();
            
            // Insere o cliente no banco de dados
            var idCliente = await clienteService.InserirClienteAsync(cliente);
            
            // Armazena o ID do cliente criado
            contato.ClienteSelecionadoId = idCliente;
            contato.ClienteEncontradoNome = contato.ClienteNome;
            contato.ClienteEncontradoCpfCnpj = contato.ClienteCpfCnpj;
            
            // Limpa dados temporários de cadastro
            contato.ClienteNome = null;
            contato.ClienteCpfCnpj = null;
            contato.ClienteTelefone = null;
            contato.ClienteEndereco = null;
            
            // Vai para seleção de produtos
            contato.ProdutosSelecionados = new List<ProdutoSelecionado>();
            contato.Estado = EstadoFluxo.BuscandoProduto;
            
            return CriarResposta($"✅ Cliente cadastrado com sucesso!\n\n" +
                   $"Nome: {cliente.NomeRazaoCompleto}\n" +
                   $"CPF/CNPJ: {FormatarCpfCnpj(cliente.CpfCnpjCompleto ?? "")}\n" +
                   $"Telefone: {FormatarTelefone(cliente.Fone ?? "")}\n" +
                   $"Endereço: {cliente.EnderecoLogradouro}\n\n" +
                   $"Agora Vamos Cadastrar os Produtos!\n\n" +
                   $"Digite a descrição do produto:\n\n");
        }
        catch (Exception ex)
        {
            Console.WriteLine("=== ERRO AO CADASTRAR CLIENTE ===");
            Console.WriteLine(ex.ToString());
            return CriarResposta($"❌ Erro ao cadastrar cliente: {ex.Message}\n\nPor favor, tente novamente.");
        }
    }

    private string ProcessarCadastroClienteTemplate(ContatoFluxo contato, string mensagem)
    {
        var opcao = mensagem.Trim();

        if (opcao == "1")
        {
            // Volta para cadastro normal sem template
            contato.UsandoTemplate = false;
            contato.Estado = EstadoFluxo.CadastrandoClienteNome;
            return "👤 Novo Cliente\n\nPor favor, informe o Nome ou Razão Social do cliente:";
        }

        // Envia o template formatado para o usuário preencher
        contato.Estado = EstadoFluxo.AguardandoTemplatePreenchido;
        return "*Cadastro de Cliente usando Template*!\n" +
               "Copie o template abaixo incluindo a palavra [TEMPLATE] e coloque os dados depois dos dois pontos *(:)* \n\n" +
               "[TEMPLATE]\n" +
               "[CPF_CNPJ]:\n" +
               "[NOME]:\n" +
               "[ENDERECO]:\n" +
               "[CEP]:\n" +
               "[CIDADE]:\n\n" +
               "0️⃣ Encerrar";
    }

    private async Task<ProcessarMensagemResponse> ProcessarTemplatePreenchidoAsync(ContatoFluxo contato, string mensagem)
    {
        if (mensagem.Trim() == "0")
        {
            contato.Estado = EstadoFluxo.Encerrado;
            return CriarResposta($"👋 Atendimento encerrado, {contato.Nome}!\n\nObrigado por utilizar o Boot de Pedidos Vizabel!");
        }

        // Extrai os dados do template preenchido
        var dadosTemplate = ExtrairDadosTemplate(mensagem);

        if (dadosTemplate == null)
        {
            return CriarResposta("❌ Formato do template inválido. Por favor, envie o template no formato correto:\n\n" +
                   "[TEMPLATE]\n" +
                   "[CPF_CNPJ]: 00.000.000/0001-00\n" +
                   "[NOME]: nome do cliente\n" +
                   "[ENDERECO]: endereco do cliente\n" +
                   "[CEP]: cep do cliente\n" +
                   "[CIDADE]: cidade do cliente\n\n" +
                   "0️⃣ Encerrar");
        }

        // Valida os dados obrigatórios
        if (string.IsNullOrWhiteSpace(dadosTemplate.CpfCnpj))
        {
            return CriarResposta("❌ CPF/CNPJ é obrigatório. Por favor, preencha o template novamente:\n\n" +
                   "[TEMPLATE]\n" +
                   "[CPF_CNPJ]: 00.000.000/0001-00\n" +
                   "[NOME]: nome do cliente\n" +
                   "[ENDERECO]: endereco do cliente\n" +
                   "[CEP]: cep do cliente\n" +
                   "[CIDADE]: cidade do cliente\n\n" +
                   "0️⃣ Encerrar");
        }

        if (string.IsNullOrWhiteSpace(dadosTemplate.Nome))
        {
            return CriarResposta("❌ Nome é obrigatório. Por favor, preencha o template novamente:\n\n" +
                   "[TEMPLATE]\n" +
                   "[CPF_CNPJ]: 00.000.000/0001-00\n" +
                   "[NOME]: nome do cliente\n" +
                   "[ENDERECO]: endereco do cliente\n" +
                   "[CEP]: cep do cliente\n" +
                   "[CIDADE]: cidade do cliente\n\n" +
                   "0️⃣ Encerrar");
        }

        // Preenche os dados do cliente
        contato.ClienteCpfCnpj = RemoverCaracteresEspeciais(dadosTemplate.CpfCnpj);
        contato.ClienteNome = dadosTemplate.Nome.Trim();
        contato.ClienteEndereco = dadosTemplate.Endereco?.Trim();
        
        // Valida CPF/CNPJ
        if (contato.ClienteCpfCnpj.Length != 11 && contato.ClienteCpfCnpj.Length != 14)
        {
            return CriarResposta("❌ CPF deve ter 11 dígitos ou CNPJ deve ter 14 dígitos. Por favor, corrija o template:\n\n" +
                   "[TEMPLATE]\n" +
                   "[CPF_CNPJ]: 00.000.000/0001-00\n" +
                   "[NOME]: nome do cliente\n" +
                   "[ENDERECO]: endereco do cliente\n" +
                   "[CEP]: cep do cliente\n" +
                   "[CIDADE]: cidade do cliente\n\n" +
                   "0️⃣ Encerrar");
        }

        // Armazena CEP e Cidade temporariamente para uso no cadastro
        var cepTemplate = dadosTemplate.Cep?.Trim();
        var cidadeTemplate = dadosTemplate.Cidade?.Trim();

        // Se não tem endereço, solicita
        if (string.IsNullOrWhiteSpace(contato.ClienteEndereco))
        {
            contato.Estado = EstadoFluxo.CadastrandoClienteEndereco;
            return CriarResposta($"✅ Dados do template recebidos:\n\n" +
                   $"CPF/CNPJ: {FormatarCpfCnpj(contato.ClienteCpfCnpj)}\n" +
                   $"Nome: {contato.ClienteNome}\n" +
                   $"CEP: {cepTemplate ?? "Não informado"}\n" +
                   $"Cidade: {cidadeTemplate ?? "Não informado"}\n\n" +
                   $"Por favor, informe o endereço do cliente:");
        }

        // Se tem todos os dados, cadastra o cliente diretamente usando os dados do template
        return await ProcessarCadastroClienteEnderecoComTemplateAsync(contato, contato.ClienteEndereco, cepTemplate, cidadeTemplate);
    }

    private DadosTemplate? ExtrairDadosTemplate(string mensagem)
    {
        try
        {
            // Verifica se contém [TEMPLATE]
            if (!mensagem.Contains("[TEMPLATE]", StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }

            var dados = new DadosTemplate();
            var linhas = mensagem.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);

            foreach (var linha in linhas)
            {
                var linhaTrim = linha.Trim();
                
                if (linhaTrim.StartsWith("[CPF_CNPJ]:", StringComparison.OrdinalIgnoreCase))
                {
                    dados.CpfCnpj = linhaTrim.Substring("[CPF_CNPJ]:".Length).Trim();
                }
                else if (linhaTrim.StartsWith("[NOME]:", StringComparison.OrdinalIgnoreCase))
                {
                    dados.Nome = linhaTrim.Substring("[NOME]:".Length).Trim();
                }
                else if (linhaTrim.StartsWith("[ENDERECO]:", StringComparison.OrdinalIgnoreCase))
                {
                    dados.Endereco = linhaTrim.Substring("[ENDERECO]:".Length).Trim();
                }
                else if (linhaTrim.StartsWith("[CEP]:", StringComparison.OrdinalIgnoreCase))
                {
                    dados.Cep = linhaTrim.Substring("[CEP]:".Length).Trim();
                }
                else if (linhaTrim.StartsWith("[CIDADE]:", StringComparison.OrdinalIgnoreCase))
                {
                    dados.Cidade = linhaTrim.Substring("[CIDADE]:".Length).Trim();
                }
            }

            return dados;
        }
        catch
        {
            return null;
        }
    }

    private class DadosTemplate
    {
        public string? CpfCnpj { get; set; }
        public string? Nome { get; set; }
        public string? Endereco { get; set; }
        public string? Cep { get; set; }
        public string? Cidade { get; set; }
    }

    private string RemoverCaracteresEspeciais(string texto)
    {
        if (string.IsNullOrEmpty(texto))
            return texto;

        return texto.Replace(".", "").Replace("-", "").Replace("/", "").Replace("(", "").Replace(")", "").Replace(" ", "");
    }

    private string FormatarCpfCnpj(string cpfCnpj)
    {
        if (string.IsNullOrEmpty(cpfCnpj))
            return cpfCnpj;

        if (cpfCnpj.Length == 11)
        {
            return $"{cpfCnpj.Substring(0, 3)}.{cpfCnpj.Substring(3, 3)}.{cpfCnpj.Substring(6, 3)}-{cpfCnpj.Substring(9, 2)}";
        }
        else if (cpfCnpj.Length == 14)
        {
            return $"{cpfCnpj.Substring(0, 2)}.{cpfCnpj.Substring(2, 3)}.{cpfCnpj.Substring(5, 3)}/{cpfCnpj.Substring(8, 4)}-{cpfCnpj.Substring(12, 2)}";
        }

        return cpfCnpj;
    }

    private string FormatarTelefone(string telefone)
    {
        if (string.IsNullOrEmpty(telefone))
            return telefone;

        if (telefone.Length == 10)
        {
            return $"({telefone.Substring(0, 2)}) {telefone.Substring(2, 4)}-{telefone.Substring(6, 4)}";
        }
        else if (telefone.Length == 11)
        {
            return $"({telefone.Substring(0, 2)}) {telefone.Substring(2, 5)}-{telefone.Substring(7, 4)}";
        }

        return telefone;
    }
 
    /// <summary>
    /// Valida o formato de tamanhos e quantidades
    /// Aceita: apenas número (ex: "10") ou pares de letra(s)+número (ex: "P10 M5 G3")
    /// </summary>
    private bool ValidaTamanhos(string texto)
    {
        if (string.IsNullOrWhiteSpace(texto))
            return false;

        // Padrão regex: aceita apenas número OU pares de letra(s)+número separados por espaço ou vírgula
        // ^(\d+|([A-Z]{1,3}\s*\d+)([ ,]+[A-Z]{1,3}\s*\d+)*)$
        var pattern = @"^(\d+|([A-Z]{1,3}\s*\d+)([ ,]+[A-Z]{1,3}\s*\d+)*)$";
        var textoUpper = texto.Trim().ToUpper();
        
        return Regex.IsMatch(textoUpper, pattern);
    }

    /// <summary>
    /// Processa a string de tamanhos e quantidades, validando se os tamanhos existem na lista disponível
    /// </summary>
    private List<TamanhoQuantidade>? ProcessarTamanhosQuantidades(string texto, List<TamanhoDisponivel> tamanhosDisponiveis)
    {
        if (string.IsNullOrWhiteSpace(texto) || tamanhosDisponiveis == null || tamanhosDisponiveis.Count == 0)
            return null;

        var textoUpper = texto.Trim().ToUpper();
        var resultado = new List<TamanhoQuantidade>();
        
        // Cria um dicionário com os tamanhos disponíveis (em maiúsculo para comparação)
        var tamanhosDisponiveisDict = tamanhosDisponiveis
            .ToDictionary(t => t.Tamanho.ToUpper(), t => t.Tamanho);

        // Se for apenas um número, não processa (deve ser tratado no caso sem tamanhos)
        if (int.TryParse(textoUpper, out _))
            return null;

        // Processa pares de tamanho+quantidade
        // Divide por espaços ou vírgulas
        var partes = Regex.Split(textoUpper, @"[ ,]+")
            .Where(p => !string.IsNullOrWhiteSpace(p))
            .ToList();

        foreach (var parte in partes)
        {
            // Extrai letras e números
            var match = Regex.Match(parte, @"^([A-Z]{1,3})(\d+)$");
            if (match.Success)
            {
                var tamanho = match.Groups[1].Value;
                if (int.TryParse(match.Groups[2].Value, out int quantidade) && quantidade > 0)
                {
                    // Verifica se o tamanho existe na lista disponível
                    if (tamanhosDisponiveisDict.ContainsKey(tamanho))
                    {
                        resultado.Add(new TamanhoQuantidade
                        {
                            Tamanho = tamanhosDisponiveisDict[tamanho], // Usa o tamanho original (com maiúsculas/minúsculas corretas)
                            Quantidade = quantidade
                        });
                    }
                    else
                    {
                        // Tamanho não encontrado
                        return null;
                    }
                }
            }
        }

        return resultado.Count > 0 ? resultado : null;
    }

    private string ProcessarVerificandoPedidoTipoBusca(ContatoFluxo contato, string mensagem)
    {
        var opcao = mensagem.Trim();

        switch (opcao)
        {
            case "1":
                contato.Estado = EstadoFluxo.VerificandoPedidoPorCodigo;
                return "📋 Buscar pedido pelo Código\n\nPor favor, informe o código do pedido:";
            
            case "2":
                contato.Estado = EstadoFluxo.VerificandoPedidoCpfCnpj;
                return "👤 Buscar pelo nome do Cliente\n\nPor favor, informe o Nome Razão, CPF ou CNPJ do cliente:";
            
            case "0":
                contato.Estado = EstadoFluxo.Encerrado;
                return $"👋 Atendimento encerrado, {contato.Nome}!\n\nObrigado por utilizar o Boot de Pedidos Vizabel!";
            
            default:
                return $"❌ Opção inválida. Por favor, escolha uma opção válida:\n\n" +
                       "Como deseja buscar o pedido?\n\n" +
                       "1️⃣ Buscar pedido pelo Código\n" +
                       "2️⃣ Buscar pelo nome do Cliente\n\n" +
                       "0️⃣ Encerrar";
        }
    }

    private async Task<ProcessarMensagemResponse> ProcessarVerificandoPedidoPorCodigoAsync(ContatoFluxo contato, string mensagem)
    {
        var opcao = mensagem.Trim();

        if (opcao == "0")
        {
            contato.Estado = EstadoFluxo.Encerrado;
            return CriarResposta($"👋 Atendimento encerrado, {contato.Nome}!\n\nObrigado por utilizar o Boot de Pedidos Vizabel!");
        }

        if (!int.TryParse(opcao, out int codPedido))
        {
            return CriarResposta("❌ Código do pedido inválido. Por favor, informe um número válido:\n\n" +
                   "0️⃣ Encerrar");
        }

        using var scope = _serviceProvider.CreateScope();
        var pedidoService = scope.ServiceProvider.GetRequiredService<IPedidoService>();
        
        var pedido = await pedidoService.BuscarPedidoPorCodigoAsync(codPedido);

        if (pedido == null)
        {
            return CriarResposta($"❌ Pedido com código {codPedido} não encontrado.\n\n" +
                   "Por favor, verifique o código e tente novamente:\n\n" +
                   "0️⃣ Encerrar");
        }

        // Busca o resumo completo do pedido
        var resumo = await pedidoService.BuscarResumoPedidoAsync(pedido.IdPedido);

        if (resumo == null)
        {
            return CriarResposta($"❌ Erro ao buscar detalhes do pedido {codPedido}.\n\n" +
                   "0️⃣ Encerrar");
        }

        // Volta para o menu inicial após mostrar o resumo
        contato.Estado = EstadoFluxo.AguardandoOpcao;
        contato.PedidosEncontrados = null;
        contato.ClienteIdParaPedidos = null;

        return CriarResposta(GerarResumoPedidoCompleto(resumo) ,true);
    }

    private string ProcessarVerificandoPedidoPorCliente(ContatoFluxo contato, string mensagem)
    {
        // Este método não é usado diretamente, mas mantido para compatibilidade
        contato.Estado = EstadoFluxo.VerificandoPedidoCpfCnpj;
        return "👤 Buscar pelo nome do Cliente\n\nPor favor, informe o Nome Razão, CPF ou CNPJ do cliente:";
    }

    private async Task<ProcessarMensagemResponse> ProcessarVerificandoPedidoCpfCnpjAsync(ContatoFluxo contato, string mensagem)
    {
        var opcao = mensagem.Trim();

        if (opcao == "0")
        {
            contato.Estado = EstadoFluxo.Encerrado;
            return CriarResposta($"👋 Atendimento encerrado, {contato.Nome}!\n\nObrigado por utilizar o Boot de Pedidos Vizabel!");
        }

        // Remove caracteres especiais do CPF/CNPJ
        var cpfCnpj = RemoverCaracteresEspeciais(opcao);

        using var scope = _serviceProvider.CreateScope();
        var clienteService = scope.ServiceProvider.GetRequiredService<IClienteService>();
        
        var clientes = await clienteService.BuscarClientesAsync(cpfCnpj);

        if (clientes == null || clientes.Count == 0)
        {
            return CriarResposta($"❌ Nenhum cliente encontrado com CPF/CNPJ: {FormatarCpfCnpj(cpfCnpj)}\n\n" +
                   "Por favor, verifique e tente novamente:\n\n" +
                   "0️⃣ Encerrar");
        }

        if (clientes.Count == 1)
        {
            // Cliente único encontrado, busca os pedidos
            var cliente = clientes[0];
            contato.ClienteIdParaPedidos = cliente.IdCliente;
            contato.Estado = EstadoFluxo.VerificandoPedidoListarPedidos;
            return await BuscarPedidosDoClienteAsync(contato, cliente.IdCliente);
        }

        // Múltiplos clientes encontrados - armazena a lista para seleção
        contato.ClientesEncontrados = clientes.Take(20).ToList();
        contato.Estado = EstadoFluxo.VerificandoPedidoSelecionarCliente;
        
        var listaClientes = string.Join("\n", contato.ClientesEncontrados.Select((c, i) => 
            $"{i + 1}. {c.NomeExibicao} - {FormatarCpfCnpj(c.CpfCnpj ?? "")}"));
        
        return CriarResposta($"🔍 Foram encontrados {clientes.Count} cliente(s):\n\n{listaClientes}\n\n" +
               "Digite o número do cliente desejado:\n\n" +
               "0️⃣ Encerrar");
    }

    private async Task<ProcessarMensagemResponse> ProcessarVerificandoPedidoSelecionarClienteAsync(ContatoFluxo contato, string mensagem)
    {
        var opcao = mensagem.Trim();

        if (opcao == "0")
        {
            contato.Estado = EstadoFluxo.Encerrado;
            return CriarResposta($"👋 Atendimento encerrado, {contato.Nome}!\n\nObrigado por utilizar o Boot de Pedidos Vizabel!");
        }

        if (!int.TryParse(opcao, out int numeroCliente) || contato.ClientesEncontrados == null || contato.ClientesEncontrados.Count == 0)
        {
            var listaClientes = string.Join("\n", contato.ClientesEncontrados?.Select((c, i) => 
                $"{i + 1}. {c.NomeExibicao} - {FormatarCpfCnpj(c.CpfCnpj ?? "")}") ?? new List<string>());
            
            return CriarResposta($"❌ Número inválido. Por favor, escolha um cliente da lista:\n\n{listaClientes}\n\n" +
                   "Digite o número do cliente desejado:\n\n" +
                   "0️⃣ Encerrar");
        }

        var indice = numeroCliente - 1;
        if (indice < 0 || indice >= contato.ClientesEncontrados.Count)
        {
            var listaClientes = string.Join("\n", contato.ClientesEncontrados.Select((c, i) => 
                $"{i + 1}. {c.NomeExibicao} - {FormatarCpfCnpj(c.CpfCnpj ?? "")}"));
            
            return CriarResposta($"❌ Número inválido. Por favor, escolha um cliente da lista:\n\n{listaClientes}\n\n" +
                   "Digite o número do cliente desejado:\n\n" +
                   "0️⃣ Encerrar");
        }

        var clienteSelecionado = contato.ClientesEncontrados[indice];
        contato.ClienteIdParaPedidos = clienteSelecionado.IdCliente;
        contato.Estado = EstadoFluxo.VerificandoPedidoListarPedidos;
        
        return await BuscarPedidosDoClienteAsync(contato, clienteSelecionado.IdCliente);
    }

    private async Task<ProcessarMensagemResponse> BuscarPedidosDoClienteAsync(ContatoFluxo contato, int idCliente)
    {
        using var scope = _serviceProvider.CreateScope();
        var pedidoService = scope.ServiceProvider.GetRequiredService<IPedidoService>();
        
        var pedidos = await pedidoService.BuscarPedidosPorClienteAsync(idCliente);

        if (pedidos == null || pedidos.Count == 0)
        {
            contato.Estado = EstadoFluxo.AguardandoOpcao;
            contato.PedidosEncontrados = null;
            contato.ClienteIdParaPedidos = null;
            
            return CriarResposta($"❌ Nenhum pedido encontrado para este cliente.\n\n{GerarMenuInicial(contato.Nome ?? "Usuário")}");
        }

        // Armazena os pedidos para seleção
        contato.PedidosEncontrados = pedidos;
        contato.Estado = EstadoFluxo.VerificandoPedidoSelecionarPedido;

        var listaPedidos = string.Join("\n", pedidos.Select((p, i) => 
            $"{i + 1}. Pedido #{p.CodPedido} - {p.DataPedido:dd/MM/yyyy} - Status: {p.IdStatusPedido}"));

        return CriarResposta($"📦 Foram encontrados {pedidos.Count} pedido(s):\n\n{listaPedidos}\n\n" +
               "Digite o número do pedido que deseja ver o resumo:\n\n" +
               "0️⃣ Encerrar");
    }

    private async Task<ProcessarMensagemResponse> ProcessarVerificandoPedidoListarPedidosAsync(ContatoFluxo contato, string mensagem)
    {
        // Este método pode ser usado se necessário, mas o fluxo principal usa BuscarPedidosDoClienteAsync
        return await ProcessarVerificandoPedidoSelecionarPedidoAsync(contato, mensagem);
    }

    private async Task<ProcessarMensagemResponse> ProcessarVerificandoPedidoSelecionarPedidoAsync(ContatoFluxo contato, string mensagem)
    {
        var opcao = mensagem.Trim();

        if (opcao == "0")
        {
            contato.Estado = EstadoFluxo.Encerrado;
            return CriarResposta($"👋 Atendimento encerrado, {contato.Nome}!\n\nObrigado por utilizar o Boot de Pedidos Vizabel!");
        }

        if (!int.TryParse(opcao, out int numeroPedido) || contato.PedidosEncontrados == null || contato.PedidosEncontrados.Count == 0)
        {
            var listaPedidos = string.Join("\n", contato.PedidosEncontrados?.Select((p, i) => 
                $"{i + 1}. Pedido #{p.CodPedido} - {p.DataPedido:dd/MM/yyyy} - Status: {p.IdStatusPedido}") ?? new List<string>());
            
            return CriarResposta($"❌ Número inválido. Por favor, escolha um pedido da lista:\n\n{listaPedidos}\n\n" +
                   "Digite o número do pedido que deseja ver o resumo:\n\n" +
                   "0️⃣ Encerrar");
        }

        var indice = numeroPedido - 1;
        if (indice < 0 || indice >= contato.PedidosEncontrados.Count)
        {
            var listaPedidos = string.Join("\n", contato.PedidosEncontrados.Select((p, i) => 
                $"{i + 1}. Pedido #{p.CodPedido} - {p.DataPedido:dd/MM/yyyy} - Status: {p.IdStatusPedido}"));
            
            return CriarResposta($"❌ Número inválido. Por favor, escolha um pedido da lista:\n\n{listaPedidos}\n\n" +
                   "Digite o número do pedido que deseja ver o resumo:\n\n" +
                   "0️⃣ Encerrar");
        }

        var pedidoSelecionado = contato.PedidosEncontrados[indice];

        using var scope = _serviceProvider.CreateScope();
        var pedidoService = scope.ServiceProvider.GetRequiredService<IPedidoService>();
        
        var resumo = await pedidoService.BuscarResumoPedidoAsync(pedidoSelecionado.IdPedido);

        if (resumo == null)
        {
            return CriarResposta($"❌ Erro ao buscar detalhes do pedido #{pedidoSelecionado.CodPedido}.\n\n" +
                   "0️⃣ Encerrar");
        }

        // Volta para o menu inicial após mostrar o resumo
        contato.Estado = EstadoFluxo.AguardandoOpcao;
        contato.PedidosEncontrados = null;
        contato.ClienteIdParaPedidos = null;
        contato.ClientesEncontrados = null;

        return CriarResposta(GerarResumoPedidoCompleto(resumo) ,true);
    }

    private string GerarResumoPedidoCompleto(ResumoPedido resumo)
    {
        var texto = $"📋 *RESUMO DO PEDIDO*\n\n";
        
        texto += $"🔢 Código do Pedido: {resumo.CodPedido}\n";
        
        if (!string.IsNullOrEmpty(resumo.NomeCliente))
        {
            texto += $"👤 Cliente: {resumo.NomeCliente}\n";
        }
        
        if (!string.IsNullOrEmpty(resumo.CpfCnpjCliente))
        {
            texto += $"📄 CPF/CNPJ: {FormatarCpfCnpj(resumo.CpfCnpjCliente)}\n";
        }
        
        texto += $"📅 Data do Pedido: {resumo.DataPedido:dd/MM/yyyy HH:mm}\n";
        texto += $"📅 Data de Entrega: {resumo.DataEntrega:dd/MM/yyyy}\n";
        
        if (!string.IsNullOrEmpty(resumo.DescricaoStatusPedido))
        {
            texto += $"📊 Status: {resumo.DescricaoStatusPedido}\n";
        }
        else
        {
            texto += $"📊 Status ID: {resumo.IdStatusPedido}\n";
        }
        
        if (!string.IsNullOrEmpty(resumo.DescricaoFormaPagamento))
        {
            texto += $"💳 Forma de Pagamento: {resumo.DescricaoFormaPagamento}\n";
        }
        
        if (resumo.EmitirNfe.HasValue)
        {
            texto += $"📄 Emitir NFe: {(resumo.EmitirNfe.Value ? "Sim" : "Não")}\n";
        }
        
        if (!string.IsNullOrEmpty(resumo.Observacoes))
        {
            texto += $"\n📝 Observações: {resumo.Observacoes}\n";
        }

        if (resumo.Produtos != null && resumo.Produtos.Count > 0)
        {
            texto += $"\n🛍️ *PRODUTOS:*\n\n";
            
            for (int i = 0; i < resumo.Produtos.Count; i++)
            {
                var produto = resumo.Produtos[i];
                texto += $"{i + 1}. {produto.DescricaoProduto ?? $"Produto ID: {produto.IdProduto}"}\n";
                
               
                if (produto.Tamanhos != null && produto.Tamanhos.Count > 0)
                {
                    // Exibe cada tamanho com suas informações de etapa e funcionário
                    foreach (var tamanho in produto.Tamanhos)
                    {
                        texto += $" {tamanho.Tamanho} *Qtd*. {tamanho.Quantidade}";
                        
                        if (!string.IsNullOrEmpty(tamanho.DescricaoEtapaProducao))
                        {
                            texto += $" *Etapa* {tamanho.DescricaoEtapaProducao}";
                        }
                        
                        if (!string.IsNullOrEmpty(tamanho.NomeFuncionario))
                        {
                            texto += $" *Resp.* {tamanho.NomeFuncionario}";
                        }
                        
                        texto += "\n";
                    }
                }
                else if (produto.Quantidade.HasValue)
                {
                    texto += $"   Quantidade: {produto.Quantidade.Value}\n";
                }
                
                texto += "\n";
            }
        }

        return texto;
    }

    public static void ConverterBase64ParaJpeg(string base64, string caminhoArquivo)
    {
        if (base64.Contains(","))
            base64 = base64.Substring(base64.IndexOf(',') + 1);

        byte[] bytes = Convert.FromBase64String(base64);

        using var image = Image.Load(bytes);
        image.Save(caminhoArquivo, new JpegEncoder
        {
            Quality = 90
        });
    }

    /// <summary>
    /// Converte uma imagem em base64 para JPEG em formato byte array
    /// </summary>
    private static byte[] ConverterBase64ParaJpegBytes(string base64)
    {
        // Remove o prefixo se existir
        if (base64.Contains(","))
        {
            base64 = base64.Substring(base64.IndexOf(',') + 1);
        }

        // Converte base64 para byte array
        byte[] bytes = Convert.FromBase64String(base64);

        // Carrega a imagem e converte para JPEG
        using var image = Image.Load(bytes);
        using var ms = new MemoryStream();
        image.Save(ms, new JpegEncoder
        {
            Quality = 90
        });
        
        return ms.ToArray();
    }



}


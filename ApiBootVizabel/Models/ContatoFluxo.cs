namespace ApiMensageria.Models;

public class ContatoFluxo
{
    public string IdContato { get; set; } = string.Empty;
    public EstadoFluxo Estado { get; set; } = EstadoFluxo.Inicial;
    public string? Nome { get; set; }
    public DateTime UltimaInteracao { get; set; } = DateTime.Now;
    
    // Dados temporários para cadastro de cliente
    public string? ClienteNome { get; set; }
    public string? ClienteCpfCnpj { get; set; }
    public string? ClienteTelefone { get; set; }
    public string? ClienteEndereco { get; set; }
    public bool UsandoTemplate { get; set; }
    public int? ClienteSelecionadoId { get; set; }
    
    // Dados do cliente encontrado para confirmação
    public string? ClienteEncontradoNome { get; set; }
    public string? ClienteEncontradoCpfCnpj { get; set; }
    
    // Lista temporária de clientes encontrados para seleção
    public List<Cliente>? ClientesEncontrados { get; set; }
    
    // Dados do produto selecionado
    public int? ProdutoSelecionadoId { get; set; }
    public int? GradeSelecionadaId { get; set; }
    
    // Lista temporária de produtos encontrados para seleção
    public List<ProdutoBusca>? ProdutosEncontrados { get; set; }
    
    // Lista temporária de tamanhos disponíveis para seleção
    public List<TamanhoDisponivel>? TamanhosDisponiveis { get; set; }
    
    // Lista de tamanhos e quantidades selecionados para o produto atual
    public List<TamanhoQuantidade>? TamanhosSelecionados { get; set; }
    
    // Quantidade do produto quando não há tamanhos disponíveis
    public int? QuantidadeProduto { get; set; }
    
    // Lista de produtos selecionados no pedido
    public List<ProdutoSelecionado>? ProdutosSelecionados { get; set; }
    
    // Dados de forma de pagamento
    public List<FormaPagamento>? FormasPagamentoDisponiveis { get; set; }
    public int? FormaPagamentoSelecionadaId { get; set; }
    public string? FormaPagamentoSelecionadaDescricao { get; set; }
    
    // Dados de NFe
    public bool? EmitirNfe { get; set; }
    
    // Dados para verificação de pedidos
    public List<Pedido>? PedidosEncontrados { get; set; }
    public int? ClienteIdParaPedidos { get; set; }
    
    // Dados temporários para imagem do produto
    public int? IdPedidoProdutoAtual { get; set; }
    public string? ImagemBase64 { get; set; }
}


namespace ApiMensageria.Models;

public class ResumoPedido
{
    public int IdPedido { get; set; }
    public int CodPedido { get; set; }
    public int IdCliente { get; set; }
    public string? NomeCliente { get; set; }
    public string? CpfCnpjCliente { get; set; }
    public DateTime DataPedido { get; set; }
    public DateTime DataEntrega { get; set; }
    public int IdStatusPedido { get; set; }
    public string? DescricaoStatusPedido { get; set; }
    public int? IdVendedor { get; set; }
    public bool Ativo { get; set; }
    public string? Observacoes { get; set; }
    public int? IdFormaPagamento { get; set; }
    public string? DescricaoFormaPagamento { get; set; }
    public bool? EmitirNfe { get; set; }
    public List<ResumoPedidoProduto>? Produtos { get; set; }
}

public class ResumoPedidoProduto
{
    public int IdProduto { get; set; }
    public int IdPedidoProduto { get; set; }
    public string? DescricaoProduto { get; set; }
    public int? Quantidade { get; set; }
    public int? IdGrade { get; set; }
    public string? DescricaoGrade { get; set; }
    public List<ResumoPedidoTamanho>? Tamanhos { get; set; }
}

public class ResumoPedidoTamanho
{
    public string? Tamanho { get; set; }
    public int Quantidade { get; set; }
    public int? IdGradePedProd { get; set; }
    public int? IdEtapaProducao { get; set; }
    public string? DescricaoEtapaProducao { get; set; }
    public string? NomeFuncionario { get; set; }
}




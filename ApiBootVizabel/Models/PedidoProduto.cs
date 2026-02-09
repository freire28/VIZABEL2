namespace ApiMensageria.Models;

public class PedidoProduto
{
    public int IdPedidoProduto { get; set; }
    public int IdPedido { get; set; }
    public int IdProduto { get; set; }
    public int? Quantidade { get; set; }
    public int? IdEtapaProducao { get; set; }
    public int? IdGrade { get; set; }
    public int? IdFuncionarioResponsavel { get; set; }
    public decimal? ValorVenda { get; set; }
}




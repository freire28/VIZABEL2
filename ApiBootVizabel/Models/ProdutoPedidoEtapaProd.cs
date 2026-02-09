namespace ApiMensageria.Models;

public class ProdutoPedidoEtapaProd
{
    public int Id { get; set; }
    public int IdEtapaProducao { get; set; }
    public int IdPedidoProduto { get; set; }
    public int? IdFuncionario { get; set; }
    public bool Concluido { get; set; }
    public bool? Perda { get; set; }
    public int? QuantidadeProduzida { get; set; }
    public int? QuantidadePerda { get; set; }
    public int? IdPerda { get; set; }
    public int? IdTamanho { get; set; }
    public int? Quantidade { get; set; }
    public int? IdGradePedProd { get; set; }
    public bool? Reposicao { get; set; }
    public DateTime? DataInicio { get; set; }
    public DateTime? DataFim { get; set; }
}





namespace ApiMensageria.Models;

public class PedidoProdutoTamanho
{
    public int IdGradePedProd { get; set; }
    public int IdPedidoProduto { get; set; }
    public int IdGradeTamanho { get; set; }
    public int Quantidade { get; set; }
    public int? IdEtapa { get; set; }
}




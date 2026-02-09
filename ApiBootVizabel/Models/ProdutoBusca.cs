namespace ApiMensageria.Models;

public class ProdutoBusca
{
    public long Indice { get; set; }
    public int IdProduto { get; set; }
    public int? IdGrade { get; set; }
    public string Descricao { get; set; } = string.Empty;
    public bool FabricacaoTerceirizada { get; set; }
}








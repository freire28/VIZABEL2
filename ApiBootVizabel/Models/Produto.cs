namespace ApiMensageria.Models;

public class Produto
{
    public int IdProduto { get; set; }
    public string? Descricao { get; set; }
    public bool? Ativo { get; set; }
    public int? PrazoEntrega { get; set; }
    public bool FabricacaoTerceirizada { get; set; }
    public string? Ncmsh { get; set; }
    public string? Csosn { get; set; }
    public string? Cfop { get; set; }
    public bool? MateriaPrima { get; set; }
}








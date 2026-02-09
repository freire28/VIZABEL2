namespace ApiMensageria.Models;

public class ProdutoSelecionado
{
    public int IdProduto { get; set; }
    public int? IdGrade { get; set; }
    public string? Descricao { get; set; }
    public List<TamanhoQuantidade>? TamanhosQuantidades { get; set; }
    public int? Quantidade { get; set; } // Quando não há tamanhos disponíveis
    public string? ImagemBase64 { get; set; } // Imagem temporária em base64
}







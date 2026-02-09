namespace ApiMensageria.Models;

public class PedidoImagem
{
    public int IdImagem { get; set; }
    public int IdPedidoProduto { get; set; }
    public string? Descricao { get; set; }
    public byte[] Imagem { get; set; } = Array.Empty<byte>();
}


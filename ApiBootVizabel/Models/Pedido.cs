namespace ApiMensageria.Models;

public class Pedido
{
    public int IdPedido { get; set; }
    public int CodPedido { get; set; }
    public int IdCliente { get; set; }
    public DateTime DataPedido { get; set; }
    public DateTime? DataEntrega { get; set; } 
    public int IdStatusPedido { get; set; }
    public int? IdVendedor { get; set; }
    public bool Ativo { get; set; }
    public string? Observacoes { get; set; }
    public int? IdFormaPagamento { get; set; }
    public bool? EmitirNfe { get; set; }
}




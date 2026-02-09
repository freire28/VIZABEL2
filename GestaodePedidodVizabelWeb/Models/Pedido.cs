using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GestaoPedidosVizabel.Models
{
    [Table("PEDIDOS")]
    public class Pedido
    {
        [Key]
        [Column("ID_PEDIDO")]
        [Display(Name = "ID")]
        public int IdPedido { get; set; }

        [Column("COD_PEDIDO")]
        [Display(Name = "Código do Pedido")]
        public int CodPedido { get; set; }

        [Required(ErrorMessage = "O cliente é obrigatório")]
        [Column("ID_CLIENTE")]
        [Display(Name = "Cliente")]
        public int IdCliente { get; set; }

        [Required(ErrorMessage = "A data do pedido é obrigatória")]
        [Column("DATA_PEDIDO")]
        [Display(Name = "Data do Pedido")]
        [DataType(DataType.Date)]
        public DateTime DataPedido { get; set; } = DateTime.Now;

        [Required(ErrorMessage = "A data de entrega é obrigatória")]
        [Column("DATA_ENTREGA")]
        [Display(Name = "Data de Entrega")]
        [DataType(DataType.Date)]
        public DateTime DataEntrega { get; set; } = DateTime.Now.AddDays(7);

        [Required(ErrorMessage = "O status do pedido é obrigatório")]
        [Column("ID_STATUSPEDIDO")]
        [Display(Name = "Status")]
        public int IdStatuspedido { get; set; }

        [Column("ID_VENDEDOR")]
        [Display(Name = "Vendedor")]
        public int? IdVendedor { get; set; }

        [Column("ID_FORMAPAGAMENTO")]
        [Display(Name = "Forma de Pagamento")]
        public int? IdFormapagamento { get; set; }

        [Column("ATIVO")]
        [Display(Name = "Ativo")]
        public bool Ativo { get; set; } = true;

        [Column("OBSERVACOES")]
        [Display(Name = "Observações")]
        [StringLength(1000)]
        [DataType(DataType.MultilineText)]
        public string? Observacoes { get; set; }

        [Column("EMITIR_NFE")]
        [Display(Name = "Emitir NF-e")]
        public bool? EmitirNfe { get; set; }

        // Propriedades de navegação
        [ForeignKey("IdCliente")]
        public virtual Cliente? Cliente { get; set; }

        [ForeignKey("IdStatuspedido")]
        public virtual StatusPedido? StatusPedido { get; set; }

        [ForeignKey("IdVendedor")]
        public virtual Funcionario? Vendedor { get; set; }

        [ForeignKey("IdFormapagamento")]
        public virtual FormaPagamento? FormaPagamento { get; set; }

        public virtual ICollection<PedidoProduto> PedidoProdutos { get; set; } = new List<PedidoProduto>();
    }
}


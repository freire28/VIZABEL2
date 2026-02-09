using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GestaoPedidosVizabel.Models
{
    [Table("PEDIDO_IMAGENS")]
    public class PedidoImagem
    {
        [Key]
        [Column("ID_IMAGEM")]
        [Display(Name = "ID")]
        public int IdImagem { get; set; }

        [Required]
        [Column("ID_PEDIDOPRODUTO")]
        [Display(Name = "Pedido Produto")]
        public int IdPedidoproduto { get; set; }

        [Column("DESCRICAO")]
        [Display(Name = "Descrição")]
        [StringLength(60)]
        public string? Descricao { get; set; }

        [Required]
        [Column("IMAGEM")]
        [Display(Name = "Imagem")]
        public byte[] Imagem { get; set; } = Array.Empty<byte>();

        // Propriedades de navegação
        [ForeignKey("IdPedidoproduto")]
        public virtual PedidoProduto? PedidoProduto { get; set; }
    }
}


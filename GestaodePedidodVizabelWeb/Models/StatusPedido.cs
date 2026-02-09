using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GestaoPedidosVizabel.Models
{
    [Table("STATUS_PEDIDO")]
    public class StatusPedido
    {
        [Key]
        [Column("ID_STATUSPEDIDO")]
        [Display(Name = "ID")]
        public int IdStatuspedido { get; set; }

        [Required(ErrorMessage = "A descrição é obrigatória")]
        [Column("DESCRICAO")]
        [Display(Name = "Descrição")]
        [StringLength(60)]
        public string Descricao { get; set; } = string.Empty;

        [Column("ATIVO")]
        [Display(Name = "Ativo")]
        public bool Ativo { get; set; } = true;
    }
}





















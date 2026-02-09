using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GestaoPedidosVizabel.Models
{
    [Table("FORMAS_PAGAMENTO")]
    public class FormaPagamento
    {
        [Key]
        [Column("ID_FORMAPAGAMENTO")]
        [Display(Name = "ID")]
        public int IdFormapagamento { get; set; }

        [Column("CODIGO")]
        [Display(Name = "Código")]
        [StringLength(2)]
        public string? Codigo { get; set; }

        [Required(ErrorMessage = "A descrição é obrigatória")]
        [Column("DESCRICAO")]
        [Display(Name = "Descrição")]
        [StringLength(10)]
        public string Descricao { get; set; } = string.Empty;

        [Column("ATIVO")]
        [Display(Name = "Ativo")]
        public bool Ativo { get; set; } = true;
    }
}



















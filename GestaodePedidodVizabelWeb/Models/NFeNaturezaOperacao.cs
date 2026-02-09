using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GestaoPedidosVizabel.Models
{
    [Table("NFE_NATUREZA_OPERACAO")]
    public class NFeNaturezaOperacao
    {
        [Key]
        [Column("ID_NATUREZA")]
        [Display(Name = "ID")]
        public int IdNatureza { get; set; }

        [Required(ErrorMessage = "A descrição é obrigatória")]
        [Column("DESCRICAO")]
        [Display(Name = "Descrição")]
        [StringLength(255)]
        public string Descricao { get; set; } = string.Empty;

        [Column("CFOP")]
        [Display(Name = "CFOP")]
        [StringLength(4)]
        public string? Cfop { get; set; }

        [Column("ATIVO")]
        [Display(Name = "Ativo")]
        public bool Ativo { get; set; } = true;
    }
}


using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GestaoPedidosVizabel.Models
{
    [Table("TABELA_IPT")]
    public class TabelaIpt
    {
        [Key]
        [Column("CODIGO")]
        [Display(Name = "Código")]
        [StringLength(20)]
        public string Codigo { get; set; } = string.Empty;

        [Column("DESCRICAO")]
        [Display(Name = "Descrição")]
        [StringLength(255)]
        public string? Descricao { get; set; }

        [Column("ATIVO")]
        [Display(Name = "Ativo")]
        public bool? Ativo { get; set; } = true;
    }
}


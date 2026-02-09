using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GestaoPedidosVizabel.Models
{
    [Table("NFE_PAGAMENTO")]
    public class NFePagamento
    {
        [Key]
        [Column("ID_PAGAMENTO")]
        [Display(Name = "ID")]
        public long IdPagamento { get; set; }

        [Required]
        [Column("ID_NFE")]
        [Display(Name = "NF-e")]
        public long IdNfe { get; set; }

        [Required]
        [Column("TIPO_PAGAMENTO")]
        [Display(Name = "Tipo de Pagamento")]
        [StringLength(2)]
        public string TipoPagamento { get; set; } = string.Empty;

        [Required]
        [Column("VALOR_PAGO", TypeName = "decimal(15,2)")]
        [Display(Name = "Valor Pago")]
        public decimal ValorPago { get; set; }

        // Propriedade de navegação
        [ForeignKey("IdNfe")]
        public virtual NFe? NFe { get; set; }
    }
}







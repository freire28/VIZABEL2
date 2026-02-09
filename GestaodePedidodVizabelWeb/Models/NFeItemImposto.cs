using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GestaoPedidosVizabel.Models
{
    [Table("NFE_ITEM_IMPOSTO")]
    public class NFeItemImposto
    {
        [Key]
        [Column("ID_IMPOSTO")]
        [Display(Name = "ID")]
        public long IdImposto { get; set; }

        [Required]
        [Column("ID_ITEM")]
        [Display(Name = "Item")]
        public long IdItem { get; set; }

        [Required]
        [Column("ORIGEM")]
        [Display(Name = "Origem")]
        public byte Origem { get; set; }

        [Column("CST_CSOSN")]
        [Display(Name = "CST/CSOSN")]
        [StringLength(3)]
        public string CstCsosn { get; set; } = "000";

        [Required]
        [Column("BASE_ICMS", TypeName = "decimal(15,2)")]
        [Display(Name = "Base ICMS")]
        public decimal BaseIcms { get; set; } = 0;

        [Required]
        [Column("ALIQUOTA_ICMS", TypeName = "decimal(5,2)")]
        [Display(Name = "Alíquota ICMS")]
        public decimal AliquotaIcms { get; set; } = 0;

        [Required]
        [Column("VALOR_ICMS", TypeName = "decimal(15,2)")]
        [Display(Name = "Valor ICMS")]
        public decimal ValorIcms { get; set; } = 0;

        [Column("BASE_PIS", TypeName = "decimal(15,2)")]
        [Display(Name = "Base PIS")]
        public decimal? BasePis { get; set; }

        [Column("VALOR_PIS", TypeName = "decimal(15,2)")]
        [Display(Name = "Valor PIS")]
        public decimal? ValorPis { get; set; }

        [Column("BASE_COFINS", TypeName = "decimal(15,2)")]
        [Display(Name = "Base COFINS")]
        public decimal? BaseCofins { get; set; }

        [Column("VALOR_COFINS", TypeName = "decimal(15,2)")]
        [Display(Name = "Valor COFINS")]
        public decimal? ValorCofins { get; set; }

        // Propriedade de navegação
        [ForeignKey("IdItem")]
        public virtual NFeItem? Item { get; set; }
    }
}

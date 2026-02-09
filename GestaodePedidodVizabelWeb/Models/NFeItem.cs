using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GestaoPedidosVizabel.Models
{
    [Table("NFE_ITEM")]
    public class NFeItem
    {
        [Key]
        [Column("ID_ITEM")]
        [Display(Name = "ID")]
        public long IdItem { get; set; }

        [Required]
        [Column("ID_NFE")]
        [Display(Name = "NF-e")]
        public long IdNfe { get; set; }

        [Required]
        [Column("COD_PRODUTO")]
        [Display(Name = "Código do Produto")]
        [StringLength(60)]
        public string CodProduto { get; set; } = string.Empty;

        [Required]
        [Column("DESCRICAO")]
        [Display(Name = "Descrição")]
        [StringLength(200)]
        public string Descricao { get; set; } = string.Empty;

        [Required]
        [Column("NCM")]
        [Display(Name = "NCM")]
        [StringLength(8)]
        public string Ncm { get; set; } = string.Empty;

        [Required]
        [Column("CFOP")]
        [Display(Name = "CFOP")]
        [StringLength(4)]
        public string Cfop { get; set; } = string.Empty;

        [Required]
        [Column("UNIDADE")]
        [Display(Name = "Unidade")]
        [StringLength(10)]
        public string Unidade { get; set; } = string.Empty;

        [Required]
        [Column("QUANTIDADE", TypeName = "decimal(15,4)")]
        [Display(Name = "Quantidade")]
        public decimal Quantidade { get; set; }

        [Required]
        [Column("VALOR_UNITARIO", TypeName = "decimal(15,4)")]
        [Display(Name = "Valor Unitário")]
        public decimal ValorUnitario { get; set; }

        [Required]
        [Column("VALOR_TOTAL", TypeName = "decimal(15,2)")]
        [Display(Name = "Valor Total")]
        public decimal ValorTotal { get; set; }

        // Propriedade de navegação
        [ForeignKey("IdNfe")]
        public virtual NFe? NFe { get; set; }

        // Relação 1:1 com Imposto
        public virtual NFeItemImposto? Imposto { get; set; }
    }
}



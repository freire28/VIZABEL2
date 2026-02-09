using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GestaoPedidosVizabel.Models
{
    [Table("PRODUTOS")]
    public class Produto
    {
        [Key]
        [Column("ID_PRODUTO")]
        [Display(Name = "ID")]
        public int IdProduto { get; set; }

        [Column("DESCRICAO")]
        [Display(Name = "Descrição")]
        [StringLength(200)]
        public string? Descricao { get; set; }

        [Column("ATIVO")]
        [Display(Name = "Ativo")]
        public bool? Ativo { get; set; } = true;

        [Column("PRAZO_ENTREGA")]
        [Display(Name = "Prazo de Entrega (dias)")]
        [Range(0, int.MaxValue, ErrorMessage = "O prazo de entrega deve ser maior ou igual a zero")]
        public int? PrazoEntrega { get; set; }

        [Column("FABRICACAO_TERCEIRIZADA")]
        [Display(Name = "Fabricação Terceirizada")]
        public bool FabricacaoTerceirizada { get; set; }

        [Column("NCMSH")]
        [Display(Name = "NCM/SH")]
        [StringLength(20)]
        public string? Ncmsh { get; set; }

        [Column("CSOSN")]
        [Display(Name = "CSOSN")]
        [StringLength(20)]
        public string? Csosn { get; set; }

        [Column("CFOP")]
        [Display(Name = "CFOP")]
        [StringLength(20)]
        public string? Cfop { get; set; }

        // Navegação
        public virtual ICollection<ProdutoEtapaProducao> ProdutoEtapasProducao { get; set; } = new List<ProdutoEtapaProducao>();
        public virtual ICollection<ProdutoGrade> ProdutoGrades { get; set; } = new List<ProdutoGrade>();
    }
}


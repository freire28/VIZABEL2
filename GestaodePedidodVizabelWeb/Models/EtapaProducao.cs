using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GestaoPedidosVizabel.Models
{
    [Table("ETAPAS_PRODUCAO")]
    public class EtapaProducao
    {
        [Key]
        [Column("ID_ETAPA")]
        [Display(Name = "ID")]
        public int IdEtapa { get; set; }

        [Required(ErrorMessage = "A descrição é obrigatória")]
        [Column("DESCRICAO")]
        [Display(Name = "Descrição")]
        [StringLength(60)]
        public string Descricao { get; set; } = string.Empty;

        [Column("ATIVO")]
        [Display(Name = "Ativo")]
        public bool Ativo { get; set; } = true;

        [Required(ErrorMessage = "A quantidade de dias é obrigatória")]
        [Column("QUANTIDADE_DIAS")]
        [Display(Name = "Quantidade de Dias")]
        [Range(0, int.MaxValue, ErrorMessage = "A quantidade de dias deve ser maior ou igual a zero")]
        public int QuantidadeDias { get; set; }

        [Column("ID_FUNCAO")]
        [Display(Name = "ID Função")]
        public int? IdFuncao { get; set; }

        // Navegação
        public virtual ICollection<ProdutoEtapaProducao> ProdutoEtapasProducao { get; set; } = new List<ProdutoEtapaProducao>();
    }
}


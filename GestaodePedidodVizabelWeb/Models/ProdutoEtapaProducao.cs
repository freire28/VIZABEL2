using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GestaoPedidosVizabel.Models
{
    [Table("PRODUTOS_ETAPAS_PRODUCAO")]
    public class ProdutoEtapaProducao
    {
        [Key]
        [Column("ID_PRODUTO_ETAPA")]
        [Display(Name = "ID")]
        public int IdProdutoEtapa { get; set; }

        [Column("ID_PRODUTO")]
        [Display(Name = "Produto")]
        public int? IdProduto { get; set; }

        [Column("ID_ETAPA")]
        [Display(Name = "Etapa")]
        public int? IdEtapa { get; set; }

        [Column("SEQUENCIA")]
        [Display(Name = "Sequência")]
        [Range(1, int.MaxValue, ErrorMessage = "A sequência deve ser maior que zero")]
        public int? Sequencia { get; set; }

        [Column("ATIVO")]
        [Display(Name = "Ativo")]
        public bool Ativo { get; set; } = true;

        // Navegação
        public virtual Produto? Produto { get; set; }
        public virtual EtapaProducao? EtapaProducao { get; set; }
    }
}





















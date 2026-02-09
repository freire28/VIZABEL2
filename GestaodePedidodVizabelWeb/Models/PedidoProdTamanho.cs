using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GestaoPedidosVizabel.Models
{
    [Table("PEDIDO_PROD_TAMANHOS")]
    public class PedidoProdTamanho
    {
        [Column("ID_GRADE_PED_PROD")]
        [Display(Name = "ID Grade Ped Prod")]
        public int IdGradePedProd { get; set; }

        [Column("ID_PEDIDOPRODUTO")]
        [Display(Name = "ID Pedido Produto")]
        public int IdPedidoproduto { get; set; }

        [Required]
        [Column("ID_GRADE_TAMANHO")]
        [Display(Name = "Grade Tamanho")]
        public int IdGradeTamanho { get; set; }

        [Required]
        [Column("QUANTIDADE")]
        [Display(Name = "Quantidade")]
        public int Quantidade { get; set; }

        [Column("ID_ETAPA")]
        [Display(Name = "Etapa")]
        public int? IdEtapa { get; set; }

        // Propriedades de navegação
        [ForeignKey("IdPedidoproduto")]
        public virtual PedidoProduto? PedidoProduto { get; set; }

        [ForeignKey("IdGradeTamanho")]
        public virtual GradeTamanho? GradeTamanho { get; set; }

        [ForeignKey("IdEtapa")]
        public virtual EtapaProducao? Etapa { get; set; }
    }
}


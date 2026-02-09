using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GestaoPedidosVizabel.Models
{
    [Table("PRODUTO_PEDIDO_ETAPA_PROD")]
    public class ProdutoPedidoEtapaProd
    {
        [Key]
        [Column("ID")]
        [Display(Name = "ID")]
        public int Id { get; set; }

        [Required]
        [Column("ID_ETAPA_PRODUCAO")]
        [Display(Name = "Etapa de Produção")]
        public int IdEtapaProducao { get; set; }

        [Required]
        [Column("ID_PEDIDO_PRODUTO")]
        [Display(Name = "Pedido Produto")]
        public int IdPedidoProduto { get; set; }

        [Column("ID_FUNCIONARIO")]
        [Display(Name = "Funcionário")]
        public int? IdFuncionario { get; set; }

        [Column("CONCLUIDO")]
        [Display(Name = "Concluído")]
        public bool Concluido { get; set; } = false;

        [Required]
        [Column("ID_GRADE_PED_PROD")]
        [Display(Name = "Grade Ped Prod")]
        public int IdGradePedProd { get; set; }

        [Column("QUANTIDADE")]
        [Display(Name = "Quantidade")]
        public int Quantidade { get; set; }

        [Column("ID_TAMANHO")]
        [Display(Name = "Tamanho")]
        public int? IdTamanho { get; set; }

        [Column("REPOSICAO")]
        [Display(Name = "Reposição")]
        public bool Reposicao { get; set; } = false;

        // Propriedades de navegação
        [ForeignKey("IdEtapaProducao")]
        public virtual EtapaProducao? EtapaProducao { get; set; }

        [ForeignKey("IdPedidoProduto")]
        public virtual PedidoProduto? PedidoProduto { get; set; }

        [ForeignKey("IdFuncionario")]
        public virtual Funcionario? Funcionario { get; set; }
    }
}


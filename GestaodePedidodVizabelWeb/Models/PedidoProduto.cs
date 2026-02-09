using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GestaoPedidosVizabel.Models
{
    [Table("PEDIDO_PRODUTOS")]
    public class PedidoProduto
    {
        [Key]
        [Column("ID_PEDIDOPRODUTO")]
        [Display(Name = "ID")]
        public int IdPedidoproduto { get; set; }

        [Required]
        [Column("ID_PEDIDO")]
        [Display(Name = "Pedido")]
        public int IdPedido { get; set; }

        [Required]
        [Column("ID_PRODUTO")]
        [Display(Name = "Produto")]
        public int IdProduto { get; set; }

        [Column("ID_GRADE")]
        [Display(Name = "Grade (SubProduto)")]
        public int? IdGrade { get; set; }

        [Column("QUANTIDADE")]
        [Display(Name = "Quantidade")]
        public int? Quantidade { get; set; }

        [Column("ID_ETAPA_PRODUCAO")]
        [Display(Name = "Etapa de Produção")]
        public int? IdEtapaProducao { get; set; }

        [Column("ID_FUNCIONARIO_RESPONSAVEL")]
        [Display(Name = "Funcionário Responsável")]
        public int? IdFuncionarioResponsavel { get; set; }

        [Column("VALOR_VENDA")]
        [Display(Name = "Valor de Venda")]
        [DataType(DataType.Currency)]
        public decimal? ValorVenda { get; set; }

        // Propriedades de navegação
        [ForeignKey("IdPedido")]
        public virtual Pedido? Pedido { get; set; }

        [ForeignKey("IdProduto")]
        public virtual Produto? Produto { get; set; }

        [ForeignKey("IdGrade")]
        public virtual Grade? Grade { get; set; }

        [ForeignKey("IdEtapaProducao")]
        public virtual EtapaProducao? EtapaProducao { get; set; }

        [ForeignKey("IdFuncionarioResponsavel")]
        public virtual Funcionario? FuncionarioResponsavel { get; set; }

        public virtual ICollection<PedidoProdTamanho> PedidoProdTamanhos { get; set; } = new List<PedidoProdTamanho>();
        public virtual ICollection<PedidoImagem> PedidoImagens { get; set; } = new List<PedidoImagem>();
    }
}



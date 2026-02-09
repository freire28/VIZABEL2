using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GestaoPedidosVizabel.Models
{
    [Table("NFE")]
    public class NFe
    {
        [Key]
        [Column("ID_NFE")]
        [Display(Name = "ID")]
        public long IdNfe { get; set; }

        [Required]
        [Column("ID_EMPRESA")]
        [Display(Name = "Empresa")]
        public int IdEmpresa { get; set; }

        [Required]
        [Column("MODELO")]
        [Display(Name = "Modelo")]
        [StringLength(2)]
        public string Modelo { get; set; } = "55";

        [Required]
        [Column("SERIE")]
        [Display(Name = "Série")]
        public int Serie { get; set; }

        [Required]
        [Column("NUMERO")]
        [Display(Name = "Número")]
        public int Numero { get; set; }

        [Required]
        [Column("NATUREZA_OPERACAO")]
        [Display(Name = "Natureza da Operação")]
        [StringLength(100)]
        public string NaturezaOperacao { get; set; } = string.Empty;

        [Required]
        [Column("DATA_EMISSAO")]
        [Display(Name = "Data de Emissão")]
        [DataType(DataType.DateTime)]
        public DateTime DataEmissao { get; set; }

        [Required]
        [Column("TIPO_NFE")]
        [Display(Name = "Tipo NF-e")]
        public byte TipoNfe { get; set; }

        [Required]
        [Column("FINALIDADE")]
        [Display(Name = "Finalidade")]
        public byte Finalidade { get; set; }

        [Required]
        [Column("AMBIENTE")]
        [Display(Name = "Ambiente")]
        public byte Ambiente { get; set; }

        [Required]
        [Column("VALOR_PRODUTOS", TypeName = "decimal(15,2)")]
        [Display(Name = "Valor dos Produtos")]
        public decimal ValorProdutos { get; set; }

        [Required]
        [Column("VALOR_TOTAL_NFE", TypeName = "decimal(15,2)")]
        [Display(Name = "Valor Total NF-e")]
        public decimal ValorTotalNfe { get; set; }

        [Required]
        [Column("STATUS")]
        [Display(Name = "Status")]
        [StringLength(20)]
        public string Status { get; set; } = string.Empty;

        [Column("CHAVE_ACESSO")]
        [Display(Name = "Chave de Acesso")]
        [StringLength(255)]
        public string? ChaveAcesso { get; set; }

        [Column("DATA_SAIDA")]
        [Display(Name = "Data de Saída")]
        [DataType(DataType.DateTime)]
        public DateTime? DataSaida { get; set; }

        [Column("REGIME_TRIBUTARIO")]
        [Display(Name = "Regime Tributário")]
        [StringLength(50)]
        public string? RegimeTributario { get; set; }

        [Column("ID_PEDIDO")]
        [Display(Name = "Pedido")]
        public int? IdPedido { get; set; }

        // Propriedades de navegação
        [ForeignKey("IdPedido")]
        public virtual Pedido? Pedido { get; set; }

        public virtual NFeDestinatario? Destinatario { get; set; }
        public virtual ICollection<NFeItem> Itens { get; set; } = new List<NFeItem>();
        public virtual ICollection<NFePagamento> Pagamentos { get; set; } = new List<NFePagamento>();
    }
}


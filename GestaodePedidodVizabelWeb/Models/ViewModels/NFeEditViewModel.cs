using System.ComponentModel.DataAnnotations;

namespace GestaoPedidosVizabel.Models.ViewModels
{
    public class NFeEditViewModel
    {
        [Required]
        public long IdNfe { get; set; }

        [Required(ErrorMessage = "O campo Empresa é obrigatório")]
        [Display(Name = "Empresa")]
        public int IdEmpresa { get; set; }

        [Required(ErrorMessage = "O campo Modelo é obrigatório")]
        [Display(Name = "Modelo")]
        [StringLength(2)]
        public string Modelo { get; set; } = "55";

        [Required(ErrorMessage = "O campo Série é obrigatório")]
        [Display(Name = "Série")]
        public int Serie { get; set; }

        [Required(ErrorMessage = "O campo Número é obrigatório")]
        [Display(Name = "Número")]
        public int Numero { get; set; }

        [Required(ErrorMessage = "O campo Natureza da Operação é obrigatório")]
        [Display(Name = "Natureza da Operação")]
        [StringLength(100)]
        public string NaturezaOperacao { get; set; } = string.Empty;

        [Required(ErrorMessage = "O campo Data de Emissão é obrigatório")]
        [Display(Name = "Data de Emissão")]
        [DataType(DataType.DateTime)]
        public DateTime DataEmissao { get; set; }

        [Required(ErrorMessage = "O campo Tipo NF-e é obrigatório")]
        [Display(Name = "Tipo NF-e")]
        [Range(0, 255, ErrorMessage = "O tipo deve estar entre 0 e 255")]
        public byte TipoNfe { get; set; }

        [Required(ErrorMessage = "O campo Finalidade é obrigatório")]
        [Display(Name = "Finalidade")]
        [Range(0, 255, ErrorMessage = "A finalidade deve estar entre 0 e 255")]
        public byte Finalidade { get; set; }

        [Required(ErrorMessage = "O campo Valor dos Produtos é obrigatório")]
        [Display(Name = "Valor dos Produtos")]
        [Range(0.01, double.MaxValue, ErrorMessage = "O valor deve ser maior que zero")]
        public decimal ValorProdutos { get; set; }

        [Required(ErrorMessage = "O campo Valor Total NF-e é obrigatório")]
        [Display(Name = "Valor Total NF-e")]
        [Range(0.01, double.MaxValue, ErrorMessage = "O valor deve ser maior que zero")]
        public decimal ValorTotalNfe { get; set; }

        [Required(ErrorMessage = "O campo Status é obrigatório")]
        [Display(Name = "Status")]
        [StringLength(20)]
        public string Status { get; set; } = string.Empty;

        [Display(Name = "Chave de Acesso")]
        [StringLength(255)]
        public string? ChaveAcesso { get; set; }

        [Display(Name = "Data de Saída")]
        [DataType(DataType.DateTime)]
        public DateTime? DataSaida { get; set; }

        [Display(Name = "Regime Tributário")]
        [StringLength(50)]
        public string? RegimeTributario { get; set; }

        [Display(Name = "Pedido")]
        public int? IdPedido { get; set; }

        // Dados do Pedido (para verificar EmitirNfe e StatusPedido)
        public bool? EmitirNfe { get; set; }
        public StatusPedidoViewModel? StatusPedido { get; set; }

        // Dados do Destinatário
        public NFeDestinatarioViewModel? Destinatario { get; set; }

        // Itens existentes
        public List<NFeItemEditViewModel> Itens { get; set; } = new List<NFeItemEditViewModel>();

        // Novos itens a serem adicionados
        public List<NFeItemEditViewModel> NovosItens { get; set; } = new List<NFeItemEditViewModel>();

        // IDs dos itens removidos
        public List<long> ItensRemovidos { get; set; } = new List<long>();

        // Pagamentos da NF-e
        public List<NFePagamentoViewModel> Pagamentos { get; set; } = new List<NFePagamentoViewModel>();

        // IDs dos pagamentos removidos
        public List<long> PagamentosRemovidos { get; set; } = new List<long>();

        // Novos pagamentos a serem adicionados
        public List<NFePagamentoViewModel> NovosPagamentos { get; set; } = new List<NFePagamentoViewModel>();
    }

    public class NFeItemEditViewModel
    {
        public long? IdItem { get; set; }

        public long IdNfe { get; set; } // Será preenchido automaticamente no serviço

        [Required(ErrorMessage = "O campo Código do Produto é obrigatório")]
        [Display(Name = "Código do Produto")]
        [StringLength(60)]
        public string CodProduto { get; set; } = string.Empty;

        [Required(ErrorMessage = "O campo Descrição é obrigatório")]
        [Display(Name = "Descrição")]
        [StringLength(200)]
        public string Descricao { get; set; } = string.Empty;

        [Required(ErrorMessage = "O campo NCM é obrigatório")]
        [Display(Name = "NCM")]
        [StringLength(8, MinimumLength = 8, ErrorMessage = "O NCM deve ter 8 caracteres")]
        [Validacoes.NcmAttribute]
        public string Ncm { get; set; } = string.Empty;

        [Required(ErrorMessage = "O campo CFOP é obrigatório")]
        [Display(Name = "CFOP")]
        [StringLength(4, MinimumLength = 4, ErrorMessage = "O CFOP deve ter 4 caracteres")]
        [Validacoes.CfopAttribute]
        public string Cfop { get; set; } = string.Empty;

        [Required(ErrorMessage = "O campo Unidade é obrigatório")]
        [Display(Name = "Unidade")]
        [StringLength(10)]
        public string Unidade { get; set; } = string.Empty;

        [Required(ErrorMessage = "O campo Quantidade é obrigatório")]
        [Display(Name = "Quantidade")]
        [Range(0.0001, double.MaxValue, ErrorMessage = "A quantidade deve ser maior que zero")]
        public decimal Quantidade { get; set; }

        [Required(ErrorMessage = "O campo Valor Unitário é obrigatório")]
        [Display(Name = "Valor Unitário")]
        [Range(0.0001, double.MaxValue, ErrorMessage = "O valor unitário deve ser maior que zero")]
        public decimal ValorUnitario { get; set; }

        [Required(ErrorMessage = "O campo Valor Total é obrigatório")]
        [Display(Name = "Valor Total")]
        [Range(0.01, double.MaxValue, ErrorMessage = "O valor total deve ser maior que zero")]
        public decimal ValorTotal { get; set; }

        // Impostos do item
        public NFeItemImpostoViewModel? Imposto { get; set; }
    }

    public class NFeItemImpostoViewModel
    {
        public long? IdImposto { get; set; }

        public long IdItem { get; set; } // Será preenchido automaticamente no serviço

        [Display(Name = "Origem")]
        [Range(0, 255, ErrorMessage = "A origem deve estar entre 0 e 255")]
        public byte Origem { get; set; }

        [Display(Name = "CST/CSOSN")]
        [StringLength(3, ErrorMessage = "O CST/CSOSN deve ter no máximo 3 caracteres")]
        public string? CstCsosn { get; set; }

        [Display(Name = "Base ICMS")]
        [Range(0, double.MaxValue, ErrorMessage = "A base ICMS deve ser maior ou igual a zero")]
        public decimal BaseIcms { get; set; }

        [Display(Name = "Alíquota ICMS")]
        [Range(0, 100, ErrorMessage = "A alíquota ICMS deve estar entre 0 e 100")]
        public decimal AliquotaIcms { get; set; }

        [Display(Name = "Valor ICMS")]
        [Range(0, double.MaxValue, ErrorMessage = "O valor ICMS deve ser maior ou igual a zero")]
        public decimal ValorIcms { get; set; }

        [Display(Name = "Base PIS")]
        [Range(0, double.MaxValue, ErrorMessage = "A base PIS deve ser maior ou igual a zero")]
        public decimal? BasePis { get; set; }

        [Display(Name = "Valor PIS")]
        [Range(0, double.MaxValue, ErrorMessage = "O valor PIS deve ser maior ou igual a zero")]
        public decimal? ValorPis { get; set; }

        [Display(Name = "Base COFINS")]
        [Range(0, double.MaxValue, ErrorMessage = "A base COFINS deve ser maior ou igual a zero")]
        public decimal? BaseCofins { get; set; }

        [Display(Name = "Valor COFINS")]
        [Range(0, double.MaxValue, ErrorMessage = "O valor COFINS deve ser maior ou igual a zero")]
        public decimal? ValorCofins { get; set; }
    }

    public class StatusPedidoViewModel
    {
        public int IdStatuspedido { get; set; }
        public string Descricao { get; set; } = string.Empty;
    }
}


using System.ComponentModel.DataAnnotations;

namespace GestaoPedidosVizabel.Models.ViewModels
{
    public class NFeCreateViewModel
    {
        [Display(Name = "Empresa")]
        public int IdEmpresa { get; set; } = 1; // Valor padrão

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
        public DateTime DataEmissao { get; set; } = DateTime.Now;

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

        // Dados do Destinatário
        public NFeDestinatarioViewModel? Destinatario { get; set; }

        // Itens da NF-e
        public List<NFeItemEditViewModel> Itens { get; set; } = new List<NFeItemEditViewModel>();

        // Pagamentos da NF-e
        public List<NFePagamentoViewModel> Pagamentos { get; set; } = new List<NFePagamentoViewModel>();
    }

    public class NFePagamentoViewModel
    {
        public long? IdPagamento { get; set; }

        [Display(Name = "Tipo de Pagamento")]
        [StringLength(2, ErrorMessage = "O tipo de pagamento deve ter no máximo 2 caracteres")]
        public string? TipoPagamento { get; set; }

        [Display(Name = "Valor Pago")]
        [Range(0, double.MaxValue, ErrorMessage = "O valor deve ser maior ou igual a zero")]
        public decimal ValorPago { get; set; }
    }

    public class NFeDestinatarioViewModel
    {
        [Required(ErrorMessage = "O campo Nome é obrigatório")]
        [Display(Name = "Nome")]
        [StringLength(150)]
        public string Nome { get; set; } = string.Empty;

        [Required(ErrorMessage = "O campo CNPJ/CPF é obrigatório")]
        [Display(Name = "CNPJ/CPF")]
        [StringLength(14)]
        public string CnpjCpf { get; set; } = string.Empty;

        [Required(ErrorMessage = "O campo Indicador IE é obrigatório")]
        [Display(Name = "Indicador IE Destinatário")]
        [Range(1, 9, ErrorMessage = "O indicador deve ser 1, 2 ou 9")]
        public byte IndIeDest { get; set; }

        [Display(Name = "Inscrição Estadual")]
        [StringLength(20)]
        public string? Ie { get; set; }

        [Required(ErrorMessage = "O campo Logradouro é obrigatório")]
        [Display(Name = "Logradouro")]
        [StringLength(150)]
        public string Logradouro { get; set; } = string.Empty;

        [Required(ErrorMessage = "O campo Número é obrigatório")]
        [Display(Name = "Número")]
        [StringLength(10)]
        public string Numero { get; set; } = string.Empty;

        [Required(ErrorMessage = "O campo Bairro é obrigatório")]
        [Display(Name = "Bairro")]
        [StringLength(100)]
        public string Bairro { get; set; } = string.Empty;

        [Display(Name = "Código do Município")]
        public int? CodMun { get; set; }

        [Required(ErrorMessage = "O campo Município é obrigatório")]
        [Display(Name = "Município")]
        [StringLength(100)]
        public string Municipio { get; set; } = string.Empty;

        [Required(ErrorMessage = "O campo UF é obrigatório")]
        [Display(Name = "UF")]
        [StringLength(2, MinimumLength = 2, ErrorMessage = "A UF deve ter 2 caracteres")]
        public string Uf { get; set; } = string.Empty;

        [Required(ErrorMessage = "O campo CEP é obrigatório")]
        [Display(Name = "CEP")]
        [StringLength(8, MinimumLength = 8, ErrorMessage = "O CEP deve ter 8 caracteres")]
        public string Cep { get; set; } = string.Empty;
    }
}


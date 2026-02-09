using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using GestaoPedidosVizabel.Models.Validacoes;

namespace GestaoPedidosVizabel.Models
{
    [Table("CLIENTES")]
    [CpfCnpj(ErrorMessage = "CPF ou CNPJ inválido conforme o tipo de pessoa")]
    public class Cliente
    {
        [Key]
        [Column("ID_CLIENTE")]
        [Display(Name = "ID")]
        public int IdCliente { get; set; }

        [Required(ErrorMessage = "O nome/razão social é obrigatório")]
        [Column("NOMERAZAO")]
        [Display(Name = "Nome / Razão Social")]
        [StringLength(250)]
        public string Nomerazao { get; set; } = string.Empty;

        [Column("TIPO_PESSOA")]
        [Display(Name = "Tipo de Pessoa")]
        public bool TipoPessoa { get; set; } // false = Física, true = Jurídica

        [Column("FANTASIA")]
        [Display(Name = "Nome Fantasia")]
        [StringLength(60)]
        public string? Fantasia { get; set; }

        [Column("CPFCNPJ")]
        [Display(Name = "CPF/CNPJ")]
        [StringLength(20)]
        public string? Cpfcnpj { get; set; }

        [Column("RG_IE")]
        [Display(Name = "RG / IE")]
        [StringLength(60)]
        public string? RgIe { get; set; }

        [Column("ENDERECO_LOGRADOURO")]
        [Display(Name = "Logradouro")]
        [StringLength(255)]
        public string? EnderecoLogradouro { get; set; }

        [Column("ENDERECO_NUMERO")]
        [Display(Name = "Número")]
        [StringLength(10)]
        public string? EnderecoNumero { get; set; }

        [Column("ENDERECO_COMPLEMENTO")]
        [Display(Name = "Complemento")]
        [StringLength(60)]
        public string? EnderecoComplemento { get; set; }

        [Column("ENDERECO_BAIRRO")]
        [Display(Name = "Bairro")]
        [StringLength(60)]
        public string? EnderecoBairro { get; set; }

        [Column("ENDERECO_CIDADE")]
        [Display(Name = "Cidade")]
        [StringLength(60)]
        public string? EnderecoCidade { get; set; }

        [Column("ENDERECO_UF")]
        [Display(Name = "UF")]
        [StringLength(2)]
        public string? EnderecoUf { get; set; }

        [Column("ENDERECO_CEP")]
        [Display(Name = "CEP")]
        [StringLength(10)]
        public string? EnderecoCep { get; set; }

        [Column("ENDERECO_IBGE")]
        [Display(Name = "Código IBGE")]
        [StringLength(10)]
        public string? EnderecoIbge { get; set; }

        [Column("EMAIL")]
        [Display(Name = "E-mail")]
        [EmailAddress(ErrorMessage = "E-mail inválido")]
        [StringLength(200)]
        public string? Email { get; set; }

        [Column("FONE")]
        [Display(Name = "Telefone")]
        [StringLength(20)]
        public string? Fone { get; set; }

        [Column("WHATSAPP")]
        [Display(Name = "WhatsApp")]
        [StringLength(20)]
        public string? Whatsapp { get; set; }

        [Column("CONTATO")]
        [Display(Name = "Contato")]
        [StringLength(60)]
        public string? Contato { get; set; }

        [Column("FONE_CONTATO")]
        [Display(Name = "Telefone do Contato")]
        [StringLength(20)]
        public string? FoneContato { get; set; }

        [Column("COD_CLIENTE")]
        [Display(Name = "Código do Cliente")]
        public int? CodCliente { get; set; }

        [Column("ATIVO")]
        [Display(Name = "Ativo")]
        public bool Ativo { get; set; } = true;
    }
}

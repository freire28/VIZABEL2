using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GestaoPedidosVizabel.Models
{
    [Table("EMPRESA")]
    public class Empresa
    {
        [Key]
        [Column("ID_EMPRESA")]
        [Display(Name = "ID")]
        public int IdEmpresa { get; set; }

        [Column("CNPJ")]
        [Display(Name = "CNPJ")]
        [StringLength(60)]
        public string? Cnpj { get; set; }

        [Required]
        [Column("RAZAO_SOCIAL")]
        [Display(Name = "Razão Social")]
        [StringLength(250)]
        public string RazaoSocial { get; set; } = string.Empty;

        [Column("FANTASIA")]
        [Display(Name = "Nome Fantasia")]
        [StringLength(255)]
        public string? Fantasia { get; set; }

        [Column("EMAIL")]
        [Display(Name = "E-mail")]
        [StringLength(255)]
        [EmailAddress]
        public string? Email { get; set; }

        [Column("INSCRICAO_ESTADUAL")]
        [Display(Name = "Inscrição Estadual")]
        [StringLength(50)]
        public string? InscricaoEstadual { get; set; }

        [Column("INSCRICAO_MUNICIPAL")]
        [Display(Name = "Inscrição Municipal")]
        [StringLength(60)]
        public string? InscricaoMunicipal { get; set; }

        [Column("FONE")]
        [Display(Name = "Telefone")]
        [StringLength(20)]
        public string? Fone { get; set; }

        [Column("CERTIFICADO_DIGITAL")]
        [Display(Name = "Certificado Digital")]
        public byte[]? CertificadoDigital { get; set; }

        [Column("ENDERECO_CEP")]
        [Display(Name = "CEP")]
        [StringLength(60)]
        public string? EnderecoCep { get; set; }

        [Column("ENDERECO_LOGRADOURO")]
        [Display(Name = "Logradouro")]
        [StringLength(60)]
        public string? EnderecoLogradouro { get; set; }

        [Column("ENDERECO_UF")]
        [Display(Name = "UF")]
        [StringLength(2)]
        public string? EnderecoUf { get; set; }

        [Column("ENDERECO_COD_MUNICIPIO")]
        [Display(Name = "Código do Município")]
        public int? EnderecoCodMunicipio { get; set; }

        [Column("ENDERECO_BAIRRO")]
        [Display(Name = "Bairro")]
        [StringLength(60)]
        public string? EnderecoBairro { get; set; }

        [Column("ENDERECO_NUMERO")]
        [Display(Name = "Número")]
        [StringLength(60)]
        public string? EnderecoNumero { get; set; }

        [Column("ENDERECO_COMPLEMENTO")]
        [Display(Name = "Complemento")]
        [MaxLength(50)]
        public byte[]? EnderecoComplemento { get; set; }

        // Propriedade auxiliar para converter complemento de byte[] para string (se necessário)
        [NotMapped]
        public string? EnderecoComplementoStr
        {
            get
            {
                if (EnderecoComplemento == null || EnderecoComplemento.Length == 0)
                    return null;
                try
                {
                    return System.Text.Encoding.UTF8.GetString(EnderecoComplemento).TrimEnd('\0');
                }
                catch
                {
                    return null;
                }
            }
        }

        [Column("ENDERECO_CIDADE")]
        [Display(Name = "Cidade")]
        [StringLength(60)]
        public string? EnderecoCidade { get; set; }

        [Column("LOGOTIPO")]
        [Display(Name = "Logotipo")]
        public byte[]? Logotipo { get; set; }

        [Column("CRT")]
        [Display(Name = "Código de Regime Tributário")]
        public int? Crt { get; set; } // 1=Simples Nacional, 2=Simples Nacional - excesso de sublimite, 3=Regime Normal
    }
}

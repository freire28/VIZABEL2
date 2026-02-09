using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GestaoPedidosVizabel.Models
{
    [Table("NFE_DESTINATARIO")]
    public class NFeDestinatario
    {
        [Key]
        [Column("ID_DESTINATARIO")]
        [Display(Name = "ID")]
        public long IdDestinatario { get; set; }

        [Required]
        [Column("ID_NFE")]
        [Display(Name = "NF-e")]
        public long IdNfe { get; set; }

        [Required]
        [Column("CNPJ_CPF")]
        [Display(Name = "CNPJ/CPF")]
        [StringLength(14)]
        public string CnpjCpf { get; set; } = string.Empty;

        [Required]
        [Column("NOME")]
        [Display(Name = "Nome")]
        [StringLength(150)]
        public string Nome { get; set; } = string.Empty;

        [Required]
        [Column("IND_IE_DEST")]
        [Display(Name = "Indicador IE Destinatário")]
        public byte IndIeDest { get; set; } // 1=Contrib, 2=Isento, 9=Não contribuinte

        [Column("IE")]
        [Display(Name = "Inscrição Estadual")]
        [StringLength(20)]
        public string? Ie { get; set; }

        [Required]
        [Column("LOGRADOURO")]
        [Display(Name = "Logradouro")]
        [StringLength(150)]
        public string Logradouro { get; set; } = string.Empty;

        [Required]
        [Column("NUMERO")]
        [Display(Name = "Número")]
        [StringLength(10)]
        public string Numero { get; set; } = string.Empty;

        [Required]
        [Column("BAIRRO")]
        [Display(Name = "Bairro")]
        [StringLength(100)]
        public string Bairro { get; set; } = string.Empty;

        [Required]
        [Column("COD_MUN")]
        [Display(Name = "Código do Município")]
        public int CodMun { get; set; }

        [Required]
        [Column("MUNICIPIO")]
        [Display(Name = "Município")]
        [StringLength(100)]
        public string Municipio { get; set; } = string.Empty;

        [Required]
        [Column("UF")]
        [Display(Name = "UF")]
        [StringLength(2)]
        public string Uf { get; set; } = string.Empty;

        [Required]
        [Column("CEP")]
        [Display(Name = "CEP")]
        [StringLength(8)]
        public string Cep { get; set; } = string.Empty;

        // Propriedade de navegação
        [ForeignKey("IdNfe")]
        public virtual NFe? NFe { get; set; }

        // Propriedade calculada para exibir o tipo de IE
        [NotMapped]
        public string TipoIeDest
        {
            get
            {
                return IndIeDest switch
                {
                    1 => "Contribuinte",
                    2 => "Isento",
                    9 => "Não Contribuinte",
                    _ => "Desconhecido"
                };
            }
        }
    }
}


using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GestaoPedidosVizabel.Models
{
    [Table("CONFIGURACOES")]
    public class Configuracao
    {
        [Key]
        [Column("ID_CONFIGURACAO")]
        [Display(Name = "ID")]
        public int IdConfiguracao { get; set; }

        [Required(ErrorMessage = "A chave é obrigatória")]
        [Column("CHAVE")]
        [Display(Name = "Chave")]
        [StringLength(60)]
        public string Chave { get; set; } = string.Empty;

        [Required(ErrorMessage = "A descrição é obrigatória")]
        [Column("DESCRICAO")]
        [Display(Name = "Descrição")]
        [StringLength(255)]
        public string Descricao { get; set; } = string.Empty;

        [Required(ErrorMessage = "O valor é obrigatório")]
        [Column("VALOR")]
        [Display(Name = "Valor")]
        [StringLength(60)]
        public string Valor { get; set; } = string.Empty;

        [Column("ATIVO")]
        [Display(Name = "Ativo")]
        public bool Ativo { get; set; } = true;

        [Column("CONSIDERAR_NO_PRAZO_ENTREGA")]
        [Display(Name = "Considerar no Prazo de Entrega")]
        public bool? ConsiderarNoPrazoEntrega { get; set; }
    }
}





















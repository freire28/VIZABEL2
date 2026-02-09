using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GestaoPedidosVizabel.Models
{
    [Table("TAMANHOS_PRODUTOS")]
    public class TamanhoProduto
    {
        [Key]
        [Column("ID_TAMANHOPRODUTO")]
        [Display(Name = "ID")]
        public int IdTamanhoproduto { get; set; }

        [Required(ErrorMessage = "O tamanho é obrigatório")]
        [Column("TAMANHO")]
        [Display(Name = "Tamanho")]
        [StringLength(60)]
        public string Tamanho { get; set; } = string.Empty;

        [Column("ATIVO")]
        [Display(Name = "Ativo")]
        public bool? Ativo { get; set; } = true;

        // Navegação
        public virtual ICollection<GradeTamanho> GradeTamanhos { get; set; } = new List<GradeTamanho>();
    }
}


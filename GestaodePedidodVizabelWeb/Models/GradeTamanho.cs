using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GestaoPedidosVizabel.Models
{
    [Table("GRADE_TAMANHOS")]
    public class GradeTamanho
    {
        [Key]
        [Column("ID_GRADE_TAMANHO")]
        [Display(Name = "ID")]
        public int IdGradeTamanho { get; set; }

        [Column("ID_GRADE")]
        [Display(Name = "Grade")]
        public int IdGrade { get; set; }

        [Column("ID_TAMANHO")]
        [Display(Name = "Tamanho")]
        public int IdTamanho { get; set; }

        // Navegação
        public virtual Grade? Grade { get; set; }
        public virtual TamanhoProduto? TamanhoProduto { get; set; }
    }
}


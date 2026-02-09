using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GestaoPedidosVizabel.Models
{
    [Table("GRADES")]
    public class Grade
    {
        [Key]
        [Column("ID_GRADE")]
        [Display(Name = "ID")]
        public int IdGrade { get; set; }

        [Column("DESCRICAO")]
        [Display(Name = "Descrição")]
        [StringLength(60)]
        public string? Descricao { get; set; }

        [Column("ATIVO")]
        [Display(Name = "Ativo")]
        public bool? Ativo { get; set; } = true;

        // Navegação
        public virtual ICollection<GradeTamanho> GradeTamanhos { get; set; } = new List<GradeTamanho>();
        public virtual ICollection<ProdutoGrade> ProdutoGrades { get; set; } = new List<ProdutoGrade>();
    }
}


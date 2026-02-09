using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GestaoPedidosVizabel.Models
{
    [Table("PRODUTO_GRADES")]
    public class ProdutoGrade
    {
        [Key]
        [Column("ID_PRODUTO_GRADE")]
        [Display(Name = "ID")]
        public int IdProdutoGrade { get; set; }

        [Column("ID_PRODUTO")]
        [Display(Name = "Produto")]
        public int IdProduto { get; set; }

        [Column("ID_GRADE")]
        [Display(Name = "Grade")]
        public int IdGrade { get; set; }

        [Column("ATIVO")]
        [Display(Name = "Ativo")]
        public bool Ativo { get; set; } = true;

        // Navegação
        public virtual Produto? Produto { get; set; }
        public virtual Grade? Grade { get; set; }
    }
}





















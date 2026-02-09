using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GestaoPedidosVizabel.Models
{
    [Table("FUNCOES")]
    public class Funcao
    {
        [Key]
        [Column("ID_FUNCAO")]
        [Display(Name = "ID")]
        public int IdFuncao { get; set; }

        [Required(ErrorMessage = "A descrição é obrigatória")]
        [Column("DESCRICAO")]
        [Display(Name = "Descrição")]
        [StringLength(60)]
        public string Descricao { get; set; } = string.Empty;

        [Column("ATIVO")]
        [Display(Name = "Ativo")]
        public bool Ativo { get; set; } = true;

        // Navegação
        public virtual ICollection<FuncionarioFuncao> FuncionarioFuncoes { get; set; } = new List<FuncionarioFuncao>();
    }
}


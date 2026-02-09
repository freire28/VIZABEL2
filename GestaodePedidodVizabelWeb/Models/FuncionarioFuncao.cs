using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GestaoPedidosVizabel.Models
{
    [Table("FUNCIONARIOS_FUNCOES")]
    public class FuncionarioFuncao
    {
        [Key]
        [Column("ID_FUNCIONARIO_FUNCAO")]
        [Display(Name = "ID")]
        public int IdFuncionarioFuncao { get; set; }

        [Column("ID_FUNCIONARIO")]
        [Display(Name = "Funcionário")]
        public int IdFuncionario { get; set; }

        [Column("ID_FUNCAO")]
        [Display(Name = "Função")]
        public int IdFuncao { get; set; }

        [Column("ATIVO")]
        [Display(Name = "Ativo")]
        public bool Ativo { get; set; } = true;

        // Navegação
        public virtual Funcionario? Funcionario { get; set; }
        public virtual Funcao? Funcao { get; set; }
    }
}





















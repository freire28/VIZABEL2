using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GestaoPedidosVizabel.Models
{
    [Table("FUNCIONARIOS")]
    public class Funcionario
    {
        [Key]
        [Column("ID_FUNCIONARIO")]
        [Display(Name = "ID")]
        public int IdFuncionario { get; set; }

        [Required(ErrorMessage = "O nome é obrigatório")]
        [Column("NOME")]
        [Display(Name = "Nome")]
        [StringLength(60)]
        public string Nome { get; set; } = string.Empty;

        [Column("CELULAR")]
        [Display(Name = "Celular")]
        [StringLength(20)]
        public string? Celular { get; set; }

        [Column("FUNCAO_PRINCIPAL")]
        [Display(Name = "Função Principal")]
        public int? FuncaoPrincipal { get; set; }

        [Column("ATIVO")]
        [Display(Name = "Ativo")]
        public bool Ativo { get; set; } = true;

        [Column("VENDEDOR")]
        [Display(Name = "Vendedor")]
        public bool? Vendedor { get; set; }

        [Column("PIN")]
        [Display(Name = "PIN")]
        [StringLength(5)]
        public string? Pin { get; set; }

        // Navegação
        public virtual ICollection<FuncionarioFuncao> FuncionarioFuncoes { get; set; } = new List<FuncionarioFuncao>();
    }
}


using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GestaoPedidosVizabel.Models
{
    [Table("USUARIO_PERMISSOES")]
    public class UsuarioPermissao
    {
        [Key]
        [Column("ID_USUARIO_PERMISSAO")]
        [Display(Name = "ID")]
        public int IdUsuarioPermissao { get; set; }

        [Required]
        [Column("ID_USUARIO")]
        [Display(Name = "Usuário")]
        public int IdUsuario { get; set; }

        [Required]
        [Column("ID_PERMISSAO")]
        [Display(Name = "Permissão")]
        public int IdPermissao { get; set; }

        [Column("PODE_VISUALIZAR")]
        [Display(Name = "Pode Visualizar")]
        public bool PodeVisualizar { get; set; } = true;

        [Column("PODE_INCLUIR")]
        [Display(Name = "Pode Incluir")]
        public bool PodeIncluir { get; set; }

        [Column("PODE_ALTERAR")]
        [Display(Name = "Pode Alterar")]
        public bool PodeAlterar { get; set; }

        [Column("PODE_EXCLUIR")]
        [Display(Name = "Pode Excluir")]
        public bool PodeExcluir { get; set; }

        // Propriedades de navegação
        [ForeignKey("IdUsuario")]
        public virtual Usuario? Usuario { get; set; }

        [ForeignKey("IdPermissao")]
        public virtual Permissao? Permissao { get; set; }
    }
}




















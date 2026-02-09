using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GestaoPedidosVizabel.Models
{
    [Table("PERMISSOES")]
    public class Permissao
    {
        [Key]
        [Column("ID_PERMISSAO")]
        [Display(Name = "ID")]
        public int IdPermissao { get; set; }

        [Required]
        [Column("CONTROLLER")]
        [Display(Name = "Controller")]
        [StringLength(100)]
        public string Controller { get; set; } = string.Empty;

        [Required]
        [Column("ACAO")]
        [Display(Name = "Ação")]
        [StringLength(100)]
        public string Acao { get; set; } = string.Empty;

        [Column("DESCRICAO")]
        [Display(Name = "Descrição")]
        [StringLength(200)]
        public string? Descricao { get; set; }

        [Column("ATIVO")]
        [Display(Name = "Ativo")]
        public bool Ativo { get; set; } = true;

        // Propriedades de navegação
        public virtual ICollection<UsuarioPermissao> UsuarioPermissoes { get; set; } = new List<UsuarioPermissao>();
    }
}




















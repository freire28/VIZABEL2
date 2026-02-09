using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GestaoPedidosVizabel.Models
{
    [Table("USUARIOS")]
    public class Usuario
    {
        [Key]
        [Column("ID_USUARIO")]
        [Display(Name = "ID")]
        public int IdUsuario { get; set; }

        [Required]
        [Column("LOGIN")]
        [Display(Name = "Login")]
        [StringLength(50)]
        public string Login { get; set; } = string.Empty;

        [Required]
        [Column("SENHA")]
        [Display(Name = "Senha")]
        [StringLength(255)]
        public string Senha { get; set; } = string.Empty;

        [Required]
        [Column("NOME")]
        [Display(Name = "Nome")]
        [StringLength(100)]
        public string Nome { get; set; } = string.Empty;

        [Column("EMAIL")]
        [Display(Name = "E-mail")]
        [StringLength(200)]
        [EmailAddress]
        public string? Email { get; set; }

        [Column("ADMINISTRADOR")]
        [Display(Name = "Administrador")]
        public bool Administrador { get; set; }

        [Column("ATIVO")]
        [Display(Name = "Ativo")]
        public bool Ativo { get; set; } = true;

        [Column("DATA_CRIACAO")]
        [Display(Name = "Data de Criação")]
        [DataType(DataType.DateTime)]
        public DateTime? DataCriacao { get; set; }

        // Propriedades de navegação
        public virtual ICollection<UsuarioPermissao> UsuarioPermissoes { get; set; } = new List<UsuarioPermissao>();
    }
}




















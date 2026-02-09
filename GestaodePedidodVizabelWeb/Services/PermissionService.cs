using GestaoPedidosVizabel.Data;
using GestaoPedidosVizabel.Models;
using Microsoft.EntityFrameworkCore;

namespace GestaoPedidosVizabel.Services
{
    public class PermissionService
    {
        private readonly ApplicationDbContext _context;

        public PermissionService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<bool> HasPermissionAsync(int? userId, string controller, string action, string permissionType)
        {
            if (userId == null)
                return false;

            var usuario = await _context.Usuarios
                .Include(u => u.UsuarioPermissoes)
                    .ThenInclude(up => up.Permissao)
                .FirstOrDefaultAsync(u => u.IdUsuario == userId && u.Ativo);

            if (usuario == null)
                return false;

            // Administrador tem todas as permissões
            if (usuario.Administrador)
                return true;

            var permissao = await _context.Permissoes
                .FirstOrDefaultAsync(p => p.Controller.ToLower() == controller.ToLower() 
                    && p.Acao.ToLower() == action.ToLower() 
                    && p.Ativo);

            if (permissao == null)
                return false;

            var usuarioPermissao = usuario.UsuarioPermissoes
                .FirstOrDefault(up => up.IdPermissao == permissao.IdPermissao);

            if (usuarioPermissao == null)
                return false;

            return permissionType.ToLower() switch
            {
                "visualizar" => usuarioPermissao.PodeVisualizar,
                "incluir" => usuarioPermissao.PodeIncluir,
                "alterar" => usuarioPermissao.PodeAlterar,
                "excluir" => usuarioPermissao.PodeExcluir,
                _ => false
            };
        }

        public async Task InitializePermissionsAsync()
        {
            var controllers = new[]
            {
                "Clientes", "Configuracoes", "EtapasProducao", "FormasPagamento",
                "Funcionarios", "Funcoes", "Grades", "Pedidos", "Produtos",
                "StatusPedidos", "TamanhoProdutos", "Usuarios"
            };

            var actions = new[] { "Index", "Create", "Edit", "Delete", "Details" };

            foreach (var controller in controllers)
            {
                foreach (var action in actions)
                {
                    var exists = await _context.Permissoes
                        .AnyAsync(p => p.Controller == controller && p.Acao == action);

                    if (!exists)
                    {
                        var descricao = $"{controller} - {action}";
                        _context.Permissoes.Add(new Permissao
                        {
                            Controller = controller,
                            Acao = action,
                            Descricao = descricao,
                            Ativo = true
                        });
                    }
                }
            }

            await _context.SaveChangesAsync();
        }
    }
}




















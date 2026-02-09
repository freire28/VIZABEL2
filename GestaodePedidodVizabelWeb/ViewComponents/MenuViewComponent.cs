using Microsoft.AspNetCore.Mvc;
using GestaoPedidosVizabel.Services;
using System.Security.Claims;

namespace GestaoPedidosVizabel.ViewComponents
{
    public class MenuViewComponent : ViewComponent
    {
        private readonly PermissionService _permissionService;

        public MenuViewComponent(PermissionService permissionService)
        {
            _permissionService = permissionService;
        }

        public async Task<IViewComponentResult> InvokeAsync()
        {
            var user = ViewContext.HttpContext.User;
            var menuItems = new List<MenuItem>();

            if (user.Identity?.IsAuthenticated == true)
            {
                var userIdClaim = user.FindFirst(ClaimTypes.NameIdentifier);
                int? userId = null;
                if (userIdClaim != null && int.TryParse(userIdClaim.Value, out int id))
                {
                    userId = id;
                }

                var isAdmin = user.FindFirst("IsAdmin")?.Value == "True";

                // Home - sempre visível se autenticado
                menuItems.Add(new MenuItem
                {
                    Controller = "Home",
                    Action = "Index",
                    Text = "Início",
                    Icon = "bi-house-door",
                    HasPermission = true
                });

                // Pedidos
                var pedidosItems = new List<MenuItem>();
                if (isAdmin || (userId.HasValue && await _permissionService.HasPermissionAsync(userId, "Pedidos", "Index", "visualizar")))
                {
                    pedidosItems.Add(new MenuItem { Controller = "Pedidos", Action = "Index", Text = "Pedidos", Icon = "bi-cart-check", HasPermission = true });
                }
                if (isAdmin || (userId.HasValue && await _permissionService.HasPermissionAsync(userId, "StatusPedidos", "Index", "visualizar")))
                {
                    pedidosItems.Add(new MenuItem { Controller = "StatusPedidos", Action = "Index", Text = "Status Pedido", Icon = "bi-flag", HasPermission = true });
                }
                if (pedidosItems.Any())
                {
                    menuItems.Add(new MenuItem
                    {
                        Text = "Pedidos",
                        Icon = "bi-cart",
                        HasSubmenu = true,
                        SubmenuItems = pedidosItems
                    });
                }

                // Cadastros
                var cadastrosItems = new List<MenuItem>();
                if (isAdmin || (userId.HasValue && await _permissionService.HasPermissionAsync(userId, "Clientes", "Index", "visualizar")))
                {
                    cadastrosItems.Add(new MenuItem { Controller = "Clientes", Action = "Index", Text = "Clientes", Icon = "bi-people", HasPermission = true });
                }
                if (isAdmin || (userId.HasValue && await _permissionService.HasPermissionAsync(userId, "EtapasProducao", "Index", "visualizar")))
                {
                    cadastrosItems.Add(new MenuItem { Controller = "EtapasProducao", Action = "Index", Text = "Etapas de Produção", Icon = "bi-diagram-3", HasPermission = true });
                }
                if (isAdmin || (userId.HasValue && await _permissionService.HasPermissionAsync(userId, "Funcoes", "Index", "visualizar")))
                {
                    cadastrosItems.Add(new MenuItem { Controller = "Funcoes", Action = "Index", Text = "Cargos e Funções", Icon = "bi-briefcase", HasPermission = true });
                }
                if (isAdmin || (userId.HasValue && await _permissionService.HasPermissionAsync(userId, "Funcionarios", "Index", "visualizar")))
                {
                    cadastrosItems.Add(new MenuItem { Controller = "Funcionarios", Action = "Index", Text = "Funcionários", Icon = "bi-people-fill", HasPermission = true });
                }
                if (isAdmin || (userId.HasValue && await _permissionService.HasPermissionAsync(userId, "Produtos", "Index", "visualizar")))
                {
                    cadastrosItems.Add(new MenuItem { Controller = "Produtos", Action = "Index", Text = "Produtos", Icon = "bi-box-seam", HasPermission = true });
                }
                if (cadastrosItems.Any())
                {
                    menuItems.Add(new MenuItem
                    {
                        Text = "Cadastros",
                        Icon = "bi-folder",
                        HasSubmenu = true,
                        SubmenuItems = cadastrosItems
                    });
                }

                // Fiscal
                var fiscalItems = new List<MenuItem>();
                if (isAdmin || (userId.HasValue && await _permissionService.HasPermissionAsync(userId, "NFe", "Index", "visualizar")))
                {
                    fiscalItems.Add(new MenuItem { Controller = "NFe", Action = "Index", Text = "NF-e", Icon = "bi-receipt", HasPermission = true });
                }
                if (isAdmin || (userId.HasValue && await _permissionService.HasPermissionAsync(userId, "NaturezaOperacao", "Index", "visualizar")))
                {
                    fiscalItems.Add(new MenuItem { Controller = "NaturezaOperacao", Action = "Index", Text = "Natureza de Operação", Icon = "bi-file-text", HasPermission = true });
                }
                if (fiscalItems.Any())
                {
                    menuItems.Add(new MenuItem
                    {
                        Text = "Fiscal",
                        Icon = "bi-file-earmark-text",
                        HasSubmenu = true,
                        SubmenuItems = fiscalItems
                    });
                }

                // Configurações
                var configItems = new List<MenuItem>();
                if (isAdmin || (userId.HasValue && await _permissionService.HasPermissionAsync(userId, "Configuracoes", "Index", "visualizar")))
                {
                    configItems.Add(new MenuItem { Controller = "Configuracoes", Action = "Index", Text = "Configurações", Icon = "bi-sliders", HasPermission = true });
                }
                if (isAdmin || (userId.HasValue && await _permissionService.HasPermissionAsync(userId, "TamanhoProdutos", "Index", "visualizar")))
                {
                    configItems.Add(new MenuItem { Controller = "TamanhoProdutos", Action = "Index", Text = "Tamanho Produtos", Icon = "bi-rulers", HasPermission = true });
                }
                if (isAdmin || (userId.HasValue && await _permissionService.HasPermissionAsync(userId, "Grades", "Index", "visualizar")))
                {
                    configItems.Add(new MenuItem { Controller = "Grades", Action = "Index", Text = "Grades Tamanhos", Icon = "bi-grid", HasPermission = true });
                }
                if (isAdmin || (userId.HasValue && await _permissionService.HasPermissionAsync(userId, "FormasPagamento", "Index", "visualizar")))
                {
                    configItems.Add(new MenuItem { Controller = "FormasPagamento", Action = "Index", Text = "Formas de Pagamento", Icon = "bi-credit-card", HasPermission = true });
                }
                if (isAdmin)
                {
                    configItems.Add(new MenuItem { Controller = "Account", Action = "Index", Text = "Usuários", Icon = "bi-person-badge", HasPermission = true });
                }
                if (configItems.Any())
                {
                    menuItems.Add(new MenuItem
                    {
                        Text = "Configurações",
                        Icon = "bi-gear",
                        HasSubmenu = true,
                        SubmenuItems = configItems
                    });
                }
            }

            return View(menuItems);
        }
    }

    public class MenuItem
    {
        public string? Controller { get; set; }
        public string? Action { get; set; }
        public string Text { get; set; } = string.Empty;
        public string Icon { get; set; } = string.Empty;
        public bool HasPermission { get; set; }
        public bool HasSubmenu { get; set; }
        public List<MenuItem>? SubmenuItems { get; set; }
    }
}






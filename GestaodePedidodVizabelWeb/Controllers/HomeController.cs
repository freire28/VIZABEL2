using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using GestaoPedidosVizabel.Models;
using GestaoPedidosVizabel.Services;
using System.Security.Claims;

namespace GestaoPedidosVizabel.Controllers
{
    [Authorize]
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly PermissionService _permissionService;

        public HomeController(ILogger<HomeController> logger, PermissionService permissionService)
        {
            _logger = logger;
            _permissionService = permissionService;
        }

        public async Task<IActionResult> Index()
        {
            var user = User;
            var userIdClaim = user.FindFirst(ClaimTypes.NameIdentifier);
            int? userId = null;
            if (userIdClaim != null && int.TryParse(userIdClaim.Value, out int id))
            {
                userId = id;
            }

            var isAdmin = user.FindFirst("IsAdmin")?.Value == "True";

            // Verificar permissões para cada card
            ViewBag.CanViewPedidos = isAdmin || (userId.HasValue && await _permissionService.HasPermissionAsync(userId, "Pedidos", "Index", "visualizar"));
            ViewBag.CanViewClientes = isAdmin || (userId.HasValue && await _permissionService.HasPermissionAsync(userId, "Clientes", "Index", "visualizar"));
            ViewBag.CanViewProdutos = isAdmin || (userId.HasValue && await _permissionService.HasPermissionAsync(userId, "Produtos", "Index", "visualizar"));

            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}


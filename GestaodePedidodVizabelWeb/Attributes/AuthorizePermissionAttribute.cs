using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Authorization;
using GestaoPedidosVizabel.Services;
using System.Security.Claims;

namespace GestaoPedidosVizabel.Attributes
{
    public class AuthorizePermissionAttribute : Attribute, IAsyncAuthorizationFilter
    {
        private readonly string? _permissionType;
        private readonly bool _skipIfNotSet;

        public AuthorizePermissionAttribute(string? permissionType = null, bool skipIfNotSet = false)
        {
            _permissionType = permissionType;
            _skipIfNotSet = skipIfNotSet;
        }

        public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
        {
            // Controllers que não precisam de autenticação - verificar PRIMEIRO
            var controllerName = context.RouteData.Values["controller"]?.ToString() ?? "";
            var action = context.RouteData.Values["action"]?.ToString()?.ToLower() ?? "";
            
            // Permitir acesso anônimo ao AccountController e HomeController
            if (controllerName.ToLower() == "account" || controllerName.ToLower() == "home")
            {
                return;
            }

            // Verificar se a ação ou controller tem AllowAnonymous
            var endpoint = context.HttpContext.GetEndpoint();
            if (endpoint?.Metadata?.GetMetadata<AllowAnonymousAttribute>() != null)
            {
                return;
            }

            // Verificar se o controller tem AllowAnonymous
            var controllerDescriptor = context.ActionDescriptor as Microsoft.AspNetCore.Mvc.Controllers.ControllerActionDescriptor;
            if (controllerDescriptor?.ControllerTypeInfo?.GetCustomAttributes(typeof(AllowAnonymousAttribute), true).Length > 0)
            {
                return;
            }

            // Verificar se a ação tem AllowAnonymous
            if (controllerDescriptor?.MethodInfo?.GetCustomAttributes(typeof(AllowAnonymousAttribute), true).Length > 0)
            {
                return;
            }

            var userIdClaim = context.HttpContext.User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int userId))
            {
                context.Result = new RedirectToActionResult("Login", "Account", new { returnUrl = context.HttpContext.Request.Path });
                return;
            }

            var isAdmin = context.HttpContext.User.FindFirst("IsAdmin")?.Value == "True";
            if (isAdmin)
            {
                return; // Administrador tem todas as permissões
            }

            // Se não especificou tipo de permissão e skipIfNotSet é true, não verifica
            if (_skipIfNotSet && string.IsNullOrEmpty(_permissionType))
            {
                return;
            }

            var permissionService = context.HttpContext.RequestServices.GetRequiredService<PermissionService>();

            // Determinar tipo de permissão se não especificado
            var permissionType = _permissionType ?? action.ToLower() switch
            {
                "index" or "details" => "visualizar",
                "create" => "incluir",
                "edit" => "alterar",
                "delete" => "excluir",
                _ => "visualizar"
            };

            // O RouteData já retorna o nome do controller sem "Controller" (ex: "Clientes", "Pedidos")
            // Garantir primeira letra maiúscula para corresponder ao banco de dados
            var controller = controllerName;
            if (!string.IsNullOrEmpty(controller))
            {
                controller = char.ToUpper(controller[0]) + controller.Substring(1).ToLower();
            }
            
            var hasPermission = await permissionService.HasPermissionAsync(userId, controller, action, permissionType);

            if (!hasPermission)
            {
                context.Result = new RedirectToActionResult("AccessDenied", "Account", null);
            }
        }
    }
}


using GestaoPedidosVizabel.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using System.Security.Claims;

namespace GestaoPedidosVizabel.Filters
{
    public class PermissionAuthorizationFilter : IAsyncAuthorizationFilter
    {
        private readonly PermissionService _permissionService;
        private readonly string _permissionType;

        public PermissionAuthorizationFilter(PermissionService permissionService, string permissionType = "visualizar")
        {
            _permissionService = permissionService;
            _permissionType = permissionType;
        }

        public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
        {
            var userIdClaim = context.HttpContext.User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int userId))
            {
                context.Result = new RedirectToActionResult("Login", "Account", null);
                return;
            }

            var controller = context.RouteData.Values["controller"]?.ToString() ?? "";
            var action = context.RouteData.Values["action"]?.ToString() ?? "";

            // Determinar tipo de permissão baseado na ação
            var permissionType = _permissionType;
            if (string.IsNullOrEmpty(permissionType))
            {
                permissionType = action.ToLower() switch
                {
                    "index" or "details" => "visualizar",
                    "create" => "incluir",
                    "edit" => "alterar",
                    "delete" => "excluir",
                    _ => "visualizar"
                };
            }

            var hasPermission = await _permissionService.HasPermissionAsync(userId, controller, action, permissionType);

            if (!hasPermission)
            {
                context.Result = new ForbidResult();
            }
        }
    }

    public class RequirePermissionAttribute : Attribute
    {
        public string PermissionType { get; set; } = "visualizar";

        public RequirePermissionAttribute(string permissionType = "visualizar")
        {
            PermissionType = permissionType;
        }
    }
}




















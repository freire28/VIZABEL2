using GestaoPedidosVizabel.Models;
using GestaoPedidosVizabel.Services;
using Microsoft.EntityFrameworkCore;

namespace GestaoPedidosVizabel.Data
{
    public static class DatabaseInitializer
    {
        public static async Task InitializeAsync(ApplicationDbContext context, PasswordHasherService passwordHasher, PermissionService permissionService)
        {
            // Inicializar permissões
            await permissionService.InitializePermissionsAsync();

            // Criar usuário administrador padrão se não existir
            var adminExists = await context.Usuarios.AnyAsync(u => u.Login == "admin");
            if (!adminExists)
            {
                var admin = new Usuario
                {
                    Login = "admin",
                    Senha = passwordHasher.HashPassword("admin123"),
                    Nome = "Administrador",
                    Email = "admin@vizabel.com",
                    Administrador = true,
                    Ativo = true,
                    DataCriacao = DateTime.Now
                };

                context.Usuarios.Add(admin);
                await context.SaveChangesAsync();
            }
        }
    }
}




















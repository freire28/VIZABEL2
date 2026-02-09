using Microsoft.EntityFrameworkCore;
using ApiMensageria.Models;

namespace ApiMensageria.Data;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
    {
    }

    // DbSet não é necessário para queries raw, mas pode ser útil para futuras operações
    // public DbSet<Cliente> Clientes { get; set; }
}









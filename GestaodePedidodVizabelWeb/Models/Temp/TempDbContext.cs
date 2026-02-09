using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace GestaoPedidosVizabel.Models.Temp;

public partial class TempDbContext : DbContext
{
    public TempDbContext()
    {
    }

    public TempDbContext(DbContextOptions<TempDbContext> options)
        : base(options)
    {
    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
#warning To protect potentially sensitive information in your connection string, you should move it out of source code. You can avoid scaffolding the connection string by using the Name= syntax to read it from configuration - see https://go.microsoft.com/fwlink/?linkid=2131148. For more guidance on storing connection strings, see https://go.microsoft.com/fwlink/?LinkId=723263.
        => optionsBuilder.UseSqlServer("Server=191.252.221.249;Database=VIZABEL;User Id=sa;Password=@3Xsojfb7;TrustServerCertificate=True;");

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}

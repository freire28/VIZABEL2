using ApiMensageria.Data;
using ApiMensageria.Models;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using System.Data;

namespace ApiMensageria.Services;

public interface IFormaPagamentoService
{
    Task<List<FormaPagamento>> BuscarFormasPagamentoAsync();
}

public class FormaPagamentoService : IFormaPagamentoService
{
    private readonly ApplicationDbContext _context;

    public FormaPagamentoService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<FormaPagamento>> BuscarFormasPagamentoAsync()
    {
        try
        {
            var formasPagamento = new List<FormaPagamento>();

            var connectionString = _context.Database.GetDbConnection().ConnectionString;
            if (string.IsNullOrWhiteSpace(connectionString))
                return formasPagamento;

            var sql = @"
                SELECT ID_FORMAPAGAMENTO,
                       codigo,
                       codigo + ' - ' + descricao AS cod,
                       descricao
                  FROM FORMAS_PAGAMENTO
                 WHERE ativo = 1 
                   AND APARECE_NO_BOOT = 1
                 ORDER BY codigo";

            await using var connection = new SqlConnection(connectionString);
            await connection.OpenAsync();

            await using var command = new SqlCommand(sql, connection);
            await using var reader = await command.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                formasPagamento.Add(new FormaPagamento
                {
                    IdFormaPagamento = reader.GetInt32(0),
                    Codigo = reader.IsDBNull(1) ? string.Empty : reader.GetString(1),                    
                    Descricao = reader.IsDBNull(3) ? string.Empty : reader.GetString(3)
                });
            }

            return formasPagamento;
        }
        catch (Exception ex)
        {
            Console.WriteLine("=== ERRO AO BUSCAR FORMAS DE PAGAMENTO ===");
            Console.WriteLine(ex.ToString());
            throw;
        }
    }
}




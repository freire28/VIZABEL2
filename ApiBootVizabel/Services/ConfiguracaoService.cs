using ApiMensageria.Data;
using ApiMensageria.Models;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using System.Data;

namespace ApiMensageria.Services;

public interface IConfiguracaoService
{
    Task<Configuracao?> BuscarConfiguracaoPorChaveAsync(string chave);
}

public class ConfiguracaoService : IConfiguracaoService
{
    private readonly ApplicationDbContext _context;

    public ConfiguracaoService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Configuracao?> BuscarConfiguracaoPorChaveAsync(string chave)
    {
        try
        {
            var connectionString = _context.Database.GetDbConnection().ConnectionString;
            if (string.IsNullOrWhiteSpace(connectionString))
                return null;

            const string sql = @"
                SELECT ID_CONFIGURACAO, CHAVE, DESCRICAO, VALOR, ATIVO, CONSIDERAR_NO_PRAZO_ENTREGA
                  FROM CONFIGURACOES
                 WHERE CHAVE = @Chave
                   AND ATIVO = 1";

            await using var connection = new SqlConnection(connectionString);
            await connection.OpenAsync();

            await using var command = new SqlCommand(sql, connection);
            command.Parameters.Add("@Chave", SqlDbType.VarChar, 60).Value = chave;

            await using var reader = await command.ExecuteReaderAsync();

            if (!await reader.ReadAsync())
                return null;

            return new Configuracao
            {
                IdConfiguracao = Convert.ToInt32(reader["ID_CONFIGURACAO"]),
                Chave = reader["CHAVE"] as string ?? string.Empty,
                Descricao = reader["DESCRICAO"] as string ?? string.Empty,
                Valor = reader["VALOR"] as string ?? string.Empty,
                Ativo = Convert.ToBoolean(reader["ATIVO"]),
                ConsiderarNoPrazoEntrega = reader["CONSIDERAR_NO_PRAZO_ENTREGA"] as bool?
            };
        }
        catch (Exception ex)
        {
            Console.WriteLine("=== ERRO AO BUSCAR CONFIGURAÇÃO POR CHAVE ===");
            Console.WriteLine(ex.ToString());
            return null;
        }
    }
}





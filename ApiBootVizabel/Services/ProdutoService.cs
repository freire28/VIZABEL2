using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using ApiMensageria.Data;
using ApiMensageria.Models;

namespace ApiMensageria.Services;

public interface IProdutoService
{
    Task<List<ProdutoBusca>> BuscarProdutosAsync(string descricao);
    Task<List<TamanhoDisponivel>> BuscarTamanhosDisponiveisAsync(int idProduto, int idGrade);
    Task<List<int?>> BuscarPrazosEntregaAsync(List<int> idsProdutos);
}

public class ProdutoService : IProdutoService
{
    private readonly ApplicationDbContext _context;

    public ProdutoService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<ProdutoBusca>> BuscarProdutosAsync(string descricao)
    {
        var produtos = new List<ProdutoBusca>();

        var connectionString = _context.Database.GetDbConnection().ConnectionString;
        
        if (string.IsNullOrEmpty(connectionString))
            return produtos;

        var sql = @"
            SELECT ROW_NUMBER() OVER (ORDER BY A.ID_PRODUTO) AS Indice, 
                   A.ID_PRODUTO,
                   C.ID_GRADE,
                   A.DESCRICAO + COALESCE(' - ' + NULLIF(C.DESCRICAO, ''), '') AS DESCRICAO, 
                   A.FABRICACAO_TERCEIRIZADA 
                FROM PRODUTOS A 
                LEFT JOIN PRODUTO_GRADES B ON A.ID_PRODUTO = B.ID_PRODUTO 
                LEFT JOIN GRADES C ON B.ID_GRADE = C.ID_GRADE 
                WHERE A.ATIVO = 1   
                  AND NOT EXISTS (
                        SELECT 1
                        FROM STRING_SPLIT(@pDescricao, ' ')
                        WHERE A.DESCRICAO NOT LIKE '%' + value + '%'
                  );";

        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync();

        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@pDescricao", $"%{descricao}%");

        await using var reader = await command.ExecuteReaderAsync();
        
        while (await reader.ReadAsync())
        {
            produtos.Add(new ProdutoBusca
            {
                Indice = reader.GetInt64(0),
                IdProduto = reader.GetInt32(1),
                IdGrade = reader.IsDBNull(2) ? null : reader.GetInt32(2),
                Descricao = reader.IsDBNull(3) ? string.Empty : reader.GetString(3),
                FabricacaoTerceirizada = reader.GetBoolean(4)
            });
        }

        return produtos;
    }

    public async Task<List<TamanhoDisponivel>> BuscarTamanhosDisponiveisAsync(int idProduto, int idGrade)
    {
        var tamanhos = new List<TamanhoDisponivel>();

        var connectionString = _context.Database.GetDbConnection().ConnectionString;
        
        if (string.IsNullOrEmpty(connectionString))
            return tamanhos;

        var sql = @"
            SELECT DISTINCT E.TAMANHO 
              FROM PRODUTOS A 
                     INNER JOIN PRODUTO_GRADES B ON A.ID_PRODUTO = B.ID_PRODUTO 
                     INNER JOIN GRADES C ON B.ID_GRADE = C.ID_GRADE 
                     INNER JOIN GRADE_TAMANHOS D ON D.ID_GRADE = C.ID_GRADE 
                     INNER JOIN TAMANHOS_PRODUTOS E ON D.ID_TAMANHO = E.ID_TAMANHOPRODUTO 
              WHERE A.ID_PRODUTO = @ID_PRODUTO
                AND C.ID_GRADE = @ID_GRADE 
              ORDER BY TAMANHO";

        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync();

        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@ID_PRODUTO", idProduto);
        command.Parameters.AddWithValue("@ID_GRADE", idGrade);

        await using var reader = await command.ExecuteReaderAsync();
        
        while (await reader.ReadAsync())
        {
            tamanhos.Add(new TamanhoDisponivel
            {
                Tamanho = reader.IsDBNull(0) ? string.Empty : reader.GetString(0)
            });
        }

        return tamanhos;
    }

    public async Task<List<int?>> BuscarPrazosEntregaAsync(List<int> idsProdutos)
    {
        var prazos = new List<int?>();

        if (idsProdutos == null || idsProdutos.Count == 0)
            return prazos;

        var connectionString = _context.Database.GetDbConnection().ConnectionString;
        
        if (string.IsNullOrEmpty(connectionString))
            return prazos;

        // Cria uma lista de parâmetros para a query IN
        var parametros = string.Join(",", idsProdutos.Select((_, i) => $"@IdProduto{i}"));
        
        var sql = $@"
            SELECT PRAZO_ENTREGA
              FROM PRODUTOS
             WHERE ID_PRODUTO IN ({parametros})";

        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync();

        await using var command = new SqlCommand(sql, connection);
        
        // Adiciona os parâmetros
        for (int i = 0; i < idsProdutos.Count; i++)
        {
            command.Parameters.AddWithValue($"@IdProduto{i}", idsProdutos[i]);
        }

        await using var reader = await command.ExecuteReaderAsync();
        
        while (await reader.ReadAsync())
        {
            if (reader.IsDBNull(0))
            {
                prazos.Add(null);
            }
            else
            {
                prazos.Add(reader.GetInt32(0));
            }
        }

        return prazos;
    }
}


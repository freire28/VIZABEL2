using ApiMensageria.Data;
using ApiMensageria.Models;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using System.Data;

namespace ApiMensageria.Services;

public interface IClienteService
{
    Task<List<Cliente>> BuscarClientesAsync(string filtro);
    Task<int> InserirClienteAsync(Cliente cliente);
    Task<int> BuscarProximoCodClienteAsync();
}

public class ClienteService : IClienteService
{
    private readonly ApplicationDbContext _context;

    public ClienteService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<Cliente>> BuscarClientesAsync(string filtro)
    {
        try
        {
            var clientes = new List<Cliente>();

            var connectionString = _context.Database.GetDbConnection().ConnectionString;
            if (string.IsNullOrWhiteSpace(connectionString))
                return clientes;

            var filtroTrim = filtro?.Trim() ?? "";
            if (string.IsNullOrEmpty(filtroTrim))
                return clientes;

            var filtroTexto = $"%{filtroTrim.ToUpper()}%";
            var filtroNumerico = new string(filtroTrim.Where(char.IsDigit).ToArray());

            // Só incluir condição de texto OU de CPF/CNPJ; quando filtroNumerico está vazio,
            // LIKE '%' + '' + '%' vira LIKE '%%' e retorna todos os registros
            var condicoes = new List<string>();
            condicoes.Add("(UPPER(NOMERAZAO) LIKE @pFiltroTexto OR UPPER(FANTASIA) LIKE @pFiltroTexto)");
            if (!string.IsNullOrEmpty(filtroNumerico))
                condicoes.Add("(REPLACE(REPLACE(REPLACE(ISNULL(CPFCNPJ,''), '.', ''), '-', ''), '/', '') LIKE @pFiltroCpfCnpj)");

            var whereClause = string.Join(" OR ", condicoes);
            var sql = $@"
            SELECT ROW_NUMBER() OVER (ORDER BY ID_CLIENTE) AS CONTADOR,
                   ID_CLIENTE, 
                   COALESCE(NULLIF(FANTASIA, ''), NOMERAZAO) AS NOMEEXIBICAO, 
                   CPFCNPJ,
                   NOMERAZAO,
                   FANTASIA
              FROM CLIENTES 
             WHERE {whereClause}";

            await using var connection = new SqlConnection(connectionString);
            await connection.OpenAsync();

            await using var command = new SqlCommand(sql, connection);

            command.Parameters.Add("@pFiltroTexto", SqlDbType.VarChar, 200).Value = "%" + filtroTexto + "%"; 
            if (!string.IsNullOrEmpty(filtroNumerico))
                command.Parameters.Add("@pFiltroCpfCnpj", SqlDbType.VarChar, 50).Value = "%" + filtroNumerico + "%";

            Console.WriteLine("=== DEBUG SQL ===");
            Console.WriteLine($"Filtro Texto: {filtroTexto}");
            Console.WriteLine($"Filtro Num�rico: {filtroNumerico}");

            await using var reader = await command.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                clientes.Add(new Cliente
                {
                    Contador = reader.GetInt64(0),
                    IdCliente = reader.GetInt32(1),
                    NomeExibicao = reader.IsDBNull(2) ? string.Empty : reader.GetString(2),
                    CpfCnpj = reader.IsDBNull(3) ? null : reader.GetString(3),
                    NomeRazao = reader.IsDBNull(4) ? null : reader.GetString(4),
                    Fantasia = reader.IsDBNull(5) ? null : reader.GetString(5)
                });
            }

            Console.WriteLine($"Total encontrado: {clientes.Count}");

            return clientes;
        }
        catch (Exception ex)
        {
            Console.WriteLine("=== ERRO AO BUSCAR CLIENTES ===");
            Console.WriteLine(ex.ToString());
            throw;
        }
    }

    public async Task<int> InserirClienteAsync(Cliente cliente)
    {
        try
        {
            var connectionString = _context.Database.GetDbConnection().ConnectionString;
            if (string.IsNullOrWhiteSpace(connectionString))
                throw new Exception("Connection string não configurada");

            // Busca o próximo código de cliente se não foi informado
            if (!cliente.CodCliente.HasValue)
            {
                cliente.CodCliente = await BuscarProximoCodClienteAsync();
            }

            var sql = @"
                INSERT INTO CLIENTES 
                    (NOMERAZAO, TIPO_PESSOA, FANTASIA, CPFCNPJ, ENDERECO_LOGRADOURO, 
                     ENDERECO_NUMERO, ENDERECO_COMPLEMENTO, ENDERECO_BAIRRO, ENDERECO_CIDADE, 
                     ENDERECO_CEP, EMAIL, ATIVO, COD_CLIENTE, CONTATO, FONE, WHATSAPP, 
                     FONE_CONTATO, RG_IE, ENDERECO_UF, ENDERECO_IBGE)
                VALUES 
                    (@NomeRazao, @TipoPessoa, @Fantasia, @CpfCnpj, @EnderecoLogradouro,
                     @EnderecoNumero, @EnderecoComplemento, @EnderecoBairro, @EnderecoCidade,
                     @EnderecoCep, @Email, @Ativo, @CodCliente, @Contato, @Fone, @Whatsapp,
                     @FoneContato, @RgIe, @EnderecoUf, @EnderecoIbge);
                SELECT CAST(SCOPE_IDENTITY() AS INT);";

            await using var connection = new SqlConnection(connectionString);
            await connection.OpenAsync();

            await using var command = new SqlCommand(sql, connection);
            command.Parameters.AddWithValue("@NomeRazao", cliente.NomeRazaoCompleto);
            command.Parameters.AddWithValue("@TipoPessoa", cliente.TipoPessoa);
            command.Parameters.AddWithValue("@Fantasia", cliente.FantasiaCompleto ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@CpfCnpj", cliente.CpfCnpjCompleto ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@EnderecoLogradouro", cliente.EnderecoLogradouro ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@EnderecoNumero", cliente.EnderecoNumero ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@EnderecoComplemento", cliente.EnderecoComplemento ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@EnderecoBairro", cliente.EnderecoBairro ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@EnderecoCidade", cliente.EnderecoCidade ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@EnderecoCep", cliente.EnderecoCep ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@Email", cliente.Email ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@Ativo", cliente.Ativo);
            command.Parameters.AddWithValue("@CodCliente", cliente.CodCliente ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@Contato", cliente.Contato ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@Fone", cliente.Fone ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@Whatsapp", cliente.Whatsapp ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@FoneContato", cliente.FoneContato ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@RgIe", cliente.RgIe ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@EnderecoUf", cliente.EnderecoUf ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@EnderecoIbge", cliente.EnderecoIbge ?? (object)DBNull.Value);

            var idCliente = (int)await command.ExecuteScalarAsync();
            return idCliente;
        }
        catch (Exception ex)
        {
            Console.WriteLine("=== ERRO AO INSERIR CLIENTE ===");
            Console.WriteLine(ex.ToString());
            throw;
        }
    }

    public async Task<int> BuscarProximoCodClienteAsync()
    {
        try
        {
            var connectionString = _context.Database.GetDbConnection().ConnectionString;
            if (string.IsNullOrWhiteSpace(connectionString))
                return 1;

            var sql = @"
                SELECT ISNULL(MAX(COD_CLIENTE), 0) + 1 AS PROXIMO_COD
                  FROM CLIENTES";

            await using var connection = new SqlConnection(connectionString);
            await connection.OpenAsync();

            await using var command = new SqlCommand(sql, connection);
            var result = await command.ExecuteScalarAsync();
            
            if (result != null && result != DBNull.Value)
            {
                return Convert.ToInt32(result);
            }

            return 1;
        }
        catch (Exception ex)
        {
            Console.WriteLine("=== ERRO AO BUSCAR PRÓXIMO CÓDIGO DE CLIENTE ===");
            Console.WriteLine(ex.ToString());
            return 1;
        }
    }
}


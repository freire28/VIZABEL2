using ApiMensageria.Data;
using ApiMensageria.Models;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using System.Data;

namespace ApiMensageria.Services;

public interface IPedidoService
{
    Task<int> InserirPedidoAsync(Pedido pedido, List<PedidoProduto> produtos, List<ProdutoSelecionado> produtosSelecionados);
    Task<int> BuscarIdGradeTamanhoAsync(int idGrade, string tamanho);
    Task<int> BuscarProximoCodPedidoAsync();
    Task<Pedido?> BuscarPedidoPorCodigoAsync(int codPedido);
    Task<List<Pedido>> BuscarPedidosPorClienteAsync(int idCliente);
    Task<ResumoPedido?> BuscarResumoPedidoAsync(int idPedido);
    Task<int> InserirImagemPedidoAsync(int idPedidoProduto, byte[] imagem, string? descricao = null);
}

public class PedidoService : IPedidoService
{
    private readonly ApplicationDbContext _context;

    public PedidoService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<int> InserirPedidoAsync(Pedido pedido, List<PedidoProduto> produtos, List<ProdutoSelecionado> produtosSelecionados)
    {
        var connectionString = _context.Database.GetDbConnection().ConnectionString;
        if (string.IsNullOrWhiteSpace(connectionString))
            throw new Exception("Connection string não configurada");

        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync();

        await using var transaction = connection.BeginTransaction();

        try
        {
            // 1. Inserir PEDIDO
            var sqlPedido = @"
                INSERT INTO PEDIDOS 
                    (COD_PEDIDO, ID_CLIENTE, DATA_PEDIDO, DATA_ENTREGA, ID_STATUSPEDIDO, 
                     ID_VENDEDOR, ATIVO, OBSERVACOES, ID_FORMAPAGAMENTO, EMITIR_NFE)
                VALUES 
                    (@CodPedido, @IdCliente, @DataPedido, @DataEntrega, @IdStatusPedido,
                     @IdVendedor, @Ativo, @Observacoes, @IdFormaPagamento, @EmitirNfe);
                SELECT CAST(SCOPE_IDENTITY() AS INT);";

            await using var cmdPedido = new SqlCommand(sqlPedido, connection, transaction);
            cmdPedido.Parameters.AddWithValue("@CodPedido", pedido.CodPedido);
            cmdPedido.Parameters.AddWithValue("@IdCliente", pedido.IdCliente);
            cmdPedido.Parameters.AddWithValue("@DataPedido", pedido.DataPedido);
            cmdPedido.Parameters.AddWithValue("@DataEntrega", pedido.DataEntrega);
            cmdPedido.Parameters.AddWithValue("@IdStatusPedido", pedido.IdStatusPedido);
            cmdPedido.Parameters.AddWithValue("@IdVendedor", pedido.IdVendedor ?? (object)DBNull.Value);
            cmdPedido.Parameters.AddWithValue("@Ativo", pedido.Ativo);
            cmdPedido.Parameters.AddWithValue("@Observacoes", pedido.Observacoes ?? (object)DBNull.Value);
            cmdPedido.Parameters.AddWithValue("@IdFormaPagamento", pedido.IdFormaPagamento ?? (object)DBNull.Value);
            cmdPedido.Parameters.AddWithValue("@EmitirNfe", pedido.EmitirNfe ?? (object)DBNull.Value);

            var idPedido = (int)await cmdPedido.ExecuteScalarAsync();

            // 2. Inserir PEDIDO_PRODUTOS e PEDIDO_PROD_TAMANHOS
            for (int i = 0; i < produtos.Count; i++)
            {
                var produto = produtos[i];
                produto.IdPedido = idPedido;

                // Inserir PEDIDO_PRODUTO
                var sqlPedidoProduto = @"
                    INSERT INTO PEDIDO_PRODUTOS 
                        (ID_PEDIDO, ID_PRODUTO, QUANTIDADE, ID_ETAPA_PRODUCAO, ID_GRADE, 
                         ID_FUNCIONARIO_RESPONSAVEL, VALOR_VENDA)
                    VALUES 
                        (@IdPedido, @IdProduto, @Quantidade, @IdEtapaProducao, @IdGrade,
                         @IdFuncionarioResponsavel, @ValorVenda);
                    SELECT CAST(SCOPE_IDENTITY() AS INT);";

                await using var cmdPedidoProduto = new SqlCommand(sqlPedidoProduto, connection, transaction);
                cmdPedidoProduto.Parameters.AddWithValue("@IdPedido", produto.IdPedido);
                cmdPedidoProduto.Parameters.AddWithValue("@IdProduto", produto.IdProduto);
                cmdPedidoProduto.Parameters.AddWithValue("@Quantidade", produto.Quantidade ?? (object)DBNull.Value);
                cmdPedidoProduto.Parameters.AddWithValue("@IdEtapaProducao", produto.IdEtapaProducao ?? (object)DBNull.Value);
                cmdPedidoProduto.Parameters.AddWithValue("@IdGrade", produto.IdGrade ?? (object)DBNull.Value);
                cmdPedidoProduto.Parameters.AddWithValue("@IdFuncionarioResponsavel", produto.IdFuncionarioResponsavel ?? (object)DBNull.Value);
                cmdPedidoProduto.Parameters.AddWithValue("@ValorVenda", produto.ValorVenda ?? (object)DBNull.Value);

                var idPedidoProduto = (int)await cmdPedidoProduto.ExecuteScalarAsync();

                // 3. Inserir PEDIDO_PROD_TAMANHOS se houver tamanhos
                var produtoSelecionado = produtosSelecionados[i];

                // Verifica se ID_ETAPA_PRODUCAO é 4 para inserir em PRODUTO_PEDIDO_ETAPA_PROD
                var deveInserirEtapaProd = pedido.IdStatusPedido == 3;

                if (produtoSelecionado.TamanhosQuantidades != null && produtoSelecionado.TamanhosQuantidades.Count > 0)
                {
                    // Processa cada tamanho e quantidade
                    foreach (var tamanhoQuantidade in produtoSelecionado.TamanhosQuantidades)
                    {
                        if (produto.IdGrade.HasValue)
                        {
                            // Busca ID_GRADE_TAMANHO dentro da transação
                            var idGradeTamanho = await BuscarIdGradeTamanhoAsyncTransacao(connection, transaction, produto.IdGrade.Value, tamanhoQuantidade.Tamanho);
                            
                            if (idGradeTamanho > 0)
                            {
                                // Busca ID_TAMANHO a partir do ID_GRADE_TAMANHO
                                var idTamanho = await BuscarIdTamanhoPorGradeTamanhoAsync(connection, transaction, idGradeTamanho);
                                
                                var sqlTamanho = @"
                                    INSERT INTO PEDIDO_PROD_TAMANHOS 
                                        (ID_PEDIDOPRODUTO, ID_GRADE_TAMANHO, QUANTIDADE, ID_ETAPA)
                                    VALUES 
                                        (@IdPedidoProduto, @IdGradeTamanho, @Quantidade, @IdEtapa);
                                    SELECT CAST(SCOPE_IDENTITY() AS INT);";

                                await using var cmdTamanho = new SqlCommand(sqlTamanho, connection, transaction);
                                cmdTamanho.Parameters.AddWithValue("@IdPedidoProduto", idPedidoProduto);
                                cmdTamanho.Parameters.AddWithValue("@IdGradeTamanho", idGradeTamanho);
                                cmdTamanho.Parameters.AddWithValue("@Quantidade", tamanhoQuantidade.Quantidade);
                                cmdTamanho.Parameters.AddWithValue("@IdEtapa", 1);

                                var idGradePedProd = (int)await cmdTamanho.ExecuteScalarAsync();

                                // 4. Se ID_ETAPA_PRODUCAO for 4, insere em PRODUTO_PEDIDO_ETAPA_PROD
                                if (deveInserirEtapaProd)
                                {
                                    var sqlEtapaProd = @"
                                        INSERT INTO PRODUTO_PEDIDO_ETAPA_PROD 
                                            (ID_ETAPA_PRODUCAO, ID_PEDIDO_PRODUTO, ID_FUNCIONARIO, CONCLUIDO, 
                                             PERDA, QUANTIDADE_PRODUZIDA, QUANTIDADE_PERDA, ID_PERDA, 
                                             ID_TAMANHO, QUANTIDADE, ID_GRADE_PED_PROD, REPOSICAO, 
                                             DATA_INICIO, DATA_FIM)
                                        VALUES 
                                            (@IdEtapaProducao, @IdPedidoProduto, @IdFuncionario, @Concluido,
                                             @Perda, @QuantidadeProduzida, @QuantidadePerda, @IdPerda,
                                             @IdTamanho, @Quantidade, @IdGradePedProd, @Reposicao,
                                             @DataInicio, @DataFim);";

                                    await using var cmdEtapaProd = new SqlCommand(sqlEtapaProd, connection, transaction);
                                    cmdEtapaProd.Parameters.AddWithValue("@IdEtapaProducao", 4);
                                    cmdEtapaProd.Parameters.AddWithValue("@IdPedidoProduto", idPedidoProduto);
                                    cmdEtapaProd.Parameters.AddWithValue("@IdFuncionario", produto.IdFuncionarioResponsavel ?? (object)DBNull.Value);
                                    cmdEtapaProd.Parameters.AddWithValue("@Concluido", false); // Inicialmente não concluído
                                    cmdEtapaProd.Parameters.AddWithValue("@Perda", DBNull.Value);
                                    cmdEtapaProd.Parameters.AddWithValue("@QuantidadeProduzida", DBNull.Value);
                                    cmdEtapaProd.Parameters.AddWithValue("@QuantidadePerda", DBNull.Value);
                                    cmdEtapaProd.Parameters.AddWithValue("@IdPerda", DBNull.Value);
                                    cmdEtapaProd.Parameters.AddWithValue("@IdTamanho", idTamanho.HasValue ? (object)idTamanho.Value : DBNull.Value);
                                    cmdEtapaProd.Parameters.AddWithValue("@Quantidade", tamanhoQuantidade.Quantidade);
                                    cmdEtapaProd.Parameters.AddWithValue("@IdGradePedProd", idGradePedProd);
                                    cmdEtapaProd.Parameters.AddWithValue("@Reposicao", 0);
                                    cmdEtapaProd.Parameters.AddWithValue("@DataInicio", DBNull.Value);
                                    cmdEtapaProd.Parameters.AddWithValue("@DataFim", DBNull.Value);

                                    await cmdEtapaProd.ExecuteNonQueryAsync();

                                }
                            }
                        }
                    }
                }
            }

            await transaction.CommitAsync();
            return idPedido;
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    private async Task<int?> BuscarIdTamanhoPorGradeTamanhoAsync(SqlConnection connection, SqlTransaction transaction, int idGradeTamanho)
    {
        try
        {
            var sql = @"
                SELECT GT.ID_TAMANHO
                  FROM GRADE_TAMANHOS GT
                 WHERE GT.ID_GRADE_TAMANHO = @IdGradeTamanho";

            await using var command = new SqlCommand(sql, connection, transaction);
            command.Parameters.AddWithValue("@IdGradeTamanho", idGradeTamanho);

            var result = await command.ExecuteScalarAsync();
            
            if (result != null && result != DBNull.Value)
            {
                return Convert.ToInt32(result);
            }

            return null;
        }
        catch (Exception ex)
        {
            Console.WriteLine("=== ERRO AO BUSCAR ID_TAMANHO POR GRADE_TAMANHO ===");
            Console.WriteLine(ex.ToString());
            return null;
        }
    }

    private async Task<int> BuscarIdGradeTamanhoAsyncTransacao(SqlConnection connection, SqlTransaction transaction, int idGrade, string tamanho)
    {
        try
        {
            var sql = @"
                SELECT A.ID_GRADE_TAMANHO
                  FROM GRADE_TAMANHOS A
                 INNER JOIN TAMANHOS_PRODUTOS B ON A.ID_TAMANHO = B.ID_TAMANHOPRODUTO
                 WHERE A.id_grade = @ID_GRADE
                   AND UPPER(B.TAMANHO) = @TAMANHO";

            await using var command = new SqlCommand(sql, connection, transaction);
            command.Parameters.AddWithValue("@ID_GRADE", idGrade);
            command.Parameters.AddWithValue("@TAMANHO", tamanho.ToUpper());

            var result = await command.ExecuteScalarAsync();
            
            if (result != null && result != DBNull.Value)
            {
                return Convert.ToInt32(result);
            }

            return 0;
        }
        catch (Exception ex)
        {
            Console.WriteLine("=== ERRO AO BUSCAR ID_GRADE_TAMANHO (Transação) ===");
            Console.WriteLine(ex.ToString());
            return 0;
        }
    }

    public async Task<int> BuscarIdGradeTamanhoAsync(int idGrade, string tamanho)
    {
        try
        {
            var connectionString = _context.Database.GetDbConnection().ConnectionString;
            if (string.IsNullOrWhiteSpace(connectionString))
                return 0;

            var sql = @"
                SELECT A.ID_GRADE_TAMANHO
                  FROM GRADE_TAMANHOS A
                 INNER JOIN TAMANHOS_PRODUTOS B ON A.ID_TAMANHO = B.ID_TAMANHOPRODUTO
                 WHERE A.id_grade = @ID_GRADE
                   AND UPPER(B.TAMANHO) = @TAMANHO";

            await using var connection = new SqlConnection(connectionString);
            await connection.OpenAsync();

            await using var command = new SqlCommand(sql, connection);
            command.Parameters.AddWithValue("@ID_GRADE", idGrade);
            command.Parameters.AddWithValue("@TAMANHO", tamanho.ToUpper());

            var result = await command.ExecuteScalarAsync();
            
            if (result != null && result != DBNull.Value)
            {
                return Convert.ToInt32(result);
            }

            return 0;
        }
        catch (Exception ex)
        {
            Console.WriteLine("=== ERRO AO BUSCAR ID_GRADE_TAMANHO ===");
            Console.WriteLine(ex.ToString());
            return 0;
        }
    }

    public async Task<int> BuscarProximoCodPedidoAsync()
    {
        try
        {
            var connectionString = _context.Database.GetDbConnection().ConnectionString;
            if (string.IsNullOrWhiteSpace(connectionString))
                return 1;

            var sql = @"
                SELECT ISNULL(MAX(COD_PEDIDO), 0) + 1 AS PROXIMO_COD
                  FROM PEDIDOS";

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
            Console.WriteLine("=== ERRO AO BUSCAR PRÓXIMO CÓDIGO DE PEDIDO ===");
            Console.WriteLine(ex.ToString());
            return 1;
        }
    }

    public async Task<Pedido?> BuscarPedidoPorCodigoAsync(int codPedido)
    {
        try
        {
            var connectionString = _context.Database.GetDbConnection().ConnectionString;
            if (string.IsNullOrWhiteSpace(connectionString))
                return null;

            const string sql = @"
            SELECT ID_PEDIDO, COD_PEDIDO, ID_CLIENTE, DATA_PEDIDO, DATA_ENTREGA, 
                   ID_STATUSPEDIDO, ID_VENDEDOR, ATIVO, OBSERVACOES, ID_FORMAPAGAMENTO, EMITIR_NFE
              FROM PEDIDOS
             WHERE COD_PEDIDO = @CodPedido
               AND ATIVO = 1";

            await using var connection = new SqlConnection(connectionString);
            await connection.OpenAsync();

            await using var command = new SqlCommand(sql, connection);
            command.Parameters.Add("@CodPedido", SqlDbType.Int).Value = codPedido;

            await using var reader = await command.ExecuteReaderAsync();

            if (!await reader.ReadAsync())
                return null;

            return new Pedido
            {
                IdPedido = Convert.ToInt32(reader["ID_PEDIDO"]),
                CodPedido = Convert.ToInt32(reader["COD_PEDIDO"]),
                IdCliente = Convert.ToInt32(reader["ID_CLIENTE"]),
                DataPedido = Convert.ToDateTime(reader["DATA_PEDIDO"]),

                DataEntrega = reader["DATA_ENTREGA"] == DBNull.Value
                    ? null
                    : Convert.ToDateTime(reader["DATA_ENTREGA"]),

                IdStatusPedido = Convert.ToInt32(reader["ID_STATUSPEDIDO"]),
                IdVendedor = reader["ID_VENDEDOR"] as int?,
                Ativo = Convert.ToBoolean(reader["ATIVO"]),
                Observacoes = reader["OBSERVACOES"] as string,
                IdFormaPagamento = reader["ID_FORMAPAGAMENTO"] as int?,
                EmitirNfe = reader["EMITIR_NFE"] as bool?
            };
        }
        catch (Exception ex)
        {
            Console.WriteLine("=== ERRO AO BUSCAR PEDIDO POR CÓDIGO ===" + ex.ToString);
            Console.WriteLine(ex);
            return null;
        }
    }

    public async Task<List<Pedido>> BuscarPedidosPorClienteAsync(int idCliente)
    {
        try
        {
            var pedidos = new List<Pedido>();
            var connectionString = _context.Database.GetDbConnection().ConnectionString;
            if (string.IsNullOrWhiteSpace(connectionString))
                return pedidos;

            var sql = @"
                SELECT ID_PEDIDO, COD_PEDIDO, ID_CLIENTE, DATA_PEDIDO, DATA_ENTREGA, 
                       ID_STATUSPEDIDO, ID_VENDEDOR, ATIVO, OBSERVACOES, ID_FORMAPAGAMENTO, EMITIR_NFE
                  FROM PEDIDOS
                 WHERE ID_CLIENTE = @IdCliente
                   AND ATIVO = 1
                 ORDER BY DATA_PEDIDO DESC";

            await using var connection = new SqlConnection(connectionString);
            await connection.OpenAsync();

            await using var command = new SqlCommand(sql, connection);
            command.Parameters.AddWithValue("@IdCliente", idCliente);

            await using var reader = await command.ExecuteReaderAsync();
            
            while (await reader.ReadAsync())
            {
                pedidos.Add(new Pedido
                {
                    IdPedido = reader.GetInt32(0),
                    CodPedido = reader.GetInt32(1),
                    IdCliente = reader.GetInt32(2),
                    DataPedido = reader.GetDateTime(3),
                    DataEntrega = reader.GetDateTime(4),
                    IdStatusPedido = reader.GetInt32(5),
                    IdVendedor = reader.IsDBNull(6) ? null : reader.GetInt32(6),
                    Ativo = reader.GetBoolean(7),
                    Observacoes = reader.IsDBNull(8) ? null : reader.GetString(8),
                    IdFormaPagamento = reader.IsDBNull(9) ? null : reader.GetInt32(9),
                    EmitirNfe = reader.IsDBNull(10) ? null : reader.GetBoolean(10)
                });
            }

            return pedidos;
        }
        catch (Exception ex)
        {
            Console.WriteLine("=== ERRO AO BUSCAR PEDIDOS POR CLIENTE ===");
            Console.WriteLine(ex.ToString());
            return new List<Pedido>();
        }
    }

    public async Task<ResumoPedido?> BuscarResumoPedidoAsync(int idPedido)
    {
        try
        {
            var connectionString = _context.Database.GetDbConnection().ConnectionString;
            if (string.IsNullOrWhiteSpace(connectionString))
                return null;

            // Busca dados do pedido, cliente, status e forma de pagamento
            var sqlPedido = @"
                SELECT P.ID_PEDIDO, P.COD_PEDIDO, P.ID_CLIENTE, P.DATA_PEDIDO, P.DATA_ENTREGA, 
                       P.ID_STATUSPEDIDO, P.ID_VENDEDOR, P.ATIVO, P.OBSERVACOES, 
                       P.ID_FORMAPAGAMENTO, P.EMITIR_NFE,
                       COALESCE(NULLIF(C.FANTASIA, ''), C.NOMERAZAO) AS NOMECLIENTE,
                       C.CPFCNPJ AS CPFCNPJCLIENTE,
                       ISNULL(SP.DESCRICAO, '') AS DESCRICAOSTATUS,
                       ISNULL(FP.DESCRICAO, '') AS DESCRICAOFORMAPAGAMENTO
                  FROM PEDIDOS P
                  LEFT JOIN CLIENTES C ON P.ID_CLIENTE = C.ID_CLIENTE
                  LEFT JOIN STATUS_PEDIDO SP ON P.ID_STATUSPEDIDO = SP.ID_STATUSPEDIDO
                  LEFT JOIN FORMAS_PAGAMENTO FP ON P.ID_FORMAPAGAMENTO = FP.ID_FORMAPAGAMENTO
                 WHERE P.ID_PEDIDO = @IdPedido
                   AND P.ATIVO = 1";

            await using var connection = new SqlConnection(connectionString);
            await connection.OpenAsync();

            await using var command = new SqlCommand(sqlPedido, connection);
            command.Parameters.AddWithValue("@IdPedido", idPedido);

            await using var reader = await command.ExecuteReaderAsync();
            
            if (!await reader.ReadAsync())
                return null;

            var resumo = new ResumoPedido
            {
                IdPedido = reader.GetInt32(0),
                CodPedido = reader.GetInt32(1),
                IdCliente = reader.GetInt32(2),
                DataPedido = reader.GetDateTime(3),
                DataEntrega = reader.GetDateTime(4),
                IdStatusPedido = reader.GetInt32(5),
                IdVendedor = reader.IsDBNull(6) ? null : reader.GetInt32(6),
                Ativo = reader.GetBoolean(7),
                Observacoes = reader.IsDBNull(8) ? null : reader.GetString(8),
                IdFormaPagamento = reader.IsDBNull(9) ? null : reader.GetInt32(9),
                EmitirNfe = reader.IsDBNull(10) ? null : reader.GetBoolean(10),
                NomeCliente = reader.IsDBNull(11) ? null : reader.GetString(11),
                CpfCnpjCliente = reader.IsDBNull(12) ? null : reader.GetString(12),
                DescricaoStatusPedido = reader.IsDBNull(13) ? null : reader.GetString(13),
                DescricaoFormaPagamento = reader.IsDBNull(14) ? null : reader.GetString(14),
                Produtos = new List<ResumoPedidoProduto>()
            };

            reader.Close();

            // Busca produtos do pedido
            var sqlProdutos = @"
                SELECT PP.ID_PEDIDOPRODUTO, PP.ID_PRODUTO, PP.QUANTIDADE, PP.ID_GRADE,
                       PR.DESCRICAO AS DESCRICAOPRODUTO,
                       G.DESCRICAO AS DESCRICAOGRADE,
                       PR.FABRICACAO_TERCEIRIZADA
                  FROM PEDIDO_PRODUTOS PP
                  LEFT JOIN PRODUTOS PR ON PP.ID_PRODUTO = PR.ID_PRODUTO
                  LEFT JOIN GRADES G ON PP.ID_GRADE = G.ID_GRADE
                 WHERE PP.ID_PEDIDO = @IdPedido";

            await using var commandProdutos = new SqlCommand(sqlProdutos, connection);
            commandProdutos.Parameters.AddWithValue("@IdPedido", idPedido);

            await using var readerProdutos = await commandProdutos.ExecuteReaderAsync();
            
            while (await readerProdutos.ReadAsync())
            {
                var idPedidoProduto = readerProdutos.GetInt32(0);
                var fabricacaoTerceirizada = readerProdutos.IsDBNull(6) ? false : readerProdutos.GetBoolean(6);
                
                var produto = new ResumoPedidoProduto
                {
                    IdPedidoProduto = idPedidoProduto,
                    IdProduto = readerProdutos.GetInt32(1),
                    Quantidade = readerProdutos.IsDBNull(2) ? null : readerProdutos.GetInt32(2),
                    IdGrade = readerProdutos.IsDBNull(3) ? null : readerProdutos.GetInt32(3),
                    DescricaoProduto = readerProdutos.IsDBNull(4) ? null : readerProdutos.GetString(4),
                    DescricaoGrade = readerProdutos.IsDBNull(5) ? null : readerProdutos.GetString(5),
                    Tamanhos = new List<ResumoPedidoTamanho>()
                };

                // Busca tamanhos do produto com informações de etapa e funcionário
                if (produto.IdGrade.HasValue)
                {
                    // Busca tamanhos com etapa e funcionário por tamanho
                    // Apenas para produtos com FABRICACAO_TERCEIRIZADA = 0
                    var sqlTamanhos = !fabricacaoTerceirizada ? @"
                        SELECT TP.TAMANHO, 
                               PPT.QUANTIDADE,
                               PPT.ID_GRADE_PED_PROD,
                               D.ID_ETAPA_PRODUCAO AS ID_ETAPA,
                               I.DESCRICAO AS ETAPAATUAL,
                               J.NOME AS FUNCIONARIO
                          FROM PEDIDO_PROD_TAMANHOS PPT
                          INNER JOIN GRADE_TAMANHOS GT ON PPT.ID_GRADE_TAMANHO = GT.ID_GRADE_TAMANHO
                          INNER JOIN TAMANHOS_PRODUTOS TP ON GT.ID_TAMANHO = TP.ID_TAMANHOPRODUTO
                          LEFT JOIN PRODUTO_PEDIDO_ETAPA_PROD D ON PPT.ID_GRADE_PED_PROD = D.ID_GRADE_PED_PROD
                          LEFT JOIN ETAPAS_PRODUCAO I ON I.ID_ETAPA = D.ID_ETAPA_PRODUCAO
                          LEFT JOIN FUNCIONARIOS J ON J.ID_FUNCIONARIO = D.ID_FUNCIONARIO
                         WHERE PPT.ID_PEDIDOPRODUTO = @IdPedidoProduto" : @"
                        SELECT TP.TAMANHO, 
                               PPT.QUANTIDADE,
                               PPT.ID_GRADE_PED_PROD,
                               NULL AS ID_ETAPA,
                               NULL AS ETAPAATUAL,
                               NULL AS FUNCIONARIO
                          FROM PEDIDO_PROD_TAMANHOS PPT
                          INNER JOIN GRADE_TAMANHOS GT ON PPT.ID_GRADE_TAMANHO = GT.ID_GRADE_TAMANHO
                          INNER JOIN TAMANHOS_PRODUTOS TP ON GT.ID_TAMANHO = TP.ID_TAMANHOPRODUTO
                         WHERE PPT.ID_PEDIDOPRODUTO = @IdPedidoProduto";

                    await using var commandTamanhos = new SqlCommand(sqlTamanhos, connection);
                    commandTamanhos.Parameters.AddWithValue("@IdPedidoProduto", idPedidoProduto);

                    await using var readerTamanhos = await commandTamanhos.ExecuteReaderAsync();
                    
                    while (await readerTamanhos.ReadAsync())
                    {
                        var tamanho = new ResumoPedidoTamanho
                        {
                            Tamanho = readerTamanhos.IsDBNull(0) ? null : readerTamanhos.GetString(0),
                            Quantidade = readerTamanhos.GetInt32(1),
                            IdGradePedProd = readerTamanhos.IsDBNull(2) ? null : readerTamanhos.GetInt32(2)
                        };

                        // Se não for terceirizado, busca informações de etapa e funcionário
                        if (!fabricacaoTerceirizada)
                        {
                            tamanho.IdEtapaProducao = readerTamanhos.IsDBNull(3) ? null : readerTamanhos.GetInt32(3);
                            tamanho.DescricaoEtapaProducao = readerTamanhos.IsDBNull(4) ? null : readerTamanhos.GetString(4);
                            tamanho.NomeFuncionario = readerTamanhos.IsDBNull(5) ? null : readerTamanhos.GetString(5);
                            
                            // Se não houver funcionário, define como "Sem Responsável"
                            if (string.IsNullOrWhiteSpace(tamanho.NomeFuncionario))
                            {
                                tamanho.NomeFuncionario = "Sem Responsável";
                            }
                        }

                        produto.Tamanhos.Add(tamanho);
                    }
                }

                resumo.Produtos.Add(produto);
            }

            return resumo;
        }
        catch (Exception ex)
        {
            Console.WriteLine("=== ERRO AO BUSCAR RESUMO DO PEDIDO ===");
            Console.WriteLine(ex.ToString());
            return null;
        }
    }

    public async Task<int> InserirImagemPedidoAsync(int idPedidoProduto, byte[] imagem, string? descricao = null)
    {
        try
        {
            var connectionString = _context.Database.GetDbConnection().ConnectionString;
            if (string.IsNullOrWhiteSpace(connectionString))
                throw new Exception("Connection string não configurada");

            await using var connection = new SqlConnection(connectionString);
            await connection.OpenAsync();

            var sql = @"
                INSERT INTO PEDIDO_IMAGENS 
                    (ID_PEDIDOPRODUTO, DESCRICAO, IMAGEM)
                VALUES 
                    (@IdPedidoProduto, @Descricao, @Imagem);
                SELECT CAST(SCOPE_IDENTITY() AS INT);";

            await using var command = new SqlCommand(sql, connection);
            command.Parameters.AddWithValue("@IdPedidoProduto", idPedidoProduto);
            command.Parameters.AddWithValue("@Descricao", descricao ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@Imagem", imagem);

            var idImagem = (int)await command.ExecuteScalarAsync();
            return idImagem;
        }
        catch (Exception ex)
        {
            Console.WriteLine("=== ERRO AO INSERIR IMAGEM DO PEDIDO ===");
            Console.WriteLine(ex.ToString());
            throw;
        }
    }
}


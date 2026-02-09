using System.Data;
using System.Collections.Generic;
using Microsoft.Data.SqlClient;

namespace TerminalWeb.Services;

public class PedidoService
{
    private readonly string _connectionString;

    public PedidoService(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("DefaultConnection") 
            ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
    }

    public DataTable BuscarPedidosEmProducao(int idEtapaProducao = 4)
    {
        var dataTable = new DataTable();

        try
        {
            using var connection = new SqlConnection(_connectionString);
            using var command = new SqlCommand("pBuscaPedidosEmProducao", connection)
            {
                CommandType = CommandType.StoredProcedure
            };

            command.Parameters.AddWithValue("@ID_ETAPA_PRODUCAO", idEtapaProducao);

            connection.Open();
            using var adapter = new SqlDataAdapter(command);
            adapter.Fill(dataTable);
        }
        catch (Exception ex)
        {
            throw new Exception($"Erro ao buscar pedidos em produção: {ex.Message}", ex);
        }

        return dataTable;
    }

    public DataTable BuscarFuncionariosDisponiveis(int idEtapa = 4, int? idPedido = null)
    {
        var dataTable = new DataTable();

        try
        {
            using var connection = new SqlConnection(_connectionString);
            connection.Open();

            // Primeiro, verificar se há funcionário associado ao pedido específico
            if (idPedido.HasValue)
            {
                // Verificar se existe funcionário associado
                var queryVerificarAssociado = @"
                    SELECT ID_FUNCIONARIO 
                    FROM PRODUTO_PEDIDO_ETAPA_PROD 
                    WHERE ID = @ID 
                      AND CONCLUIDO = 0 
                      AND ID_FUNCIONARIO IS NOT NULL 
                      AND ID_FUNCIONARIO > 0";

                using var commandVerificar = new SqlCommand(queryVerificarAssociado, connection);
                commandVerificar.Parameters.AddWithValue("@ID", idPedido.Value);
                
                var idFuncionarioAssociado = commandVerificar.ExecuteScalar();
                
                // Se há funcionário associado, buscar apenas esse funcionário
                if (idFuncionarioAssociado != null && idFuncionarioAssociado != DBNull.Value)
                {
                    var queryAssociado = @"
                        SELECT A.*, LEFT(B.NOME, CHARINDEX(' ', B.NOME + ' ') - 1) AS NOME 
                        FROM PRODUTO_PEDIDO_ETAPA_PROD A 
                        INNER JOIN FUNCIONARIOS B ON A.ID_FUNCIONARIO = B.ID_FUNCIONARIO 
                        WHERE A.CONCLUIDO = 0 
                          AND A.ID = @ID 
                          AND A.ID_FUNCIONARIO = @ID_FUNCIONARIO";

                    using var commandAssociado = new SqlCommand(queryAssociado, connection);
                    commandAssociado.Parameters.AddWithValue("@ID", idPedido.Value);
                    commandAssociado.Parameters.AddWithValue("@ID_FUNCIONARIO", Convert.ToInt32(idFuncionarioAssociado));

                    using var adapterAssociado = new SqlDataAdapter(commandAssociado);
                    adapterAssociado.Fill(dataTable);

                    // Se encontrou funcionário associado, retornar apenas esse funcionário
                    if (dataTable.Rows.Count > 0)
                    {
                        return dataTable;
                    }
                }
            }

            // Se não encontrou funcionário associado, buscar funcionários disponíveis
            dataTable.Clear();

            var queryDisponiveis = @"
                DECLARE @IDETAPA INT;
                DECLARE @SEPARAR_MAIS_DE_UM_PEDIDO VARCHAR(10);
                DECLARE @ID_FUNCAO INT;
                SET @IDETAPA = @ID_ETAPA;
                IF @IDETAPA = 4 
                BEGIN 
                    SELECT @SEPARAR_MAIS_DE_UM_PEDIDO = VALOR 
                    FROM CONFIGURACOES 
                    WHERE CHAVE = 'SEPARAR_MAIS_DE_UM_PEDIDO' 
                END;
                ELSE 
                BEGIN 
                    SET @SEPARAR_MAIS_DE_UM_PEDIDO = 'NAO'; 
                END;
                SELECT @ID_FUNCAO = ID_FUNCAO 
                FROM ETAPAS_PRODUCAO 
                WHERE ID_ETAPA = @IDETAPA;
                SELECT DISTINCT A.ID_FUNCIONARIO, LEFT(A.NOME, CHARINDEX(' ', A.NOME + ' ') - 1) AS NOME 
                FROM FUNCIONARIOS A 
                LEFT JOIN FUNCIONARIOS_FUNCOES B ON A.ID_FUNCIONARIO = B.ID_FUNCIONARIO 
                WHERE A.ATIVO = 1 
                AND ((A.FUNCAO_PRINCIPAL = @ID_FUNCAO) OR (B.ID_FUNCAO = @ID_FUNCAO)) 
                AND ( 
                    (@IDETAPA = 4 AND @SEPARAR_MAIS_DE_UM_PEDIDO = 'SIM') 
                    OR NOT EXISTS ( 
                        SELECT 1 
                        FROM PRODUTO_PEDIDO_ETAPA_PROD P 
                        WHERE COALESCE(P.CONCLUIDO, 0) = 0 
                        AND P.ID_ETAPA_PRODUCAO = @IDETAPA 
                        AND P.ID_FUNCIONARIO = A.ID_FUNCIONARIO 
                    ) 
                )";

            using var commandDisponiveis = new SqlCommand(queryDisponiveis, connection);
            commandDisponiveis.Parameters.AddWithValue("@ID_ETAPA", idEtapa);

            using var adapterDisponiveis = new SqlDataAdapter(commandDisponiveis);
            adapterDisponiveis.Fill(dataTable);
        }
        catch (Exception ex)
        {
            throw new Exception($"Erro ao buscar funcionários disponíveis: {ex.Message}", ex);
        }

        return dataTable;
    }

    /// <summary>
    /// Busca uma etapa em aberto (não concluída e sem funcionário atribuído) para um pedido específico
    /// </summary>
    /// <param name="idPedidoProduto">ID do pedido produto</param>
    /// <param name="idTamanho">ID do tamanho (-1 para ignorar)</param>
    /// <param name="idGradePedProd">ID da grade do pedido produto (-1 para ignorar)</param>
    /// <param name="idEtapaProducao">ID da etapa de produção</param>
    /// <returns>ID da etapa encontrada ou 0 se não encontrada</returns>
    public int BuscaEtapaEmAberto(int idPedidoProduto, int idTamanho, int idGradePedProd, int idEtapaProducao)
    {
        try
        {
            using var connection = new SqlConnection(_connectionString);
            using var command = new SqlCommand(@"
                SELECT ID 
                FROM PRODUTO_PEDIDO_ETAPA_PROD
                WHERE ID_PEDIDO_PRODUTO = @ID_PEDIDO_PRODUTO 
                  AND ((@ID_TAMANHO = -1) OR (ID_TAMANHO = @ID_TAMANHO))
                  AND ((@ID_GRADE_PED_PROD = -1) OR (ID_GRADE_PED_PROD = @ID_GRADE_PED_PROD))
                  AND CONCLUIDO = @CONCLUIDO 
                  AND COALESCE(ID_FUNCIONARIO, 0) = 0 
                  AND ID_ETAPA_PRODUCAO = @ID_ETAPA_PRODUCAO", connection);

            command.Parameters.AddWithValue("@ID_PEDIDO_PRODUTO", idPedidoProduto);
            command.Parameters.AddWithValue("@ID_TAMANHO", idTamanho);
            command.Parameters.AddWithValue("@ID_GRADE_PED_PROD", idGradePedProd);
            command.Parameters.AddWithValue("@CONCLUIDO", 0);
            command.Parameters.AddWithValue("@ID_ETAPA_PRODUCAO", idEtapaProducao);

            connection.Open();
            var result = command.ExecuteScalar();

            if (result != null && result != DBNull.Value)
            {
                return Convert.ToInt32(result);
            }

            return 0;
        }
        catch (Exception ex)
        {
            throw new Exception($"Erro ao buscar etapa em aberto: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Atualiza o funcionário atribuído a uma etapa de produção
    /// </summary>
    /// <param name="id">ID da etapa de produção</param>
    /// <param name="idFuncionario">ID do funcionário a ser atribuído</param>
    /// <returns>True se a atualização foi bem-sucedida (pelo menos uma linha foi atualizada), False caso contrário</returns>
    public bool AtualizaFuncionarioEtapa(int id, int idFuncionario)
    {
        try
        {
            using var connection = new SqlConnection(_connectionString);
            using var command = new SqlCommand(@"
                UPDATE PRODUTO_PEDIDO_ETAPA_PROD 
                SET ID_FUNCIONARIO = @ID_FUNCIONARIO
                WHERE ID = @ID 
                  AND CONCLUIDO = 0 
                  AND COALESCE(ID_FUNCIONARIO, 0) = 0", connection);

            command.Parameters.AddWithValue("@ID", id);
            command.Parameters.AddWithValue("@ID_FUNCIONARIO", idFuncionario);

            connection.Open();
            int rowsAffected = command.ExecuteNonQuery();

            return rowsAffected > 0;
        }
        catch (Exception ex)
        {
            throw new Exception($"Erro ao atualizar funcionário da etapa: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Valida a senha (PIN) de um funcionário
    /// </summary>
    /// <param name="pin">PIN digitado pelo funcionário</param>
    /// <param name="idFuncionario">ID do funcionário</param>
    /// <returns>True se o PIN é válido para o funcionário, False caso contrário</returns>
    public bool ValidarSenhaFuncionario(string pin, int idFuncionario)
    {
        try
        {
            using var connection = new SqlConnection(_connectionString);
            using var command = new SqlCommand(@"
                SELECT 1 
                FROM FUNCIONARIOS 
                WHERE PIN = @PIN 
                  AND ID_FUNCIONARIO = @ID_FUNCIONARIO", connection);

            command.Parameters.AddWithValue("@PIN", pin);
            command.Parameters.AddWithValue("@ID_FUNCIONARIO", idFuncionario);

            connection.Open();
            var result = command.ExecuteScalar();

            return result != null && result != DBNull.Value;
        }
        catch (Exception ex)
        {
            throw new Exception($"Erro ao validar senha do funcionário: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Verifica se o funcionário está associado a um pedido específico
    /// </summary>
    /// <param name="idFuncionario">ID do funcionário</param>
    /// <param name="idPedidoProduto">ID do pedido produto</param>
    /// <param name="idTamanho">ID do tamanho (-1 para ignorar)</param>
    /// <param name="idGradePedProd">ID da grade do pedido produto (-1 para ignorar)</param>
    /// <param name="idEtapaProducao">ID da etapa de produção</param>
    /// <returns>True se o funcionário está associado, False caso contrário</returns>
    public bool VerificarFuncionarioAssociado(int idFuncionario, int idPedidoProduto, int idTamanho, int idGradePedProd, int idEtapaProducao)
    {
        try
        {
            using var connection = new SqlConnection(_connectionString);
            using var command = new SqlCommand(@"
                SELECT COUNT(1) 
                FROM PRODUTO_PEDIDO_ETAPA_PROD
                WHERE ID_PEDIDO_PRODUTO = @ID_PEDIDO_PRODUTO 
                  AND ((@ID_TAMANHO = -1) OR (ID_TAMANHO = @ID_TAMANHO))
                  AND ((@ID_GRADE_PED_PROD = -1) OR (ID_GRADE_PED_PROD = @ID_GRADE_PED_PROD))
                  AND CONCLUIDO = 0 
                  AND ID_FUNCIONARIO = @ID_FUNCIONARIO 
                  AND ID_ETAPA_PRODUCAO = @ID_ETAPA_PRODUCAO", connection);

            command.Parameters.AddWithValue("@ID_PEDIDO_PRODUTO", idPedidoProduto);
            command.Parameters.AddWithValue("@ID_TAMANHO", idTamanho);
            command.Parameters.AddWithValue("@ID_GRADE_PED_PROD", idGradePedProd);
            command.Parameters.AddWithValue("@ID_FUNCIONARIO", idFuncionario);
            command.Parameters.AddWithValue("@ID_ETAPA_PRODUCAO", idEtapaProducao);

            connection.Open();
            var result = command.ExecuteScalar();

            if (result != null && result != DBNull.Value)
            {
                return Convert.ToInt32(result) > 0;
            }

            return false;
        }
        catch (Exception ex)
        {
            throw new Exception($"Erro ao verificar funcionário associado: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Busca o ID da etapa quando o funcionário está associado
    /// </summary>
    public int BuscarIdEtapaFuncionarioAssociado(int idFuncionario, int idPedidoProduto, int idTamanho, int idGradePedProd, int idEtapaProducao)
    {
        try
        {
            using var connection = new SqlConnection(_connectionString);
            using var command = new SqlCommand(@"
                SELECT ID 
                FROM PRODUTO_PEDIDO_ETAPA_PROD
                WHERE ID_PEDIDO_PRODUTO = @ID_PEDIDO_PRODUTO 
                  AND ((@ID_TAMANHO = -1) OR (ID_TAMANHO = @ID_TAMANHO))
                  AND ((@ID_GRADE_PED_PROD = -1) OR (ID_GRADE_PED_PROD = @ID_GRADE_PED_PROD))
                  AND CONCLUIDO = 0 
                  AND ID_FUNCIONARIO = @ID_FUNCIONARIO 
                  AND ID_ETAPA_PRODUCAO = @ID_ETAPA_PRODUCAO", connection);

            command.Parameters.AddWithValue("@ID_PEDIDO_PRODUTO", idPedidoProduto);
            command.Parameters.AddWithValue("@ID_TAMANHO", idTamanho);
            command.Parameters.AddWithValue("@ID_GRADE_PED_PROD", idGradePedProd);
            command.Parameters.AddWithValue("@ID_FUNCIONARIO", idFuncionario);
            command.Parameters.AddWithValue("@ID_ETAPA_PRODUCAO", idEtapaProducao);

            connection.Open();
            var result = command.ExecuteScalar();

            if (result != null && result != DBNull.Value)
            {
                return Convert.ToInt32(result);
            }

            return 0;
        }
        catch (Exception ex)
        {
            throw new Exception($"Erro ao buscar ID da etapa do funcionário associado: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Atualiza um registro na tabela PRODUTO_PEDIDO_ETAPA_PROD
    /// </summary>
    /// <param name="idChave">ID do registro a ser atualizado</param>
    /// <param name="idEtapaProducao">ID da etapa de produção (0 para não atualizar)</param>
    /// <param name="idPedidoProduto">ID do pedido produto (0 para não atualizar)</param>
    /// <param name="idFuncionario">ID do funcionário (0 para não atualizar)</param>
    /// <param name="concluido">Concluído (0 para não atualizar, >0 para true)</param>
    /// <param name="perda">Perda (0 para não atualizar, >0 para true)</param>
    /// <param name="qtdeProduzida">Quantidade produzida (0 para não atualizar)</param>
    /// <param name="qtdePerda">Quantidade de perda (0 para não atualizar)</param>
    /// <param name="idPerda">ID da perda (0 para não atualizar)</param>
    /// <param name="idTamanho">ID do tamanho (0 para não atualizar)</param>
    /// <param name="qtde">Quantidade (0 para não atualizar)</param>
    /// <param name="idGradePedProd">ID da grade do pedido produto (0 para não atualizar)</param>
    /// <param name="reposicao">Reposição (0 para não atualizar, >0 para true)</param>
    /// <param name="dataInicio">Data de início (DateTime.MinValue para não atualizar)</param>
    /// <param name="dataFim">Data de fim (DateTime.MinValue para não atualizar)</param>
    /// <returns>True se pelo menos uma linha foi atualizada, False caso contrário</returns>
    public bool AtualizarProdutoPedidoEtapaProd(
        int idChave,
        int idEtapaProducao = 0,
        int idPedidoProduto = 0,
        int idFuncionario = 0,
        int concluido = 0,
        int perda = 0,
        int qtdeProduzida = 0,
        int qtdePerda = 0,
        int idPerda = 0,
        int idTamanho = 0,
        int qtde = 0,
        int idGradePedProd = 0,
        int reposicao = 0,
        DateTime? dataInicio = null,
        DateTime? dataFim = null)
    {
        try
        {
            var sqlSet = new List<string>();

            // Constrói a lista de campos a serem atualizados
            if (idEtapaProducao > 0) sqlSet.Add("ID_ETAPA_PRODUCAO = @ID_ETAPA_PRODUCAO");
            if (idPedidoProduto > 0) sqlSet.Add("ID_PEDIDO_PRODUTO = @ID_PEDIDO_PRODUTO");
            if (idFuncionario > 0) sqlSet.Add("ID_FUNCIONARIO = @ID_FUNCIONARIO");
            if (concluido >= 0) sqlSet.Add("CONCLUIDO = @CONCLUIDO"); // Permite 0 ou 1
            if (perda >= 0) sqlSet.Add("PERDA = @PERDA"); // Permite 0 ou 1
            if (qtdeProduzida >= 0) sqlSet.Add("QUANTIDADE_PRODUZIDA = @QUANTIDADE_PRODUZIDA"); // Permite 0
            if (qtdePerda >= 0) sqlSet.Add("QUANTIDADE_PERDA = @QUANTIDADE_PERDA"); // Permite 0
            if (idPerda >= 0) sqlSet.Add("ID_PERDA = @ID_PERDA"); // Permite 0
            if (idTamanho > 0) sqlSet.Add("ID_TAMANHO = @ID_TAMANHO");
            if (qtde > 0) sqlSet.Add("QUANTIDADE = @QUANTIDADE");
            if (idGradePedProd > 0) sqlSet.Add("ID_GRADE_PED_PROD = @ID_GRADE_PED_PROD");
            if (reposicao >= 0) sqlSet.Add("REPOSICAO = @REPOSICAO"); // Permite 0 ou 1
            if (dataInicio.HasValue && dataInicio.Value != DateTime.MinValue) sqlSet.Add("DATA_INICIO = @DATA_INICIO");
            if (dataFim.HasValue && dataFim.Value != DateTime.MinValue) sqlSet.Add("DATA_FIM = @DATA_FIM");

            // Se não há nada para atualizar, retorna false
            if (sqlSet.Count == 0)
            {
                return false;
            }

            // Monta a query SQL
            var sqlQuery = $"UPDATE PRODUTO_PEDIDO_ETAPA_PROD SET {string.Join(", ", sqlSet)} WHERE ID = @ID_CHAVE";

            using var connection = new SqlConnection(_connectionString);
            using var command = new SqlCommand(sqlQuery, connection);

            // Adiciona apenas os parâmetros que serão usados
            if (idEtapaProducao > 0) command.Parameters.AddWithValue("@ID_ETAPA_PRODUCAO", idEtapaProducao);
            if (idPedidoProduto > 0) command.Parameters.AddWithValue("@ID_PEDIDO_PRODUTO", idPedidoProduto);
            if (idFuncionario > 0) command.Parameters.AddWithValue("@ID_FUNCIONARIO", idFuncionario);
            if (concluido >= 0) command.Parameters.AddWithValue("@CONCLUIDO", concluido > 0);
            if (perda >= 0) command.Parameters.AddWithValue("@PERDA", perda > 0);
            if (qtdeProduzida >= 0) command.Parameters.AddWithValue("@QUANTIDADE_PRODUZIDA", qtdeProduzida);
            if (qtdePerda >= 0) command.Parameters.AddWithValue("@QUANTIDADE_PERDA", qtdePerda);
            if (idPerda >= 0) command.Parameters.AddWithValue("@ID_PERDA", idPerda);
            if (idTamanho > 0) command.Parameters.AddWithValue("@ID_TAMANHO", idTamanho);
            if (qtde > 0) command.Parameters.AddWithValue("@QUANTIDADE", qtde);
            if (idGradePedProd > 0) command.Parameters.AddWithValue("@ID_GRADE_PED_PROD", idGradePedProd);
            if (reposicao >= 0) command.Parameters.AddWithValue("@REPOSICAO", reposicao > 0);
            if (dataInicio.HasValue && dataInicio.Value != DateTime.MinValue) 
                command.Parameters.AddWithValue("@DATA_INICIO", dataInicio.Value);
            if (dataFim.HasValue && dataFim.Value != DateTime.MinValue) 
                command.Parameters.AddWithValue("@DATA_FIM", dataFim.Value);

            command.Parameters.AddWithValue("@ID_CHAVE", idChave);

            connection.Open();
            int rowsAffected = command.ExecuteNonQuery();

            return rowsAffected > 0;
        }
        catch (Exception ex)
        {
            throw new Exception($"Erro ao atualizar produto pedido etapa prod: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Insere um novo registro na tabela PRODUTO_PEDIDO_ETAPA_PROD
    /// </summary>
    /// <param name="idEtapaProducao">ID da etapa de produção</param>
    /// <param name="idPedidoProduto">ID do pedido produto</param>
    /// <param name="idGradePedProd">ID da grade do pedido produto</param>
    /// <param name="quantidade">Quantidade</param>
    /// <param name="idTamanho">ID do tamanho</param>
    /// <returns>True se o registro foi inserido com sucesso, False caso contrário</returns>
    public bool InserirProdutoPedidoEtapaProd(
        int idEtapaProducao,
        int idPedidoProduto,
        int idGradePedProd,
        int quantidade,
        int idTamanho,
        int reposicao = 0)
    {
        try
        {
            using var connection = new SqlConnection(_connectionString);
            using var command = new SqlCommand(@"
                INSERT INTO PRODUTO_PEDIDO_ETAPA_PROD 
                (ID_ETAPA_PRODUCAO, ID_PEDIDO_PRODUTO, CONCLUIDO, ID_GRADE_PED_PROD, QUANTIDADE, ID_TAMANHO, REPOSICAO)
                VALUES 
                (@ID_ETAPA_PRODUCAO, @ID_PEDIDO_PRODUTO, @CONCLUIDO, @ID_GRADE_PED_PROD, @QUANTIDADE, @ID_TAMANHO, @REPOSICAO)", connection);

            command.Parameters.AddWithValue("@ID_ETAPA_PRODUCAO", idEtapaProducao);
            command.Parameters.AddWithValue("@ID_PEDIDO_PRODUTO", idPedidoProduto);
            command.Parameters.AddWithValue("@CONCLUIDO", 0);
            command.Parameters.AddWithValue("@ID_GRADE_PED_PROD", idGradePedProd);
            command.Parameters.AddWithValue("@QUANTIDADE", quantidade);
            command.Parameters.AddWithValue("@ID_TAMANHO", idTamanho);
            command.Parameters.AddWithValue("@REPOSICAO", reposicao);

            connection.Open();
            int rowsAffected = command.ExecuteNonQuery();

            return rowsAffected > 0;
        }
        catch (Exception ex)
        {
            throw new Exception($"Erro ao inserir produto pedido etapa prod: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Processa evento de atualização de estoque (aumentar ou diminuir)
    /// A lógica de cálculo é feita no service
    /// </summary>
    /// <param name="idChave">ID do registro na tabela PRODUTO_PEDIDO_ETAPA_PROD</param>
    /// <param name="valorAtual">Valor atual do estoque</param>
    /// <param name="operacao">Operação: "aumentar" ou "diminuir"</param>
    /// <returns>Novo valor calculado, ou -1 se houver erro</returns>
    public int ProcessarEventoEstoque(int idChave, int valorAtual, string operacao)
    {
        try
        {
            if (idChave <= 0 || valorAtual < 0)
            {
                return -1;
            }

            int novoValor = valorAtual;

            if (operacao.ToLower() == "aumentar")
            {
                novoValor = valorAtual + 1;
            }
            else if (operacao.ToLower() == "diminuir")
            {
                // Lógica de negócio: não pode ser menor que 0
                if (valorAtual > 0)
                {
                    novoValor = valorAtual - 1;
                }
                else
                {
                    novoValor = 0; // Já está no mínimo
                }
            }
            else
            {
                return -1; // Operação inválida
            }

            // Garantir que o valor não seja negativo
            if (novoValor < 0)
            {
                novoValor = 0;
            }

            // Aqui você pode adicionar validações ou atualizações no banco se necessário
            // Por exemplo, atualizar um campo de controle ou fazer log

            return novoValor;
        }
        catch (Exception ex)
        {
            throw new Exception($"Erro ao processar evento de estoque: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Processa evento de atualização de cortar (aumentar ou diminuir)
    /// A lógica de cálculo é feita no service
    /// </summary>
    /// <param name="idChave">ID do registro na tabela PRODUTO_PEDIDO_ETAPA_PROD</param>
    /// <param name="valorAtual">Valor atual de cortar</param>
    /// <param name="operacao">Operação: "aumentar" ou "diminuir"</param>
    /// <returns>Novo valor calculado, ou -1 se houver erro</returns>
    public int ProcessarEventoCortar(int idChave, int valorAtual, string operacao)
    {
        try
        {
            if (idChave <= 0 || valorAtual < 0)
            {
                return -1;
            }

            int novoValor = valorAtual;

            if (operacao.ToLower() == "aumentar")
            {
                novoValor = valorAtual + 1;
            }
            else if (operacao.ToLower() == "diminuir")
            {
                // Lógica de negócio: não pode ser menor que 0
                if (valorAtual > 0)
                {
                    novoValor = valorAtual - 1;
                }
                else
                {
                    novoValor = 0; // Já está no mínimo
                }
            }
            else
            {
                return -1; // Operação inválida
            }

            // Garantir que o valor não seja negativo
            if (novoValor < 0)
            {
                novoValor = 0;
            }

            // Aqui você pode adicionar validações ou atualizações no banco se necessário
            // Por exemplo, atualizar um campo de controle ou fazer log

            return novoValor;
        }
        catch (Exception ex)
        {
            throw new Exception($"Erro ao processar evento de cortar: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Busca uma configuração pela chave
    /// </summary>
    /// <param name="chave">Chave da configuração</param>
    /// <returns>Valor da configuração ou string vazia se não encontrada</returns>
    public string BuscaConfiguracao(string chave)
    {
        try
        {
            using var connection = new SqlConnection(_connectionString);
            // Buscar todas as colunas e retornar a segunda (assumindo que a primeira é CHAVE)
            using var command = new SqlCommand("SELECT * FROM CONFIGURACOES WHERE CHAVE = @CHAVE", connection);
            command.Parameters.AddWithValue("@CHAVE", chave);
            connection.Open();
            
            using var reader = command.ExecuteReader();
            if (reader.Read() && reader.FieldCount > 1)
            {
                // Retorna a segunda coluna (assumindo que a primeira é CHAVE)
                var valor = reader.GetValue(1);
                return valor != null && valor != DBNull.Value ? valor.ToString() ?? string.Empty : string.Empty;
            }

            return string.Empty;
        }
        catch (Exception ex)
        {
            throw new Exception($"Erro ao buscar configuração: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Valida se existe pendência de etapa
    /// </summary>
    /// <param name="idPedidoProduto">ID do pedido produto</param>
    /// <param name="idTamanho">ID do tamanho</param>
    /// <param name="idGradePedProd">ID da grade do pedido produto</param>
    /// <param name="idEtapaOrigem">ID da etapa de origem (0 para ignorar)</param>
    /// <param name="idEtapaDestino">ID da etapa de destino</param>
    /// <returns>-1 se houver pendência, 0 caso contrário</returns>
    public int ValidaPendenciaEtapa(int idPedidoProduto, int idTamanho, int idGradePedProd, int concluido, int idEtapaProducao)
    {
        try
        {
            using var connection = new SqlConnection(_connectionString);
            using var command = new SqlCommand(@"
                SELECT ID_ETAPA_PRODUCAO 
                FROM PRODUTO_PEDIDO_ETAPA_PROD
                WHERE ID_PEDIDO_PRODUTO = @ID_PEDIDO_PRODUTO 
                  AND ID_TAMANHO = @ID_TAMANHO
                  AND ID_GRADE_PED_PROD = @ID_GRADE_PED_PROD
                  AND CONCLUIDO = @CONCLUIDO
                  AND ID_ETAPA_PRODUCAO = @ID_ETAPA_PRODUCAO", connection);

            command.Parameters.AddWithValue("@ID_PEDIDO_PRODUTO", idPedidoProduto);
            command.Parameters.AddWithValue("@ID_TAMANHO", idTamanho);
            command.Parameters.AddWithValue("@ID_GRADE_PED_PROD", idGradePedProd);
            command.Parameters.AddWithValue("@CONCLUIDO", concluido);
            command.Parameters.AddWithValue("@ID_ETAPA_PRODUCAO", idEtapaProducao);

            connection.Open();
            var result = command.ExecuteScalar();

            // Se encontrar registro, retorna o ID_ETAPA_PRODUCAO, senão retorna -1 (conforme lógica Delphi)
            if (result != null && result != DBNull.Value)
            {
                return Convert.ToInt32(result);
            }

            return -1;
        }
        catch (Exception ex)
        {
            throw new Exception($"Erro ao validar pendência de etapa: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Busca a próxima etapa de produção para um produto baseado na etapa atual
    /// </summary>
    /// <param name="idProduto">ID do produto</param>
    /// <param name="idEtapaAtual">ID da etapa atual</param>
    /// <returns>ID da próxima etapa ou 0 se não houver próxima etapa</returns>
    public int BuscaProximaEtapa(int idProduto, int idEtapaAtual)
    {
        try
        {
            using var connection = new SqlConnection(_connectionString);
            connection.Open();

            // Usar o SQL exato fornecido
            using var command = new SqlCommand(@"
                DECLARE @IDEtapaAtual INT;
                DECLARE @Sequencia INT;
                DECLARE @ID_PRODUTO INT;

                SET @ID_PRODUTO = @ID_PRODUTO_PARAM;
                SET @IDEtapaAtual = @ID_ETAPA_ATUAL_PARAM;

                SELECT @Sequencia = sequencia
                FROM PRODUTOS_ETAPAS_PRODUCAO
                WHERE ID_PRODUTO = @ID_PRODUTO
                  AND ID_ETAPA = @IDEtapaAtual;

                SELECT TOP 1 ID_ETAPA
                FROM PRODUTOS_ETAPAS_PRODUCAO
                WHERE ID_PRODUTO = @ID_PRODUTO
                  AND SEQUENCIA > @Sequencia
                ORDER BY SEQUENCIA ASC;", connection);

            command.Parameters.AddWithValue("@ID_PRODUTO_PARAM", idProduto);
            command.Parameters.AddWithValue("@ID_ETAPA_ATUAL_PARAM", idEtapaAtual);

            var result = command.ExecuteScalar();

            if (result != null && result != DBNull.Value)
            {
                return Convert.ToInt32(result);
            }

            return 0; // Não há próxima etapa
        }
        catch (Exception ex)
        {
            throw new Exception($"Erro ao buscar próxima etapa: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Busca todos os registros da tabela PERDA_PRODUCAO
    /// </summary>
    /// <returns>DataTable com os dados de PERDA_PRODUCAO</returns>
    public DataTable BuscarPerdaProducao()
    {
        var dataTable = new DataTable();

        try
        {
            using var connection = new SqlConnection(_connectionString);
            using var command = new SqlCommand("SELECT * FROM PERDA_PRODUCAO", connection);

            connection.Open();
            using var adapter = new SqlDataAdapter(command);
            adapter.Fill(dataTable);
        }
        catch (Exception ex)
        {
            throw new Exception($"Erro ao buscar perda produção: {ex.Message}", ex);
        }

        return dataTable;
    }

    /// <summary>
    /// Busca o ID_PRODUTO a partir do ID_PEDIDO_PRODUTO
    /// </summary>
    /// <param name="idPedidoProduto">ID do pedido produto</param>
    /// <returns>ID do produto ou 0 se não encontrado</returns>
    public int BuscarIdProduto(int idPedidoProduto)
    {
        try
        {
            using var connection = new SqlConnection(_connectionString);
            using var command = new SqlCommand("SELECT ID_PRODUTO FROM PEDIDO_PRODUTOS WHERE ID_PEDIDOPRODUTO = @ID_PEDIDO_PRODUTO", connection);

            command.Parameters.AddWithValue("@ID_PEDIDO_PRODUTO", idPedidoProduto);

            connection.Open();
            var result = command.ExecuteScalar();

            if (result != null && result != DBNull.Value)
            {
                return Convert.ToInt32(result);
            }

            return 0;
        }
        catch (Exception ex)
        {
            throw new Exception($"Erro ao buscar ID do produto: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Busca o ID_PEDIDO a partir do ID_PEDIDO_PRODUTO
    /// </summary>
    /// <param name="idPedidoProduto">ID do pedido produto</param>
    /// <returns>ID do pedido ou 0 se não encontrado</returns>
    public int BuscarIdPedido(int idPedidoProduto)
    {
        try
        {
            using var connection = new SqlConnection(_connectionString);
            using var command = new SqlCommand("SELECT ID_PEDIDO FROM PEDIDO_PRODUTOS WHERE ID_PEDIDOPRODUTO = @ID_PEDIDO_PRODUTO", connection);

            command.Parameters.AddWithValue("@ID_PEDIDO_PRODUTO", idPedidoProduto);

            connection.Open();
            var result = command.ExecuteScalar();

            if (result != null && result != DBNull.Value)
            {
                return Convert.ToInt32(result);
            }

            return 0;
        }
        catch (Exception ex)
        {
            throw new Exception($"Erro ao buscar ID do pedido: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Busca o ID_ETAPA_PRODUCAO atual a partir do ID do registro
    /// </summary>
    /// <param name="id">ID do registro em PRODUTO_PEDIDO_ETAPA_PROD</param>
    /// <returns>ID da etapa de produção ou 0 se não encontrado</returns>
    public int BuscarIdEtapaProducaoAtual(int id)
    {
        try
        {
            using var connection = new SqlConnection(_connectionString);
            using var command = new SqlCommand("SELECT ID_ETAPA_PRODUCAO FROM PRODUTO_PEDIDO_ETAPA_PROD WHERE ID = @ID", connection);

            command.Parameters.AddWithValue("@ID", id);

            connection.Open();
            var result = command.ExecuteScalar();

            if (result != null && result != DBNull.Value)
            {
                return Convert.ToInt32(result);
            }

            return 0;
        }
        catch (Exception ex)
        {
            throw new Exception($"Erro ao buscar ID da etapa de produção: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Busca os dados do pedido para a tela de Expedição
    /// </summary>
    /// <param name="idPedido">ID do pedido</param>
    /// <returns>DataTable com os dados do pedido ou null se não encontrado</returns>
    public DataTable BuscarDadosExpedicao(int idPedido)
    {
        var dataTable = new DataTable();

        try
        {
            using var connection = new SqlConnection(_connectionString);
            using var command = new SqlCommand(@"
                SELECT A.ID_PEDIDO
                      ,A.COD_PEDIDO
                      ,CONVERT(DATE, A.DATA_ENTREGA) AS DATA_ENTREGA
                      ,A.OBSERVACOES
                      ,B.FANTASIA
                      ,B.NOMERAZAO
                      ,B.ENDERECO_LOGRADOURO
                      ,B.ENDERECO_CEP
                      ,B.ENDERECO_CIDADE
                      ,B.ENDERECO_NUMERO
                      ,B.ENDERECO_BAIRRO
                      ,B.ENDERECO_COMPLEMENTO
                      ,'Rua:' + COALESCE(B.ENDERECO_LOGRADOURO,'') + ' Nº: '+ COALESCE(B.ENDERECO_NUMERO,'') + ' Cep: ' +COALESCE(B.ENDERECO_CEP,'') + ' Bairro: '+ COALESCE(B.ENDERECO_BAIRRO,'') + ' Cidade: '+ COALESCE(B.ENDERECO_CIDADE,'') + ' UF: '+ COALESCE(B.ENDERECO_UF,'') as ENDERECOCOMPLETO
                FROM PEDIDOS A
                INNER JOIN CLIENTES B ON A.ID_CLIENTE = B.ID_CLIENTE
                WHERE A.ID_PEDIDO = @ID_PEDIDO", connection);

            command.Parameters.AddWithValue("@ID_PEDIDO", idPedido);

            connection.Open();
            using var adapter = new SqlDataAdapter(command);
            adapter.Fill(dataTable);
        }
        catch (Exception ex)
        {
            throw new Exception($"Erro ao buscar dados de expedição: {ex.Message}", ex);
        }

        return dataTable;
    }

    /// <summary>
    /// Busca os produtos do pedido para a grid de Expedição
    /// </summary>
    /// <param name="idPedido">ID do pedido</param>
    /// <returns>DataTable com os produtos do pedido</returns>
    public DataTable BuscarProdutosExpedicao(int idPedido)
    {
        var dataTable = new DataTable();

        try
        {
            using var connection = new SqlConnection(_connectionString);
            using var command = new SqlCommand(@"
                DECLARE @ID_PEDIDO INT;
                SET @ID_PEDIDO = @ID_PEDIDO_PARAM;

                SELECT A.ID_PEDIDO
                       ,B.ID_PEDIDOPRODUTO
                       ,D.ID_TAMANHO
                       ,H.TAMANHO
                       ,CASE
                          WHEN D.PERDA IS NULL THEN D.QUANTIDADE
                          WHEN D.PERDA = 0 THEN D.QUANTIDADE
                          WHEN D.PERDA = 1 THEN D.QUANTIDADE_PRODUZIDA
                          ELSE D.QUANTIDADE
                        END AS QUANTIDADE
                       ,F.ID_GRADE_PED_PROD
                       ,D.ID_ETAPA_PRODUCAO AS ID_ETAPA
                       ,I.DESCRICAO ETAPAATUAL
                       ,C.DESCRICAO
                       ,C.FABRICACAO_TERCEIRIZADA
                       ,C.DESCRICAO + ' - ' + H.TAMANHO AS DESCRTAMANHO
                FROM PEDIDOS A
                INNER JOIN PEDIDO_PRODUTOS B ON A.ID_PEDIDO = B.ID_PEDIDO
                INNER JOIN PRODUTOS C ON C.ID_PRODUTO = B.ID_PRODUTO
                INNER JOIN PRODUTO_PEDIDO_ETAPA_PROD D ON B.ID_PEDIDOPRODUTO = D.ID_PEDIDO_PRODUTO
                LEFT JOIN GRADES E ON B.ID_GRADE = E.ID_GRADE
                LEFT JOIN PEDIDO_PROD_TAMANHOS F ON F.ID_PEDIDOPRODUTO = B.ID_PEDIDOPRODUTO AND F.ID_GRADE_PED_PROD = D.ID_GRADE_PED_PROD
                LEFT JOIN GRADE_TAMANHOS G ON G.ID_GRADE_TAMANHO = F.ID_GRADE_TAMANHO
                LEFT JOIN TAMANHOS_PRODUTOS H ON H.ID_TAMANHOPRODUTO = G.ID_TAMANHO
                LEFT JOIN ETAPAS_PRODUCAO I ON i.ID_ETAPA = d.ID_ETAPA_PRODUCAO
                WHERE A.ID_PEDIDO = @ID_PEDIDO
                  AND C.FABRICACAO_TERCEIRIZADA = 0
                  AND D.ID_ETAPA_PRODUCAO = 8

                UNION ALL

                SELECT A.ID_PEDIDO
                       ,B.ID_PEDIDOPRODUTO
                       ,D.ID_TAMANHO
                       ,H.TAMANHO
                       ,CASE
                          WHEN D.PERDA IS NULL THEN D.QUANTIDADE
                          WHEN D.PERDA = 0 THEN D.QUANTIDADE
                          WHEN D.PERDA = 1 THEN D.QUANTIDADE_PRODUZIDA
                          ELSE D.QUANTIDADE
                        END AS QUANTIDADE
                       ,F.ID_GRADE_PED_PROD
                       ,D.ID_ETAPA_PRODUCAO AS ID_ETAPA
                       ,I.DESCRICAO ETAPAATUAL
                       ,C.DESCRICAO
                       ,C.FABRICACAO_TERCEIRIZADA
                       ,C.DESCRICAO + ' - ' + H.TAMANHO AS DESCRTAMANHO
                FROM PEDIDOS A
                INNER JOIN PEDIDO_PRODUTOS B ON A.ID_PEDIDO = B.ID_PEDIDO
                INNER JOIN PRODUTOS C ON C.ID_PRODUTO = B.ID_PRODUTO
                INNER JOIN PRODUTO_PEDIDO_ETAPA_PROD D ON B.ID_PEDIDOPRODUTO = D.ID_PEDIDO_PRODUTO
                LEFT JOIN GRADES E ON B.ID_GRADE = E.ID_GRADE
                LEFT JOIN PEDIDO_PROD_TAMANHOS F ON F.ID_PEDIDOPRODUTO = B.ID_PEDIDOPRODUTO AND F.ID_GRADE_PED_PROD = D.ID_GRADE_PED_PROD
                LEFT JOIN GRADE_TAMANHOS G ON G.ID_GRADE_TAMANHO = F.ID_GRADE_TAMANHO
                LEFT JOIN TAMANHOS_PRODUTOS H ON H.ID_TAMANHOPRODUTO = G.ID_TAMANHO
                LEFT JOIN ETAPAS_PRODUCAO I ON i.ID_ETAPA = d.ID_ETAPA_PRODUCAO
                WHERE A.ID_PEDIDO = @ID_PEDIDO
                  AND C.FABRICACAO_TERCEIRIZADA = 0
                  AND D.CONCLUIDO = 0
                  AND D.ID_ETAPA_PRODUCAO <> 8", connection);

            command.Parameters.AddWithValue("@ID_PEDIDO_PARAM", idPedido);

            connection.Open();
            using var adapter = new SqlDataAdapter(command);
            adapter.Fill(dataTable);
        }
        catch (Exception ex)
        {
            throw new Exception($"Erro ao buscar produtos de expedição: {ex.Message}", ex);
        }

        return dataTable;
    }

    /// <summary>
    /// Valida se todos os produtos do pedido estão na etapa 8 (Expedição)
    /// </summary>
    /// <param name="idPedido">ID do pedido</param>
    /// <returns>True se todos estão na etapa 8, False caso contrário</returns>
    public bool ValidarTodosProdutosNaEtapa8(int idPedido)
    {
        try
        {
            using var connection = new SqlConnection(_connectionString);
            using var command = new SqlCommand(@"
                SELECT COUNT(*) 
                FROM PRODUTO_PEDIDO_ETAPA_PROD D
                INNER JOIN PEDIDO_PRODUTOS B ON D.ID_PEDIDO_PRODUTO = B.ID_PEDIDOPRODUTO
                INNER JOIN PRODUTOS C ON C.ID_PRODUTO = B.ID_PRODUTO
                WHERE B.ID_PEDIDO = @ID_PEDIDO
                  AND C.FABRICACAO_TERCEIRIZADA = 0
                  AND D.ID_ETAPA_PRODUCAO <> 8
                  AND D.CONCLUIDO = 0", connection);

            command.Parameters.AddWithValue("@ID_PEDIDO", idPedido);

            connection.Open();
            var result = command.ExecuteScalar();

            if (result != null && result != DBNull.Value)
            {
                int count = Convert.ToInt32(result);
                return count == 0; // Se count == 0, todos estão na etapa 8
            }

            return false;
        }
        catch (Exception ex)
        {
            throw new Exception($"Erro ao validar produtos na etapa 8: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Conclui o pedido atualizando as etapas e o status
    /// </summary>
    /// <param name="idPedido">ID do pedido</param>
    /// <param name="idFuncionario">ID do funcionário</param>
    /// <returns>True se concluído com sucesso, False caso contrário</returns>
    public bool ConcluirPedido(int idPedido, int idFuncionario)
    {
        try
        {
            using var connection = new SqlConnection(_connectionString);
            connection.Open();
            using var transaction = connection.BeginTransaction();

            try
            {
                // Primeiro UPDATE: Concluir etapas e atualizar funcionário na etapa 8
                using var command1 = new SqlCommand(@"
                    UPDATE PRODUTO_PEDIDO_ETAPA_PROD
                    SET CONCLUIDO = 1,
                        ID_FUNCIONARIO = CASE
                                          WHEN ID_ETAPA_PRODUCAO = 8 THEN @ID_FUNCIONARIO
                                          ELSE ID_FUNCIONARIO
                                        END
                    WHERE ID_PEDIDO_PRODUTO IN (
                          SELECT ID_PEDIDOPRODUTO
                          FROM PEDIDO_PRODUTOS
                          WHERE ID_PEDIDO = @ID_PEDIDO
                      )", connection, transaction);

                command1.Parameters.AddWithValue("@ID_PEDIDO", idPedido);
                command1.Parameters.AddWithValue("@ID_FUNCIONARIO", idFuncionario);
                command1.ExecuteNonQuery();

                // Segundo UPDATE: Atualizar status do pedido
                using var command2 = new SqlCommand(@"
                    UPDATE PEDIDOS 
                    SET ID_STATUSPEDIDO = 4 
                    WHERE ID_PEDIDO = @ID_PEDIDO", connection, transaction);

                command2.Parameters.AddWithValue("@ID_PEDIDO", idPedido);
                command2.ExecuteNonQuery();

                transaction.Commit();
                return true;
            }
            catch
            {
                transaction.Rollback();
                throw;
            }
        }
        catch (Exception ex)
        {
            throw new Exception($"Erro ao concluir pedido: {ex.Message}", ex);
        }
    }
}


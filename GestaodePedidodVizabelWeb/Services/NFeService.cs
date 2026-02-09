using Microsoft.EntityFrameworkCore;
using GestaoPedidosVizabel.Data;
using GestaoPedidosVizabel.Models;
using GestaoPedidosVizabel.Models.ViewModels;
using GestaoPedidosVizabel.Helpers;
using System.Text.RegularExpressions;

namespace GestaoPedidosVizabel.Services
{
    public interface INFeService
    {
        Task<NFe> CreateNFeAsync(NFeCreateViewModel viewModel);
        Task<NFe> UpdateNFeAsync(NFeEditViewModel viewModel);
        Task<bool> ValidateNcmAsync(string ncm);
        Task<bool> ValidateCfopAsync(string cfop);
        decimal CalculateValorTotalNfe(decimal valorProdutos, decimal totalImpostos);
        decimal CalculateValorProdutos(ICollection<NFeItem> itens);
        Task LogAuditoriaAsync(string acao, long? idNfe, string usuario, string detalhes);
    }

    public class NFeService : INFeService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<NFeService> _logger;

        public NFeService(ApplicationDbContext context, ILogger<NFeService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<NFe> CreateNFeAsync(NFeCreateViewModel viewModel)
        {
            _logger.LogInformation("=== INICIANDO CreateNFeAsync ===");
            _logger.LogInformation($"Itens recebidos: {viewModel.Itens?.Count ?? 0}");
            _logger.LogInformation($"Pagamentos recebidos: {viewModel.Pagamentos?.Count ?? 0}");
            
            if (viewModel.Pagamentos != null && viewModel.Pagamentos.Any())
            {
                _logger.LogInformation("=== DETALHES DOS PAGAMENTOS NO SERVICE ===");
                foreach (var pag in viewModel.Pagamentos)
                {
                    _logger.LogInformation($"Pagamento - Tipo: '{pag.TipoPagamento}', Valor: {pag.ValorPago}");
                }
            }
            
            // Buscar ambiente da configuração
            var ambienteStr = await ConfiguracaoHelper.BuscarValorConfiguracaoAsync(_context, "AMBIENTE_NFE");
            byte ambiente = 2; // Padrão: Homologação (2)
            if (!string.IsNullOrEmpty(ambienteStr) && byte.TryParse(ambienteStr, out var ambienteValue))
            {
                ambiente = ambienteValue;
            }
            else
            {
                

















                
            // Calcular totais automaticamente se não foram informados
            var nfe = new NFe
            {
                IdEmpresa = viewModel.IdEmpresa > 0 ? viewModel.IdEmpresa : 1, // Valor padrão se não informado
                Modelo = viewModel.Modelo,
                Serie = viewModel.Serie,
                Numero = viewModel.Numero,
                NaturezaOperacao = viewModel.NaturezaOperacao,
                DataEmissao = viewModel.DataEmissao,
                TipoNfe = viewModel.TipoNfe,
                Finalidade = viewModel.Finalidade,
                Ambiente = ambiente, // Buscado da configuração
                ValorProdutos = viewModel.ValorProdutos,
                ValorTotalNfe = viewModel.ValorTotalNfe,
                Status = !string.IsNullOrWhiteSpace(viewModel.Status) ? viewModel.Status : "Em edição", // Status padrão: "Em edição"
                ChaveAcesso = viewModel.ChaveAcesso,
                DataSaida = viewModel.DataSaida,
                RegimeTributario = viewModel.RegimeTributario,
                IdPedido = viewModel.IdPedido
            };

            _context.Add(nfe);
            await _context.SaveChangesAsync();
            _logger.LogInformation($"NF-e criada com ID: {nfe.IdNfe}");

            // Processar destinatário
            if (viewModel.Destinatario != null)
            {
                // Se houver IdPedido, buscar o UF do cliente do pedido
                string? ufDestinatario = null;
                if (viewModel.IdPedido.HasValue && viewModel.IdPedido.Value > 0)
                {
                    var pedido = await _context.Pedidos
                        .Include(p => p.Cliente)
                        .FirstOrDefaultAsync(p => p.IdPedido == viewModel.IdPedido.Value);
                    
                    if (pedido?.Cliente != null && !string.IsNullOrWhiteSpace(pedido.Cliente.EnderecoUf))
                    {
                        ufDestinatario = pedido.Cliente.EnderecoUf.Trim().ToUpper();
                        _logger.LogInformation($"UF do destinatário obtido do pedido {viewModel.IdPedido.Value}: {ufDestinatario}");
                    }
                }
                
                // Usar UF do cliente se encontrado, senão usar o do viewModel, senão usar padrão
                var uf = !string.IsNullOrWhiteSpace(ufDestinatario) 
                    ? ufDestinatario 
                    : (!string.IsNullOrWhiteSpace(viewModel.Destinatario.Uf) 
                        ? viewModel.Destinatario.Uf.Trim().ToUpper() 
                        : "XX");
                
                // Garantir que campos obrigatórios tenham valores válidos
                var destinatario = new NFeDestinatario
                {
                    IdNfe = nfe.IdNfe,
                    Nome = !string.IsNullOrWhiteSpace(viewModel.Destinatario.Nome) ? viewModel.Destinatario.Nome.Trim() : "NÃO INFORMADO",
                    CnpjCpf = !string.IsNullOrWhiteSpace(viewModel.Destinatario.CnpjCpf) ? viewModel.Destinatario.CnpjCpf.Trim() : "00000000000000",
                    IndIeDest = viewModel.Destinatario.IndIeDest > 0 ? viewModel.Destinatario.IndIeDest : (byte)9, // 9 = Não contribuinte
                    Ie = viewModel.Destinatario.Ie,
                    Logradouro = !string.IsNullOrWhiteSpace(viewModel.Destinatario.Logradouro) ? viewModel.Destinatario.Logradouro.Trim() : "NÃO INFORMADO",
                    Numero = !string.IsNullOrWhiteSpace(viewModel.Destinatario.Numero) ? viewModel.Destinatario.Numero.Trim() : "S/N",
                    Bairro = !string.IsNullOrWhiteSpace(viewModel.Destinatario.Bairro) ? viewModel.Destinatario.Bairro.Trim() : "NÃO INFORMADO",
                    CodMun = viewModel.Destinatario.CodMun ?? 0, // Valor padrão se não informado
                    Municipio = !string.IsNullOrWhiteSpace(viewModel.Destinatario.Municipio) ? viewModel.Destinatario.Municipio.Trim() : "NÃO INFORMADO",
                    Uf = uf,
                    Cep = !string.IsNullOrWhiteSpace(viewModel.Destinatario.Cep) ? viewModel.Destinatario.Cep.Trim() : "00000000"
                };
                _context.Add(destinatario);
                await _context.SaveChangesAsync();
            }

            // Processar itens
            if (viewModel.Itens != null && viewModel.Itens.Any())
            {
                _logger.LogInformation($"Processando {viewModel.Itens.Count()} itens para NF-e {nfe.IdNfe}");
                
                int itensSalvos = 0;
                foreach (var itemViewModel in viewModel.Itens)
                {
                    try
                    {
                        // Validar se os campos obrigatórios estão preenchidos
                        if (string.IsNullOrWhiteSpace(itemViewModel.CodProduto) ||
                            string.IsNullOrWhiteSpace(itemViewModel.Descricao) ||
                            string.IsNullOrWhiteSpace(itemViewModel.Ncm) ||
                            string.IsNullOrWhiteSpace(itemViewModel.Cfop))
                        {
                            _logger.LogWarning($"Item pulado - campos obrigatórios vazios. CodProduto: {itemViewModel.CodProduto}, Descricao: {itemViewModel.Descricao}, NCM: {itemViewModel.Ncm}, CFOP: {itemViewModel.Cfop}");
                            continue; // Pular itens inválidos
                        }

                        // Calcular ValorTotal se não foi informado ou se está incorreto
                        var quantidade = itemViewModel.Quantidade > 0 ? itemViewModel.Quantidade : 0;
                        var valorUnitario = itemViewModel.ValorUnitario >= 0 ? itemViewModel.ValorUnitario : 0;
                        var valorTotal = quantidade * valorUnitario;
                        
                        // Se o ValorTotal informado está diferente do calculado, usar o calculado
                        if (itemViewModel.ValorTotal != valorTotal)
                        {
                            _logger.LogInformation($"ValorTotal recalculado para item - Informado: {itemViewModel.ValorTotal}, Calculado: {valorTotal}");
                        }

                        var novoItem = new NFeItem
                        {
                            IdNfe = nfe.IdNfe,
                            CodProduto = itemViewModel.CodProduto?.Trim() ?? string.Empty,
                            Descricao = itemViewModel.Descricao?.Trim() ?? string.Empty,
                            Ncm = itemViewModel.Ncm?.Trim() ?? string.Empty,
                            Cfop = itemViewModel.Cfop?.Trim() ?? string.Empty,
                            Unidade = !string.IsNullOrWhiteSpace(itemViewModel.Unidade) ? itemViewModel.Unidade.Trim() : "UN",
                            Quantidade = quantidade,
                            ValorUnitario = valorUnitario,
                            ValorTotal = valorTotal
                        };

                        _context.Add(novoItem);
                        await _context.SaveChangesAsync();
                        itensSalvos++;

                        _logger.LogInformation($"Item {novoItem.IdItem} salvo com sucesso - CodProduto: {novoItem.CodProduto}, Descricao: {novoItem.Descricao}");

                        // Adicionar impostos se fornecidos
                        if (itemViewModel.Imposto != null)
                        {
                            // Garantir que CstCsosn tenha 3 caracteres (padrão "000" se vazio)
                            var cstCsosn = itemViewModel.Imposto.CstCsosn?.Trim() ?? string.Empty;
                            if (string.IsNullOrWhiteSpace(cstCsosn) || cstCsosn.Length != 3)
                            {
                                cstCsosn = "000"; // Valor padrão
                            }
                            
                            var novoImposto = new NFeItemImposto
                            {
                                IdItem = novoItem.IdItem,
                                Origem = itemViewModel.Imposto.Origem,
                                CstCsosn = cstCsosn,
                                BaseIcms = itemViewModel.Imposto.BaseIcms,
                                AliquotaIcms = itemViewModel.Imposto.AliquotaIcms,
                                ValorIcms = itemViewModel.Imposto.ValorIcms,
                                BasePis = itemViewModel.Imposto.BasePis,
                                ValorPis = itemViewModel.Imposto.ValorPis,
                                BaseCofins = itemViewModel.Imposto.BaseCofins,
                                ValorCofins = itemViewModel.Imposto.ValorCofins
                            };
                            _context.Add(novoImposto);
                            await _context.SaveChangesAsync();
                            _logger.LogInformation($"Imposto {novoImposto.IdImposto} salvo para item {novoItem.IdItem}");
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, $"Erro ao salvar item - CodProduto: {itemViewModel.CodProduto}, Descricao: {itemViewModel.Descricao}");
                        _logger.LogError($"Stack Trace: {ex.StackTrace}");
                        if (ex.InnerException != null)
                        {
                            _logger.LogError($"Inner Exception: {ex.InnerException.Message}");
                        }
                        throw; // Re-lançar exceção para que o controller possa tratá-la
                    }
                }

                _logger.LogInformation($"Total de itens salvos: {itensSalvos} de {viewModel.Itens.Count()}");

                // Recalcular totais automaticamente
                await RecalcularTotaisAsync(nfe);
                _context.Update(nfe);
                await _context.SaveChangesAsync();
            }
            else
            {
                _logger.LogWarning($"Nenhum item encontrado no viewModel para NF-e {nfe.IdNfe}");
                _logger.LogWarning($"viewModel.Itens é null: {viewModel.Itens == null}");
                if (viewModel.Itens != null)
                {
                    _logger.LogWarning($"viewModel.Itens.Count: {viewModel.Itens.Count()}");
                }
            }

            // Processar pagamentos
            if (viewModel.Pagamentos != null && viewModel.Pagamentos.Any())
            {
                _logger.LogInformation($"Processando {viewModel.Pagamentos.Count()} pagamentos para NF-e {nfe.IdNfe}");
                
                int pagamentosSalvos = 0;
                foreach (var pagamentoViewModel in viewModel.Pagamentos)
                {
                    try
                    {
                        _logger.LogInformation($"Processando pagamento - TipoPagamento: '{pagamentoViewModel.TipoPagamento}', ValorPago: {pagamentoViewModel.ValorPago}");
                        
                        // Validar e normalizar TipoPagamento
                        var tipoPagamento = pagamentoViewModel.TipoPagamento?.Trim() ?? string.Empty;
                        if (string.IsNullOrWhiteSpace(tipoPagamento))
                        {
                            _logger.LogWarning($"Pagamento pulado - TipoPagamento vazio ou null");
                            continue;
                        }
                        
                        // Garantir que TipoPagamento tenha 2 caracteres (preencher com 0 à esquerda se necessário)
                        if (tipoPagamento.Length == 1)
                        {
                            tipoPagamento = "0" + tipoPagamento;
                        }
                        else if (tipoPagamento.Length > 2)
                        {
                            tipoPagamento = tipoPagamento.Substring(0, 2);
                        }
                        
                        // Validar ValorPago
                        if (pagamentoViewModel.ValorPago <= 0)
                        {
                            _logger.LogWarning($"Pagamento pulado - ValorPago inválido: {pagamentoViewModel.ValorPago}");
                            continue;
                        }

                        var novoPagamento = new NFePagamento
                        {
                            IdNfe = nfe.IdNfe,
                            TipoPagamento = tipoPagamento,
                            ValorPago = pagamentoViewModel.ValorPago
                        };
                        
                        _logger.LogInformation($"Criando pagamento - IdNfe: {novoPagamento.IdNfe}, TipoPagamento: '{novoPagamento.TipoPagamento}', ValorPago: {novoPagamento.ValorPago}");

                        _context.NFePagamentos.Add(novoPagamento);
                        _logger.LogInformation($"Pagamento adicionado ao contexto. Estado: {_context.Entry(novoPagamento).State}");
                        
                        await _context.SaveChangesAsync();
                        _logger.LogInformation($"SaveChangesAsync executado. IdPagamento gerado: {novoPagamento.IdPagamento}");
                        
                        pagamentosSalvos++;
                        _logger.LogInformation($"Pagamento {novoPagamento.IdPagamento} salvo com sucesso - Tipo: '{novoPagamento.TipoPagamento}', Valor: {novoPagamento.ValorPago}");
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, $"Erro ao salvar pagamento - TipoPagamento: {pagamentoViewModel.TipoPagamento}, ValorPago: {pagamentoViewModel.ValorPago}");
                        throw;
                    }
                }
                
                _logger.LogInformation($"Total de pagamentos salvos: {pagamentosSalvos} de {viewModel.Pagamentos.Count()}");
            }
            else
            {
                _logger.LogWarning($"Nenhum pagamento encontrado no viewModel para NF-e {nfe.IdNfe}");
                _logger.LogWarning($"viewModel.Pagamentos é null: {viewModel.Pagamentos == null}");
                if (viewModel.Pagamentos != null)
                {
                    _logger.LogWarning($"viewModel.Pagamentos.Count: {viewModel.Pagamentos.Count()}");
                }
            }

            _logger.LogInformation("=== FINALIZANDO CreateNFeAsync ===");
            return nfe;
        }

        public async Task<NFe> UpdateNFeAsync(NFeEditViewModel viewModel)
        {
            var nfe = await _context.NFes
                .Include(n => n.Destinatario)
                .Include(n => n.Itens)
                    .ThenInclude(i => i.Imposto)
                .FirstOrDefaultAsync(n => n.IdNfe == viewModel.IdNfe);

            if (nfe == null)
                throw new InvalidOperationException("NF-e não encontrada");

            // Atualizar dados da NF-e
            // Nota: O campo Ambiente não é atualizado na edição, mantém o valor original
            nfe.IdEmpresa = viewModel.IdEmpresa;
            nfe.Modelo = viewModel.Modelo;
            nfe.Serie = viewModel.Serie;
            nfe.Numero = viewModel.Numero;
            nfe.NaturezaOperacao = viewModel.NaturezaOperacao;
            nfe.DataEmissao = viewModel.DataEmissao;
            nfe.TipoNfe = viewModel.TipoNfe;
            nfe.Finalidade = viewModel.Finalidade;
            nfe.Status = viewModel.Status;
            nfe.ChaveAcesso = viewModel.ChaveAcesso;
            nfe.DataSaida = viewModel.DataSaida;
            nfe.RegimeTributario = viewModel.RegimeTributario;
            nfe.IdPedido = viewModel.IdPedido;

            // Processar destinatário
            if (viewModel.Destinatario != null)
            {
                // Se houver IdPedido, buscar o UF do cliente do pedido
                string? ufDestinatario = null;
                if (viewModel.IdPedido.HasValue && viewModel.IdPedido.Value > 0)
                {
                    var pedido = await _context.Pedidos
                        .Include(p => p.Cliente)
                        .FirstOrDefaultAsync(p => p.IdPedido == viewModel.IdPedido.Value);
                    
                    if (pedido?.Cliente != null && !string.IsNullOrWhiteSpace(pedido.Cliente.EnderecoUf))
                    {
                        ufDestinatario = pedido.Cliente.EnderecoUf.Trim().ToUpper();
                        _logger.LogInformation($"UF do destinatário obtido do pedido {viewModel.IdPedido.Value}: {ufDestinatario}");
                    }
                }
                
                // Usar UF do cliente se encontrado, senão usar o do viewModel, senão manter o existente ou usar padrão
                var uf = !string.IsNullOrWhiteSpace(ufDestinatario) 
                    ? ufDestinatario 
                    : (!string.IsNullOrWhiteSpace(viewModel.Destinatario.Uf) 
                        ? viewModel.Destinatario.Uf.Trim().ToUpper() 
                        : null);
                
                if (nfe.Destinatario != null)
                {
                    // Atualizar destinatário existente
                    nfe.Destinatario.Nome = !string.IsNullOrWhiteSpace(viewModel.Destinatario.Nome) ? viewModel.Destinatario.Nome.Trim() : nfe.Destinatario.Nome;
                    nfe.Destinatario.CnpjCpf = !string.IsNullOrWhiteSpace(viewModel.Destinatario.CnpjCpf) ? viewModel.Destinatario.CnpjCpf.Trim() : nfe.Destinatario.CnpjCpf;
                    nfe.Destinatario.IndIeDest = viewModel.Destinatario.IndIeDest > 0 ? viewModel.Destinatario.IndIeDest : nfe.Destinatario.IndIeDest;
                    nfe.Destinatario.Ie = viewModel.Destinatario.Ie;
                    nfe.Destinatario.Logradouro = !string.IsNullOrWhiteSpace(viewModel.Destinatario.Logradouro) ? viewModel.Destinatario.Logradouro.Trim() : nfe.Destinatario.Logradouro;
                    nfe.Destinatario.Numero = !string.IsNullOrWhiteSpace(viewModel.Destinatario.Numero) ? viewModel.Destinatario.Numero.Trim() : nfe.Destinatario.Numero;
                    nfe.Destinatario.Bairro = !string.IsNullOrWhiteSpace(viewModel.Destinatario.Bairro) ? viewModel.Destinatario.Bairro.Trim() : nfe.Destinatario.Bairro;
                    nfe.Destinatario.CodMun = viewModel.Destinatario.CodMun ?? nfe.Destinatario.CodMun;
                    nfe.Destinatario.Municipio = !string.IsNullOrWhiteSpace(viewModel.Destinatario.Municipio) ? viewModel.Destinatario.Municipio.Trim() : nfe.Destinatario.Municipio;
                    nfe.Destinatario.Uf = uf ?? nfe.Destinatario.Uf;
                    nfe.Destinatario.Cep = !string.IsNullOrWhiteSpace(viewModel.Destinatario.Cep) ? viewModel.Destinatario.Cep.Trim() : nfe.Destinatario.Cep;
                    _context.Update(nfe.Destinatario);
                }
                else
                {
                    // Criar novo destinatário
                    var destinatario = new NFeDestinatario
                    {
                        IdNfe = nfe.IdNfe,
                        Nome = !string.IsNullOrWhiteSpace(viewModel.Destinatario.Nome) ? viewModel.Destinatario.Nome.Trim() : "NÃO INFORMADO",
                        CnpjCpf = !string.IsNullOrWhiteSpace(viewModel.Destinatario.CnpjCpf) ? viewModel.Destinatario.CnpjCpf.Trim() : "00000000000000",
                        IndIeDest = viewModel.Destinatario.IndIeDest > 0 ? viewModel.Destinatario.IndIeDest : (byte)9,
                        Ie = viewModel.Destinatario.Ie,
                        Logradouro = !string.IsNullOrWhiteSpace(viewModel.Destinatario.Logradouro) ? viewModel.Destinatario.Logradouro.Trim() : "NÃO INFORMADO",
                        Numero = !string.IsNullOrWhiteSpace(viewModel.Destinatario.Numero) ? viewModel.Destinatario.Numero.Trim() : "S/N",
                        Bairro = !string.IsNullOrWhiteSpace(viewModel.Destinatario.Bairro) ? viewModel.Destinatario.Bairro.Trim() : "NÃO INFORMADO",
                        CodMun = viewModel.Destinatario.CodMun ?? 0,
                        Municipio = !string.IsNullOrWhiteSpace(viewModel.Destinatario.Municipio) ? viewModel.Destinatario.Municipio.Trim() : "NÃO INFORMADO",
                        Uf = uf ?? "XX",
                        Cep = !string.IsNullOrWhiteSpace(viewModel.Destinatario.Cep) ? viewModel.Destinatario.Cep.Trim() : "00000000"
                    };
                    _context.Add(destinatario);
                }
            }

            // Processar itens removidos
            if (viewModel.ItensRemovidos != null && viewModel.ItensRemovidos.Any())
            {
                foreach (var itemId in viewModel.ItensRemovidos)
                {
                    var itemRemover = await _context.NFeItens
                        .Include(i => i.Imposto)
                        .FirstOrDefaultAsync(i => i.IdItem == itemId && i.IdNfe == nfe.IdNfe);

                    if (itemRemover != null)
                    {
                        if (itemRemover.Imposto != null)
                        {
                            _context.NFeItemImpostos.Remove(itemRemover.Imposto);
                        }
                        _context.NFeItens.Remove(itemRemover);
                    }
                }
            }

            // Processar itens existentes atualizados
            if (viewModel.Itens != null && viewModel.Itens.Any())
            {
                foreach (var itemViewModel in viewModel.Itens.Where(i => i.IdItem.HasValue))
                {
                    var item = await _context.NFeItens
                        .Include(i => i.Imposto)
                        .FirstOrDefaultAsync(i => i.IdItem == itemViewModel.IdItem!.Value && i.IdNfe == nfe.IdNfe);

                    if (item != null)
                    {
                        // Calcular ValorTotal se não foi informado ou se está incorreto
                        var quantidade = itemViewModel.Quantidade > 0 ? itemViewModel.Quantidade : 0;
                        var valorUnitario = itemViewModel.ValorUnitario >= 0 ? itemViewModel.ValorUnitario : 0;
                        var valorTotal = quantidade * valorUnitario;
                        
                        // Se o ValorTotal informado está diferente do calculado, usar o calculado
                        if (itemViewModel.ValorTotal != valorTotal)
                        {
                            _logger.LogInformation($"ValorTotal recalculado para item {item.IdItem} - Informado: {itemViewModel.ValorTotal}, Calculado: {valorTotal}");
                        }

                        item.CodProduto = itemViewModel.CodProduto;
                        item.Descricao = itemViewModel.Descricao;
                        item.Ncm = itemViewModel.Ncm;
                        item.Cfop = itemViewModel.Cfop;
                        item.Unidade = itemViewModel.Unidade;
                        item.Quantidade = quantidade;
                        item.ValorUnitario = valorUnitario;
                        item.ValorTotal = valorTotal;

                        // Atualizar impostos
                        if (itemViewModel.Imposto != null)
                        {
                            // Garantir que CstCsosn tenha 3 caracteres (padrão "000" se vazio)
                            var cstCsosn = itemViewModel.Imposto.CstCsosn?.Trim() ?? string.Empty;
                            if (string.IsNullOrWhiteSpace(cstCsosn) || cstCsosn.Length != 3)
                            {
                                cstCsosn = "000"; // Valor padrão
                            }
                            
                            if (item.Imposto != null)
                            {
                                item.Imposto.Origem = itemViewModel.Imposto.Origem;
                                item.Imposto.CstCsosn = cstCsosn;
                                item.Imposto.BaseIcms = itemViewModel.Imposto.BaseIcms;
                                item.Imposto.AliquotaIcms = itemViewModel.Imposto.AliquotaIcms;
                                item.Imposto.ValorIcms = itemViewModel.Imposto.ValorIcms;
                                item.Imposto.BasePis = itemViewModel.Imposto.BasePis;
                                item.Imposto.ValorPis = itemViewModel.Imposto.ValorPis;
                                item.Imposto.BaseCofins = itemViewModel.Imposto.BaseCofins;
                                item.Imposto.ValorCofins = itemViewModel.Imposto.ValorCofins;
                                _context.Update(item.Imposto);
                            }
                            else
                            {
                                var novoImposto = new NFeItemImposto
                                {
                                    IdItem = item.IdItem,
                                    Origem = itemViewModel.Imposto.Origem,
                                    CstCsosn = cstCsosn,
                                    BaseIcms = itemViewModel.Imposto.BaseIcms,
                                    AliquotaIcms = itemViewModel.Imposto.AliquotaIcms,
                                    ValorIcms = itemViewModel.Imposto.ValorIcms,
                                    BasePis = itemViewModel.Imposto.BasePis,
                                    ValorPis = itemViewModel.Imposto.ValorPis,
                                    BaseCofins = itemViewModel.Imposto.BaseCofins,
                                    ValorCofins = itemViewModel.Imposto.ValorCofins
                                };
                                _context.Add(novoImposto);
                            }
                        }

                        _context.Update(item);
                    }
                }
            }

            // Processar novos itens
            if (viewModel.NovosItens != null && viewModel.NovosItens.Any())
            {
                foreach (var itemViewModel in viewModel.NovosItens)
                {
                    // Calcular ValorTotal se não foi informado ou se está incorreto
                    var quantidade = itemViewModel.Quantidade > 0 ? itemViewModel.Quantidade : 0;
                    var valorUnitario = itemViewModel.ValorUnitario >= 0 ? itemViewModel.ValorUnitario : 0;
                    var valorTotal = quantidade * valorUnitario;
                    
                    // Se o ValorTotal informado está diferente do calculado, usar o calculado
                    if (itemViewModel.ValorTotal != valorTotal)
                    {
                        _logger.LogInformation($"ValorTotal recalculado para novo item - Informado: {itemViewModel.ValorTotal}, Calculado: {valorTotal}");
                    }

                    var novoItem = new NFeItem
                    {
                        IdNfe = nfe.IdNfe,
                        CodProduto = itemViewModel.CodProduto,
                        Descricao = itemViewModel.Descricao,
                        Ncm = itemViewModel.Ncm,
                        Cfop = itemViewModel.Cfop,
                        Unidade = itemViewModel.Unidade,
                        Quantidade = quantidade,
                        ValorUnitario = valorUnitario,
                        ValorTotal = valorTotal
                    };

                    _context.Add(novoItem);
                    await _context.SaveChangesAsync();

                    // Adicionar impostos se fornecidos
                    if (itemViewModel.Imposto != null)
                    {
                        // Garantir que CstCsosn tenha 3 caracteres (padrão "000" se vazio)
                        var cstCsosn = itemViewModel.Imposto.CstCsosn?.Trim() ?? string.Empty;
                        if (string.IsNullOrWhiteSpace(cstCsosn) || cstCsosn.Length != 3)
                        {
                            cstCsosn = "000"; // Valor padrão
                        }
                        
                        var novoImposto = new NFeItemImposto
                        {
                            IdItem = novoItem.IdItem,
                            Origem = itemViewModel.Imposto.Origem,
                            CstCsosn = cstCsosn,
                            BaseIcms = itemViewModel.Imposto.BaseIcms,
                            AliquotaIcms = itemViewModel.Imposto.AliquotaIcms,
                            ValorIcms = itemViewModel.Imposto.ValorIcms,
                            BasePis = itemViewModel.Imposto.BasePis,
                            ValorPis = itemViewModel.Imposto.ValorPis,
                            BaseCofins = itemViewModel.Imposto.BaseCofins,
                            ValorCofins = itemViewModel.Imposto.ValorCofins
                        };
                        _context.Add(novoImposto);
                    }
                }
            }

            // Processar pagamentos removidos
            if (viewModel.PagamentosRemovidos != null && viewModel.PagamentosRemovidos.Any())
            {
                foreach (var pagamentoId in viewModel.PagamentosRemovidos)
                {
                    var pagamentoRemover = await _context.NFePagamentos
                        .FirstOrDefaultAsync(p => p.IdPagamento == pagamentoId && p.IdNfe == nfe.IdNfe);

                    if (pagamentoRemover != null)
                    {
                        _context.NFePagamentos.Remove(pagamentoRemover);
                    }
                }
            }

            // Processar pagamentos existentes atualizados
            if (viewModel.Pagamentos != null && viewModel.Pagamentos.Any())
            {
                foreach (var pagamentoViewModel in viewModel.Pagamentos.Where(p => p.IdPagamento.HasValue))
                {
                    var pagamento = await _context.NFePagamentos
                        .FirstOrDefaultAsync(p => p.IdPagamento == pagamentoViewModel.IdPagamento!.Value && p.IdNfe == nfe.IdNfe);

                    if (pagamento != null)
                    {
                        pagamento.TipoPagamento = pagamentoViewModel.TipoPagamento?.Trim() ?? string.Empty;
                        pagamento.ValorPago = pagamentoViewModel.ValorPago;
                        _context.Update(pagamento);
                    }
                }
            }

            // Processar novos pagamentos
            if (viewModel.NovosPagamentos != null && viewModel.NovosPagamentos.Any())
            {
                _logger.LogInformation($"Processando {viewModel.NovosPagamentos.Count()} novos pagamentos para NF-e {nfe.IdNfe}");
                
                int pagamentosSalvos = 0;
                foreach (var pagamentoViewModel in viewModel.NovosPagamentos)
                {
                    try
                    {
                        _logger.LogInformation($"Processando novo pagamento - TipoPagamento: '{pagamentoViewModel.TipoPagamento}', ValorPago: {pagamentoViewModel.ValorPago}");
                        
                        // Validar e normalizar TipoPagamento
                        var tipoPagamento = pagamentoViewModel.TipoPagamento?.Trim() ?? string.Empty;
                        if (string.IsNullOrWhiteSpace(tipoPagamento))
                        {
                            _logger.LogWarning($"Novo pagamento pulado - TipoPagamento vazio ou null");
                            continue;
                        }
                        
                        // Garantir que TipoPagamento tenha 2 caracteres
                        if (tipoPagamento.Length == 1)
                        {
                            tipoPagamento = "0" + tipoPagamento;
                        }
                        else if (tipoPagamento.Length > 2)
                        {
                            tipoPagamento = tipoPagamento.Substring(0, 2);
                        }
                        
                        // Validar ValorPago
                        if (pagamentoViewModel.ValorPago <= 0)
                        {
                            _logger.LogWarning($"Novo pagamento pulado - ValorPago inválido: {pagamentoViewModel.ValorPago}");
                            continue;
                        }

                        var novoPagamento = new NFePagamento
                        {
                            IdNfe = nfe.IdNfe,
                            TipoPagamento = tipoPagamento,
                            ValorPago = pagamentoViewModel.ValorPago
                        };
                        
                        _logger.LogInformation($"Criando novo pagamento - IdNfe: {novoPagamento.IdNfe}, TipoPagamento: '{novoPagamento.TipoPagamento}', ValorPago: {novoPagamento.ValorPago}");

                        _context.NFePagamentos.Add(novoPagamento);
                        await _context.SaveChangesAsync();
                        pagamentosSalvos++;
                        _logger.LogInformation($"Novo pagamento {novoPagamento.IdPagamento} salvo - Tipo: '{novoPagamento.TipoPagamento}', Valor: {novoPagamento.ValorPago}");
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, $"Erro ao salvar novo pagamento - TipoPagamento: {pagamentoViewModel.TipoPagamento}, ValorPago: {pagamentoViewModel.ValorPago}");
                        throw;
                    }
                }
                
                _logger.LogInformation($"Total de novos pagamentos salvos: {pagamentosSalvos} de {viewModel.NovosPagamentos.Count()}");
            }
            else
            {
                _logger.LogWarning($"Nenhum novo pagamento encontrado no viewModel para NF-e {nfe.IdNfe}");
            }

            // Recalcular totais automaticamente
            await RecalcularTotaisAsync(nfe);

            _context.Update(nfe);
            await _context.SaveChangesAsync();

            return nfe;
        }

        private async Task RecalcularTotaisAsync(NFe nfe)
        {
            // Recarregar itens
            var itens = await _context.NFeItens
                .Where(i => i.IdNfe == nfe.IdNfe)
                .ToListAsync();

            // Calcular valor dos produtos
            nfe.ValorProdutos = CalculateValorProdutos(itens);

            // Calcular total de impostos
            var impostos = await _context.NFeItemImpostos
                .Where(imp => itens.Select(i => i.IdItem).Contains(imp.IdItem))
                .ToListAsync();

            var totalImpostos = impostos.Sum(i => i.ValorIcms + (i.ValorPis ?? 0) + (i.ValorCofins ?? 0));

            // Calcular valor total da NF-e
            nfe.ValorTotalNfe = CalculateValorTotalNfe(nfe.ValorProdutos, totalImpostos);
        }

        public decimal CalculateValorTotalNfe(decimal valorProdutos, decimal totalImpostos)
        {
            return valorProdutos + totalImpostos;
        }

        public decimal CalculateValorProdutos(ICollection<NFeItem> itens)
        {
            if (itens == null || !itens.Any())
                return 0;

            return itens.Sum(i => i.ValorTotal);
        }

        public async Task<bool> ValidateNcmAsync(string ncm)
        {
            if (string.IsNullOrWhiteSpace(ncm))
                return false;

            // NCM deve ter 8 dígitos numéricos
            if (!Regex.IsMatch(ncm, @"^\d{8}$"))
                return false;

            // Validações adicionais podem ser implementadas aqui
            // Por exemplo, verificar se o NCM existe em uma tabela de referência
            return await Task.FromResult(true);
        }

        public async Task<bool> ValidateCfopAsync(string cfop)
        {
            if (string.IsNullOrWhiteSpace(cfop))
                return false;

            // CFOP deve ter 4 dígitos numéricos
            if (!Regex.IsMatch(cfop, @"^\d{4}$"))
                return false;

            // Validações adicionais podem ser implementadas aqui
            // Por exemplo, verificar se o CFOP é válido para a operação
            return await Task.FromResult(true);
        }

        public async Task LogAuditoriaAsync(string acao, long? idNfe, string usuario, string detalhes)
        {
            try
            {
                var logMessage = $"NF-e Auditoria - Ação: {acao}, ID NF-e: {idNfe}, Usuário: {usuario}, Detalhes: {detalhes}, Data: {DateTime.Now:yyyy-MM-dd HH:mm:ss}";
                
                _logger.LogInformation(logMessage);

                // Aqui você pode adicionar persistência em banco de dados se necessário
                // Por exemplo, criar uma tabela NFE_AUDITORIA
                await Task.CompletedTask;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao registrar auditoria de NF-e");
            }
        }
    }
}


using Microsoft.AspNetCore.Mvc;
using TerminalWeb.Services;

namespace TerminalWeb.Controllers;

public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;
    private readonly PedidoService _pedidoService;

    public HomeController(ILogger<HomeController> logger, PedidoService pedidoService)
    {
        _logger = logger;
        _pedidoService = pedidoService;
    }

    public IActionResult Index(int? idEtapaProducao)
    {
        try
        {
            var idEtapa = idEtapaProducao ?? 4;
            var pedidos = _pedidoService.BuscarPedidosEmProducao(idEtapa);
            
            ViewBag.Pedidos = pedidos;
            ViewBag.PedidoService = _pedidoService;
            ViewBag.IdEtapaProducao = idEtapa;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao buscar pedidos em produção");
            ViewBag.Error = $"Erro ao carregar dados: {ex.Message}";
            ViewBag.Pedidos = null;
            ViewBag.PedidoService = _pedidoService;
            ViewBag.IdEtapaProducao = idEtapaProducao ?? 4;
        }

        return View();
    }

    public IActionResult SelecionarSetor()
    {
        return View();
    }

    public IActionResult SelecionarSetorPartial()
    {
        return PartialView("_SelecionarSetorPartial");
    }

    public IActionResult DigitarSenha()
    {
        return PartialView("_DigitarSenha");
    }

    public IActionResult EstoqueSeparacao()
    {
        return PartialView("_EstoqueSeparacao");
    }

    public IActionResult ConcluirEtapa()
    {
        return PartialView("_ConcluirEtapa");
    }

    public IActionResult Expedicao(int idPedido)
    {
        try
        {
            var dados = _pedidoService.BuscarDadosExpedicao(idPedido);
            var produtos = _pedidoService.BuscarProdutosExpedicao(idPedido);
            ViewBag.DadosExpedicao = dados;
            ViewBag.ProdutosExpedicao = produtos;
            return PartialView("_Expedicao");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao buscar dados de expedição");
            ViewBag.Error = $"Erro ao carregar dados: {ex.Message}";
            ViewBag.DadosExpedicao = null;
            ViewBag.ProdutosExpedicao = null;
            return PartialView("_Expedicao");
        }
    }

    [HttpPost]
    public IActionResult ValidarConclusaoPedido([FromBody] ValidarConclusaoPedidoRequest request)
    {
        try
        {
            if (request.IdPedido <= 0)
            {
                return Json(new { sucesso = false, mensagem = "ID do pedido inválido" });
            }

            var todosNaEtapa8 = _pedidoService.ValidarTodosProdutosNaEtapa8(request.IdPedido);

            if (!todosNaEtapa8)
            {
                return Json(new { 
                    sucesso = false, 
                    podeConcluir = false,
                    mensagem = "Existem produtos do pedido ainda em produção." 
                });
            }

            return Json(new { 
                sucesso = true, 
                podeConcluir = true
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao validar conclusão do pedido");
            return Json(new { sucesso = false, mensagem = $"Erro: {ex.Message}" });
        }
    }

    [HttpPost]
    public IActionResult ConcluirPedido([FromBody] ConcluirPedidoRequest request)
    {
        try
        {
            if (request.IdPedido <= 0 || request.IdFuncionario <= 0)
            {
                return Json(new { sucesso = false, mensagem = "Dados inválidos" });
            }

            var concluido = _pedidoService.ConcluirPedido(request.IdPedido, request.IdFuncionario);

            if (concluido)
            {
                return Json(new { 
                    sucesso = true, 
                    mensagem = "Pedido concluído com sucesso" 
                });
            }
            else
            {
                return Json(new { 
                    sucesso = false, 
                    mensagem = "Não foi possível concluir o pedido" 
                });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao concluir pedido");
            return Json(new { sucesso = false, mensagem = $"Erro: {ex.Message}" });
        }
    }

    [HttpGet]
    public IActionResult ImprimirEtiqueta(int idPedido)
    {
        try
        {
            var dados = _pedidoService.BuscarDadosExpedicao(idPedido);
            ViewBag.DadosExpedicao = dados;
            return View("_EtiquetaExpedicao");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao buscar dados para impressão de etiqueta");
            ViewBag.Error = $"Erro ao carregar dados: {ex.Message}";
            ViewBag.DadosExpedicao = null;
            return View("_EtiquetaExpedicao");
        }
    }

    [HttpGet]
    public IActionResult ImprimirPedido(int idPedido)
    {
        try
        {
            var dados = _pedidoService.BuscarDadosExpedicao(idPedido);
            var produtos = _pedidoService.BuscarProdutosExpedicao(idPedido);
            ViewBag.DadosExpedicao = dados;
            ViewBag.ProdutosExpedicao = produtos;
            return View("_ImprimirPedido");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao buscar dados para impressão do pedido");
            ViewBag.Error = $"Erro ao carregar dados: {ex.Message}";
            ViewBag.DadosExpedicao = null;
            ViewBag.ProdutosExpedicao = null;
            return View("_ImprimirPedido");
        }
    }

    [HttpGet]
    public IActionResult BuscarIdPedido(int idPedidoProduto)
    {
        try
        {
            if (idPedidoProduto <= 0)
            {
                return Json(new { sucesso = false, mensagem = "ID do pedido produto inválido" });
            }

            var idPedido = _pedidoService.BuscarIdPedido(idPedidoProduto);
            
            if (idPedido > 0)
            {
                return Json(new { sucesso = true, idPedido = idPedido });
            }
            else
            {
                return Json(new { sucesso = false, mensagem = "Pedido não encontrado" });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao buscar ID do pedido");
            return Json(new { sucesso = false, mensagem = $"Erro: {ex.Message}" });
        }
    }

    [HttpGet]
    public IActionResult BuscarPerdaProducao()
    {
        try
        {
            var perdas = _pedidoService.BuscarPerdaProducao();
            var lista = new List<object>();

            foreach (System.Data.DataRow row in perdas.Rows)
            {
                var item = new Dictionary<string, object>();
                foreach (System.Data.DataColumn column in perdas.Columns)
                {
                    item[column.ColumnName] = row[column] ?? "";
                }
                lista.Add(item);
            }

            return Json(new { sucesso = true, dados = lista });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao buscar perda produção");
            return Json(new { sucesso = false, mensagem = $"Erro: {ex.Message}" });
        }
    }

    [HttpPost]
    public IActionResult ValidarPendenciaCorte([FromBody] ValidarPendenciaCorteRequest request)
    {
        try
        {
            if (request.IdPedidoProduto <= 0 || request.IdTamanho <= 0 || request.IdGradePedProd <= 0)
            {
                return Json(new { sucesso = false, mensagem = "Dados inválidos" });
            }

            // Validar pendência de etapa de corte (etapa 2)
            // O método ValidaPendenciaEtapa retorna o ID_ETAPA_PRODUCAO se encontrar registro, ou -1 se não encontrar
            // Se retornar diferente de -1, significa que há pendência (não pode processar)
            var validacaoPendencia = _pedidoService.ValidaPendenciaEtapa(
                request.IdPedidoProduto,
                request.IdTamanho,
                request.IdGradePedProd,
                0, // CONCLUIDO = 0 (pendente)
                2  // ID_ETAPA_PRODUCAO = 2 (Corte)
            );

            // Se retornar diferente de -1, há pendência de corte
            if (validacaoPendencia != -1)
            {
                return Json(new { 
                    sucesso = false, 
                    temPendencia = true,
                    mensagem = "Não é possível enviar pois existe solicitação de corte pendente."
                });
            }

            return Json(new { 
                sucesso = true, 
                temPendencia = false
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao validar pendência de corte");
            return Json(new { sucesso = false, mensagem = $"Erro: {ex.Message}" });
        }
    }

    [HttpPost]
    public IActionResult ValidarSenha([FromBody] ValidarSenhaRequest request)
    {
        try
        {
            // Log para debug
            _logger.LogInformation("ValidarSenha recebido: Pin={Pin}, IdFuncionario={IdFuncionario}, IdPedidoProduto={IdPedidoProduto}, IdTamanho={IdTamanho}, IdGradePedProd={IdGradePedProd}, IdEtapaProducao={IdEtapaProducao}",
                request.Pin, request.IdFuncionario, request.IdPedidoProduto, request.IdTamanho, request.IdGradePedProd, request.IdEtapaProducao);

            if (string.IsNullOrEmpty(request.Pin) || request.IdFuncionario <= 0)
            {
                return Json(new { sucesso = false, mensagem = "Dados inválidos" });
            }

            var senhaValida = _pedidoService.ValidarSenhaFuncionario(request.Pin, request.IdFuncionario);

            if (!senhaValida)
            {
                return Json(new { sucesso = false, mensagem = "Senha inválida" });
            }

            // Verificar se o funcionário já está associado a um pedido
            var funcionarioAssociado = _pedidoService.VerificarFuncionarioAssociado(
                request.IdFuncionario,
                request.IdPedidoProduto,
                request.IdTamanho,
                request.IdGradePedProd,
                request.IdEtapaProducao
            );

            if (funcionarioAssociado)
            {
                // Buscar o ID da etapa quando o funcionário está associado
                var idChave = _pedidoService.BuscarIdEtapaFuncionarioAssociado(
                    request.IdFuncionario,
                    request.IdPedidoProduto,
                    request.IdTamanho,
                    request.IdGradePedProd,
                    request.IdEtapaProducao
                );

                // Se a etapa for 8 (Expedição), buscar o ID_PEDIDO
                int? idPedido = null;
                if (request.IdEtapaProducao == 8 && request.IdPedidoProduto > 0)
                {
                    idPedido = _pedidoService.BuscarIdPedido(request.IdPedidoProduto);
                }

                // Se o funcionário já está associado, retornar sucesso indicando que deve abrir a tela apropriada
                return Json(new { 
                    sucesso = true, 
                    mensagem = "Senha validada com sucesso",
                    funcionarioAssociado = true,
                    idChave = idChave,
                    idEtapaProducao = request.IdEtapaProducao,
                    idPedido = idPedido
                });
            }

            // Se a etapa for 8 (Expedição), não validar etapa em aberto e permitir abrir a tela diretamente
            if (request.IdEtapaProducao == 8)
            {
                // Buscar o ID_PEDIDO para a tela de Expedição
                int? idPedido = null;
                if (request.IdPedidoProduto > 0)
                {
                    idPedido = _pedidoService.BuscarIdPedido(request.IdPedidoProduto);
                }

                return Json(new { 
                    sucesso = true, 
                    mensagem = "Senha validada com sucesso",
                    funcionarioAssociado = false,
                    idEtapaProducao = request.IdEtapaProducao,
                    idPedido = idPedido
                });
            }

            // Se o funcionário não está associado, buscar etapa em aberto e associar
            _logger.LogInformation("Buscando etapa em aberto: IdPedidoProduto={IdPedidoProduto}, IdTamanho={IdTamanho}, IdGradePedProd={IdGradePedProd}, IdEtapaProducao={IdEtapaProducao}",
                request.IdPedidoProduto, request.IdTamanho, request.IdGradePedProd, request.IdEtapaProducao);

            var idEtapa = _pedidoService.BuscaEtapaEmAberto(
                request.IdPedidoProduto,
                request.IdTamanho,
                request.IdGradePedProd,
                request.IdEtapaProducao
            );

            if (idEtapa > 0)
            {
                // Atualizar funcionário na etapa
                var atualizado = _pedidoService.AtualizaFuncionarioEtapa(idEtapa, request.IdFuncionario);
                
                if (atualizado)
                {
                    return Json(new { 
                        sucesso = true, 
                        mensagem = "Funcionário associado com sucesso",
                        idEtapa = idEtapa,
                        funcionarioAssociado = false
                    });
                }
                else
                {
                    return Json(new { 
                        sucesso = false, 
                        mensagem = "Não foi possível associar o funcionário à etapa" 
                    });
                }
            }
            else
            {
                return Json(new { 
                    sucesso = false, 
                    mensagem = "Não há etapa em aberto para este pedido" 
                });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao validar senha e associar funcionário");
            return Json(new { sucesso = false, mensagem = $"Erro: {ex.Message}" });
        }
    }

    [HttpPost]
    public IActionResult ProcessarEtapaEstoque([FromBody] ProcessarEtapaEstoqueRequest request)
    {
        try
        {
            _logger.LogInformation("ProcessarEtapaEstoque recebido: IdChave={IdChave}, IdFuncionario={IdFuncionario}, IdEtapaProducao={IdEtapaProducao}, QuantidadeAtual={QuantidadeAtual}, QuantidadeCortar={QuantidadeCortar}, IdPedidoProduto={IdPedidoProduto}, IdTamanho={IdTamanho}, IdGradePedProd={IdGradePedProd}, QuantidadePedido={QuantidadePedido}, IdEtapaProducaoAtual={IdEtapaProducaoAtual}",
                request.IdChave, request.IdFuncionario, request.IdEtapaProducao, request.QuantidadeAtual, request.QuantidadeCortar, request.IdPedidoProduto, request.IdTamanho, request.IdGradePedProd, request.QuantidadePedido, request.IdEtapaProducaoAtual);

            if (request.IdChave <= 0 || request.IdFuncionario <= 0 || request.IdEtapaProducao <= 0)
            {
                return Json(new { sucesso = false, mensagem = "Dados inválidos" });
            }

            // Validar: Se a etapa atual é 4 (Estoque) e estoqueEmEstoque < estoqueQuantidade, não processar
            // Mas apenas para botões que NÃO são Cortar (IdEtapaProducao != 2)
            if (request.IdEtapaProducao != 2 && request.IdEtapaProducaoAtual == 4 && request.QuantidadeAtual < request.QuantidadePedido)
            {
                return Json(new { 
                    sucesso = false, 
                    mensagem = "Não é possível processar: a quantidade em estoque é menor que a quantidade do pedido. A etapa não será atualizada nem marcada como concluída." 
                });
            }

            if (request.IdEtapaProducao != 2 )
            { 
                // Fluxo 1: Atualizar o registro existente
                var atualizado = _pedidoService.AtualizarProdutoPedidoEtapaProd(
                idChave: request.IdChave,
                idEtapaProducao: 0,
                idPedidoProduto: 0,
                idFuncionario: request.IdFuncionario,
                concluido: 1,
                perda: 0,
                qtdeProduzida: request.QuantidadeAtual,
                qtdePerda: 0,
                idPerda: 0,
                idTamanho: 0,
                qtde: 0,
                idGradePedProd: 0,
                reposicao: 0,
                dataInicio: null,
                dataFim: DateTime.Now
            );

                if (!atualizado)
                {
                    return Json(new { sucesso = false, mensagem = "Não foi possível atualizar o registro" });
                }

            }


            // Fluxo 2: Inserir um novo registro na tabela PRODUTO_PEDIDO_ETAPA_PROD
            var inserido = _pedidoService.InserirProdutoPedidoEtapaProd(
                idEtapaProducao: request.IdEtapaProducao,
                idPedidoProduto: request.IdPedidoProduto,
                idGradePedProd: request.IdGradePedProd,
                quantidade: request.QuantidadeCortar,
                idTamanho: request.IdTamanho
            );

            if (!inserido)
            {
                return Json(new { sucesso = false, mensagem = "Não foi possível inserir o novo registro" });
            }

            return Json(new { 
                sucesso = true, 
                mensagem = "Etapa processada com sucesso" 
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao processar etapa de estoque");
            return Json(new { sucesso = false, mensagem = $"Erro: {ex.Message}" });
        }
    }

    [HttpPost]
    public IActionResult AtualizarEstoque([FromBody] AtualizarEstoqueRequest request)
    {
        try
        {
            _logger.LogInformation("AtualizarEstoque recebido: IdChave={IdChave}, ValorAtual={ValorAtual}, Operacao={Operacao}",
                request.IdChave, request.ValorAtual, request.Operacao);

            if (request.IdChave <= 0 || request.ValorAtual < 0 || string.IsNullOrEmpty(request.Operacao))
            {
                return Json(new { sucesso = false, mensagem = "Dados inválidos" });
            }

            var novoValor = _pedidoService.ProcessarEventoEstoque(request.IdChave, request.ValorAtual, request.Operacao);

            if (novoValor < 0)
            {
                return Json(new { sucesso = false, mensagem = "Não foi possível processar o evento" });
            }

            return Json(new { 
                sucesso = true, 
                mensagem = "Estoque atualizado com sucesso",
                novoValor = novoValor
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao atualizar estoque");
            return Json(new { sucesso = false, mensagem = $"Erro: {ex.Message}" });
        }
    }

    [HttpPost]
    public IActionResult AtualizarCortar([FromBody] AtualizarCortarRequest request)
    {
        try
        {
            _logger.LogInformation("AtualizarCortar recebido: IdChave={IdChave}, ValorAtual={ValorAtual}, Operacao={Operacao}",
                request.IdChave, request.ValorAtual, request.Operacao);

            if (request.IdChave <= 0 || request.ValorAtual < 0 || string.IsNullOrEmpty(request.Operacao))
            {
                return Json(new { sucesso = false, mensagem = "Dados inválidos" });
            }

            var novoValor = _pedidoService.ProcessarEventoCortar(request.IdChave, request.ValorAtual, request.Operacao);

            if (novoValor < 0)
            {
                return Json(new { sucesso = false, mensagem = "Não foi possível processar o evento" });
            }

            return Json(new { 
                sucesso = true, 
                mensagem = "Cortar atualizado com sucesso",
                novoValor = novoValor
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao atualizar cortar");
            return Json(new { sucesso = false, mensagem = $"Erro: {ex.Message}" });
        }
    }

    [HttpPost]
    public IActionResult ProcessarCostura([FromBody] ProcessarCosturaRequest request)
    {
        try
        {
            _logger.LogInformation("ProcessarCostura recebido: IdChave={IdChave}, IdFuncionario={IdFuncionario}, IdPedidoProduto={IdPedidoProduto}, IdTamanho={IdTamanho}, IdGradePedProd={IdGradePedProd}, QuantidadeAtual={QuantidadeAtual}, QuantidadePedido={QuantidadePedido}",
                request.IdChave, request.IdFuncionario, request.IdPedidoProduto, request.IdTamanho, request.IdGradePedProd, request.QuantidadeAtual, request.QuantidadePedido);

            if (request.IdPedidoProduto <= 0 || request.IdTamanho <= 0 || request.IdGradePedProd <= 0)
            {
                return Json(new { sucesso = false, mensagem = "Dados inválidos" });
            }

            // Validar pendência de etapa (etapa 2 = Corte)
            // O método ValidaPendenciaEtapa retorna o ID_ETAPA_PRODUCAO se encontrar registro, ou -1 se não encontrar
            // Se retornar diferente de -1, significa que há pendência (não pode processar)
            var validacaoPendencia = _pedidoService.ValidaPendenciaEtapa(
                request.IdPedidoProduto,
                request.IdTamanho,
                request.IdGradePedProd,
                0, // CONCLUIDO = 0 (pendente)
                2  // ID_ETAPA_PRODUCAO = 2 (Corte)
            );

            if (validacaoPendencia != -1)
            {
                return Json(new { 
                    sucesso = false, 
                    mensagem = "Não é possível Enviar para costura pois existe solicitação de corte Pendente",
                    temPendencia = true
                });
            }

            // Verificar se quantidade atual é menor que quantidade do pedido
            if (request.QuantidadeAtual < request.QuantidadePedido)
            {
                // Buscar configuração
                var enviaCosturaSemEstoque = _pedidoService.BuscaConfiguracao("ENVIA_COSTURA_SEM_ESTOQUE");
                
                if (enviaCosturaSemEstoque == "SIM")
                {
                    // Retornar que precisa de confirmação
                    return Json(new { 
                        sucesso = false, 
                        precisaConfirmacao = true,
                        mensagem = "A quantidade não é suficiente para produção. Deseja continuar?"
                    });
                }
                else
                {
                    return Json(new { 
                        sucesso = false, 
                        mensagem = "A quantidade não é suficiente para produção."
                    });
                }
            }

            // Se chegou aqui, pode processar normalmente
            // Atualizar o registro atual
            var atualizado = _pedidoService.AtualizarProdutoPedidoEtapaProd(
                idChave: request.IdChave,
                idEtapaProducao: 0,
                idPedidoProduto: 0,
                idFuncionario: request.IdFuncionario,
                concluido: 1,
                perda: 0,
                qtdeProduzida: request.QuantidadeAtual,
                qtdePerda: 0,
                idPerda: 0,
                idTamanho: 0,
                qtde: 0,
                idGradePedProd: 0,
                reposicao: 0,
                dataInicio: null,
                dataFim: DateTime.Now
            );

            if (!atualizado)
            {
                return Json(new { sucesso = false, mensagem = "Não foi possível atualizar o registro" });
            }

            // Inserir nova etapa (3 = Costura)
            var inserido = _pedidoService.InserirProdutoPedidoEtapaProd(
                idEtapaProducao: 3,
                idPedidoProduto: request.IdPedidoProduto,
                idGradePedProd: request.IdGradePedProd,
                quantidade: request.QuantidadeAtual,
                idTamanho: request.IdTamanho
            );

            if (!inserido)
            {
                return Json(new { sucesso = false, mensagem = "Não foi possível inserir a etapa de costura" });
            }

            return Json(new { 
                sucesso = true, 
                mensagem = "Enviado Pedido de Costura" 
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao processar costura");
            return Json(new { sucesso = false, mensagem = $"Erro: {ex.Message}" });
        }
    }

    [HttpPost]
    public IActionResult ProcessarCosturaComConfirmacao([FromBody] ProcessarCosturaRequest request)
    {
        try
        {
            // Processar com quantidade atual (menor que a do pedido)
            var atualizado = _pedidoService.AtualizarProdutoPedidoEtapaProd(
                idChave: request.IdChave,
                idEtapaProducao: 0,
                idPedidoProduto: 0,
                idFuncionario: request.IdFuncionario,
                concluido: 1,
                perda: 0,
                qtdeProduzida: request.QuantidadeAtual,
                qtdePerda: 0,
                idPerda: 0,
                idTamanho: 0,
                qtde: 0,
                idGradePedProd: 0,
                reposicao: 0,
                dataInicio: null,
                dataFim: DateTime.Now
            );

            if (!atualizado)
            {
                return Json(new { sucesso = false, mensagem = "Não foi possível atualizar o registro" });
            }

            // Inserir nova etapa (3 = Costura) com quantidade atual
            var inserido = _pedidoService.InserirProdutoPedidoEtapaProd(
                idEtapaProducao: 3,
                idPedidoProduto: request.IdPedidoProduto,
                idGradePedProd: request.IdGradePedProd,
                quantidade: request.QuantidadeAtual,
                idTamanho: request.IdTamanho
            );

            if (!inserido)
            {
                return Json(new { sucesso = false, mensagem = "Não foi possível inserir a etapa de costura" });
            }

            return Json(new { 
                sucesso = true, 
                mensagem = "Enviado Pedido de Costura" 
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao processar costura com confirmação");
            return Json(new { sucesso = false, mensagem = $"Erro: {ex.Message}" });
        }
    }

    [HttpPost]
    public IActionResult ProcessarEtapa([FromBody] ProcessarEtapaRequest request)
    {
        try
        {
            _logger.LogInformation("ProcessarEtapa recebido: IdChave={IdChave}, IdFuncionario={IdFuncionario}, IdPedidoProduto={IdPedidoProduto}, IdTamanho={IdTamanho}, IdGradePedProd={IdGradePedProd}, QuantidadeAtual={QuantidadeAtual}, QuantidadePedido={QuantidadePedido}, IdEtapaProducao={IdEtapaProducao}",
                request.IdChave, request.IdFuncionario, request.IdPedidoProduto, request.IdTamanho, request.IdGradePedProd, request.QuantidadeAtual, request.QuantidadePedido, request.IdEtapaProducao);

            if (request.IdPedidoProduto <= 0 || request.IdTamanho <= 0 || request.IdGradePedProd <= 0 || request.IdEtapaProducao <= 0)
            {
                return Json(new { sucesso = false, mensagem = "Dados inválidos" });
            }

            // Validar pendência de etapa (etapa 2 = Corte)
            // O método ValidaPendenciaEtapa retorna o ID_ETAPA_PRODUCAO se encontrar registro (há pendência), ou -1 se não encontrar (não há pendência)
            var validacaoPendencia = _pedidoService.ValidaPendenciaEtapa(
                request.IdPedidoProduto,
                request.IdTamanho,
                request.IdGradePedProd,
                0, // CONCLUIDO = 0 (pendente)
                2  // ID_ETAPA_PRODUCAO = 2 (Corte)
            );

            // Se retornar diferente de -1, há pendência de corte (não pode processar)
            if (validacaoPendencia != -1)
            {
                var mensagemErro = $"Não é possível Enviar para {request.NomeEtapa} pois existe solicitação de corte Pendente";
                return Json(new { 
                    sucesso = false, 
                    mensagem = mensagemErro,
                    temPendencia = true
                });
            }

            // Verificar se quantidade atual é menor que quantidade do pedido
            if (request.QuantidadeAtual < request.QuantidadePedido)
            {
                // Buscar configuração
                var enviaCosturaSemEstoque = _pedidoService.BuscaConfiguracao("ENVIA_COSTURA_SEM_ESTOQUE");
                
                if (enviaCosturaSemEstoque == "SIM")
                {
                    // Retornar que precisa de confirmação
                    return Json(new { 
                        sucesso = false, 
                        precisaConfirmacao = true,
                        mensagem = "A quantidade não é suficiente para produção. Deseja continuar?"
                    });
                }
                else
                {
                    return Json(new { 
                        sucesso = false, 
                        mensagem = "A quantidade não é suficiente para produção."
                    });
                }
            }

            // Se chegou aqui, pode processar normalmente
            // Atualizar o registro atual
            var atualizado = _pedidoService.AtualizarProdutoPedidoEtapaProd(
                idChave: request.IdChave,
                idEtapaProducao: 0,
                idPedidoProduto: 0,
                idFuncionario: request.IdFuncionario,
                concluido: 1,
                perda: 0,
                qtdeProduzida: request.QuantidadeAtual,
                qtdePerda: 0,
                idPerda: 0,
                idTamanho: 0,
                qtde: 0,
                idGradePedProd: 0,
                reposicao: 0,
                dataInicio: null,
                dataFim: DateTime.Now
            );

            if (!atualizado)
            {
                return Json(new { sucesso = false, mensagem = "Não foi possível atualizar o registro" });
            }

            // Inserir nova etapa
            var inserido = _pedidoService.InserirProdutoPedidoEtapaProd(
                idEtapaProducao: request.IdEtapaProducao,
                idPedidoProduto: request.IdPedidoProduto,
                idGradePedProd: request.IdGradePedProd,
                quantidade: request.QuantidadeAtual,
                idTamanho: request.IdTamanho
            );

            if (!inserido)
            {
                return Json(new { sucesso = false, mensagem = $"Não foi possível inserir a etapa de {request.NomeEtapa}" });
            }

            return Json(new { 
                sucesso = true, 
                mensagem = $"Enviado Pedido de {request.NomeEtapa}" 
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao processar etapa");
            return Json(new { sucesso = false, mensagem = $"Erro: {ex.Message}" });
        }
    }

    [HttpPost]
    public IActionResult ProcessarEtapaComConfirmacao([FromBody] ProcessarEtapaRequest request)
    {
        try
        {
            // Processar com quantidade atual (menor que a do pedido)
            var atualizado = _pedidoService.AtualizarProdutoPedidoEtapaProd(
                idChave: request.IdChave,
                idEtapaProducao: 0,
                idPedidoProduto: 0,
                idFuncionario: request.IdFuncionario,
                concluido: 1,
                perda: 0,
                qtdeProduzida: request.QuantidadeAtual,
                qtdePerda: 0,
                idPerda: 0,
                idTamanho: 0,
                qtde: 0,
                idGradePedProd: 0,
                reposicao: 0,
                dataInicio: null,
                dataFim: DateTime.Now
            );

            if (!atualizado)
            {
                return Json(new { sucesso = false, mensagem = "Não foi possível atualizar o registro" });
            }

            // Inserir nova etapa com quantidade atual
            var inserido = _pedidoService.InserirProdutoPedidoEtapaProd(
                idEtapaProducao: request.IdEtapaProducao,
                idPedidoProduto: request.IdPedidoProduto,
                idGradePedProd: request.IdGradePedProd,
                quantidade: request.QuantidadeAtual,
                idTamanho: request.IdTamanho
            );

            if (!inserido)
            {
                return Json(new { sucesso = false, mensagem = $"Não foi possível inserir a etapa de {request.NomeEtapa}" });
            }

            return Json(new { 
                sucesso = true, 
                mensagem = $"Enviado Pedido de {request.NomeEtapa}" 
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao processar etapa com confirmação");
            return Json(new { sucesso = false, mensagem = $"Erro: {ex.Message}" });
        }
    }

    [HttpPost]
    public IActionResult FinalizarEtapa([FromBody] FinalizarEtapaRequest request)
    {
        try
        {
            _logger.LogInformation("FinalizarEtapa recebido: IdChave={IdChave}, IdPedidoProduto={IdPedidoProduto}, IdTamanho={IdTamanho}, IdGradePedProd={IdGradePedProd}, QuantidadePerda={QuantidadePerda}, QuantidadeProduzida={QuantidadeProduzida}, IdPerda={IdPerda}",
                request.IdChave, request.IdPedidoProduto, request.IdTamanho, request.IdGradePedProd, request.QuantidadePerda, request.QuantidadeProduzida, request.IdPerda);

            if (request.IdChave <= 0 || request.IdPedidoProduto <= 0)
            {
                return Json(new { sucesso = false, mensagem = "Dados inválidos" });
            }

            // Buscar ID_PRODUTO e ID_ETAPA_PRODUCAO atual ANTES do update
            var idProduto = _pedidoService.BuscarIdProduto(request.IdPedidoProduto);
            var idEtapaAtual = _pedidoService.BuscarIdEtapaProducaoAtual(request.IdChave);

            // Calcular PERDA (1 se quantidadePerda > 0, senão 0)
            int perda = request.QuantidadePerda > 0 ? 1 : 0;
            
            // REPOSICAO será marcado nas etapas inseridas, não na etapa atualizada
            int reposicao = request.QuantidadePerda > 0 ? 1 : 0;

            // 1. Atualizar o registro da etapa atual (reposicao = 0 na etapa atualizada)
            var atualizado = _pedidoService.AtualizarProdutoPedidoEtapaProd(
                idChave: request.IdChave,
                idEtapaProducao: 0,
                idPedidoProduto: 0,
                idFuncionario: 0,
                concluido: 1,
                perda: perda,
                qtdeProduzida: request.QuantidadeProduzida,
                qtdePerda: request.QuantidadePerda,
                idPerda: request.IdPerda,
                idTamanho: 0,
                qtde: 0,
                idGradePedProd: 0,
                reposicao: 0, // Reposição não é marcada na etapa atualizada
                dataInicio: null,
                dataFim: DateTime.Now
            );

            if (!atualizado)
            {
                return Json(new { sucesso = false, mensagem = "Não foi possível atualizar o registro da etapa atual" });
            }

            // 2. Buscar e inserir a próxima etapa
            if (idProduto > 0 && idEtapaAtual > 0)
            {
                // Buscar próxima etapa
                var proximaEtapa = _pedidoService.BuscaProximaEtapa(idProduto, idEtapaAtual);

                if (proximaEtapa > 0)
                {
                    // Inserir próxima etapa (com reposicao = 1 se houver perda)
                    var inserido = _pedidoService.InserirProdutoPedidoEtapaProd(
                        idEtapaProducao: proximaEtapa,
                        idPedidoProduto: request.IdPedidoProduto,
                        idGradePedProd: request.IdGradePedProd,
                        quantidade: request.QuantidadeProduzida,
                        idTamanho: request.IdTamanho,
                        reposicao: reposicao
                    );

                    if (!inserido)
                    {
                        _logger.LogWarning("Não foi possível inserir a próxima etapa {ProximaEtapaId} para o pedido produto {IdPedidoProduto}", proximaEtapa, request.IdPedidoProduto);
                        return Json(new { sucesso = false, mensagem = "Não foi possível inserir a próxima etapa" });
                    }
                }
                else
                {
                    _logger.LogInformation("Não há próxima etapa definida para o produto {IdProduto} após a etapa {IdEtapaAtual}", idProduto, idEtapaAtual);
                }
            }
            else
            {
                _logger.LogWarning("Não foi possível buscar ID do produto ou ID da etapa atual. IdProduto: {IdProduto}, IdEtapaAtual: {IdEtapaAtual}", idProduto, idEtapaAtual);
            }

            // Se tiver perda, inserir registro com idEtapaProducao = 4 (com reposicao = 1)
            if (request.QuantidadePerda > 0)
            {
                var inseridoPerda = _pedidoService.InserirProdutoPedidoEtapaProd(
                    idEtapaProducao: 4, // Estoque
                    idPedidoProduto: request.IdPedidoProduto,
                    idGradePedProd: request.IdGradePedProd,
                    quantidade: request.QuantidadePerda,
                    idTamanho: request.IdTamanho,
                    reposicao: 1 // Sempre marca reposicao = 1 na etapa de perda
                );

                if (!inseridoPerda)
                {
                    _logger.LogWarning("Não foi possível inserir a etapa de perda");
                }
            }

            return Json(new { 
                sucesso = true, 
                mensagem = "Etapa finalizada com sucesso!" 
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao finalizar etapa");
            return Json(new { sucesso = false, mensagem = $"Erro: {ex.Message}" });
        }
    }
}

public class ValidarSenhaRequest
{
    public string Pin { get; set; } = string.Empty;
    public int IdFuncionario { get; set; }
    public int IdPedidoProduto { get; set; }
    public int IdTamanho { get; set; }
    public int IdGradePedProd { get; set; }
    public int IdEtapaProducao { get; set; }
}

public class ProcessarEtapaEstoqueRequest
{
    public int IdChave { get; set; }
    public int IdFuncionario { get; set; }
    public int IdEtapaProducao { get; set; }
    public int QuantidadeAtual { get; set; }
    public int QuantidadeCortar { get; set; }
    public int IdPedidoProduto { get; set; }
    public int IdTamanho { get; set; }
    public int IdGradePedProd { get; set; }
    public int QuantidadePedido { get; set; }
    public int IdEtapaProducaoAtual { get; set; }
}

public class AtualizarEstoqueRequest
{
    public int IdChave { get; set; }
    public int ValorAtual { get; set; }
    public string Operacao { get; set; } = string.Empty; // "aumentar" ou "diminuir"
}

public class AtualizarCortarRequest
{
    public int IdChave { get; set; }
    public int ValorAtual { get; set; }
    public string Operacao { get; set; } = string.Empty; // "aumentar" ou "diminuir"
}

public class ProcessarCosturaRequest
{
    public int IdChave { get; set; }
    public int IdFuncionario { get; set; }
    public int IdPedidoProduto { get; set; }
    public int IdTamanho { get; set; }
    public int IdGradePedProd { get; set; }
    public int QuantidadeAtual { get; set; }
    public int QuantidadePedido { get; set; }
}

public class ProcessarEtapaRequest
{
    public int IdChave { get; set; }
    public int IdFuncionario { get; set; }
    public int IdPedidoProduto { get; set; }
    public int IdTamanho { get; set; }
    public int IdGradePedProd { get; set; }
    public int QuantidadeAtual { get; set; }
    public int QuantidadePedido { get; set; }
    public int IdEtapaProducao { get; set; }
    public string NomeEtapa { get; set; } = string.Empty;
}

public class FinalizarEtapaRequest
{
    public int IdChave { get; set; }
    public int IdPedidoProduto { get; set; }
    public int IdTamanho { get; set; }
    public int IdGradePedProd { get; set; }
    public int QuantidadePerda { get; set; }
    public int QuantidadeProduzida { get; set; }
    public int IdPerda { get; set; }
}

public class ValidarPendenciaCorteRequest
{
    public int IdPedidoProduto { get; set; }
    public int IdTamanho { get; set; }
    public int IdGradePedProd { get; set; }
}

public class ValidarConclusaoPedidoRequest
{
    public int IdPedido { get; set; }
}

public class ConcluirPedidoRequest
{
    public int IdPedido { get; set; }
    public int IdFuncionario { get; set; }
}

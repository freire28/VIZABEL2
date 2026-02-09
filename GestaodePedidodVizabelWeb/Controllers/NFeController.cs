using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using GestaoPedidosVizabel.Data;
using GestaoPedidosVizabel.Models;
using GestaoPedidosVizabel.Attributes;
using GestaoPedidosVizabel.Models.ViewModels;
using GestaoPedidosVizabel.Services;
using GestaoPedidosVizabel.Helpers;
using GestaoPedidosVizabel.Models.DTOs.NuvemFiscal;
using System.Security.Claims;
using System.Text.RegularExpressions;

namespace GestaoPedidosVizabel.Controllers
{
    [Authorize]
    public class NFeController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly INFeService _nfeService;
        private readonly INuvemFiscalService _nuvemFiscalService;

        public NFeController(ApplicationDbContext context, INFeService nfeService, INuvemFiscalService nuvemFiscalService)
        {
            _context = context;
            _nfeService = nfeService;
            _nuvemFiscalService = nuvemFiscalService;
        }

        private string GetCurrentUser()
        {
            return User?.Identity?.Name ?? "Sistema";
        }

        // GET: NFe
        [AuthorizePermission("visualizar")]
        public async Task<IActionResult> Index(string searchString)
        {
            var nfes = _context.NFes
                .Include(n => n.Itens)
                    .ThenInclude(i => i.Imposto)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(searchString))
            {
                nfes = nfes.Where(n =>
                    n.Numero.ToString().Contains(searchString) ||
                    n.Serie.ToString().Contains(searchString) ||
                    (n.ChaveAcesso != null && n.ChaveAcesso.Contains(searchString)) ||
                    (n.Status != null && n.Status.Contains(searchString)) ||
                    (n.NaturezaOperacao != null && n.NaturezaOperacao.Contains(searchString))
                );
            }

            ViewBag.SearchString = searchString;
            return View(await nfes.OrderByDescending(n => n.DataEmissao).ToListAsync());
        }

        // GET: NFe/Details/5
        [AuthorizePermission("visualizar")]
        public async Task<IActionResult> Details(long? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var nfe = await _context.NFes
                .Include(n => n.Destinatario)
                .Include(n => n.Itens)
                    .ThenInclude(i => i.Imposto)
                .FirstOrDefaultAsync(m => m.IdNfe == id);
            if (nfe == null)
            {
                return NotFound();
            }

            return View(nfe);
        }

        // GET: NFe/Create
        [AuthorizePermission("incluir")]
        public async Task<IActionResult> Create(int? idPedido = null)
        {
            // Se idPedido foi fornecido, verificar se já existe uma NFe "Em edição" para este pedido
            if (idPedido.HasValue)
            {
                var nfeExistente = await _context.NFes
                    .Where(n => n.IdPedido == idPedido.Value && n.Status == "Em edição")
                    .OrderByDescending(n => n.DataEmissao)
                    .FirstOrDefaultAsync();
                
                if (nfeExistente != null)
                {
                    // Redirecionar para edição da NFe existente
                    return RedirectToAction(nameof(Edit), new { id = nfeExistente.IdNfe });
                }
            }
            
            var pedidos = await _context.Pedidos
                .Include(p => p.Cliente)
                .Where(p => p.Ativo == true)
                .OrderByDescending(p => p.DataPedido)
                .ToListAsync();

            ViewBag.Pedidos = pedidos.Select(p => new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem
            {
                Value = p.IdPedido.ToString(),
                Text = $"Pedido #{p.CodPedido} - {(p.Cliente != null ? p.Cliente.Nomerazao : "Sem Cliente")} ({p.DataPedido:dd/MM/yyyy})",
                Selected = idPedido.HasValue && p.IdPedido == idPedido.Value
            }).ToList();

            // Buscar naturezas de operação ativas
            var naturezasOperacao = await _context.NFeNaturezaOperacoes
                .Where(n => n.Ativo == true)
                .OrderBy(n => n.Descricao)
                .ToListAsync();

            ViewBag.NaturezasOperacao = naturezasOperacao.Select(n => new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem
            {
                Value = n.Descricao,
                Text = n.Descricao
            }).ToList();

            // Criar um dicionário com CFOP por natureza de operação para uso no JavaScript
            ViewBag.NaturezasOperacaoComCfop = naturezasOperacao
                .Where(n => !string.IsNullOrWhiteSpace(n.Cfop))
                .ToDictionary(n => n.Descricao, n => n.Cfop!);

            // Buscar configurações
            var serie = await ConfiguracaoHelper.BuscarValorIntConfiguracaoAsync(_context, "SERIE_NFE");
            var modelo = await ConfiguracaoHelper.BuscarValorIntConfiguracaoAsync(_context, "MODELO_NFE");
            var regimeTributario = await ConfiguracaoHelper.BuscarValorIntConfiguracaoAsync(_context, "REGIME_NFE");
            
            // Buscar próximo número da NFe (MAX(NUMERO) + 1)
            int proximoNumero = 1;
            if (await _context.NFes.AnyAsync())
            {
                var maxNumero = await _context.NFes.MaxAsync(n => n.Numero);
                proximoNumero = maxNumero + 1;
            }
            
            var viewModel = new NFeCreateViewModel();
            if (serie > 0)
            {
                viewModel.Serie = serie;
            }
            if (modelo > 0)
            {
                viewModel.Modelo = modelo.ToString().PadLeft(2, '0');
            }
            if (regimeTributario > 0)
            {
                viewModel.RegimeTributario = regimeTributario.ToString();
            }
            viewModel.Numero = proximoNumero;
            
            // Definir status padrão como "Em Edição"
            viewModel.Status = "Em Edição";
            
            // Buscar formas de pagamento ativas
            var formasPagamento = await _context.FormasPagamento
                .Where(f => f.Ativo == true)
                .OrderBy(f => f.Codigo ?? f.Descricao)
                .ToListAsync();

            ViewBag.FormasPagamento = formasPagamento.Select(f => new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem
            {
                Value = f.Codigo ?? f.Descricao ?? "",
                Text = f.Descricao
            }).ToList();
            
            // Inicializar Destinatario se ainda não foi inicializado
            if (viewModel.Destinatario == null)
            {
                viewModel.Destinatario = new NFeDestinatarioViewModel();
            }

            // Se idPedido foi fornecido, definir no ViewModel e copiar dados do cliente
            if (idPedido.HasValue)
            {
                viewModel.IdPedido = idPedido.Value;
                
                // Buscar o pedido com o cliente
                var pedido = await _context.Pedidos
                    .Include(p => p.Cliente)
                    .FirstOrDefaultAsync(p => p.IdPedido == idPedido.Value);
                
                if (pedido?.Cliente != null)
                {
                    var cliente = pedido.Cliente;
                    
                    // Limpar e formatar CNPJ/CPF (remover caracteres especiais, limitar a 14 caracteres)
                    var cnpjCpf = cliente.Cpfcnpj ?? string.Empty;
                    cnpjCpf = Regex.Replace(cnpjCpf, @"[^\d]", "");
                    if (cnpjCpf.Length > 14) cnpjCpf = cnpjCpf.Substring(0, 14);
                    
                    // Limpar e formatar CEP (remover caracteres especiais, limitar a 8 caracteres)
                    var cep = cliente.EnderecoCep ?? string.Empty;
                    cep = Regex.Replace(cep, @"[^\d]", "");
                    if (cep.Length > 8) cep = cep.Substring(0, 8);
                    
                    // Converter código IBGE para int
                    int? codMun = null;
                    if (!string.IsNullOrWhiteSpace(cliente.EnderecoIbge))
                    {
                        var ibgeLimpo = Regex.Replace(cliente.EnderecoIbge, @"[^\d]", "");
                        if (int.TryParse(ibgeLimpo, out var codMunParsed))
                        {
                            codMun = codMunParsed;
                        }
                    }
                    
                    // Garantir que CEP tenha 8 caracteres (preencher com zeros à esquerda se necessário)
                    if (string.IsNullOrWhiteSpace(cep))
                    {
                        cep = "00000000";
                    }
                    else if (cep.Length < 8)
                    {
                        cep = cep.PadLeft(8, '0');
                    }
                    
                    // Garantir que CNPJ/CPF tenha valor padrão se vazio
                    if (string.IsNullOrWhiteSpace(cnpjCpf))
                    {
                        cnpjCpf = "00000000000000";
                    }

                    // Criar e preencher o destinatário com os dados do cliente
                    viewModel.Destinatario = new NFeDestinatarioViewModel
                    {
                        Nome = !string.IsNullOrWhiteSpace(cliente.Nomerazao) ? cliente.Nomerazao : "NÃO INFORMADO",
                        CnpjCpf = cnpjCpf,
                        Ie = cliente.RgIe,
                        Logradouro = !string.IsNullOrWhiteSpace(cliente.EnderecoLogradouro) ? cliente.EnderecoLogradouro : "NÃO INFORMADO",
                        Numero = !string.IsNullOrWhiteSpace(cliente.EnderecoNumero) ? cliente.EnderecoNumero : "S/N",
                        Bairro = !string.IsNullOrWhiteSpace(cliente.EnderecoBairro) ? cliente.EnderecoBairro : "NÃO INFORMADO",
                        Municipio = !string.IsNullOrWhiteSpace(cliente.EnderecoCidade) ? cliente.EnderecoCidade : "NÃO INFORMADO",
                        CodMun = codMun ?? 0,
                        Uf = !string.IsNullOrWhiteSpace(cliente.EnderecoUf) ? cliente.EnderecoUf.Trim().ToUpper() : "XX",
                        Cep = cep,
                        // Definir IndIeDest: se tem IE é contribuinte (1), senão não contribuinte (9)
                        IndIeDest = string.IsNullOrWhiteSpace(cliente.RgIe) ? (byte)1 : (byte)9
                    }; 
                    
                    // Buscar produtos do pedido e adicionar como itens da NFe
                    var pedidoProdutos = await _context.PedidoProdutos
                        .Include(pp => pp.Produto)
                        .Include(pp => pp.PedidoProdTamanhos)
                        .Where(pp => pp.IdPedido == idPedido.Value)
                        .ToListAsync();
                    
                    if (pedidoProdutos.Any())
                    {
                        viewModel.Itens = new List<NFeItemEditViewModel>();
                        
                        foreach (var pedidoProduto in pedidoProdutos)
                        {
                            if (pedidoProduto.Produto != null)
                            {
                                var produto = pedidoProduto.Produto;
                                
                                // Calcular quantidade: se houver tamanhos, somar as quantidades dos tamanhos
                                // Caso contrário, usar a quantidade do pedido produto
                                decimal quantidade = 0;
                                if (pedidoProduto.PedidoProdTamanhos != null && pedidoProduto.PedidoProdTamanhos.Any())
                                {
                                    quantidade = pedidoProduto.PedidoProdTamanhos.Sum(t => t.Quantidade);
                                }
                                else if (pedidoProduto.Quantidade.HasValue)
                                {
                                    quantidade = pedidoProduto.Quantidade.Value;
                                }
                                
                                // Só adicionar se a quantidade for maior que zero
                                if (quantidade > 0)
                                {
                                    // Limpar e formatar NCM (remover caracteres especiais, limitar a 8 caracteres)
                                    var ncm = produto.Ncmsh ?? string.Empty;
                                    ncm = Regex.Replace(ncm, @"[^\d]", "");
                                    if (ncm.Length > 8) ncm = ncm.Substring(0, 8);
                                    if (string.IsNullOrWhiteSpace(ncm)) ncm = "00000000";
                                    
                                    // Limpar e formatar CFOP (remover caracteres especiais, limitar a 4 caracteres)
                                    var cfop = produto.Cfop ?? string.Empty;
                                    cfop = Regex.Replace(cfop, @"[^\d]", "");
                                    if (cfop.Length > 4) cfop = cfop.Substring(0, 4);
                                    if (string.IsNullOrWhiteSpace(cfop)) cfop = "5102"; // CFOP padrão para venda
                                    
                                    // Calcular valor unitário e total
                                    var valorUnitario = pedidoProduto.ValorVenda ?? 0;
                                    var valorTotal = quantidade * valorUnitario;
                                    
                                    // Criar item da NFe
                                    var item = new NFeItemEditViewModel
                                    {
                                        CodProduto = produto.IdProduto.ToString(),
                                        Descricao = produto.Descricao ?? "PRODUTO SEM DESCRIÇÃO",
                                        Ncm = ncm,
                                        Cfop = cfop,
                                        Unidade = "UN", // Unidade padrão
                                        Quantidade = quantidade,
                                        ValorUnitario = valorUnitario,
                                        ValorTotal = valorTotal
                                    };
                                    
                                    // Se o produto tem CSOSN, criar o imposto
                                    if (!string.IsNullOrWhiteSpace(produto.Csosn))
                                    {
                                        item.Imposto = new NFeItemImpostoViewModel
                                        {
                                            Origem = 0, // Origem padrão (pode ser ajustado conforme necessário)
                                            CstCsosn = produto.Csosn
                                        };
                                    }
                                    
                                    viewModel.Itens.Add(item);
                                }
                            }
                        }
                    }
                }
            }

            // Adicionar descrição do regime tributário ao ViewBag para exibição
            ViewBag.RegimeTributarioDescricao = ConfiguracaoHelper.ObterDescricaoRegimeTributario(viewModel.RegimeTributario);

            return View(viewModel);
        }

        // POST: NFe/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        [AuthorizePermission("incluir")]
        public async Task<IActionResult> Create(NFeCreateViewModel viewModel)
        {
            // Buscar configurações e garantir que sejam usadas
            var serie = await ConfiguracaoHelper.BuscarValorIntConfiguracaoAsync(_context, "SERIE_NFE");
            var modelo = await ConfiguracaoHelper.BuscarValorIntConfiguracaoAsync(_context, "MODELO_NFE");
            var regimeTributario = await ConfiguracaoHelper.BuscarValorConfiguracaoAsync(_context, "REGIME_NFE");
            
            if (serie > 0)
            {
                viewModel.Serie = serie;
            }
            if (modelo > 0)
            {
                viewModel.Modelo = modelo.ToString().PadLeft(2, '0');
            }
            if (!string.IsNullOrWhiteSpace(regimeTributario))
            {
                viewModel.RegimeTributario = regimeTributario;
            }

            // Definir status padrão como "Em Edição" se não foi informado
            if (string.IsNullOrWhiteSpace(viewModel.Status))
            {
                viewModel.Status = "Em Edição";
            }

            // Normalizar valores decimais que podem vir com vírgula do formulário
            if (Request.Form.ContainsKey("ValorProdutos"))
            {
                viewModel.ValorProdutos = ParseDecimal(Request.Form["ValorProdutos"].ToString());
            }
            if (Request.Form.ContainsKey("ValorTotalNfe"))
            {
                viewModel.ValorTotalNfe = ParseDecimal(Request.Form["ValorTotalNfe"].ToString());
            }

            // Log para debug
            System.Diagnostics.Debug.WriteLine($"=== CREATE NFE POST CHAMADO ===");
            System.Diagnostics.Debug.WriteLine($"Itens no ViewModel (automático): {viewModel.Itens?.Count ?? 0}");
            System.Diagnostics.Debug.WriteLine($"Pagamentos no ViewModel (automático): {viewModel.Pagamentos?.Count ?? 0}");
            System.Diagnostics.Debug.WriteLine($"ModelState.IsValid inicial: {ModelState.IsValid}");
            
            // Se não houver itens no ViewModel, tentar fazer binding manual
            if (viewModel.Itens == null || !viewModel.Itens.Any())
            {
                // Verificar se foi enviado via Request.Form
                var itensCount = Request.Form.Keys.Count(k => k.StartsWith("Itens[") && k.Contains(".CodProduto"));
                System.Diagnostics.Debug.WriteLine($"Itens encontrados no Request.Form: {itensCount}");
                
                if (itensCount > 0)
                {
                    // Tentar fazer binding manual - buscar todos os índices possíveis
                    var itens = new List<NFeItemEditViewModel>();
                    
                    // Buscar todos os índices únicos dos itens
                    var indices = new HashSet<int>();
                    foreach (var key in Request.Form.Keys)
                    {
                        if (key.StartsWith("Itens[") && key.Contains("].CodProduto"))
                        {
                            var match = System.Text.RegularExpressions.Regex.Match(key, @"Itens\[(\d+)\]");
                            if (match.Success && int.TryParse(match.Groups[1].Value, out var idx))
                            {
                                indices.Add(idx);
                            }
                        }
                    }
                    
                    System.Diagnostics.Debug.WriteLine($"Índices encontrados: {string.Join(", ", indices.OrderBy(i => i))}");
                    
                    foreach (var itemIndex in indices.OrderBy(i => i))
                    {
                        var codProdutoKey = $"Itens[{itemIndex}].CodProduto";
                        if (Request.Form.ContainsKey(codProdutoKey))
                        {
                            var codProduto = Request.Form[codProdutoKey].ToString();
                            if (!string.IsNullOrWhiteSpace(codProduto))
                            {
                                var unidade = Request.Form[$"Itens[{itemIndex}].Unidade"].ToString();
                                if (string.IsNullOrWhiteSpace(unidade))
                                {
                                    unidade = "UN"; // Valor padrão
                                }

                                var item = new NFeItemEditViewModel
                                {
                                    CodProduto = codProduto.Trim(),
                                    Descricao = (Request.Form[$"Itens[{itemIndex}].Descricao"].ToString() ?? string.Empty).Trim(),
                                    Ncm = (Request.Form[$"Itens[{itemIndex}].Ncm"].ToString() ?? string.Empty).Trim(),
                                    Cfop = (Request.Form[$"Itens[{itemIndex}].Cfop"].ToString() ?? string.Empty).Trim(),
                                    Unidade = unidade.Trim(),
                                    Quantidade = ParseDecimal(Request.Form[$"Itens[{itemIndex}].Quantidade"].ToString()),
                                    ValorUnitario = ParseDecimal(Request.Form[$"Itens[{itemIndex}].ValorUnitario"].ToString()),
                                    ValorTotal = ParseDecimal(Request.Form[$"Itens[{itemIndex}].ValorTotal"].ToString())
                                };

                                // Log para debug
                                var qtdRaw = Request.Form[$"Itens[{itemIndex}].Quantidade"].ToString();
                                var vUnitRaw = Request.Form[$"Itens[{itemIndex}].ValorUnitario"].ToString();
                                var vTotalRaw = Request.Form[$"Itens[{itemIndex}].ValorTotal"].ToString();
                                System.Diagnostics.Debug.WriteLine($"Item {itemIndex}: QtdRaw='{qtdRaw}' -> {item.Quantidade}, VUnitRaw='{vUnitRaw}' -> {item.ValorUnitario}, VTotalRaw='{vTotalRaw}' -> {item.ValorTotal}");
                                System.Diagnostics.Debug.WriteLine($"Item {itemIndex}: Calculado esperado = {item.Quantidade * item.ValorUnitario}");

                                System.Diagnostics.Debug.WriteLine($"Item {itemIndex}: CodProduto={item.CodProduto}, Descricao={item.Descricao}, NCM={item.Ncm}, CFOP={item.Cfop}");

                                // Processar impostos se existirem
                                if (Request.Form.ContainsKey($"Itens[{itemIndex}].Imposto.Origem"))
                                {
                                    var origemStr = Request.Form[$"Itens[{itemIndex}].Imposto.Origem"].ToString();
                                    var cstCsosn = Request.Form[$"Itens[{itemIndex}].Imposto.CstCsosn"].ToString() ?? string.Empty;
                                    
                                    // Só criar imposto se houver dados válidos
                                    if (!string.IsNullOrWhiteSpace(origemStr) || !string.IsNullOrWhiteSpace(cstCsosn))
                                    {
                                        item.Imposto = new NFeItemImpostoViewModel
                                        {
                                            Origem = byte.TryParse(origemStr, out var origem) ? origem : (byte)0,
                                            CstCsosn = cstCsosn,
                                            BaseIcms = ParseDecimal(Request.Form[$"Itens[{itemIndex}].Imposto.BaseIcms"].ToString()),
                                            AliquotaIcms = ParseDecimal(Request.Form[$"Itens[{itemIndex}].Imposto.AliquotaIcms"].ToString()),
                                            ValorIcms = ParseDecimal(Request.Form[$"Itens[{itemIndex}].Imposto.ValorIcms"].ToString())
                                        };

                                        var basePis = ParseDecimal(Request.Form[$"Itens[{itemIndex}].Imposto.BasePis"].ToString());
                                        if (basePis > 0) item.Imposto.BasePis = basePis;
                                        var valPis = ParseDecimal(Request.Form[$"Itens[{itemIndex}].Imposto.ValorPis"].ToString());
                                        if (valPis > 0) item.Imposto.ValorPis = valPis;
                                        var baseCofins = ParseDecimal(Request.Form[$"Itens[{itemIndex}].Imposto.BaseCofins"].ToString());
                                        if (baseCofins > 0) item.Imposto.BaseCofins = baseCofins;
                                        var valCofins = ParseDecimal(Request.Form[$"Itens[{itemIndex}].Imposto.ValorCofins"].ToString());
                                        if (valCofins > 0) item.Imposto.ValorCofins = valCofins;
                                    }
                                }

                                itens.Add(item);
                            }
                        }
                    }

                    if (itens.Any())
                    {
                        viewModel.Itens = itens;
                        System.Diagnostics.Debug.WriteLine($"Itens processados manualmente: {itens.Count}");
                    }
                }
            }

            System.Diagnostics.Debug.WriteLine($"Total de itens antes da validação: {viewModel.Itens?.Count ?? 0}");
            System.Diagnostics.Debug.WriteLine($"Pagamentos no ViewModel (automático): {viewModel.Pagamentos?.Count ?? 0}");

            // Se não houver pagamentos no ViewModel, tentar fazer binding manual
            if (viewModel.Pagamentos == null || !viewModel.Pagamentos.Any())
            {
                // Verificar se foi enviado via Request.Form
                var pagamentosCount = Request.Form.Keys.Count(k => k.StartsWith("Pagamentos[") && k.Contains(".TipoPagamento"));
                System.Diagnostics.Debug.WriteLine($"Pagamentos encontrados no Request.Form: {pagamentosCount}");
                
                if (pagamentosCount > 0)
                {
                    // Tentar fazer binding manual - buscar todos os índices possíveis
                    var pagamentos = new List<NFePagamentoViewModel>();
                    
                    // Buscar todos os índices únicos dos pagamentos
                    var indices = new HashSet<int>();
                    foreach (var key in Request.Form.Keys)
                    {
                        if (key.StartsWith("Pagamentos[") && key.Contains("].TipoPagamento"))
                        {
                            var match = System.Text.RegularExpressions.Regex.Match(key, @"Pagamentos\[(\d+)\]");
                            if (match.Success && int.TryParse(match.Groups[1].Value, out var idx))
                            {
                                indices.Add(idx);
                            }
                        }
                    }
                    
                    System.Diagnostics.Debug.WriteLine($"Índices de pagamentos encontrados: {string.Join(", ", indices.OrderBy(i => i))}");
                    
                    foreach (var pagamentoIndex in indices.OrderBy(i => i))
                    {
                        var tipoPagamentoKey = $"Pagamentos[{pagamentoIndex}].TipoPagamento";
                        var valorPagoKey = $"Pagamentos[{pagamentoIndex}].ValorPago";
                        
                        if (Request.Form.ContainsKey(tipoPagamentoKey))
                        {
                            var tipoPagamento = Request.Form[tipoPagamentoKey].ToString()?.Trim() ?? string.Empty;
                            var valorPagoStr = Request.Form.ContainsKey(valorPagoKey) ? Request.Form[valorPagoKey].ToString() : "0";
                            var valorPago = ParseDecimal(valorPagoStr);
                            
                            System.Diagnostics.Debug.WriteLine($"Pagamento {pagamentoIndex}: TipoPagamento='{tipoPagamento}', ValorPagoStr='{valorPagoStr}', ValorPago={valorPago}");
                            
                            if (!string.IsNullOrWhiteSpace(tipoPagamento) && valorPago > 0)
                            {
                                var pagamento = new NFePagamentoViewModel
                                {
                                    TipoPagamento = tipoPagamento,
                                    ValorPago = valorPago
                                };
                                
                                pagamentos.Add(pagamento);
                                System.Diagnostics.Debug.WriteLine($"Pagamento {pagamentoIndex} adicionado: Tipo={pagamento.TipoPagamento}, Valor={pagamento.ValorPago}");
                            }
                            else
                            {
                                System.Diagnostics.Debug.WriteLine($"Pagamento {pagamentoIndex} rejeitado: TipoPagamento vazio={string.IsNullOrWhiteSpace(tipoPagamento)}, ValorPago={valorPago}");
                            }
                        }
                    }
                    
                    if (pagamentos.Any())
                    {
                        viewModel.Pagamentos = pagamentos;
                        System.Diagnostics.Debug.WriteLine($"Pagamentos processados manualmente: {pagamentos.Count}");
                    }
                }
            }

            // Validar NCM e CFOP dos itens (apenas avisar, não bloquear)
            if (viewModel.Itens != null && viewModel.Itens.Any())
            {
                foreach (var item in viewModel.Itens)
                {
                    var index = viewModel.Itens.IndexOf(item);
                    if (!string.IsNullOrWhiteSpace(item.Ncm) && !await _nfeService.ValidateNcmAsync(item.Ncm))
                    {
                        ModelState.AddModelError($"Itens[{index}].Ncm", "NCM inválido.");
                    }
                    if (!string.IsNullOrWhiteSpace(item.Cfop) && !await _nfeService.ValidateCfopAsync(item.Cfop))
                    {
                        ModelState.AddModelError($"Itens[{index}].Cfop", "CFOP inválido.");
                    }
                }
            }

            // Remover erros de validação de NCM/CFOP, Impostos, Destinatario e Pagamentos do ModelState para não bloquear o salvamento
            // (mas manter os avisos para o usuário)
            var keysToRemove = ModelState.Keys.Where(k => 
                k.Contains(".Ncm") || 
                k.Contains(".Cfop") || 
                k.Contains(".Imposto.") ||
                k.Contains("Destinatario.") ||
                k.Contains("Pagamentos.")).ToList();
            foreach (var key in keysToRemove)
            {
                ModelState.Remove(key);
            }
            
            // Se houver erro em Destinatario.CodMun por valor vazio, definir valor padrão
            if (viewModel.Destinatario != null && (!viewModel.Destinatario.CodMun.HasValue || viewModel.Destinatario.CodMun.Value == 0))
            {
                viewModel.Destinatario.CodMun = 0; // Valor padrão
            }

            // Log de erros de validação
            System.Diagnostics.Debug.WriteLine($"=== MODELSTATE VALID: {ModelState.IsValid} ===");
            System.Diagnostics.Debug.WriteLine($"Total de erros: {ModelState.ErrorCount}");
            
            if (!ModelState.IsValid)
            {
                System.Diagnostics.Debug.WriteLine("=== ERROS DE VALIDAÇÃO ===");
                foreach (var error in ModelState)
                {
                    if (error.Value.Errors.Any())
                    {
                        System.Diagnostics.Debug.WriteLine($"Campo: {error.Key}, Erros: {string.Join(", ", error.Value.Errors.Select(e => e.ErrorMessage))}");
                    }
                }
                
                // Remover TODOS os erros de validação para permitir o salvamento
                // (mantendo apenas os avisos visuais)
                var allKeys = ModelState.Keys.ToList();
                foreach (var key in allKeys)
                {
                    if (key.Contains("Itens.") || 
                        key.Contains("Pagamentos.") || 
                        key.Contains("Destinatario.") ||
                        key.Contains(".Ncm") ||
                        key.Contains(".Cfop") ||
                        key.Contains(".Imposto."))
                    {
                        ModelState.Remove(key);
                        System.Diagnostics.Debug.WriteLine($"Erro removido do ModelState: {key}");
                    }
                }
                
                System.Diagnostics.Debug.WriteLine($"=== MODELSTATE APÓS REMOÇÃO: {ModelState.IsValid} ===");
            }

            // Forçar ModelState.IsValid = true para permitir salvamento mesmo com erros de validação
            // (os dados serão validados no service)
            if (!ModelState.IsValid)
            {
                System.Diagnostics.Debug.WriteLine("=== FORÇANDO MODELSTATE PARA VALID ===");
                // Remover todos os erros restantes
                var remainingErrors = ModelState.Keys.Where(k => ModelState[k]?.Errors.Any() == true).ToList();
                foreach (var key in remainingErrors)
                {
                    ModelState.Remove(key);
                }
            }

            // SEMPRE tentar salvar, mesmo se houver erros de validação
            // (os erros críticos serão tratados no service)
            try
            {
                System.Diagnostics.Debug.WriteLine($"=== INICIANDO CRIAÇÃO DE NFE ===");
                System.Diagnostics.Debug.WriteLine($"Total de itens: {viewModel.Itens?.Count ?? 0}");
                System.Diagnostics.Debug.WriteLine($"Total de pagamentos: {viewModel.Pagamentos?.Count ?? 0}");
                
                if (viewModel.Pagamentos != null && viewModel.Pagamentos.Any())
                {
                    System.Diagnostics.Debug.WriteLine("=== DETALHES DOS PAGAMENTOS NO CONTROLLER ===");
                    foreach (var pag in viewModel.Pagamentos)
                    {
                        System.Diagnostics.Debug.WriteLine($"Pagamento - Tipo: '{pag.TipoPagamento}', Valor: {pag.ValorPago}");
                    }
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("=== NENHUM PAGAMENTO NO VIEWMODEL ===");
                    // Tentar buscar diretamente do Request.Form como último recurso
                    var pagamentosForm = new List<NFePagamentoViewModel>();
                    var pagIndex = 0;
                    while (Request.Form.ContainsKey($"Pagamentos[{pagIndex}].TipoPagamento"))
                    {
                        var tipo = Request.Form[$"Pagamentos[{pagIndex}].TipoPagamento"].ToString();
                        var valorStr = Request.Form[$"Pagamentos[{pagIndex}].ValorPago"].ToString();
                        var valor = ParseDecimal(valorStr);
                        if (valor > 0 && !string.IsNullOrWhiteSpace(tipo))
                        {
                            pagamentosForm.Add(new NFePagamentoViewModel
                            {
                                TipoPagamento = tipo.Trim(),
                                ValorPago = valor
                            });
                            System.Diagnostics.Debug.WriteLine($"Pagamento encontrado no Request.Form[{pagIndex}]: Tipo={tipo}, Valor={valor}");
                        }
                        pagIndex++;
                    }
                    if (pagamentosForm.Any())
                    {
                        viewModel.Pagamentos = pagamentosForm;
                        System.Diagnostics.Debug.WriteLine($"Pagamentos adicionados do Request.Form: {pagamentosForm.Count}");
                    }
                }
                
                System.Diagnostics.Debug.WriteLine($"=== CHAMANDO CreateNFeAsync ===");
                var nfe = await _nfeService.CreateNFeAsync(viewModel);
                System.Diagnostics.Debug.WriteLine($"=== CreateNFeAsync RETORNOU - ID: {nfe.IdNfe} ===");

                // Log de auditoria
                await _nfeService.LogAuditoriaAsync(
                    "CRIAR",
                    nfe.IdNfe,
                    GetCurrentUser(),
                    $"NF-e {nfe.Serie}/{nfe.Numero} criada com sucesso"
                );

                TempData["SuccessMessage"] = "NF-e cadastrada com sucesso!";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"=== ERRO AO CRIAR NFE ===");
                System.Diagnostics.Debug.WriteLine($"Mensagem: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"Stack Trace: {ex.StackTrace}");
                if (ex.InnerException != null)
                {
                    System.Diagnostics.Debug.WriteLine($"Inner Exception: {ex.InnerException.Message}");
                }
                
                ModelState.AddModelError("", $"Erro ao cadastrar NF-e: {ex.Message}");
                if (ex.InnerException != null)
                {
                    ModelState.AddModelError("", $"Detalhes: {ex.InnerException.Message}");
                }
            }

            // Recarregar pedidos em caso de erro
            var pedidos = await _context.Pedidos
                .Include(p => p.Cliente)
                .Where(p => p.Ativo == true)
                .OrderByDescending(p => p.DataPedido)
                .ToListAsync();

            ViewBag.Pedidos = pedidos.Select(p => new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem
            {
                Value = p.IdPedido.ToString(),
                Text = $"Pedido #{p.CodPedido} - {(p.Cliente != null ? p.Cliente.Nomerazao : "Sem Cliente")} ({p.DataPedido:dd/MM/yyyy})",
                Selected = p.IdPedido == viewModel.IdPedido
            }).ToList();

            // Recarregar naturezas de operação ativas
            var naturezasOperacao = await _context.NFeNaturezaOperacoes
                .Where(n => n.Ativo == true)
                .OrderBy(n => n.Descricao)
                .ToListAsync();

            ViewBag.NaturezasOperacao = naturezasOperacao.Select(n => new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem
            {
                Value = n.Descricao,
                Text = n.Descricao,
                Selected = n.Descricao == viewModel.NaturezaOperacao
            }).ToList();

            // Criar um dicionário com CFOP por natureza de operação para uso no JavaScript
            ViewBag.NaturezasOperacaoComCfop = naturezasOperacao
                .Where(n => !string.IsNullOrWhiteSpace(n.Cfop))
                .ToDictionary(n => n.Descricao, n => n.Cfop!);

            return View(viewModel);
        }

        // GET: NFe/Edit/5
        [AuthorizePermission("alterar")]
        public async Task<IActionResult> Edit(long? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var nfe = await _context.NFes
                .Include(n => n.Destinatario)
                .Include(n => n.Itens)
                    .ThenInclude(i => i.Imposto)
                .Include(n => n.Pagamentos)
                .Include(n => n.Pedido)
                    .ThenInclude(p => p.StatusPedido)
                .FirstOrDefaultAsync(m => m.IdNfe == id);
            if (nfe == null)
            {
                return NotFound();
            }

            var pedidos = await _context.Pedidos
                .Include(p => p.Cliente)
                .Where(p => p.Ativo == true)
                .OrderByDescending(p => p.DataPedido)
                .ToListAsync();

            ViewBag.Pedidos = pedidos.Select(p => new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem
            {
                Value = p.IdPedido.ToString(),
                Text = $"Pedido #{p.CodPedido} - {(p.Cliente != null ? p.Cliente.Nomerazao : "Sem Cliente")} ({p.DataPedido:dd/MM/yyyy})",
                Selected = p.IdPedido == nfe.IdPedido
            }).ToList();

            // Buscar naturezas de operação ativas
            var naturezasOperacao = await _context.NFeNaturezaOperacoes
                .Where(n => n.Ativo == true)
                .OrderBy(n => n.Descricao)
                .ToListAsync();

            var viewModel = NFeMapper.ToEditViewModel(nfe);

            ViewBag.NaturezasOperacao = naturezasOperacao.Select(n => new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem
            {
                Value = n.Descricao,
                Text = n.Descricao,
                Selected = n.Descricao == viewModel.NaturezaOperacao
            }).ToList();
            
            // Adicionar descrição do regime tributário ao ViewBag para exibição
            ViewBag.RegimeTributarioDescricao = ConfiguracaoHelper.ObterDescricaoRegimeTributario(viewModel.RegimeTributario);
            
            // Buscar formas de pagamento ativas
            var formasPagamento = await _context.FormasPagamento
                .Where(f => f.Ativo == true)
                .OrderBy(f => f.Codigo ?? f.Descricao)
                .ToListAsync();

            ViewBag.FormasPagamento = formasPagamento.Select(f => new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem
            {
                Value = f.Codigo ?? f.Descricao ?? "",
                Text = f.Descricao
            }).ToList();
            
            return View(viewModel);
        }

        // POST: NFe/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [AuthorizePermission("alterar")]
        public async Task<IActionResult> Edit(long id, NFeEditViewModel viewModel)
        {
            System.Diagnostics.Debug.WriteLine($"=== EDIT NFE POST CHAMADO ===");
            System.Diagnostics.Debug.WriteLine($"Pagamentos existentes: {viewModel.Pagamentos?.Count ?? 0}");
            System.Diagnostics.Debug.WriteLine($"Novos pagamentos: {viewModel.NovosPagamentos?.Count ?? 0}");
            System.Diagnostics.Debug.WriteLine($"Pagamentos removidos: {viewModel.PagamentosRemovidos?.Count ?? 0}");
            
            if (id != viewModel.IdNfe)
            {
                return NotFound();
            }

            // Se não houver novos pagamentos no ViewModel, tentar fazer binding manual
            if (viewModel.NovosPagamentos == null || !viewModel.NovosPagamentos.Any())
            {
                // Verificar se foi enviado via Request.Form
                var novosPagamentosCount = Request.Form.Keys.Count(k => k.StartsWith("NovosPagamentos[") && k.Contains(".TipoPagamento"));
                System.Diagnostics.Debug.WriteLine($"Novos pagamentos encontrados no Request.Form: {novosPagamentosCount}");
                
                if (novosPagamentosCount > 0)
                {
                    // Tentar fazer binding manual
                    var novosPagamentos = new List<NFePagamentoViewModel>();
                    
                    // Buscar todos os índices únicos dos novos pagamentos
                    var indices = new HashSet<int>();
                    foreach (var key in Request.Form.Keys)
                    {
                        if (key.StartsWith("NovosPagamentos[") && key.Contains("].TipoPagamento"))
                        {
                            var match = System.Text.RegularExpressions.Regex.Match(key, @"NovosPagamentos\[(\d+)\]");
                            if (match.Success && int.TryParse(match.Groups[1].Value, out var idx))
                            {
                                indices.Add(idx);
                            }
                        }
                    }
                    
                    System.Diagnostics.Debug.WriteLine($"Índices de novos pagamentos encontrados: {string.Join(", ", indices.OrderBy(i => i))}");
                    
                    foreach (var pagamentoIndex in indices.OrderBy(i => i))
                    {
                        var tipoPagamentoKey = $"NovosPagamentos[{pagamentoIndex}].TipoPagamento";
                        var valorPagoKey = $"NovosPagamentos[{pagamentoIndex}].ValorPago";
                        
                        if (Request.Form.ContainsKey(tipoPagamentoKey))
                        {
                            var tipoPagamento = Request.Form[tipoPagamentoKey].ToString()?.Trim() ?? string.Empty;
                            var valorPagoStr = Request.Form.ContainsKey(valorPagoKey) ? Request.Form[valorPagoKey].ToString() : "0";
                            var valorPago = ParseDecimal(valorPagoStr);
                            
                            System.Diagnostics.Debug.WriteLine($"Novo pagamento {pagamentoIndex}: TipoPagamento='{tipoPagamento}', ValorPagoStr='{valorPagoStr}', ValorPago={valorPago}");
                            
                            if (!string.IsNullOrWhiteSpace(tipoPagamento) && valorPago > 0)
                            {
                                var pagamento = new NFePagamentoViewModel
                                {
                                    TipoPagamento = tipoPagamento,
                                    ValorPago = valorPago
                                };
                                
                                novosPagamentos.Add(pagamento);
                                System.Diagnostics.Debug.WriteLine($"Novo pagamento {pagamentoIndex} adicionado: Tipo={pagamento.TipoPagamento}, Valor={pagamento.ValorPago}");
                            }
                            else
                            {
                                System.Diagnostics.Debug.WriteLine($"Novo pagamento {pagamentoIndex} rejeitado: TipoPagamento vazio={string.IsNullOrWhiteSpace(tipoPagamento)}, ValorPago={valorPago}");
                            }
                        }
                    }
                    
                    if (novosPagamentos.Any())
                    {
                        viewModel.NovosPagamentos = novosPagamentos;
                        System.Diagnostics.Debug.WriteLine($"Novos pagamentos processados manualmente: {novosPagamentos.Count}");
                    }
                }
            }

            // Validar NCM e CFOP dos itens
            if (viewModel.Itens != null)
            {
                foreach (var item in viewModel.Itens)
                {
                    if (!await _nfeService.ValidateNcmAsync(item.Ncm))
                    {
                        ModelState.AddModelError($"Itens[{viewModel.Itens.IndexOf(item)}].Ncm", "NCM inválido.");
                    }
                    if (!await _nfeService.ValidateCfopAsync(item.Cfop))
                    {
                        ModelState.AddModelError($"Itens[{viewModel.Itens.IndexOf(item)}].Cfop", "CFOP inválido.");
                    }
                }
            }

            if (viewModel.NovosItens != null)
            {
                foreach (var item in viewModel.NovosItens)
                {
                    var index = viewModel.NovosItens.IndexOf(item);
                    if (!await _nfeService.ValidateNcmAsync(item.Ncm))
                    {
                        ModelState.AddModelError($"NovosItens[{index}].Ncm", "NCM inválido.");
                    }
                    if (!await _nfeService.ValidateCfopAsync(item.Cfop))
                    {
                        ModelState.AddModelError($"NovosItens[{index}].Cfop", "CFOP inválido.");
                    }
                }
            }

            // Buscar configurações e garantir que sejam usadas
            var modelo = await ConfiguracaoHelper.BuscarValorIntConfiguracaoAsync(_context, "MODELO_NFE");
            var regimeTributario = await ConfiguracaoHelper.BuscarValorIntConfiguracaoAsync(_context, "REGIME_NFE");
            
            if (modelo > 0)
            {
                viewModel.Modelo = modelo.ToString().PadLeft(2, '0');
            }
            if (regimeTributario > 0)
            {
                viewModel.RegimeTributario = regimeTributario.ToString();
            }

            // Remover erros de validação de pagamentos do ModelState para não bloquear o salvamento
            var keysToRemove = ModelState.Keys.Where(k => 
                k.Contains("Pagamentos.") ||
                k.Contains("NovosPagamentos.")).ToList();
            foreach (var key in keysToRemove)
            {
                ModelState.Remove(key);
            }

            // SEMPRE tentar salvar, mesmo se houver erros de validação
            try
            {
                System.Diagnostics.Debug.WriteLine($"=== CHAMANDO UpdateNFeAsync ===");
                var nfe = await _nfeService.UpdateNFeAsync(viewModel);
                System.Diagnostics.Debug.WriteLine($"=== UpdateNFeAsync RETORNOU - ID: {nfe.IdNfe} ===");

                // Log de auditoria
                await _nfeService.LogAuditoriaAsync(
                    "EDITAR",
                    nfe.IdNfe,
                    GetCurrentUser(),
                    $"NF-e {nfe.Serie}/{nfe.Numero} atualizada com sucesso"
                );

                TempData["SuccessMessage"] = "NF-e atualizada com sucesso!";
                return RedirectToAction(nameof(Index));
            }
            catch (InvalidOperationException ex)
            {
                ModelState.AddModelError("", ex.Message);
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!NFeExists(viewModel.IdNfe))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", $"Erro ao atualizar NF-e: {ex.Message}");
            }

            // Recarregar dados para a view em caso de erro
            var nfeReload = await _context.NFes
                .Include(n => n.Destinatario)
                .Include(n => n.Itens)
                    .ThenInclude(i => i.Imposto)
                .Include(n => n.Pagamentos)
                .Include(n => n.Pedido)
                    .ThenInclude(p => p.StatusPedido)
                .FirstOrDefaultAsync(n => n.IdNfe == viewModel.IdNfe);

            if (nfeReload != null)
            {
                viewModel = NFeMapper.ToEditViewModel(nfeReload);
            }

            // Recarregar pedidos
            var pedidos = await _context.Pedidos
                .Include(p => p.Cliente)
                .Where(p => p.Ativo == true)
                .OrderByDescending(p => p.DataPedido)
                .ToListAsync();

            ViewBag.Pedidos = pedidos.Select(p => new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem
            {
                Value = p.IdPedido.ToString(),
                Text = $"Pedido #{p.CodPedido} - {(p.Cliente != null ? p.Cliente.Nomerazao : "Sem Cliente")} ({p.DataPedido:dd/MM/yyyy})",
                Selected = p.IdPedido == viewModel.IdPedido
            }).ToList();

            // Recarregar naturezas de operação ativas
            var naturezasOperacao = await _context.NFeNaturezaOperacoes
                .Where(n => n.Ativo == true)
                .OrderBy(n => n.Descricao)
                .ToListAsync();

            ViewBag.NaturezasOperacao = naturezasOperacao.Select(n => new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem
            {
                Value = n.Descricao,
                Text = n.Descricao,
                Selected = n.Descricao == viewModel.NaturezaOperacao
            }).ToList();

            // Adicionar descrição do regime tributário ao ViewBag para exibição
            ViewBag.RegimeTributarioDescricao = ConfiguracaoHelper.ObterDescricaoRegimeTributario(viewModel.RegimeTributario);
            
            // Buscar formas de pagamento ativas
            var formasPagamento = await _context.FormasPagamento
                .Where(f => f.Ativo == true)
                .OrderBy(f => f.Codigo ?? f.Descricao)
                .ToListAsync();

            ViewBag.FormasPagamento = formasPagamento.Select(f => new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem
            {
                Value = f.Codigo ?? f.Descricao ?? "",
                Text = f.Descricao
            }).ToList();

            return View(viewModel);
        }

        // GET: NFe/Delete/5
        [AuthorizePermission("excluir")]
        public async Task<IActionResult> Delete(long? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var nfe = await _context.NFes.FirstOrDefaultAsync(m => m.IdNfe == id);
            if (nfe == null)
            {
                return NotFound();
            }

            return View(nfe);
        }

        // POST: NFe/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [AuthorizePermission("excluir")]
        public async Task<IActionResult> DeleteConfirmed(long id)
        {
            var nfe = await _context.NFes
                .Include(n => n.Destinatario)
                .Include(n => n.Itens)
                    .ThenInclude(i => i.Imposto)
                .Include(n => n.Pagamentos)
                .FirstOrDefaultAsync(m => m.IdNfe == id);

            if (nfe != null)
            {
                try
                {
                    // Log de auditoria antes de excluir
                    await _nfeService.LogAuditoriaAsync(
                        "EXCLUIR",
                        nfe.IdNfe,
                        GetCurrentUser(),
                        $"NF-e {nfe.Serie}/{nfe.Numero} excluída"
                    );

                    // Excluir impostos dos itens primeiro
                    if (nfe.Itens != null)
                    {
                        foreach (var item in nfe.Itens)
                        {
                            if (item.Imposto != null)
                            {
                                _context.NFeItemImpostos.Remove(item.Imposto);
                            }
                        }
                        await _context.SaveChangesAsync();

                        // Excluir itens
                        _context.NFeItens.RemoveRange(nfe.Itens);
                        await _context.SaveChangesAsync();
                    }

                    // Excluir pagamentos
                    if (nfe.Pagamentos != null && nfe.Pagamentos.Any())
                    {
                        _context.NFePagamentos.RemoveRange(nfe.Pagamentos);
                        await _context.SaveChangesAsync();
                    }

                    // Excluir destinatário
                    if (nfe.Destinatario != null)
                    {
                        _context.NFeDestinatarios.Remove(nfe.Destinatario);
                        await _context.SaveChangesAsync();
                    }

                    // Excluir a NFe
                _context.NFes.Remove(nfe);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "NF-e excluída com sucesso!";
                }
                catch (Exception ex)
                {
                    TempData["ErrorMessage"] = $"Erro ao excluir NF-e: {ex.Message}";
                }
            }

            return RedirectToAction(nameof(Index));
        }

        // GET: NFe/AddItem/5
        [AuthorizePermission("alterar")]
        public async Task<IActionResult> AddItem(long? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var nfe = await _context.NFes
                .FirstOrDefaultAsync(m => m.IdNfe == id);
            
            if (nfe == null)
            {
                return NotFound();
            }

            ViewBag.IdNfe = nfe.IdNfe;
            ViewBag.NumeroNfe = $"{nfe.Serie}/{nfe.Numero}";
            return View();
        }

        // POST: NFe/AddItem/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [AuthorizePermission("alterar")]
        public async Task<IActionResult> AddItem(long id, [Bind("IdNfe,CodProduto,Descricao,Ncm,Cfop,Unidade,Quantidade,ValorUnitario,ValorTotal")] NFeItem item)
        {
            // Verificar se a NF-e existe
            var nfe = await _context.NFes.FindAsync(id);
            if (nfe == null)
            {
                return NotFound();
            }

            item.IdNfe = id;

            // Calcular valor total se não foi informado
            if (item.ValorTotal == 0 && item.Quantidade > 0 && item.ValorUnitario > 0)
            {
                item.ValorTotal = item.Quantidade * item.ValorUnitario;
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Add(item);
                    await _context.SaveChangesAsync(); // Salvar para obter o ID_ITEM

                    // Processar impostos se fornecidos
                    if (Request.Form.ContainsKey("Imposto.Origem") && !string.IsNullOrWhiteSpace(Request.Form["Imposto.CstCsosn"].ToString()))
                    {
                        var imposto = new NFeItemImposto
                        {
                            IdItem = item.IdItem,
                            Origem = byte.Parse(Request.Form["Imposto.Origem"].ToString()),
                            CstCsosn = Request.Form["Imposto.CstCsosn"].ToString(),
                            BaseIcms = decimal.Parse(Request.Form["Imposto.BaseIcms"].ToString()),
                            AliquotaIcms = decimal.Parse(Request.Form["Imposto.AliquotaIcms"].ToString()),
                            ValorIcms = decimal.Parse(Request.Form["Imposto.ValorIcms"].ToString())
                        };

                        var basePisStr = Request.Form["Imposto.BasePis"].ToString();
                        imposto.BasePis = string.IsNullOrWhiteSpace(basePisStr) ? null : (decimal.TryParse(basePisStr, out var basePis) ? basePis : null);

                        var valorPisStr = Request.Form["Imposto.ValorPis"].ToString();
                        imposto.ValorPis = string.IsNullOrWhiteSpace(valorPisStr) ? null : (decimal.TryParse(valorPisStr, out var valorPis) ? valorPis : null);

                        var baseCofinsStr = Request.Form["Imposto.BaseCofins"].ToString();
                        imposto.BaseCofins = string.IsNullOrWhiteSpace(baseCofinsStr) ? null : (decimal.TryParse(baseCofinsStr, out var baseCofins) ? baseCofins : null);

                        var valorCofinsStr = Request.Form["Imposto.ValorCofins"].ToString();
                        imposto.ValorCofins = string.IsNullOrWhiteSpace(valorCofinsStr) ? null : (decimal.TryParse(valorCofinsStr, out var valorCofins) ? valorCofins : null);

                        _context.Add(imposto);
                        await _context.SaveChangesAsync();
                    }

                    TempData["SuccessMessage"] = "Produto adicionado com sucesso!";
                    return RedirectToAction(nameof(Edit), new { id = id });
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", $"Erro ao adicionar produto: {ex.Message}");
                }
            }

            ViewBag.IdNfe = nfe.IdNfe;
            ViewBag.NumeroNfe = $"{nfe.Serie}/{nfe.Numero}";
            return View(item);
        }

        private bool NFeExists(long id)
        {
            return _context.NFes.Any(e => e.IdNfe == id);
        }

        // Função auxiliar para converter string numérica (suporta vírgula ou ponto)
        private decimal ParseDecimal(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return 0;

            var str = value.Trim();
            
            // Se está vazio, retorna 0
            if (string.IsNullOrEmpty(str))
                return 0;
            
            // Se não tem vírgula nem ponto, tenta parse direto
            if (str.IndexOf(',') == -1 && str.IndexOf('.') == -1)
            {
                if (decimal.TryParse(str, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var result))
                {
                    System.Diagnostics.Debug.WriteLine($"ParseDecimal: '{value}' -> {result} (sem vírgula/ponto)");
                    return result;
                }
                return 0;
            }
            
            // Se tem vírgula, assume formato brasileiro (vírgula como decimal, ponto como milhar)
            if (str.IndexOf(',') != -1)
            {
                // Remove pontos (separadores de milhar) e substitui vírgula por ponto
                var normalized = str.Replace(".", "").Replace(",", ".");
                if (decimal.TryParse(normalized, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var result))
                {
                    System.Diagnostics.Debug.WriteLine($"ParseDecimal: '{value}' -> '{normalized}' -> {result} (com vírgula)");
                    return result;
                }
                return 0;
            }
            
            // Se tem apenas ponto, assume formato americano (ponto como decimal)
            if (decimal.TryParse(str, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var result3))
            {
                System.Diagnostics.Debug.WriteLine($"ParseDecimal: '{value}' -> {result3} (com ponto)");
                return result3;
            }
            
            return 0;
        }

        // GET: NFe/BuscarProdutosPedido
        [HttpGet]
        public async Task<IActionResult> BuscarProdutosPedido(int? idPedido)
        {
            if (!idPedido.HasValue)
            {
                return Json(new { success = false, message = "Pedido não informado." });
            }

            try
            {
                var produtosPedido = await _context.PedidoProdutos
                    .Include(pp => pp.Produto)
                    .Where(pp => pp.IdPedido == idPedido.Value)
                    .ToListAsync();

                if (!produtosPedido.Any())
                {
                    return Json(new { success = false, message = "Nenhum produto encontrado para este pedido." });
                }

                var produtos = produtosPedido.Select(pp => new
                {
                    codProduto = pp.Produto?.IdProduto.ToString() ?? "",
                    descricao = pp.Produto?.Descricao ?? "",
                    ncm = !string.IsNullOrEmpty(pp.Produto?.Ncmsh) && pp.Produto.Ncmsh.Length >= 8 
                        ? pp.Produto.Ncmsh.Substring(0, 8) 
                        : (pp.Produto?.Ncmsh ?? ""),
                    cfop = !string.IsNullOrEmpty(pp.Produto?.Cfop) && pp.Produto.Cfop.Length >= 4 
                        ? pp.Produto.Cfop.Substring(0, 4) 
                        : (pp.Produto?.Cfop ?? ""),
                    unidade = "UN", // Unidade padrão
                    quantidade = pp.Quantidade ?? 0,
                    valorUnitario = pp.ValorVenda ?? 0
                }).ToList();

                return Json(new { success = true, produtos = produtos });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Erro ao buscar produtos: {ex.Message}" });
            }
        }

        // POST: NFe/TransmitirNFe/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [AuthorizePermission("alterar")]
        public async Task<IActionResult> TransmitirNFe(long id)
        {
            try
            {
                var nfe = await _context.NFes.FindAsync(id);
                if (nfe == null)
                {
                    return Json(new { success = false, message = "NFe não encontrada." });
                }

                // Verificar se a NFe já foi transmitida
                if (!string.IsNullOrEmpty(nfe.ChaveAcesso) && nfe.Status?.ToUpper() == "AUTORIZADA")
                {
                    return Json(new { success = false, message = "Esta NFe já foi transmitida e autorizada." });
                }

                // Enviar para NuvemFiscal
                var resposta = await _nuvemFiscalService.EnviarNFeAsync(id);

                if (resposta == null)
                {
                    return Json(new { success = false, message = "Erro ao transmitir NFe. Resposta vazia da API." });
                }

                // Verificar status da resposta (erro, rejeitado, etc)
                var statusUpper = resposta.Status?.ToUpper() ?? "";
                if (statusUpper == "ERRO" || statusUpper == "REJEITADO" || statusUpper == "CANCELADO" || 
                    statusUpper.Contains("ERRO") || statusUpper.Contains("REJEIT"))
                {
                    var mensagensErro = "Erro ao transmitir NFe.";
                    
                    // Priorizar motivo da autorização se disponível (para rejeições)
                    if (resposta.Autorizacao != null && !string.IsNullOrEmpty(resposta.Autorizacao.MotivoStatus))
                    {
                        mensagensErro = $"Rejeição: {resposta.Autorizacao.MotivoStatus}";
                        if (resposta.Autorizacao.CodigoStatus.HasValue)
                        {
                            mensagensErro += $" (Código: {resposta.Autorizacao.CodigoStatus.Value})";
                        }
                    }
                    // Se não tiver motivo da autorização, usar erros da lista
                    else if (resposta.Erros != null && resposta.Erros.Any())
                    {
                        mensagensErro = string.Join("; ", resposta.Erros.Select(e => e.Mensagem ?? e.Codigo ?? "Erro desconhecido"));
                    }
                    // Se não tiver erros específicos, usar o status
                    else if (!string.IsNullOrEmpty(resposta.Status))
                    {
                        mensagensErro = $"Status: {resposta.Status}";
                    }
                    
                    return Json(new { 
                        success = false, 
                        message = mensagensErro,
                        erros = resposta.Erros ?? new List<ErroDTO>(),
                        status = resposta.Status,
                        codigoStatus = resposta.Autorizacao?.CodigoStatus,
                        motivoStatus = resposta.Autorizacao?.MotivoStatus
                    });
                }

                // Verificar se houve erros na lista
                if (resposta.Erros != null && resposta.Erros.Any())
                {
                    var mensagensErro = string.Join("; ", resposta.Erros.Select(e => e.Mensagem ?? e.Codigo ?? "Erro desconhecido"));
                    return Json(new { 
                        success = false, 
                        message = $"Erro ao transmitir NFe: {mensagensErro}",
                        erros = resposta.Erros,
                        status = resposta.Status
                    });
                }

                // Verificar se a NFe foi realmente autorizada (deve ter chave de acesso)
                // A API pode retornar "chave" ou "chave_acesso" ou "autorizacao.chave_acesso"
                var chaveAcesso = resposta.ChaveAcesso ?? resposta.Chave ?? resposta.Autorizacao?.ChaveAcesso;
                if (string.IsNullOrEmpty(chaveAcesso))
                {
                    return Json(new { 
                        success = false, 
                        message = "NFe não foi autorizada. Chave de acesso não retornada pela API.",
                        status = resposta.Status
                    });
                }

                // Atualizar NFe com os dados retornados
                nfe.ChaveAcesso = chaveAcesso;
                
                if (!string.IsNullOrEmpty(resposta.Status))
                {
                    nfe.Status = resposta.Status.ToUpper() == "AUTORIZADO" ? "AUTORIZADA" : resposta.Status;
                }

                await _context.SaveChangesAsync();

                return Json(new { 
                    success = true, 
                    message = "NFe transmitida com sucesso!",
                    chaveAcesso = chaveAcesso,
                    status = resposta.Status,
                    id = resposta.Id
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Erro ao transmitir NFe: {ex.Message}" });
            }
        }
    }
}


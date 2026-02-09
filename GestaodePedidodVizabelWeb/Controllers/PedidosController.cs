using Microsoft.AspNetCore.Mvc;
using GestaoPedidosVizabel.Attributes;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using GestaoPedidosVizabel.Data;
using GestaoPedidosVizabel.Models;
using GestaoPedidosVizabel.Helpers;

namespace GestaoPedidosVizabel.Controllers
{
    [Authorize]
    public class PedidosController : Controller
    {
        private readonly ApplicationDbContext _context;

        public PedidosController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Pedidos
        [AuthorizePermission("visualizar")]
        public async Task<IActionResult> Index(string searchString)
        {
            var pedidos = _context.Pedidos
                .Include(p => p.Cliente)
                .Include(p => p.StatusPedido)
                .Include(p => p.Vendedor)
                .Include(p => p.FormaPagamento)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(searchString))
            {
                pedidos = pedidos.Where(p =>
                    p.CodPedido.ToString().Contains(searchString) ||
                    (p.Cliente != null && p.Cliente.Nomerazao != null && p.Cliente.Nomerazao.Contains(searchString)) ||
                    (p.StatusPedido != null && p.StatusPedido.Descricao != null && p.StatusPedido.Descricao.Contains(searchString)) ||
                    (p.Vendedor != null && p.Vendedor.Nome != null && p.Vendedor.Nome.Contains(searchString))
                );
            }

            ViewBag.SearchString = searchString;
            return View(await pedidos.ToListAsync());
        }

        // GET: Pedidos/Details/5
        [AuthorizePermission("visualizar")]
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var pedido = await _context.Pedidos
                .Include(p => p.Cliente)
                .Include(p => p.StatusPedido)
                .Include(p => p.Vendedor)
                .Include(p => p.FormaPagamento)
                .Include(p => p.PedidoProdutos)
                    .ThenInclude(pp => pp.Produto)
                .Include(p => p.PedidoProdutos)
                    .ThenInclude(pp => pp.Grade)
                .Include(p => p.PedidoProdutos)
                    .ThenInclude(pp => pp.PedidoProdTamanhos)
                        .ThenInclude(ppt => ppt.GradeTamanho)
                            .ThenInclude(gt => gt.TamanhoProduto)
                .Include(p => p.PedidoProdutos)
                    .ThenInclude(pp => pp.PedidoImagens)
                .FirstOrDefaultAsync(m => m.IdPedido == id);
            if (pedido == null)
            {
                return NotFound();
            }

            // Carregar produtos ativos para o dropdown
            ViewBag.Produtos = await _context.Produtos
                .Where(p => p.Ativo == true)
                .OrderBy(p => p.Descricao)
                .Select(p => new SelectListItem
                {
                    Value = p.IdProduto.ToString(),
                    Text = p.Descricao
                })
                .ToListAsync();

            return View(pedido);
        }

        // GET: Pedidos/GetGradesByProduto
        [HttpGet]
        public async Task<IActionResult> GetGradesByProduto(int idProduto)
        {
            var grades = await _context.ProdutoGrades
                .Where(pg => pg.IdProduto == idProduto && pg.Ativo == true)
                .Include(pg => pg.Grade)
                .Where(pg => pg.Grade != null && pg.Grade.Ativo == true)
                .Select(pg => new
                {
                    Value = pg.Grade.IdGrade.ToString(),
                    Text = pg.Grade.Descricao
                })
                .ToListAsync();

            return Json(grades);
        }

        // GET: Pedidos/GetTamanhosByGrade
        [HttpGet]
        public async Task<IActionResult> GetTamanhosByGrade(int idPedido, int idProduto, int idGrade, int idPedidoProduto = -1)
        {
            var tamanhos = new List<object>();

            // Buscar tamanhos já cadastrados para este pedido produto
            var tamanhosCadastrados = new Dictionary<int, (int idGradePedProd, int quantidade)>();
            if (idPedidoProduto > -1)
            {
                var cadastrados = await _context.PedidoProdTamanhos
                    .Where(ppt => ppt.IdPedidoproduto == idPedidoProduto)
                    .Select(ppt => new { ppt.IdGradeTamanho, ppt.IdGradePedProd, ppt.Quantidade })
                    .ToListAsync();

                foreach (var item in cadastrados)
                {
                    tamanhosCadastrados[item.IdGradeTamanho] = (item.IdGradePedProd, item.Quantidade);
                }
            }

            // Buscar todos os tamanhos da grade
            var gradeTamanhos = await _context.GradeTamanhos
                .Where(gt => gt.IdGrade == idGrade)
                .Include(gt => gt.TamanhoProduto)
                .ToListAsync();

            foreach (var gradeTamanho in gradeTamanhos)
            {
                var tamanho = gradeTamanho.TamanhoProduto;
                if (tamanho != null)
                {
                    var isSelected = tamanhosCadastrados.ContainsKey(gradeTamanho.IdGradeTamanho);
                    tamanhos.Add(new
                    {
                        sel = isSelected ? "1" : "0",
                        idGradePedProd = isSelected ? tamanhosCadastrados[gradeTamanho.IdGradeTamanho].idGradePedProd : -1,
                        quantidade = isSelected ? tamanhosCadastrados[gradeTamanho.IdGradeTamanho].quantidade : 0,
                        tamanho = tamanho.Tamanho,
                        idGradeTamanho = gradeTamanho.IdGradeTamanho
                    });
                }
            }

            return Json(tamanhos);
        }

        // POST: Pedidos/AdicionarProduto
        [HttpPost]
        [ValidateAntiForgeryToken]
        [AuthorizePermission("incluir")]
        public async Task<IActionResult> AdicionarProduto(int idPedido, int idProduto, int? idGrade)
        {
            var pedidoProduto = new PedidoProduto
            {
                IdPedido = idPedido,
                IdProduto = idProduto,
                IdGrade = idGrade
            };

            _context.PedidoProdutos.Add(pedidoProduto);
            await _context.SaveChangesAsync();

            // Atualizar data de entrega baseado nos produtos do pedido
            await AtualizaDataEntrega(idPedido);

            TempData["SuccessMessage"] = "Produto adicionado ao pedido com sucesso!";
            return RedirectToAction(nameof(Details), new { id = idPedido });
        }

        // POST: Pedidos/RemoverProduto
        [HttpPost]
        [ValidateAntiForgeryToken]
        [AuthorizePermission("incluir")]
        public async Task<IActionResult> RemoverProduto(int idPedido, int idPedidoProduto)
        {
            var pedidoProduto = await _context.PedidoProdutos
                .Include(pp => pp.PedidoProdTamanhos)
                .Include(pp => pp.PedidoImagens)
                .FirstOrDefaultAsync(pp => pp.IdPedidoproduto == idPedidoProduto);

            if (pedidoProduto != null)
            {
                // Remover imagens (cascade delete já cuida disso, mas vamos garantir)
                _context.PedidoImagens.RemoveRange(pedidoProduto.PedidoImagens);
                
                // Remover tamanhos
                _context.PedidoProdTamanhos.RemoveRange(pedidoProduto.PedidoProdTamanhos);
                _context.PedidoProdutos.Remove(pedidoProduto);
                await _context.SaveChangesAsync();
                
                // Atualizar data de entrega baseado nos produtos do pedido
                await AtualizaDataEntrega(idPedido);
                
                TempData["SuccessMessage"] = "Produto removido do pedido com sucesso!";
            }

            return RedirectToAction(nameof(Details), new { id = idPedido });
        }

        // POST: Pedidos/SalvarTamanhos
        [HttpPost]
        [ValidateAntiForgeryToken]
        [AuthorizePermission("incluir")]
        public async Task<IActionResult> SalvarTamanhos(int idPedido, int idPedidoProduto, string tamanhosJson)
        {
            try
            {
                // Remover tamanhos existentes
                var tamanhosExistentes = await _context.PedidoProdTamanhos
                    .Where(ppt => ppt.IdPedidoproduto == idPedidoProduto)
                    .ToListAsync();
                _context.PedidoProdTamanhos.RemoveRange(tamanhosExistentes);

                int quantidadeTotal = 0;

                // Adicionar novos tamanhos
                if (!string.IsNullOrEmpty(tamanhosJson))
                {
                    using var doc = System.Text.Json.JsonDocument.Parse(tamanhosJson);
                    var tamanhos = doc.RootElement.EnumerateArray();
                    
                    foreach (var tamanho in tamanhos)
                    {
                        if (tamanho.TryGetProperty("selecionado", out var selecionado) && 
                            selecionado.GetBoolean() &&
                            tamanho.TryGetProperty("quantidade", out var quantidade) &&
                            quantidade.GetInt32() > 0)
                        {
                            if (tamanho.TryGetProperty("idGradeTamanho", out var idGradeTamanho))
                            {
                                var quantidadeTamanho = quantidade.GetInt32();
                                quantidadeTotal += quantidadeTamanho;

                                var pedidoProdTamanho = new PedidoProdTamanho
                                {
                                    IdPedidoproduto = idPedidoProduto,
                                    IdGradeTamanho = idGradeTamanho.GetInt32(),
                                    Quantidade = quantidadeTamanho
                                };
                                _context.PedidoProdTamanhos.Add(pedidoProdTamanho);
                            }
                        }
                    }
                }

                // Atualizar a quantidade total na tabela PEDIDO_PRODUTOS
                var pedidoProduto = await _context.PedidoProdutos.FindAsync(idPedidoProduto);
                if (pedidoProduto != null)
                {
                    pedidoProduto.Quantidade = quantidadeTotal > 0 ? quantidadeTotal : null;
                }

                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Tamanhos salvos com sucesso!";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Erro ao salvar tamanhos: {ex.Message}";
            }

            return RedirectToAction(nameof(Details), new { id = idPedido });
        }

        // POST: Pedidos/UploadImagem
        [HttpPost]
        [ValidateAntiForgeryToken]
        [AuthorizePermission("incluir")]
        public async Task<IActionResult> UploadImagem(int idPedido, int idPedidoProduto, IFormFile arquivo, string? descricao)
        {
            try
            {
                if (arquivo == null || arquivo.Length == 0)
                {
                    TempData["ErrorMessage"] = "Nenhum arquivo foi selecionado.";
                    return RedirectToAction(nameof(Details), new { id = idPedido });
                }

                // Validar extensão do arquivo
                var extensoesPermitidas = new[] { ".jpg", ".jpeg", ".png", ".gif", ".bmp" };
                var extensao = Path.GetExtension(arquivo.FileName).ToLowerInvariant();
                if (!extensoesPermitidas.Contains(extensao))
                {
                    TempData["ErrorMessage"] = "Formato de arquivo não permitido. Use apenas imagens (jpg, jpeg, png, gif, bmp).";
                    return RedirectToAction(nameof(Details), new { id = idPedido });
                }

                // Validar tamanho do arquivo (máximo 5MB)
                if (arquivo.Length > 5 * 1024 * 1024)
                {
                    TempData["ErrorMessage"] = "O arquivo é muito grande. O tamanho máximo é 5MB.";
                    return RedirectToAction(nameof(Details), new { id = idPedido });
                }

                // Converter arquivo para byte array
                byte[] imagemBytes;
                using (var memoryStream = new MemoryStream())
                {
                    await arquivo.CopyToAsync(memoryStream);
                    imagemBytes = memoryStream.ToArray();
                }

                // Verificar se já existe uma imagem para este produto
                var imagemExistente = await _context.PedidoImagens
                    .FirstOrDefaultAsync(pi => pi.IdPedidoproduto == idPedidoProduto);

                if (imagemExistente != null)
                {
                    // Atualizar registro existente
                    imagemExistente.Imagem = imagemBytes;
                    imagemExistente.Descricao = descricao ?? arquivo.FileName;
                }
                else
                {
                    // Criar novo registro
                    var pedidoImagem = new PedidoImagem
                    {
                        IdPedidoproduto = idPedidoProduto,
                        Imagem = imagemBytes,
                        Descricao = descricao ?? arquivo.FileName
                    };
                    _context.PedidoImagens.Add(pedidoImagem);
                }

                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Imagem enviada com sucesso!";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Erro ao fazer upload da imagem: {ex.Message}";
            }

            return RedirectToAction(nameof(Details), new { id = idPedido });
        }

        // GET: Pedidos/ExibirImagem/id
        [HttpGet]
        public async Task<IActionResult> ExibirImagem(int id)
        {
            var imagem = await _context.PedidoImagens.FindAsync(id);
            if (imagem == null || imagem.Imagem == null || imagem.Imagem.Length == 0)
            {
                return NotFound();
            }

            // Determinar content type baseado nos primeiros bytes (magic numbers)
            string contentType = "image/jpeg"; // padrão
            if (imagem.Imagem.Length >= 4)
            {
                // PNG: 89 50 4E 47
                if (imagem.Imagem[0] == 0x89 && imagem.Imagem[1] == 0x50 && imagem.Imagem[2] == 0x4E && imagem.Imagem[3] == 0x47)
                    contentType = "image/png";
                // GIF: 47 49 46 38
                else if (imagem.Imagem[0] == 0x47 && imagem.Imagem[1] == 0x49 && imagem.Imagem[2] == 0x46 && imagem.Imagem[3] == 0x38)
                    contentType = "image/gif";
                // BMP: 42 4D
                else if (imagem.Imagem[0] == 0x42 && imagem.Imagem[1] == 0x4D)
                    contentType = "image/bmp";
                // JPEG: FF D8 FF
                else if (imagem.Imagem[0] == 0xFF && imagem.Imagem[1] == 0xD8 && imagem.Imagem[2] == 0xFF)
                    contentType = "image/jpeg";
            }

            return File(imagem.Imagem, contentType);
        }

        // POST: Pedidos/RemoverImagem
        [HttpPost]
        [ValidateAntiForgeryToken]
        [AuthorizePermission("incluir")]
        public async Task<IActionResult> RemoverImagem(int idPedido, int idPedidoImagem)
        {
            try
            {
                var imagem = await _context.PedidoImagens.FindAsync(idPedidoImagem);
                if (imagem != null)
                {
                    // Remover registro do banco
                    _context.PedidoImagens.Remove(imagem);
                    await _context.SaveChangesAsync();
                    TempData["SuccessMessage"] = "Imagem removida com sucesso!";
                }
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Erro ao remover imagem: {ex.Message}";
            }

            return RedirectToAction(nameof(Details), new { id = idPedido });
        }

        // GET: Pedidos/Create
        public async Task<IActionResult> Create()
        {
            await CarregarViewBags();
            var pedido = new Pedido
            {
                Ativo = true // Valor padrão para novos registros
            };
            return View(pedido);
        }

        // POST: Pedidos/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        [AuthorizePermission("incluir")]
        public async Task<IActionResult> Create([Bind("IdPedido,IdCliente,DataPedido,DataEntrega,IdStatuspedido,IdFormapagamento,Observacoes")] Pedido pedido)
        {
            // Calcular o próximo código do pedido automaticamente
            var maxCodPedido = await _context.Pedidos
                .MaxAsync(p => (int?)p.CodPedido);
            
            pedido.CodPedido = (maxCodPedido ?? 0) + 1;

            // Tratar checkboxes - padrão é true para novos registros
            var ativo = Request.Form["Ativo"].ToString();
            pedido.Ativo = string.IsNullOrEmpty(ativo) ? true : ativo == "true";

            var emitirNfe = Request.Form["EmitirNfe"].ToString();
            pedido.EmitirNfe = emitirNfe == "true" ? true : null;

            // Tratar IdVendedor nullable
            var idVendedorStr = Request.Form["IdVendedor"].ToString();
            if (string.IsNullOrEmpty(idVendedorStr))
            {
                pedido.IdVendedor = null;
            }
            else
            {
                if (int.TryParse(idVendedorStr, out int idVendedor))
                {
                    pedido.IdVendedor = idVendedor;
                }
            }

            if (ModelState.IsValid)
            {
                _context.Add(pedido);
                await _context.SaveChangesAsync();
                
                // Atualizar data de entrega baseado nos produtos do pedido
                await AtualizaDataEntrega(pedido.IdPedido);
                
                TempData["SuccessMessage"] = "Pedido cadastrado com sucesso!";
                return RedirectToAction(nameof(Index));
            }
            
            await CarregarViewBags();
            return View(pedido);
        }

        // GET: Pedidos/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var pedido = await _context.Pedidos
                .Include(p => p.StatusPedido)
                .FirstOrDefaultAsync(m => m.IdPedido == id);
            if (pedido == null)
            {
                return NotFound();
            }
            
            await CarregarViewBags();
            return View(pedido);
        }

        // POST: Pedidos/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [AuthorizePermission("incluir")]
        public async Task<IActionResult> Edit(int id, [Bind("IdPedido,IdCliente,DataPedido,DataEntrega,IdStatuspedido,IdFormapagamento,Observacoes")] Pedido pedido)
        {
            if (id != pedido.IdPedido)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    // Carregar o pedido existente do banco
                    var pedidoDb = await _context.Pedidos.FindAsync(id);
                    if (pedidoDb == null)
                    {
                        return NotFound();
                    }

                    // Tratar checkboxes - ler diretamente do formulário
                    var ativoValues = Request.Form["Ativo"];
                    var ativo = ativoValues.Contains("true");

                    var emitirNfeValues = Request.Form["EmitirNfe"];
                    var emitirNfe = emitirNfeValues.Contains("true") ? true : (bool?)null;

                    // Tratar IdVendedor nullable
                    var idVendedorStr = Request.Form["IdVendedor"].ToString();
                    int? idVendedor = null;
                    if (!string.IsNullOrEmpty(idVendedorStr) && int.TryParse(idVendedorStr, out int idVendedorParsed))
                    {
                        idVendedor = idVendedorParsed;
                    }

                    // Atualizar apenas os campos permitidos
                    pedidoDb.IdCliente = pedido.IdCliente;
                    pedidoDb.DataPedido = pedido.DataPedido;
                    pedidoDb.DataEntrega = pedido.DataEntrega;
                    pedidoDb.IdStatuspedido = pedido.IdStatuspedido;
                    pedidoDb.IdFormapagamento = pedido.IdFormapagamento;
                    pedidoDb.Observacoes = pedido.Observacoes;
                    pedidoDb.Ativo = ativo;  // Atribuir diretamente o valor lido do formulário
                    pedidoDb.EmitirNfe = emitirNfe;
                    pedidoDb.IdVendedor = idVendedor;

                    await _context.SaveChangesAsync();

                    // Atualizar data de entrega baseado nos produtos do pedido
                    await AtualizaDataEntrega(pedidoDb.IdPedido);

                    // Se o status do pedido for 3 (PEDIDO EM PRODUÇÃO), validar e inserir registros de etapa
                    if (pedidoDb.IdStatuspedido == 3)
                    {
                        await ProcessarEtapasProducao(pedidoDb.IdPedido);
                    }

                    TempData["SuccessMessage"] = "Pedido atualizado com sucesso!";
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!PedidoExists(pedido.IdPedido))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }
            
            // Carregar StatusPedido para a view em caso de erro de validação
            var pedidoComStatus = await _context.Pedidos
                .Include(p => p.StatusPedido)
                .FirstOrDefaultAsync(m => m.IdPedido == pedido.IdPedido);
            
            if (pedidoComStatus != null)
            {
                // Copiar os valores do formulário para o pedido carregado
                pedidoComStatus.IdCliente = pedido.IdCliente;
                pedidoComStatus.DataPedido = pedido.DataPedido;
                pedidoComStatus.DataEntrega = pedido.DataEntrega;
                pedidoComStatus.IdStatuspedido = pedido.IdStatuspedido;
                pedidoComStatus.IdFormapagamento = pedido.IdFormapagamento;
                pedidoComStatus.Observacoes = pedido.Observacoes;
                
                // Ler checkboxes do formulário
                var ativoValues = Request.Form["Ativo"];
                pedidoComStatus.Ativo = ativoValues.Contains("true");
                
                var emitirNfeValues = Request.Form["EmitirNfe"];
                pedidoComStatus.EmitirNfe = emitirNfeValues.Contains("true") ? true : (bool?)null;
                
                // Tratar IdVendedor nullable
                var idVendedorStr = Request.Form["IdVendedor"].ToString();
                int? idVendedor = null;
                if (!string.IsNullOrEmpty(idVendedorStr) && int.TryParse(idVendedorStr, out int idVendedorParsed))
                {
                    idVendedor = idVendedorParsed;
                }
                pedidoComStatus.IdVendedor = idVendedor;
            }
            
            await CarregarViewBags();
            return View(pedidoComStatus ?? pedido);
        }

        // GET: Pedidos/Delete/5
        [AuthorizePermission("excluir")]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var pedido = await _context.Pedidos
                .Include(p => p.Cliente)
                .Include(p => p.StatusPedido)
                .Include(p => p.Vendedor)
                .Include(p => p.FormaPagamento)
                .FirstOrDefaultAsync(m => m.IdPedido == id);
            if (pedido == null)
            {
                return NotFound();
            }

            return View(pedido);
        }

        // GET: Pedidos/Relatorio
        [AuthorizePermission("visualizar")]
        public async Task<IActionResult> Relatorio(string searchString)
        {
            var pedidos = _context.Pedidos
                .Include(p => p.Cliente)
                .Include(p => p.StatusPedido)
                .Include(p => p.Vendedor)
                .Include(p => p.FormaPagamento)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(searchString))
            {
                pedidos = pedidos.Where(p =>
                    p.CodPedido.ToString().Contains(searchString) ||
                    (p.Cliente != null && p.Cliente.Nomerazao != null && p.Cliente.Nomerazao.Contains(searchString)) ||
                    (p.StatusPedido != null && p.StatusPedido.Descricao != null && p.StatusPedido.Descricao.Contains(searchString)) ||
                    (p.Vendedor != null && p.Vendedor.Nome != null && p.Vendedor.Nome.Contains(searchString))
                );
            }

            var listaPedidos = await pedidos.OrderByDescending(p => p.DataPedido).ToListAsync();
            
            // Retornar view HTML para conversão
            return View(listaPedidos);
        }

        // GET: Pedidos/RelatorioPDF
        [AuthorizePermission("visualizar")]
        public async Task<IActionResult> RelatorioPDF(string searchString)
        {
            var pedidos = _context.Pedidos
                .Include(p => p.Cliente)
                .Include(p => p.StatusPedido)
                .Include(p => p.Vendedor)
                .Include(p => p.FormaPagamento)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(searchString))
            {
                pedidos = pedidos.Where(p =>
                    p.CodPedido.ToString().Contains(searchString) ||
                    (p.Cliente != null && p.Cliente.Nomerazao != null && p.Cliente.Nomerazao.Contains(searchString)) ||
                    (p.StatusPedido != null && p.StatusPedido.Descricao != null && p.StatusPedido.Descricao.Contains(searchString)) ||
                    (p.Vendedor != null && p.Vendedor.Nome != null && p.Vendedor.Nome.Contains(searchString))
                );
            }

            var listaPedidos = await pedidos.OrderByDescending(p => p.DataPedido).ToListAsync();
            
            // Retornar view HTML que será convertida para PDF no cliente usando JavaScript
            ViewBag.SearchString = searchString;
            return View("Relatorio", listaPedidos);
        }

        // POST: Pedidos/Delete/5
        [AuthorizePermission("excluir")]
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var pedido = await _context.Pedidos.FindAsync(id);
            if (pedido != null)
            {
                _context.Pedidos.Remove(pedido);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Pedido excluído com sucesso!";
            }

            return RedirectToAction(nameof(Index));
        }

        private bool PedidoExists(int id)
        {
            return _context.Pedidos.Any(e => e.IdPedido == id);
        }

        /// <summary>
        /// Atualiza a data de entrega do pedido baseado no maior prazo de entrega dos produtos
        /// </summary>
        private async Task AtualizaDataEntrega(int idPedido)
        {
            var pedido = await _context.Pedidos.FindAsync(idPedido);
            if (pedido == null)
                return;

            // Buscar o maior prazo de entrega dos produtos do pedido
            // COALESCE(MAX(PRAZO_ENTREGA), 30)
            var maxPrazoEntrega = await _context.PedidoProdutos
                .Where(pp => pp.IdPedido == idPedido)
                .Include(pp => pp.Produto)
                .Where(pp => pp.Produto != null && pp.Produto.PrazoEntrega.HasValue)
                .Select(pp => pp.Produto!.PrazoEntrega!.Value)
                .ToListAsync();

            // Se não encontrar produtos com prazo, usa 30 como padrão
            var prazoEntrega = maxPrazoEntrega.Any() ? maxPrazoEntrega.Max() : 30;

            // Atualizar a data de entrega: DataPedido + prazo em dias
            pedido.DataEntrega = pedido.DataPedido.AddDays(prazoEntrega);
            await _context.SaveChangesAsync();
        }

        /// <summary>
        /// Verifica se existe registro de etapa para o pedido produto e grade ped prod
        /// </summary>
        private async Task<bool> VerificaRegistroEtapa(int idPedidoProduto, int idGradePedProd)
        {
            var existe = await _context.ProdutoPedidoEtapaProds
                .AnyAsync(p => p.IdPedidoProduto == idPedidoProduto 
                    && p.IdEtapaProducao == 4 
                    && p.IdGradePedProd == idGradePedProd);
            
            return existe;
        }

        /// <summary>
        /// Atualiza o ID_ETAPA do PedidoProdTamanho
        /// </summary>
        private async Task AtualizaPedProdTamanhos(int idGradePedProd, int idEtapa)
        {
            var pedidoProdTamanho = await _context.PedidoProdTamanhos
                .FirstOrDefaultAsync(ppt => ppt.IdGradePedProd == idGradePedProd);
            
            if (pedidoProdTamanho != null)
            {
                pedidoProdTamanho.IdEtapa = idEtapa;
                await _context.SaveChangesAsync();
            }
        }

        /// <summary>
        /// Processa as etapas de produção quando o pedido está em produção (status = 3)
        /// </summary>
        private async Task ProcessarEtapasProducao(int idPedido)
        {
            // Buscar todos os PedidoProdTamanhos do pedido
            var pedidoProdutos = await _context.PedidoProdutos
                .Where(pp => pp.IdPedido == idPedido)
                .Include(pp => pp.PedidoProdTamanhos)
                    .ThenInclude(ppt => ppt.GradeTamanho)
                .ToListAsync();

            foreach (var pedidoProduto in pedidoProdutos)
            {
                foreach (var pedidoProdTamanho in pedidoProduto.PedidoProdTamanhos)
                {
                    // Verificar se já existe registro de etapa
                    if (!await VerificaRegistroEtapa(pedidoProduto.IdPedidoproduto, pedidoProdTamanho.IdGradePedProd))
                    {
                        // Buscar ID_TAMANHO do GradeTamanho
                        var gradeTamanho = await _context.GradeTamanhos
                            .FirstOrDefaultAsync(gt => gt.IdGradeTamanho == pedidoProdTamanho.IdGradeTamanho);

                        // Inserir novo registro
                        var produtoPedidoEtapaProd = new ProdutoPedidoEtapaProd
                        {
                            IdEtapaProducao = 4,
                            IdPedidoProduto = pedidoProduto.IdPedidoproduto,
                            IdFuncionario = null,
                            Concluido = false,
                            IdGradePedProd = pedidoProdTamanho.IdGradePedProd,
                            Quantidade = pedidoProdTamanho.Quantidade,
                            IdTamanho = gradeTamanho?.IdTamanho,
                            Reposicao = false
                        };

                        _context.ProdutoPedidoEtapaProds.Add(produtoPedidoEtapaProd);
                        await _context.SaveChangesAsync();

                        // Atualizar PedidoProdTamanhos
                        await AtualizaPedProdTamanhos(pedidoProdTamanho.IdGradePedProd, 4);
                    }
                }
            }
        }

        private async Task CarregarViewBags()
        {
            // Carregar clientes ativos
            ViewBag.Clientes = await _context.Clientes
                .Where(c => c.Ativo)
                .OrderBy(c => c.Nomerazao)
                .Select(c => new SelectListItem
                {
                    Value = c.IdCliente.ToString(),
                    Text = c.Nomerazao
                })
                .ToListAsync();

            // Carregar status de pedidos ativos
            ViewBag.StatusPedidos = await _context.StatusPedidos
                .Where(s => s.Ativo)
                .OrderBy(s => s.Descricao)
                .Select(s => new SelectListItem
                {
                    Value = s.IdStatuspedido.ToString(),
                    Text = s.Descricao
                })
                .ToListAsync();

            // Carregar vendedores (funcionários com Vendedor = true e ativos)
            ViewBag.Vendedores = await _context.Funcionarios
                .Where(f => f.Vendedor == true && f.Ativo)
                .OrderBy(f => f.Nome)
                .Select(f => new SelectListItem
                {
                    Value = f.IdFuncionario.ToString(),
                    Text = f.Nome
                })
                .ToListAsync();

            // Carregar formas de pagamento ativas
            ViewBag.FormasPagamento = await _context.FormasPagamento
                .Where(fp => fp.Ativo)
                .OrderBy(fp => fp.Descricao)
                .Select(fp => new SelectListItem
                {
                    Value = fp.IdFormapagamento.ToString(),
                    Text = fp.Descricao
                })
                .ToListAsync();
        }
    }
}


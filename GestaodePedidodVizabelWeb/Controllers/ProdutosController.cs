using Microsoft.AspNetCore.Mvc;
using GestaoPedidosVizabel.Attributes;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using GestaoPedidosVizabel.Data;
using GestaoPedidosVizabel.Models;
using GestaoPedidosVizabel.Helpers;

namespace GestaoPedidosVizabel.Controllers
{
    [Microsoft.AspNetCore.Authorization.Authorize]
    public class ProdutosController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ProdutosController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Produtos
        [AuthorizePermission("visualizar")]
        public async Task<IActionResult> Index(string searchString)
        {
            var produtos = _context.Produtos.AsQueryable();

            if (!string.IsNullOrWhiteSpace(searchString))
            {
                produtos = produtos.Where(p =>
                    (p.Descricao != null && p.Descricao.Contains(searchString)) ||
                    (p.Ncmsh != null && p.Ncmsh.Contains(searchString))
                );
            }

            ViewBag.SearchString = searchString;
            return View(await produtos.ToListAsync());
        }

        // GET: Produtos/Details/5
        [AuthorizePermission("visualizar")]
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var produto = await _context.Produtos
                .Include(p => p.ProdutoEtapasProducao)
                    .ThenInclude(pep => pep.EtapaProducao)
                .Include(p => p.ProdutoGrades)
                    .ThenInclude(pg => pg.Grade)
                .FirstOrDefaultAsync(m => m.IdProduto == id);
            if (produto == null)
            {
                return NotFound();
            }

            // Carregar etapas disponíveis (apenas ativas)
            ViewBag.EtapasDisponiveis = await _context.EtapasProducao
                .Where(e => e.Ativo)
                .OrderBy(e => e.Descricao)
                .ToListAsync();

            // Carregar grades disponíveis (apenas ativas)
            ViewBag.GradesDisponiveis = await _context.Grades
                .Where(g => g.Ativo == true)
                .OrderBy(g => g.Descricao)
                .ToListAsync();

            return View(produto);
        }

        // POST: Produtos/AdicionarEtapa
        [HttpPost]
        [ValidateAntiForgeryToken]
        [AuthorizePermission("incluir")]
        public async Task<IActionResult> AdicionarEtapa(int idProduto, int idEtapa, int? sequencia)
        {
            // Verificar se já existe
            var existe = await _context.ProdutoEtapasProducao
                .AnyAsync(pep => pep.IdProduto == idProduto && pep.IdEtapa == idEtapa);

            if (existe)
            {
                TempData["ErrorMessage"] = "Esta etapa já está adicionada a este produto.";
                return RedirectToAction(nameof(Details), new { id = idProduto });
            }

            // Calcular sequência automaticamente se não for informada
            int sequenciaCalculada;
            if (sequencia.HasValue)
            {
                sequenciaCalculada = sequencia.Value;
            }
            else
            {
                // Buscar a maior sequência existente para este produto
                var temSequencias = await _context.ProdutoEtapasProducao
                    .AnyAsync(pep => pep.IdProduto == idProduto && pep.Sequencia.HasValue);

                int maiorSequencia = 0;
                if (temSequencias)
                {
                    maiorSequencia = await _context.ProdutoEtapasProducao
                        .Where(pep => pep.IdProduto == idProduto && pep.Sequencia.HasValue)
                        .Select(pep => pep.Sequencia.Value)
                        .MaxAsync();
                }

                sequenciaCalculada = maiorSequencia + 1;
            }

            var produtoEtapa = new ProdutoEtapaProducao
            {
                IdProduto = idProduto,
                IdEtapa = idEtapa,
                Sequencia = sequenciaCalculada,
                Ativo = true
            };

            _context.ProdutoEtapasProducao.Add(produtoEtapa);
            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Etapa adicionada com sucesso!";

            return RedirectToAction(nameof(Details), new { id = idProduto });
        }

        // POST: Produtos/RemoverEtapa
        [HttpPost]
        [ValidateAntiForgeryToken]
        [AuthorizePermission("incluir")]
        public async Task<IActionResult> RemoverEtapa(int idProduto, int idProdutoEtapa)
        {
            var produtoEtapa = await _context.ProdutoEtapasProducao
                .FirstOrDefaultAsync(pep => pep.IdProdutoEtapa == idProdutoEtapa && pep.IdProduto == idProduto);

            if (produtoEtapa != null)
            {
                // Salvar a sequência da etapa que será removida
                var sequenciaRemovida = produtoEtapa.Sequencia;

                // Remover a etapa
                _context.ProdutoEtapasProducao.Remove(produtoEtapa);
                await _context.SaveChangesAsync();

                // Reajustar sequências das etapas restantes
                if (sequenciaRemovida.HasValue)
                {
                    // Buscar todas as etapas com sequência maior que a removida
                    var etapasParaReajustar = await _context.ProdutoEtapasProducao
                        .Where(pep => pep.IdProduto == idProduto 
                            && pep.Sequencia.HasValue 
                            && pep.Sequencia.Value > sequenciaRemovida.Value)
                        .OrderBy(pep => pep.Sequencia.Value)
                        .ToListAsync();

                    // Decrementar a sequência de cada etapa em 1
                    foreach (var etapa in etapasParaReajustar)
                    {
                        etapa.Sequencia = etapa.Sequencia.Value - 1;
                    }

                    await _context.SaveChangesAsync();
                }

                TempData["SuccessMessage"] = "Etapa removida e sequências reajustadas com sucesso!";
            }

            return RedirectToAction(nameof(Details), new { id = idProduto });
        }

        // POST: Produtos/ToggleEtapaAtivo
        [HttpPost]
        [ValidateAntiForgeryToken]
        [AuthorizePermission("incluir")]
        public async Task<IActionResult> ToggleEtapaAtivo(int idProduto, int idProdutoEtapa)
        {
            var produtoEtapa = await _context.ProdutoEtapasProducao
                .FirstOrDefaultAsync(pep => pep.IdProdutoEtapa == idProdutoEtapa && pep.IdProduto == idProduto);

            if (produtoEtapa != null)
            {
                produtoEtapa.Ativo = !produtoEtapa.Ativo;
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = $"Etapa {(produtoEtapa.Ativo ? "ativada" : "desativada")} com sucesso!";
            }

            return RedirectToAction(nameof(Details), new { id = idProduto });
        }

        // POST: Produtos/AtualizarSequenciaEtapa
        [HttpPost]
        [ValidateAntiForgeryToken]
        [AuthorizePermission("incluir")]
        public async Task<IActionResult> AtualizarSequenciaEtapa(int idProduto, int idProdutoEtapa, int sequencia)
        {
            if (sequencia < 1)
            {
                TempData["ErrorMessage"] = "A sequência deve ser maior que zero.";
                return RedirectToAction(nameof(Details), new { id = idProduto });
            }

            var produtoEtapa = await _context.ProdutoEtapasProducao
                .FirstOrDefaultAsync(pep => pep.IdProdutoEtapa == idProdutoEtapa && pep.IdProduto == idProduto);

            if (produtoEtapa != null)
            {
                produtoEtapa.Sequencia = sequencia;
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Sequência atualizada com sucesso!";
            }
            else
            {
                TempData["ErrorMessage"] = "Etapa não encontrada.";
            }

            return RedirectToAction(nameof(Details), new { id = idProduto });
        }

        // POST: Produtos/AumentarSequenciaEtapa
        [HttpPost]
        [ValidateAntiForgeryToken]
        [AuthorizePermission("incluir")]
        public async Task<IActionResult> AumentarSequenciaEtapa(int idProduto, int idProdutoEtapa)
        {
            var produtoEtapa = await _context.ProdutoEtapasProducao
                .FirstOrDefaultAsync(pep => pep.IdProdutoEtapa == idProdutoEtapa && pep.IdProduto == idProduto);

            if (produtoEtapa == null || !produtoEtapa.Sequencia.HasValue)
            {
                TempData["ErrorMessage"] = "Etapa não encontrada ou sem sequência definida.";
                return RedirectToAction(nameof(Details), new { id = idProduto });
            }

            // Buscar todas as etapas do produto ordenadas por sequência
            var todasEtapas = await _context.ProdutoEtapasProducao
                .Where(pep => pep.IdProduto == idProduto && pep.Sequencia.HasValue)
                .OrderBy(pep => pep.Sequencia.Value)
                .ToListAsync();

            // Encontrar a posição atual
            var etapaAtual = todasEtapas.FirstOrDefault(e => e.IdProdutoEtapa == idProdutoEtapa);
            if (etapaAtual == null)
            {
                TempData["ErrorMessage"] = "Etapa não encontrada.";
                return RedirectToAction(nameof(Details), new { id = idProduto });
            }

            var indiceAtual = todasEtapas.IndexOf(etapaAtual);

            // Verificar se não é a última
            if (indiceAtual >= todasEtapas.Count - 1)
            {
                TempData["ErrorMessage"] = "Esta etapa já é a última da sequência.";
                return RedirectToAction(nameof(Details), new { id = idProduto });
            }

            // Trocar sequências com a próxima etapa
            var proximaEtapa = todasEtapas[indiceAtual + 1];
            var sequenciaAtual = etapaAtual.Sequencia.Value;
            var sequenciaProxima = proximaEtapa.Sequencia.Value;

            etapaAtual.Sequencia = sequenciaProxima;
            proximaEtapa.Sequencia = sequenciaAtual;

            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Sequência aumentada com sucesso!";

            return RedirectToAction(nameof(Details), new { id = idProduto });
        }

        // POST: Produtos/DiminuirSequenciaEtapa
        [HttpPost]
        [ValidateAntiForgeryToken]
        [AuthorizePermission("incluir")]
        public async Task<IActionResult> DiminuirSequenciaEtapa(int idProduto, int idProdutoEtapa)
        {
            var produtoEtapa = await _context.ProdutoEtapasProducao
                .FirstOrDefaultAsync(pep => pep.IdProdutoEtapa == idProdutoEtapa && pep.IdProduto == idProduto);

            if (produtoEtapa == null || !produtoEtapa.Sequencia.HasValue)
            {
                TempData["ErrorMessage"] = "Etapa não encontrada ou sem sequência definida.";
                return RedirectToAction(nameof(Details), new { id = idProduto });
            }

            // Buscar todas as etapas do produto ordenadas por sequência
            var todasEtapas = await _context.ProdutoEtapasProducao
                .Where(pep => pep.IdProduto == idProduto && pep.Sequencia.HasValue)
                .OrderBy(pep => pep.Sequencia.Value)
                .ToListAsync();

            // Encontrar a posição atual
            var etapaAtual = todasEtapas.FirstOrDefault(e => e.IdProdutoEtapa == idProdutoEtapa);
            if (etapaAtual == null)
            {
                TempData["ErrorMessage"] = "Etapa não encontrada.";
                return RedirectToAction(nameof(Details), new { id = idProduto });
            }

            var indiceAtual = todasEtapas.IndexOf(etapaAtual);

            // Verificar se não é a primeira
            if (indiceAtual <= 0)
            {
                TempData["ErrorMessage"] = "Esta etapa já é a primeira da sequência.";
                return RedirectToAction(nameof(Details), new { id = idProduto });
            }

            // Trocar sequências com a etapa anterior
            var etapaAnterior = todasEtapas[indiceAtual - 1];
            var sequenciaAtual = etapaAtual.Sequencia.Value;
            var sequenciaAnterior = etapaAnterior.Sequencia.Value;

            etapaAtual.Sequencia = sequenciaAnterior;
            etapaAnterior.Sequencia = sequenciaAtual;

            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Sequência diminuída com sucesso!";

            return RedirectToAction(nameof(Details), new { id = idProduto });
        }

        // POST: Produtos/AdicionarGrade
        [HttpPost]
        [ValidateAntiForgeryToken]
        [AuthorizePermission("incluir")]
        public async Task<IActionResult> AdicionarGrade(int idProduto, int idGrade)
        {
            // Verificar se já existe
            var existe = await _context.ProdutoGrades
                .AnyAsync(pg => pg.IdProduto == idProduto && pg.IdGrade == idGrade);

            if (existe)
            {
                TempData["ErrorMessage"] = "Esta grade já está adicionada a este produto.";
                return RedirectToAction(nameof(Details), new { id = idProduto });
            }

            var produtoGrade = new ProdutoGrade
            {
                IdProduto = idProduto,
                IdGrade = idGrade,
                Ativo = true
            };

            _context.ProdutoGrades.Add(produtoGrade);
            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Grade adicionada com sucesso!";

            return RedirectToAction(nameof(Details), new { id = idProduto });
        }

        // POST: Produtos/RemoverGrade
        [HttpPost]
        [ValidateAntiForgeryToken]
        [AuthorizePermission("incluir")]
        public async Task<IActionResult> RemoverGrade(int idProduto, int idProdutoGrade)
        {
            var produtoGrade = await _context.ProdutoGrades
                .FirstOrDefaultAsync(pg => pg.IdProdutoGrade == idProdutoGrade && pg.IdProduto == idProduto);

            if (produtoGrade != null)
            {
                _context.ProdutoGrades.Remove(produtoGrade);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Grade removida com sucesso!";
            }

            return RedirectToAction(nameof(Details), new { id = idProduto });
        }

        // POST: Produtos/ToggleGradeAtivo
        [HttpPost]
        [ValidateAntiForgeryToken]
        [AuthorizePermission("incluir")]
        public async Task<IActionResult> ToggleGradeAtivo(int idProduto, int idProdutoGrade)
        {
            var produtoGrade = await _context.ProdutoGrades
                .FirstOrDefaultAsync(pg => pg.IdProdutoGrade == idProdutoGrade && pg.IdProduto == idProduto);

            if (produtoGrade != null)
            {
                produtoGrade.Ativo = !produtoGrade.Ativo;
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = $"Grade {(produtoGrade.Ativo ? "ativada" : "desativada")} com sucesso!";
            }

            return RedirectToAction(nameof(Details), new { id = idProduto });
        }

        // GET: Produtos/Create
        [AuthorizePermission("incluir")]
        public async Task<IActionResult> Create()
        {
            // Buscar valor padrão de NCM da configuração
            var ncmPadrao = await ConfiguracaoHelper.BuscarValorConfiguracaoAsync(_context, "NCM_NFE");
            
            // Carregar opções da TABELA_IPT (apenas registros ativos)
            // Filtrando por ATIVO = 1 (true) ou NULL
            List<TabelaIpt> opcoesNcm;
            try
            {
                opcoesNcm = await _context.TabelaIpt
                    .Where(t => t.Ativo == true || t.Ativo == null)
                    .OrderBy(t => t.Codigo)
                    .ToListAsync();
            }
            catch
            {
                // Se houver erro (campo ATIVO pode não existir), carregar todos os registros
                opcoesNcm = await _context.TabelaIpt
                    .OrderBy(t => t.Codigo)
                    .ToListAsync();
            }

            ViewBag.NcmPadrao = ncmPadrao;
            ViewBag.OpcoesNcm = opcoesNcm;

            // Se houver valor padrão, definir no modelo
            var produto = new Produto();
            if (!string.IsNullOrEmpty(ncmPadrao))
            {
                produto.Ncmsh = ncmPadrao;
            }

            return View(produto);
        }

        // POST: Produtos/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        [AuthorizePermission("incluir")]
        public async Task<IActionResult> Create([Bind("IdProduto,Descricao,PrazoEntrega,Ncmsh,Csosn")] Produto produto)
        {
            // Tratar checkboxes nullable
            var ativo = Request.Form["Ativo"].ToString();
            produto.Ativo = ativo == "true" ? true : false;

            // Tratar checkbox FabricacaoTerceirizada
            var fabricacaoTerceirizada = Request.Form["FabricacaoTerceirizada"].ToString();
            produto.FabricacaoTerceirizada = fabricacaoTerceirizada == "true";

            if (ModelState.IsValid)
            {
                _context.Add(produto);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Produto cadastrado com sucesso!";
                return RedirectToAction(nameof(Index));
            }
            return View(produto);
        }

        // GET: Produtos/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var produto = await _context.Produtos.FindAsync(id);
            if (produto == null)
            {
                return NotFound();
            }

            // Carregar opções da TABELA_IPT (apenas registros ativos)
            List<TabelaIpt> opcoesNcm;
            try
            {
                opcoesNcm = await _context.TabelaIpt
                    .Where(t => t.Ativo == true || t.Ativo == null)
                    .OrderBy(t => t.Codigo)
                    .ToListAsync();
            }
            catch
            {
                // Se houver erro (campo ATIVO pode não existir), carregar todos os registros
                opcoesNcm = await _context.TabelaIpt
                    .OrderBy(t => t.Codigo)
                    .ToListAsync();
            }

            ViewBag.OpcoesNcm = opcoesNcm;

            return View(produto);
        }

        // POST: Produtos/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [AuthorizePermission("incluir")]
        public async Task<IActionResult> Edit(int id, [Bind("IdProduto,Descricao,PrazoEntrega,Ncmsh,Csosn")] Produto produto)
        {
            if (id != produto.IdProduto)
            {
                return NotFound();
            }

            // Tratar checkboxes nullable
            var ativo = Request.Form["Ativo"].ToString();
            produto.Ativo = ativo == "true" ? true : false;

            // Tratar checkbox FabricacaoTerceirizada
            var fabricacaoTerceirizada = Request.Form["FabricacaoTerceirizada"].ToString();
            produto.FabricacaoTerceirizada = fabricacaoTerceirizada == "true";

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(produto);
                    await _context.SaveChangesAsync();
                    TempData["SuccessMessage"] = "Produto atualizado com sucesso!";
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!ProdutoExists(produto.IdProduto))
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
            return View(produto);
        }

        // GET: Produtos/Delete/5
        [AuthorizePermission("excluir")]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var produto = await _context.Produtos
                .FirstOrDefaultAsync(m => m.IdProduto == id);
            if (produto == null)
            {
                return NotFound();
            }

            return View(produto);
        }

        // POST: Produtos/Delete/5
        [AuthorizePermission("excluir")]
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var produto = await _context.Produtos.FindAsync(id);
            if (produto != null)
            {
                _context.Produtos.Remove(produto);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Produto excluído com sucesso!";
            }

            return RedirectToAction(nameof(Index));
        }

        // API: Buscar códigos NCM da TABELA_IPT
        [HttpGet]
        public async Task<IActionResult> BuscarNcm(string term)
        {
            try
            {
                var query = _context.TabelaIpt.AsQueryable();

                // Filtrar por ATIVO = true ou NULL
                try
                {
                    query = query.Where(t => t.Ativo == true || t.Ativo == null);
                }
                catch
                {
                    // Se houver erro, não filtrar por ATIVO
                }

                // Se houver termo de busca, filtrar por código ou descrição
                if (!string.IsNullOrWhiteSpace(term))
                {
                    var termLower = term.ToLower();
                    query = query.Where(t => 
                        t.Codigo.ToLower().Contains(termLower) || 
                        (t.Descricao != null && t.Descricao.ToLower().Contains(termLower))
                    );
                }

                var resultados = await query
                    .OrderBy(t => t.Codigo)
                    .Take(50) // Limitar a 50 resultados
                    .Select(t => new
                    {
                        codigo = t.Codigo,
                        descricao = t.Descricao ?? ""
                    })
                    .ToListAsync();

                return Json(resultados);
            }
            catch (Exception ex)
            {
                return Json(new { error = ex.Message });
            }
        }

        private bool ProdutoExists(int id)
        {
            return _context.Produtos.Any(e => e.IdProduto == id);
        }
    }
}


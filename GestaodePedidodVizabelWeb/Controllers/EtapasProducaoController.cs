using Microsoft.AspNetCore.Mvc;
using GestaoPedidosVizabel.Attributes;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using GestaoPedidosVizabel.Data;
using GestaoPedidosVizabel.Models;

namespace GestaoPedidosVizabel.Controllers
{
    [Microsoft.AspNetCore.Authorization.Authorize]
    public class EtapasProducaoController : Controller
    {
        private readonly ApplicationDbContext _context;

        public EtapasProducaoController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: EtapasProducao
        [AuthorizePermission("visualizar")]
        public async Task<IActionResult> Index()
        {
            var etapas = await _context.EtapasProducao.ToListAsync();
            var funcoes = await _context.Funcoes.ToListAsync();
            
            // Criar dicionário para lookup rápido
            ViewBag.FuncoesDict = funcoes.ToDictionary(f => f.IdFuncao, f => f.Descricao);
            
            return View(etapas);
        }

        // GET: EtapasProducao/Details/5
        [AuthorizePermission("visualizar")]
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var etapaProducao = await _context.EtapasProducao
                .FirstOrDefaultAsync(m => m.IdEtapa == id);
            if (etapaProducao == null)
            {
                return NotFound();
            }

            // Carregar função relacionada se existir
            if (etapaProducao.IdFuncao.HasValue)
            {
                ViewBag.Funcao = await _context.Funcoes
                    .FirstOrDefaultAsync(f => f.IdFuncao == etapaProducao.IdFuncao.Value);
            }

            return View(etapaProducao);
        }

        // GET: EtapasProducao/Create
        public async Task<IActionResult> Create()
        {
            // Carregar funções ativas para o dropdown
            ViewBag.Funcoes = await _context.Funcoes
                .Where(f => f.Ativo)
                .OrderBy(f => f.Descricao)
                .ToListAsync();
            return View();
        }

        // POST: EtapasProducao/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        [AuthorizePermission("incluir")]
        public async Task<IActionResult> Create([Bind("IdEtapa,Descricao,QuantidadeDias,IdFuncao")] EtapaProducao etapaProducao)
        {
            // Tratar checkbox
            var ativo = Request.Form["Ativo"].ToString();
            etapaProducao.Ativo = ativo == "true";

            // Tratar IdFuncao nullable
            var idFuncaoStr = Request.Form["IdFuncao"].ToString();
            if (string.IsNullOrEmpty(idFuncaoStr))
            {
                etapaProducao.IdFuncao = null;
            }
            else
            {
                if (int.TryParse(idFuncaoStr, out int idFuncao))
                {
                    etapaProducao.IdFuncao = idFuncao;
                }
            }

            if (ModelState.IsValid)
            {
                _context.Add(etapaProducao);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Etapa de produção cadastrada com sucesso!";
                return RedirectToAction(nameof(Index));
            }
            
            // Recarregar funções em caso de erro
            ViewBag.Funcoes = await _context.Funcoes
                .Where(f => f.Ativo)
                .OrderBy(f => f.Descricao)
                .ToListAsync();
            return View(etapaProducao);
        }

        // GET: EtapasProducao/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var etapaProducao = await _context.EtapasProducao.FindAsync(id);
            if (etapaProducao == null)
            {
                return NotFound();
            }
            
            // Carregar funções ativas para o dropdown
            ViewBag.Funcoes = await _context.Funcoes
                .Where(f => f.Ativo)
                .OrderBy(f => f.Descricao)
                .ToListAsync();
            return View(etapaProducao);
        }

        // POST: EtapasProducao/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [AuthorizePermission("incluir")]
        public async Task<IActionResult> Edit(int id, [Bind("IdEtapa,Descricao,QuantidadeDias,IdFuncao")] EtapaProducao etapaProducao)
        {
            if (id != etapaProducao.IdEtapa)
            {
                return NotFound();
            }

            // Tratar checkbox
            var ativo = Request.Form["Ativo"].ToString();
            etapaProducao.Ativo = ativo == "true";

            // Tratar IdFuncao nullable
            var idFuncaoStr = Request.Form["IdFuncao"].ToString();
            if (string.IsNullOrEmpty(idFuncaoStr))
            {
                etapaProducao.IdFuncao = null;
            }
            else
            {
                if (int.TryParse(idFuncaoStr, out int idFuncao))
                {
                    etapaProducao.IdFuncao = idFuncao;
                }
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(etapaProducao);
                    await _context.SaveChangesAsync();
                    TempData["SuccessMessage"] = "Etapa de produção atualizada com sucesso!";
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!EtapaProducaoExists(etapaProducao.IdEtapa))
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
            
            // Recarregar funções em caso de erro
            ViewBag.Funcoes = await _context.Funcoes
                .Where(f => f.Ativo)
                .OrderBy(f => f.Descricao)
                .ToListAsync();
            return View(etapaProducao);
        }

        // GET: EtapasProducao/Delete/5
        [AuthorizePermission("excluir")]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var etapaProducao = await _context.EtapasProducao
                .FirstOrDefaultAsync(m => m.IdEtapa == id);
            if (etapaProducao == null)
            {
                return NotFound();
            }

            return View(etapaProducao);
        }

        // POST: EtapasProducao/Delete/5
        [AuthorizePermission("excluir")]
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var etapaProducao = await _context.EtapasProducao.FindAsync(id);
            if (etapaProducao != null)
            {
                _context.EtapasProducao.Remove(etapaProducao);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Etapa de produção excluída com sucesso!";
            }

            return RedirectToAction(nameof(Index));
        }

        private bool EtapaProducaoExists(int id)
        {
            return _context.EtapasProducao.Any(e => e.IdEtapa == id);
        }
    }
}


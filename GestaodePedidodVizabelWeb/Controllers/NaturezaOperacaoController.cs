using Microsoft.AspNetCore.Mvc;
using GestaoPedidosVizabel.Attributes;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using GestaoPedidosVizabel.Data;
using GestaoPedidosVizabel.Models;

namespace GestaoPedidosVizabel.Controllers
{
    [Microsoft.AspNetCore.Authorization.Authorize]
    public class NaturezaOperacaoController : Controller
    {
        private readonly ApplicationDbContext _context;

        public NaturezaOperacaoController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: NaturezaOperacao
        [AuthorizePermission("visualizar")]
        public async Task<IActionResult> Index()
        {
            return View(await _context.NFeNaturezaOperacoes.OrderBy(n => n.Descricao).ToListAsync());
        }

        // GET: NaturezaOperacao/Details/5
        [AuthorizePermission("visualizar")]
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var naturezaOperacao = await _context.NFeNaturezaOperacoes
                .FirstOrDefaultAsync(m => m.IdNatureza == id);
            if (naturezaOperacao == null)
            {
                return NotFound();
            }

            return View(naturezaOperacao);
        }

        // GET: NaturezaOperacao/Create
        [AuthorizePermission("incluir")]
        public IActionResult Create()
        {
            return View();
        }

        // POST: NaturezaOperacao/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        [AuthorizePermission("incluir")]
        public async Task<IActionResult> Create([Bind("IdNatureza,Descricao,Ativo")] NFeNaturezaOperacao naturezaOperacao)
        {
            if (ModelState.IsValid)
            {
                _context.Add(naturezaOperacao);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Natureza de Operação cadastrada com sucesso!";
                return RedirectToAction(nameof(Index));
            }
            return View(naturezaOperacao);
        }

        // GET: NaturezaOperacao/Edit/5
        [AuthorizePermission("alterar")]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var naturezaOperacao = await _context.NFeNaturezaOperacoes.FindAsync(id);
            if (naturezaOperacao == null)
            {
                return NotFound();
            }
            return View(naturezaOperacao);
        }

        // POST: NaturezaOperacao/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [AuthorizePermission("alterar")]
        public async Task<IActionResult> Edit(int id, [Bind("IdNatureza,Descricao,Ativo")] NFeNaturezaOperacao naturezaOperacao)
        {
            if (id != naturezaOperacao.IdNatureza)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(naturezaOperacao);
                    await _context.SaveChangesAsync();
                    TempData["SuccessMessage"] = "Natureza de Operação atualizada com sucesso!";
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!NaturezaOperacaoExists(naturezaOperacao.IdNatureza))
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
            return View(naturezaOperacao);
        }

        // GET: NaturezaOperacao/Delete/5
        [AuthorizePermission("excluir")]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var naturezaOperacao = await _context.NFeNaturezaOperacoes
                .FirstOrDefaultAsync(m => m.IdNatureza == id);
            if (naturezaOperacao == null)
            {
                return NotFound();
            }

            return View(naturezaOperacao);
        }

        // POST: NaturezaOperacao/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [AuthorizePermission("excluir")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var naturezaOperacao = await _context.NFeNaturezaOperacoes.FindAsync(id);
            if (naturezaOperacao != null)
            {
                _context.NFeNaturezaOperacoes.Remove(naturezaOperacao);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Natureza de Operação excluída com sucesso!";
            }

            return RedirectToAction(nameof(Index));
        }

        private bool NaturezaOperacaoExists(int id)
        {
            return _context.NFeNaturezaOperacoes.Any(e => e.IdNatureza == id);
        }
    }
}







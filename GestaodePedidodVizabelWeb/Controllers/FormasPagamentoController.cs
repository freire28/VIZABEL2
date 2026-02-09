using Microsoft.AspNetCore.Mvc;
using GestaoPedidosVizabel.Attributes;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using GestaoPedidosVizabel.Data;
using GestaoPedidosVizabel.Models;

namespace GestaoPedidosVizabel.Controllers
{
    [Microsoft.AspNetCore.Authorization.Authorize]
    public class FormasPagamentoController : Controller
    {
        private readonly ApplicationDbContext _context;

        public FormasPagamentoController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: FormasPagamento
        [AuthorizePermission("visualizar")]
        public async Task<IActionResult> Index()
        {
            return View(await _context.FormasPagamento.ToListAsync());
        }

        // GET: FormasPagamento/Details/5
        [AuthorizePermission("visualizar")]
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var formaPagamento = await _context.FormasPagamento
                .FirstOrDefaultAsync(m => m.IdFormapagamento == id);
            if (formaPagamento == null)
            {
                return NotFound();
            }

            return View(formaPagamento);
        }

        // GET: FormasPagamento/Create
        [AuthorizePermission("incluir")]
        public IActionResult Create()
        {
            return View();
        }

        // POST: FormasPagamento/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        [AuthorizePermission("incluir")]
        public async Task<IActionResult> Create([Bind("IdFormapagamento,Descricao")] FormaPagamento formaPagamento)
        {
            // Tratar checkbox
            var ativo = Request.Form["Ativo"].ToString();
            formaPagamento.Ativo = ativo == "true";

            if (ModelState.IsValid)
            {
                _context.Add(formaPagamento);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Forma de pagamento cadastrada com sucesso!";
                return RedirectToAction(nameof(Index));
            }
            return View(formaPagamento);
        }

        // GET: FormasPagamento/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var formaPagamento = await _context.FormasPagamento.FindAsync(id);
            if (formaPagamento == null)
            {
                return NotFound();
            }
            return View(formaPagamento);
        }

        // POST: FormasPagamento/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [AuthorizePermission("incluir")]
        public async Task<IActionResult> Edit(int id, [Bind("IdFormapagamento,Descricao")] FormaPagamento formaPagamento)
        {
            if (id != formaPagamento.IdFormapagamento)
            {
                return NotFound();
            }

            // Tratar checkbox
            var ativo = Request.Form["Ativo"].ToString();
            formaPagamento.Ativo = ativo == "true";

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(formaPagamento);
                    await _context.SaveChangesAsync();
                    TempData["SuccessMessage"] = "Forma de pagamento atualizada com sucesso!";
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!FormaPagamentoExists(formaPagamento.IdFormapagamento))
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
            return View(formaPagamento);
        }

        // GET: FormasPagamento/Delete/5
        [AuthorizePermission("excluir")]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var formaPagamento = await _context.FormasPagamento
                .FirstOrDefaultAsync(m => m.IdFormapagamento == id);
            if (formaPagamento == null)
            {
                return NotFound();
            }

            return View(formaPagamento);
        }

        // POST: FormasPagamento/Delete/5
        [AuthorizePermission("excluir")]
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var formaPagamento = await _context.FormasPagamento.FindAsync(id);
            if (formaPagamento != null)
            {
                _context.FormasPagamento.Remove(formaPagamento);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Forma de pagamento excluída com sucesso!";
            }

            return RedirectToAction(nameof(Index));
        }

        private bool FormaPagamentoExists(int id)
        {
            return _context.FormasPagamento.Any(e => e.IdFormapagamento == id);
        }
    }
}





















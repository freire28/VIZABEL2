using Microsoft.AspNetCore.Mvc;
using GestaoPedidosVizabel.Attributes;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using GestaoPedidosVizabel.Data;
using GestaoPedidosVizabel.Models;

namespace GestaoPedidosVizabel.Controllers
{
    [Microsoft.AspNetCore.Authorization.Authorize]
    public class TamanhoProdutosController : Controller
    {
        private readonly ApplicationDbContext _context;

        public TamanhoProdutosController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: TamanhoProdutos
        [AuthorizePermission("visualizar")]
        public async Task<IActionResult> Index()
        {
            return View(await _context.TamanhoProdutos.ToListAsync());
        }

        // GET: TamanhoProdutos/Details/5
        [AuthorizePermission("visualizar")]
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var tamanhoProduto = await _context.TamanhoProdutos
                .FirstOrDefaultAsync(m => m.IdTamanhoproduto == id);
            if (tamanhoProduto == null)
            {
                return NotFound();
            }

            return View(tamanhoProduto);
        }

        // GET: TamanhoProdutos/Create
        [AuthorizePermission("incluir")]
        public IActionResult Create()
        {
            return View();
        }

        // POST: TamanhoProdutos/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        [AuthorizePermission("incluir")]
        public async Task<IActionResult> Create([Bind("IdTamanhoproduto,Tamanho")] TamanhoProduto tamanhoProduto)
        {
            // Tratar checkbox nullable
            var ativo = Request.Form["Ativo"].ToString();
            tamanhoProduto.Ativo = ativo == "true" ? true : false;

            if (ModelState.IsValid)
            {
                _context.Add(tamanhoProduto);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Tamanho de produto cadastrado com sucesso!";
                return RedirectToAction(nameof(Index));
            }
            return View(tamanhoProduto);
        }

        // GET: TamanhoProdutos/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var tamanhoProduto = await _context.TamanhoProdutos.FindAsync(id);
            if (tamanhoProduto == null)
            {
                return NotFound();
            }
            return View(tamanhoProduto);
        }

        // POST: TamanhoProdutos/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [AuthorizePermission("incluir")]
        public async Task<IActionResult> Edit(int id, [Bind("IdTamanhoproduto,Tamanho")] TamanhoProduto tamanhoProduto)
        {
            if (id != tamanhoProduto.IdTamanhoproduto)
            {
                return NotFound();
            }

            // Tratar checkbox nullable
            var ativo = Request.Form["Ativo"].ToString();
            tamanhoProduto.Ativo = ativo == "true" ? true : false;

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(tamanhoProduto);
                    await _context.SaveChangesAsync();
                    TempData["SuccessMessage"] = "Tamanho de produto atualizado com sucesso!";
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!TamanhoProdutoExists(tamanhoProduto.IdTamanhoproduto))
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
            return View(tamanhoProduto);
        }

        // GET: TamanhoProdutos/Delete/5
        [AuthorizePermission("excluir")]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var tamanhoProduto = await _context.TamanhoProdutos
                .FirstOrDefaultAsync(m => m.IdTamanhoproduto == id);
            if (tamanhoProduto == null)
            {
                return NotFound();
            }

            return View(tamanhoProduto);
        }

        // POST: TamanhoProdutos/Delete/5
        [AuthorizePermission("excluir")]
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var tamanhoProduto = await _context.TamanhoProdutos.FindAsync(id);
            if (tamanhoProduto != null)
            {
                _context.TamanhoProdutos.Remove(tamanhoProduto);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Tamanho de produto excluído com sucesso!";
            }

            return RedirectToAction(nameof(Index));
        }

        private bool TamanhoProdutoExists(int id)
        {
            return _context.TamanhoProdutos.Any(e => e.IdTamanhoproduto == id);
        }
    }
}





















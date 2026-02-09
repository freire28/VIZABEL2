using Microsoft.AspNetCore.Mvc;
using GestaoPedidosVizabel.Attributes;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using GestaoPedidosVizabel.Data;
using GestaoPedidosVizabel.Models;

namespace GestaoPedidosVizabel.Controllers
{
    [Microsoft.AspNetCore.Authorization.Authorize]
    public class FuncoesController : Controller
    {
        private readonly ApplicationDbContext _context;

        public FuncoesController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Funcoes
        [AuthorizePermission("visualizar")]
        public async Task<IActionResult> Index()
        {
            return View(await _context.Funcoes.ToListAsync());
        }

        // GET: Funcoes/Details/5
        [AuthorizePermission("visualizar")]
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var funcao = await _context.Funcoes
                .FirstOrDefaultAsync(m => m.IdFuncao == id);
            if (funcao == null)
            {
                return NotFound();
            }

            return View(funcao);
        }

        // GET: Funcoes/Create
        [AuthorizePermission("incluir")]
        public IActionResult Create()
        {
            return View();
        }

        // POST: Funcoes/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        [AuthorizePermission("incluir")]
        public async Task<IActionResult> Create([Bind("IdFuncao,Descricao")] Funcao funcao)
        {
            // Tratar checkbox
            var ativo = Request.Form["Ativo"].ToString();
            funcao.Ativo = ativo == "true";

            if (ModelState.IsValid)
            {
                _context.Add(funcao);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Cargo/Função cadastrado com sucesso!";
                return RedirectToAction(nameof(Index));
            }
            return View(funcao);
        }

        // GET: Funcoes/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var funcao = await _context.Funcoes.FindAsync(id);
            if (funcao == null)
            {
                return NotFound();
            }
            return View(funcao);
        }

        // POST: Funcoes/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [AuthorizePermission("incluir")]
        public async Task<IActionResult> Edit(int id, [Bind("IdFuncao,Descricao")] Funcao funcao)
        {
            if (id != funcao.IdFuncao)
            {
                return NotFound();
            }

            // Tratar checkbox
            var ativo = Request.Form["Ativo"].ToString();
            funcao.Ativo = ativo == "true";

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(funcao);
                    await _context.SaveChangesAsync();
                    TempData["SuccessMessage"] = "Cargo/Função atualizado com sucesso!";
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!FuncaoExists(funcao.IdFuncao))
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
            return View(funcao);
        }

        // GET: Funcoes/Delete/5
        [AuthorizePermission("excluir")]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var funcao = await _context.Funcoes
                .FirstOrDefaultAsync(m => m.IdFuncao == id);
            if (funcao == null)
            {
                return NotFound();
            }

            return View(funcao);
        }

        // POST: Funcoes/Delete/5
        [AuthorizePermission("excluir")]
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var funcao = await _context.Funcoes.FindAsync(id);
            if (funcao != null)
            {
                _context.Funcoes.Remove(funcao);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Cargo/Função excluído com sucesso!";
            }

            return RedirectToAction(nameof(Index));
        }

        private bool FuncaoExists(int id)
        {
            return _context.Funcoes.Any(e => e.IdFuncao == id);
        }
    }
}





















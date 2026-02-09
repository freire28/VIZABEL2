using Microsoft.AspNetCore.Mvc;
using GestaoPedidosVizabel.Attributes;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using GestaoPedidosVizabel.Data;
using GestaoPedidosVizabel.Models;

namespace GestaoPedidosVizabel.Controllers
{
    [Microsoft.AspNetCore.Authorization.Authorize]
    public class ConfiguracoesController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ConfiguracoesController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Configuracoes
        [AuthorizePermission("visualizar")]
        public async Task<IActionResult> Index()
        {
            return View(await _context.Configuracoes.ToListAsync());
        }

        // GET: Configuracoes/Details/5
        [AuthorizePermission("visualizar")]
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var configuracao = await _context.Configuracoes
                .FirstOrDefaultAsync(m => m.IdConfiguracao == id);
            if (configuracao == null)
            {
                return NotFound();
            }

            return View(configuracao);
        }

        // GET: Configuracoes/Create
        [AuthorizePermission("incluir")]
        public IActionResult Create()
        {
            return View();
        }

        // POST: Configuracoes/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        [AuthorizePermission("incluir")]
        public async Task<IActionResult> Create([Bind("IdConfiguracao,Chave,Descricao,Valor,Ativo")] Configuracao configuracao)
        {
            // Tratar checkbox nullable
            var considerarNoPrazo = Request.Form["ConsiderarNoPrazoEntrega"].ToString();
            configuracao.ConsiderarNoPrazoEntrega = considerarNoPrazo == "true" ? true : false;

            if (ModelState.IsValid)
            {
                _context.Add(configuracao);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Configuração cadastrada com sucesso!";
                return RedirectToAction(nameof(Index));
            }
            return View(configuracao);
        }

        // GET: Configuracoes/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var configuracao = await _context.Configuracoes.FindAsync(id);
            if (configuracao == null)
            {
                return NotFound();
            }
            return View(configuracao);
        }

        // POST: Configuracoes/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [AuthorizePermission("incluir")]
        public async Task<IActionResult> Edit(int id, [Bind("IdConfiguracao,Chave,Descricao,Valor,Ativo")] Configuracao configuracao)
        {
            if (id != configuracao.IdConfiguracao)
            {
                return NotFound();
            }

            // Tratar checkbox nullable
            var considerarNoPrazo = Request.Form["ConsiderarNoPrazoEntrega"].ToString();
            configuracao.ConsiderarNoPrazoEntrega = considerarNoPrazo == "true" ? true : false;

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(configuracao);
                    await _context.SaveChangesAsync();
                    TempData["SuccessMessage"] = "Configuração atualizada com sucesso!";
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!ConfiguracaoExists(configuracao.IdConfiguracao))
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
            return View(configuracao);
        }

        // GET: Configuracoes/Delete/5
        [AuthorizePermission("excluir")]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var configuracao = await _context.Configuracoes
                .FirstOrDefaultAsync(m => m.IdConfiguracao == id);
            if (configuracao == null)
            {
                return NotFound();
            }

            return View(configuracao);
        }

        // POST: Configuracoes/Delete/5
        [AuthorizePermission("excluir")]
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var configuracao = await _context.Configuracoes.FindAsync(id);
            if (configuracao != null)
            {
                _context.Configuracoes.Remove(configuracao);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Configuração excluída com sucesso!";
            }

            return RedirectToAction(nameof(Index));
        }

        private bool ConfiguracaoExists(int id)
        {
            return _context.Configuracoes.Any(e => e.IdConfiguracao == id);
        }
    }
}


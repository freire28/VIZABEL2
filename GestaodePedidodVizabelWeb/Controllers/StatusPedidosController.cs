using Microsoft.AspNetCore.Mvc;
using GestaoPedidosVizabel.Attributes;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using GestaoPedidosVizabel.Data;
using GestaoPedidosVizabel.Models;

namespace GestaoPedidosVizabel.Controllers
{
    [Microsoft.AspNetCore.Authorization.Authorize]
    public class StatusPedidosController : Controller
    {
        private readonly ApplicationDbContext _context;

        public StatusPedidosController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: StatusPedidos
        [AuthorizePermission("visualizar")]
        public async Task<IActionResult> Index()
        {
            return View(await _context.StatusPedidos.ToListAsync());
        }

        // GET: StatusPedidos/Details/5
        [AuthorizePermission("visualizar")]
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var statusPedido = await _context.StatusPedidos
                .FirstOrDefaultAsync(m => m.IdStatuspedido == id);
            if (statusPedido == null)
            {
                return NotFound();
            }

            return View(statusPedido);
        }

        // GET: StatusPedidos/Create
        [AuthorizePermission("incluir")]
        public IActionResult Create()
        {
            return View();
        }

        // POST: StatusPedidos/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        [AuthorizePermission("incluir")]
        public async Task<IActionResult> Create([Bind("IdStatuspedido,Descricao")] StatusPedido statusPedido)
        {
            // Tratar checkbox
            var ativo = Request.Form["Ativo"].ToString();
            statusPedido.Ativo = ativo == "true";

            if (ModelState.IsValid)
            {
                _context.Add(statusPedido);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Status de pedido cadastrado com sucesso!";
                return RedirectToAction(nameof(Index));
            }
            return View(statusPedido);
        }

        // GET: StatusPedidos/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var statusPedido = await _context.StatusPedidos.FindAsync(id);
            if (statusPedido == null)
            {
                return NotFound();
            }
            return View(statusPedido);
        }

        // POST: StatusPedidos/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [AuthorizePermission("incluir")]
        public async Task<IActionResult> Edit(int id, [Bind("IdStatuspedido,Descricao")] StatusPedido statusPedido)
        {
            if (id != statusPedido.IdStatuspedido)
            {
                return NotFound();
            }

            // Tratar checkbox
            var ativo = Request.Form["Ativo"].ToString();
            statusPedido.Ativo = ativo == "true";

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(statusPedido);
                    await _context.SaveChangesAsync();
                    TempData["SuccessMessage"] = "Status de pedido atualizado com sucesso!";
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!StatusPedidoExists(statusPedido.IdStatuspedido))
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
            return View(statusPedido);
        }

        // GET: StatusPedidos/Delete/5
        [AuthorizePermission("excluir")]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var statusPedido = await _context.StatusPedidos
                .FirstOrDefaultAsync(m => m.IdStatuspedido == id);
            if (statusPedido == null)
            {
                return NotFound();
            }

            return View(statusPedido);
        }

        // POST: StatusPedidos/Delete/5
        [AuthorizePermission("excluir")]
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var statusPedido = await _context.StatusPedidos.FindAsync(id);
            if (statusPedido != null)
            {
                _context.StatusPedidos.Remove(statusPedido);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Status de pedido excluído com sucesso!";
            }

            return RedirectToAction(nameof(Index));
        }

        private bool StatusPedidoExists(int id)
        {
            return _context.StatusPedidos.Any(e => e.IdStatuspedido == id);
        }
    }
}





















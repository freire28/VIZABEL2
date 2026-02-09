using Microsoft.AspNetCore.Mvc;
using GestaoPedidosVizabel.Attributes;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using GestaoPedidosVizabel.Data;
using GestaoPedidosVizabel.Models;

namespace GestaoPedidosVizabel.Controllers
{
    [Microsoft.AspNetCore.Authorization.Authorize]
    public class GradesController : Controller
    {
        private readonly ApplicationDbContext _context;

        public GradesController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Grades
        [AuthorizePermission("visualizar")]
        public async Task<IActionResult> Index()
        {
            return View(await _context.Grades.ToListAsync());
        }

        // GET: Grades/Details/5
        [AuthorizePermission("visualizar")]
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var grade = await _context.Grades
                .Include(g => g.GradeTamanhos)
                    .ThenInclude(gt => gt.TamanhoProduto)
                .FirstOrDefaultAsync(m => m.IdGrade == id);
            if (grade == null)
            {
                return NotFound();
            }

            // Carregar todos os tamanhos disponíveis
            ViewBag.TamanhosDisponiveis = await _context.TamanhoProdutos
                .Where(t => t.Ativo == true)
                .OrderBy(t => t.Tamanho)
                .ToListAsync();

            return View(grade);
        }

        // POST: Grades/AdicionarTamanho
        [HttpPost]
        [ValidateAntiForgeryToken]
        [AuthorizePermission("incluir")]
        public async Task<IActionResult> AdicionarTamanho(int idGrade, int idTamanho)
        {
            // Verificar se já existe (idTamanho na tabela GRADE_TAMANHOS corresponde ao IdTamanhoproduto)
            var existe = await _context.GradeTamanhos
                .AnyAsync(gt => gt.IdGrade == idGrade && gt.IdTamanho == idTamanho);

            if (existe)
            {
                TempData["ErrorMessage"] = "Este tamanho já está adicionado nesta grade.";
                return RedirectToAction(nameof(Details), new { id = idGrade });
            }

            var gradeTamanho = new GradeTamanho
            {
                IdGrade = idGrade,
                IdTamanho = idTamanho // Este valor corresponde ao IdTamanhoproduto
            };

            _context.GradeTamanhos.Add(gradeTamanho);
            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Tamanho adicionado com sucesso!";

            return RedirectToAction(nameof(Details), new { id = idGrade });
        }

        // POST: Grades/RemoverTamanho
        [HttpPost]
        [ValidateAntiForgeryToken]
        [AuthorizePermission("incluir")]
        public async Task<IActionResult> RemoverTamanho(int idGrade, int idGradeTamanho)
        {
            var gradeTamanho = await _context.GradeTamanhos
                .FirstOrDefaultAsync(gt => gt.IdGradeTamanho == idGradeTamanho && gt.IdGrade == idGrade);

            if (gradeTamanho != null)
            {
                _context.GradeTamanhos.Remove(gradeTamanho);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Tamanho removido com sucesso!";
            }

            return RedirectToAction(nameof(Details), new { id = idGrade });
        }

        // GET: Grades/Create
        [AuthorizePermission("incluir")]
        public IActionResult Create()
        {
            return View();
        }

        // POST: Grades/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        [AuthorizePermission("incluir")]
        public async Task<IActionResult> Create([Bind("IdGrade,Descricao")] Grade grade)
        {
            // Tratar checkbox nullable
            var ativo = Request.Form["Ativo"].ToString();
            grade.Ativo = ativo == "true" ? true : false;

            if (ModelState.IsValid)
            {
                _context.Add(grade);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Grade cadastrada com sucesso!";
                return RedirectToAction(nameof(Index));
            }
            return View(grade);
        }

        // GET: Grades/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var grade = await _context.Grades.FindAsync(id);
            if (grade == null)
            {
                return NotFound();
            }
            return View(grade);
        }

        // POST: Grades/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [AuthorizePermission("incluir")]
        public async Task<IActionResult> Edit(int id, [Bind("IdGrade,Descricao")] Grade grade)
        {
            if (id != grade.IdGrade)
            {
                return NotFound();
            }

            // Tratar checkbox nullable
            var ativo = Request.Form["Ativo"].ToString();
            grade.Ativo = ativo == "true" ? true : false;

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(grade);
                    await _context.SaveChangesAsync();
                    TempData["SuccessMessage"] = "Grade atualizada com sucesso!";
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!GradeExists(grade.IdGrade))
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
            return View(grade);
        }

        // GET: Grades/Delete/5
        [AuthorizePermission("excluir")]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var grade = await _context.Grades
                .FirstOrDefaultAsync(m => m.IdGrade == id);
            if (grade == null)
            {
                return NotFound();
            }

            return View(grade);
        }

        // POST: Grades/Delete/5
        [AuthorizePermission("excluir")]
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var grade = await _context.Grades.FindAsync(id);
            if (grade != null)
            {
                _context.Grades.Remove(grade);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Grade excluída com sucesso!";
            }

            return RedirectToAction(nameof(Index));
        }

        private bool GradeExists(int id)
        {
            return _context.Grades.Any(e => e.IdGrade == id);
        }
    }
}


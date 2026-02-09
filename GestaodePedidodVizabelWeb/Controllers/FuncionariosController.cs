using Microsoft.AspNetCore.Mvc;
using GestaoPedidosVizabel.Attributes;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using GestaoPedidosVizabel.Data;
using GestaoPedidosVizabel.Models;

namespace GestaoPedidosVizabel.Controllers
{
    [Microsoft.AspNetCore.Authorization.Authorize]
    public class FuncionariosController : Controller
    {
        private readonly ApplicationDbContext _context;

        public FuncionariosController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Funcionarios
        [AuthorizePermission("visualizar")]
        public async Task<IActionResult> Index()
        {
            var funcionarios = await _context.Funcionarios.ToListAsync();
            var funcoes = await _context.Funcoes.ToListAsync();
            
            // Criar dicionário para lookup rápido
            ViewBag.FuncoesDict = funcoes.ToDictionary(f => f.IdFuncao, f => f.Descricao);
            
            return View(funcionarios);
        }

        // GET: Funcionarios/Details/5
        [AuthorizePermission("visualizar")]
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var funcionario = await _context.Funcionarios
                .Include(f => f.FuncionarioFuncoes)
                    .ThenInclude(ff => ff.Funcao)
                .FirstOrDefaultAsync(m => m.IdFuncionario == id);
            if (funcionario == null)
            {
                return NotFound();
            }

            // Carregar função principal relacionada se existir
            if (funcionario.FuncaoPrincipal.HasValue)
            {
                ViewBag.Funcao = await _context.Funcoes
                    .FirstOrDefaultAsync(f => f.IdFuncao == funcionario.FuncaoPrincipal.Value);
            }

            // Carregar todas as funções disponíveis (apenas ativas)
            var funcoesDisponiveis = await _context.Funcoes
                .Where(f => f.Ativo)
                .OrderBy(f => f.Descricao)
                .ToListAsync();
            ViewBag.FuncoesDisponiveis = funcoesDisponiveis;

            return View(funcionario);
        }

        // POST: Funcionarios/AdicionarFuncao
        [HttpPost]
        [ValidateAntiForgeryToken]
        [AuthorizePermission("incluir")]
        public async Task<IActionResult> AdicionarFuncao(int idFuncionario, int idFuncao)
        {
            // Verificar se já existe
            var existe = await _context.FuncionarioFuncoes
                .AnyAsync(ff => ff.IdFuncionario == idFuncionario && ff.IdFuncao == idFuncao);

            if (existe)
            {
                TempData["ErrorMessage"] = "Esta função já está adicionada para este funcionário.";
                return RedirectToAction(nameof(Details), new { id = idFuncionario });
            }

            var funcionarioFuncao = new FuncionarioFuncao
            {
                IdFuncionario = idFuncionario,
                IdFuncao = idFuncao,
                Ativo = true
            };

            _context.FuncionarioFuncoes.Add(funcionarioFuncao);
            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Função adicionada com sucesso!";

            return RedirectToAction(nameof(Details), new { id = idFuncionario });
        }

        // POST: Funcionarios/RemoverFuncao
        [HttpPost]
        [ValidateAntiForgeryToken]
        [AuthorizePermission("incluir")]
        public async Task<IActionResult> RemoverFuncao(int idFuncionario, int idFuncionarioFuncao)
        {
            var funcionarioFuncao = await _context.FuncionarioFuncoes
                .FirstOrDefaultAsync(ff => ff.IdFuncionarioFuncao == idFuncionarioFuncao && ff.IdFuncionario == idFuncionario);

            if (funcionarioFuncao != null)
            {
                _context.FuncionarioFuncoes.Remove(funcionarioFuncao);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Função removida com sucesso!";
            }

            return RedirectToAction(nameof(Details), new { id = idFuncionario });
        }

        // POST: Funcionarios/ToggleFuncaoAtivo
        [HttpPost]
        [ValidateAntiForgeryToken]
        [AuthorizePermission("incluir")]
        public async Task<IActionResult> ToggleFuncaoAtivo(int idFuncionario, int idFuncionarioFuncao)
        {
            var funcionarioFuncao = await _context.FuncionarioFuncoes
                .FirstOrDefaultAsync(ff => ff.IdFuncionarioFuncao == idFuncionarioFuncao && ff.IdFuncionario == idFuncionario);

            if (funcionarioFuncao != null)
            {
                funcionarioFuncao.Ativo = !funcionarioFuncao.Ativo;
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = $"Função {(funcionarioFuncao.Ativo ? "ativada" : "desativada")} com sucesso!";
            }

            return RedirectToAction(nameof(Details), new { id = idFuncionario });
        }

        // GET: Funcionarios/Create
        public async Task<IActionResult> Create()
        {
            // Carregar funções ativas para o dropdown
            ViewBag.Funcoes = await _context.Funcoes
                .Where(f => f.Ativo)
                .OrderBy(f => f.Descricao)
                .ToListAsync();
            return View();
        }

        // POST: Funcionarios/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        [AuthorizePermission("incluir")]
        public async Task<IActionResult> Create([Bind("IdFuncionario,Nome,Celular,Pin")] Funcionario funcionario)
        {
            // Tratar checkboxes
            var ativo = Request.Form["Ativo"].ToString();
            funcionario.Ativo = ativo == "true";

            var vendedor = Request.Form["Vendedor"].ToString();
            funcionario.Vendedor = vendedor == "true" ? true : null;

            // Tratar FuncaoPrincipal nullable
            var funcaoPrincipalStr = Request.Form["FuncaoPrincipal"].ToString();
            if (string.IsNullOrEmpty(funcaoPrincipalStr))
            {
                funcionario.FuncaoPrincipal = null;
            }
            else
            {
                if (int.TryParse(funcaoPrincipalStr, out int funcaoPrincipal))
                {
                    funcionario.FuncaoPrincipal = funcaoPrincipal;
                }
            }

            if (ModelState.IsValid)
            {
                _context.Add(funcionario);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Funcionário cadastrado com sucesso!";
                return RedirectToAction(nameof(Index));
            }
            
            // Recarregar funções em caso de erro
            ViewBag.Funcoes = await _context.Funcoes
                .Where(f => f.Ativo)
                .OrderBy(f => f.Descricao)
                .ToListAsync();
            return View(funcionario);
        }

        // GET: Funcionarios/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var funcionario = await _context.Funcionarios.FindAsync(id);
            if (funcionario == null)
            {
                return NotFound();
            }
            
            // Carregar funções ativas para o dropdown
            ViewBag.Funcoes = await _context.Funcoes
                .Where(f => f.Ativo)
                .OrderBy(f => f.Descricao)
                .ToListAsync();
            return View(funcionario);
        }

        // POST: Funcionarios/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [AuthorizePermission("incluir")]
        public async Task<IActionResult> Edit(int id, [Bind("IdFuncionario,Nome,Celular,Pin")] Funcionario funcionario)
        {
            if (id != funcionario.IdFuncionario)
            {
                return NotFound();
            }

            // Tratar checkboxes
            var ativo = Request.Form["Ativo"].ToString();
            funcionario.Ativo = ativo == "true";

            var vendedor = Request.Form["Vendedor"].ToString();
            funcionario.Vendedor = vendedor == "true" ? true : null;

            // Tratar FuncaoPrincipal nullable
            var funcaoPrincipalStr = Request.Form["FuncaoPrincipal"].ToString();
            if (string.IsNullOrEmpty(funcaoPrincipalStr))
            {
                funcionario.FuncaoPrincipal = null;
            }
            else
            {
                if (int.TryParse(funcaoPrincipalStr, out int funcaoPrincipal))
                {
                    funcionario.FuncaoPrincipal = funcaoPrincipal;
                }
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(funcionario);
                    await _context.SaveChangesAsync();
                    TempData["SuccessMessage"] = "Funcionário atualizado com sucesso!";
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!FuncionarioExists(funcionario.IdFuncionario))
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
            return View(funcionario);
        }

        // GET: Funcionarios/Delete/5
        [AuthorizePermission("excluir")]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var funcionario = await _context.Funcionarios
                .FirstOrDefaultAsync(m => m.IdFuncionario == id);
            if (funcionario == null)
            {
                return NotFound();
            }

            // Carregar função relacionada se existir
            if (funcionario.FuncaoPrincipal.HasValue)
            {
                ViewBag.Funcao = await _context.Funcoes
                    .FirstOrDefaultAsync(f => f.IdFuncao == funcionario.FuncaoPrincipal.Value);
            }

            return View(funcionario);
        }

        // POST: Funcionarios/Delete/5
        [AuthorizePermission("excluir")]
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var funcionario = await _context.Funcionarios.FindAsync(id);
            if (funcionario != null)
            {
                _context.Funcionarios.Remove(funcionario);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Funcionário excluído com sucesso!";
            }

            return RedirectToAction(nameof(Index));
        }

        private bool FuncionarioExists(int id)
        {
            return _context.Funcionarios.Any(e => e.IdFuncionario == id);
        }
    }
}


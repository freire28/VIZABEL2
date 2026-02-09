using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using GestaoPedidosVizabel.Data;
using GestaoPedidosVizabel.Models;
using GestaoPedidosVizabel.Services;
using System.Security.Claims;

namespace GestaoPedidosVizabel.Controllers
{
    [AllowAnonymous]
    public class AccountController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly PasswordHasherService _passwordHasher;
        private readonly PermissionService _permissionService;

        public AccountController(ApplicationDbContext context, PasswordHasherService passwordHasher, PermissionService permissionService)
        {
            _context = context;
            _passwordHasher = passwordHasher;
            _permissionService = permissionService;
        }

        // GET: Account/Login
        public IActionResult Login(string? returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;
            return View();
        }

        // POST: Account/Login
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(string login, string senha, string? returnUrl = null)
        {
            try
            {
                if (string.IsNullOrEmpty(login) || string.IsNullOrEmpty(senha))
                {
                    ModelState.AddModelError("", "Login e senha são obrigatórios.");
                    return View();
                }

                var usuario = await _context.Usuarios
                    .FirstOrDefaultAsync(u => u.Login == login && u.Ativo);

                if (usuario == null || !_passwordHasher.VerifyPassword(senha, usuario.Senha))
                {
                    ModelState.AddModelError("", "Login ou senha inválidos.");
                    return View();
                }

                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.NameIdentifier, usuario.IdUsuario.ToString()),
                    new Claim(ClaimTypes.Name, usuario.Nome),
                    new Claim(ClaimTypes.Email, usuario.Email ?? ""),
                    new Claim("IsAdmin", usuario.Administrador.ToString())
                };

                var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                var authProperties = new AuthenticationProperties
                {
                    IsPersistent = true,
                    ExpiresUtc = DateTimeOffset.UtcNow.AddHours(8)
                };

                await HttpContext.SignInAsync(
                    CookieAuthenticationDefaults.AuthenticationScheme,
                    new ClaimsPrincipal(claimsIdentity),
                    authProperties);

                // Inicializar permissões se necessário
                await _permissionService.InitializePermissionsAsync();

                if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                {
                    return Redirect(returnUrl);
                }

                return RedirectToAction("Index", "Home");
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", $"Erro ao fazer login: {ex.Message}");
                return View();
            }
        }

        // POST: Account/Logout
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Login", "Account");
        }

        // GET: Account/AccessDenied
        public IActionResult AccessDenied()
        {
            return View();
        }

        // GET: Account/Index
        [HttpGet]
        [Authorize]
        public async Task<IActionResult> Index()
        {
            var usuarios = await _context.Usuarios
                .OrderBy(u => u.Nome)
                .ToListAsync();
            return View(usuarios);
        }

        // GET: Account/Create
        [HttpGet]
        [Authorize]
        public IActionResult Create()
        {
            return View();
        }

        // POST: Account/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize]
        public async Task<IActionResult> Create([Bind("Login,Senha,Nome,Email,Administrador,Ativo")] Usuario usuario)
        {
            if (ModelState.IsValid)
            {
                // Verificar se login já existe
                var loginExiste = await _context.Usuarios
                    .AnyAsync(u => u.Login == usuario.Login);

                if (loginExiste)
                {
                    ModelState.AddModelError("Login", "Este login já está em uso.");
                    return View(usuario);
                }

                usuario.Senha = _passwordHasher.HashPassword(usuario.Senha);
                usuario.DataCriacao = DateTime.Now;

                _context.Add(usuario);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Usuário cadastrado com sucesso!";
                return RedirectToAction(nameof(Index));
            }
            return View(usuario);
        }

        // GET: Account/Edit/5
        [HttpGet]
        [Authorize]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var usuario = await _context.Usuarios.FindAsync(id);
            if (usuario == null)
            {
                return NotFound();
            }

            // Não retornar a senha
            usuario.Senha = "";
            return View(usuario);
        }

        // POST: Account/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize]
        public async Task<IActionResult> Edit(int id, [Bind("IdUsuario,Login,Nome,Email,Administrador,Ativo")] Usuario usuario, string? novaSenha)
        {
            if (id != usuario.IdUsuario)
            {
                return NotFound();
            }

            // Remover erros de validação de campos que não estamos editando diretamente
            ModelState.Remove("Senha");
            ModelState.Remove("DataCriacao");
            
            // Se o email estiver vazio, remover erro de validação (é opcional)
            if (string.IsNullOrWhiteSpace(usuario.Email))
            {
                ModelState.Remove("Email");
            }

            // Validar manualmente os campos obrigatórios
            if (string.IsNullOrWhiteSpace(usuario.Login))
            {
                ModelState.AddModelError("Login", "O login é obrigatório.");
            }

            if (string.IsNullOrWhiteSpace(usuario.Nome))
            {
                ModelState.AddModelError("Nome", "O nome é obrigatório.");
            }

            if (ModelState.IsValid)
            {
                try
                {
                    var usuarioDb = await _context.Usuarios.FindAsync(id);
                    if (usuarioDb == null)
                    {
                        return NotFound();
                    }

                    // Verificar se login já existe em outro usuário
                    var loginExiste = await _context.Usuarios
                        .AnyAsync(u => u.Login == usuario.Login && u.IdUsuario != id);

                    if (loginExiste)
                    {
                        ModelState.AddModelError("Login", "Este login já está em uso.");
                        return View(usuario);
                    }

                    // Atualizar propriedades explicitamente
                    usuarioDb.Login = usuario.Login;
                    usuarioDb.Nome = usuario.Nome;
                    usuarioDb.Email = usuario.Email;
                    usuarioDb.Administrador = usuario.Administrador;
                    usuarioDb.Ativo = usuario.Ativo;

                    // Atualizar senha se fornecida
                    if (!string.IsNullOrWhiteSpace(novaSenha))
                    {
                        usuarioDb.Senha = _passwordHasher.HashPassword(novaSenha);
                    }

                    // Marcar propriedades específicas como modificadas
                    _context.Entry(usuarioDb).Property(u => u.Login).IsModified = true;
                    _context.Entry(usuarioDb).Property(u => u.Nome).IsModified = true;
                    _context.Entry(usuarioDb).Property(u => u.Email).IsModified = true;
                    _context.Entry(usuarioDb).Property(u => u.Administrador).IsModified = true;
                    _context.Entry(usuarioDb).Property(u => u.Ativo).IsModified = true;
                    
                    if (!string.IsNullOrWhiteSpace(novaSenha))
                    {
                        _context.Entry(usuarioDb).Property(u => u.Senha).IsModified = true;
                    }

                    var saved = await _context.SaveChangesAsync();
                    
                    if (saved > 0)
                    {
                        TempData["SuccessMessage"] = "Usuário atualizado com sucesso!";
                        return RedirectToAction(nameof(Index));
                    }
                    else
                    {
                        TempData["ErrorMessage"] = "Nenhuma alteração foi detectada.";
                    }
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!UsuarioExists(usuario.IdUsuario))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", $"Erro ao atualizar usuário: {ex.Message}");
                    return View(usuario);
                }
            }
            return View(usuario);
        }

        // GET: Account/Delete/5
        [HttpGet]
        [Authorize]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var usuario = await _context.Usuarios
                .FirstOrDefaultAsync(m => m.IdUsuario == id);
            if (usuario == null)
            {
                return NotFound();
            }

            return View(usuario);
        }

        // POST: Account/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var usuario = await _context.Usuarios.FindAsync(id);
            if (usuario != null)
            {
                _context.Usuarios.Remove(usuario);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Usuário excluído com sucesso!";
            }

            return RedirectToAction(nameof(Index));
        }

        // GET: Account/Permissoes/5
        [HttpGet]
        [Authorize]
        public async Task<IActionResult> Permissoes(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var usuario = await _context.Usuarios
                .Include(u => u.UsuarioPermissoes)
                    .ThenInclude(up => up.Permissao)
                .FirstOrDefaultAsync(u => u.IdUsuario == id);

            if (usuario == null)
            {
                return NotFound();
            }

            // Carregar todas as permissões
            var todasPermissoes = await _context.Permissoes
                .Where(p => p.Ativo)
                .OrderBy(p => p.Controller)
                .ThenBy(p => p.Acao)
                .ToListAsync();

            ViewBag.TodasPermissoes = todasPermissoes;
            return View(usuario);
        }

        // POST: Account/SalvarPermissoes
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize]
        public async Task<IActionResult> SalvarPermissoes(int idUsuario, Dictionary<int, Dictionary<string, bool>> permissoes)
        {
            var usuario = await _context.Usuarios
                .Include(u => u.UsuarioPermissoes)
                .FirstOrDefaultAsync(u => u.IdUsuario == idUsuario);

            if (usuario == null)
            {
                return NotFound();
            }

            // Remover permissões existentes
            _context.UsuarioPermissoes.RemoveRange(usuario.UsuarioPermissoes);

            // Adicionar novas permissões
            foreach (var permissaoItem in permissoes)
            {
                var idPermissao = permissaoItem.Key;
                var permissoesTipo = permissaoItem.Value;

                var usuarioPermissao = new UsuarioPermissao
                {
                    IdUsuario = idUsuario,
                    IdPermissao = idPermissao,
                    PodeVisualizar = permissoesTipo.GetValueOrDefault("visualizar", false),
                    PodeIncluir = permissoesTipo.GetValueOrDefault("incluir", false),
                    PodeAlterar = permissoesTipo.GetValueOrDefault("alterar", false),
                    PodeExcluir = permissoesTipo.GetValueOrDefault("excluir", false)
                };

                _context.UsuarioPermissoes.Add(usuarioPermissao);
            }

            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Permissões salvas com sucesso!";
            return RedirectToAction(nameof(Permissoes), new { id = idUsuario });
        }

        private bool UsuarioExists(int id)
        {
            return _context.Usuarios.Any(e => e.IdUsuario == id);
        }
    }
}


using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using GestaoPedidosVizabel.Data;
using GestaoPedidosVizabel.Models;
using GestaoPedidosVizabel.Models.Validacoes;
using GestaoPedidosVizabel.Attributes;
using GestaoPedidosVizabel.Services;

namespace GestaoPedidosVizabel.Controllers
{
    [Authorize]
    public class ClientesController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly INuvemFiscalService _nuvemFiscalService;

        public ClientesController(ApplicationDbContext context, INuvemFiscalService nuvemFiscalService)
        {
            _context = context;
            _nuvemFiscalService = nuvemFiscalService;
        }

        // GET: Clientes
        [AuthorizePermission("visualizar")]
        public async Task<IActionResult> Index(string searchString)
        {
            var clientes = _context.Clientes.AsQueryable();

            if (!string.IsNullOrWhiteSpace(searchString))
            {
                // Remover caracteres especiais do termo de busca para CPF/CNPJ
                var searchStringSemCaracteres = RemoverCaracteresEspeciais(searchString);
                
                // Buscar por nome, email e fantasia normalmente
                var clientesFiltrados = clientes.Where(c =>
                    (c.Nomerazao != null && c.Nomerazao.Contains(searchString)) ||
                    (c.Email != null && c.Email.Contains(searchString)) ||
                    (c.Fantasia != null && c.Fantasia.Contains(searchString)) ||
                    (c.Cpfcnpj != null && c.Cpfcnpj.Contains(searchString))
                );

                var resultado = await clientesFiltrados.ToListAsync();

                // Filtrar em memória para CPF/CNPJ sem caracteres especiais
                // Apenas se o termo de busca contém números (pode ser CPF/CNPJ)
                if (!string.IsNullOrEmpty(searchStringSemCaracteres) && 
                    searchStringSemCaracteres != searchString && 
                    searchStringSemCaracteres.Length >= 3) // Mínimo 3 dígitos para buscar
                {
                    // Buscar apenas clientes que ainda não foram encontrados
                    var idsExistentes = resultado.Select(c => c.IdCliente).ToHashSet();
                    
                    var clientesPorCpfCnpj = await _context.Clientes
                        .Where(c => c.Cpfcnpj != null && !idsExistentes.Contains(c.IdCliente))
                        .ToListAsync();

                    var clientesComCpfCnpjMatch = clientesPorCpfCnpj
                        .Where(c => RemoverCaracteresEspeciais(c.Cpfcnpj).Contains(searchStringSemCaracteres))
                        .ToList();

                    // Combinar resultados
                    resultado = resultado.Concat(clientesComCpfCnpjMatch).ToList();
                }

                ViewBag.SearchString = searchString;
                return View(resultado);
            }

            ViewBag.SearchString = searchString;
            return View(await clientes.ToListAsync());
        }

        /// <summary>
        /// Remove caracteres especiais de CPF/CNPJ (pontos, traços, barras, espaços)
        /// </summary>
        private string RemoverCaracteresEspeciais(string texto)
        {
            if (string.IsNullOrEmpty(texto))
                return string.Empty;

            return new string(texto.Where(c => char.IsDigit(c)).ToArray());
        }

        // GET: Clientes/Details/5
        [AuthorizePermission("visualizar")]
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var cliente = await _context.Clientes
                .FirstOrDefaultAsync(m => m.IdCliente == id);
            if (cliente == null)
            {
                return NotFound();
            }

            return View(cliente);
        }

        // GET: Clientes/Create
        [AuthorizePermission("incluir")]
        public IActionResult Create()
        {
            return View();
        }

        // POST: Clientes/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        [AuthorizePermission("incluir")]
        public async Task<IActionResult> Create([Bind("IdCliente,Nomerazao,TipoPessoa,Fantasia,Cpfcnpj,RgIe,EnderecoLogradouro,EnderecoNumero,EnderecoComplemento,EnderecoBairro,EnderecoCidade,EnderecoUf,EnderecoCep,EnderecoIbge,Email,Fone,Whatsapp,Contato,FoneContato,CodCliente,Ativo")] Cliente cliente)
        {
            // Validação customizada de CPF/CNPJ
            if (!cliente.TipoPessoa) // false = Física
            {
                if (string.IsNullOrWhiteSpace(cliente.Cpfcnpj))
                {
                    ModelState.AddModelError("Cpfcnpj", "CPF é obrigatório para pessoa física.");
                }
                else if (!ValidadorCPFCNPJ.ValidarCPF(cliente.Cpfcnpj))
                {
                    ModelState.AddModelError("Cpfcnpj", "CPF inválido.");
                }
            }
            else // true = Jurídica
            {
                if (string.IsNullOrWhiteSpace(cliente.Cpfcnpj))
                {
                    ModelState.AddModelError("Cpfcnpj", "CNPJ é obrigatório para pessoa jurídica.");
                }
                else if (!ValidadorCPFCNPJ.ValidarCNPJ(cliente.Cpfcnpj))
                {
                    ModelState.AddModelError("Cpfcnpj", "CNPJ inválido.");
                }
            }

            if (ModelState.IsValid)
            {
                _context.Add(cliente);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Cliente cadastrado com sucesso!";
                return RedirectToAction(nameof(Index));
            }
            return View(cliente);
        }

        // GET: Clientes/Edit/5
        [AuthorizePermission("alterar")]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var cliente = await _context.Clientes.FindAsync(id);
            if (cliente == null)
            {
                return NotFound();
            }
            return View(cliente);
        }

        // POST: Clientes/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [AuthorizePermission("alterar")]
        public async Task<IActionResult> Edit(int id, [Bind("IdCliente,Nomerazao,TipoPessoa,Fantasia,Cpfcnpj,RgIe,EnderecoLogradouro,EnderecoNumero,EnderecoComplemento,EnderecoBairro,EnderecoCidade,EnderecoUf,EnderecoCep,EnderecoIbge,Email,Fone,Whatsapp,Contato,FoneContato,CodCliente,Ativo")] Cliente cliente)
        {
            if (id != cliente.IdCliente)
            {
                return NotFound();
            }

            // Validação customizada de CPF/CNPJ
            if (!cliente.TipoPessoa) // false = Física
            {
                if (string.IsNullOrWhiteSpace(cliente.Cpfcnpj))
                {
                    ModelState.AddModelError("Cpfcnpj", "CPF é obrigatório para pessoa física.");
                }
                else if (!ValidadorCPFCNPJ.ValidarCPF(cliente.Cpfcnpj))
                {
                    ModelState.AddModelError("Cpfcnpj", "CPF inválido.");
                }
            }
            else // true = Jurídica
            {
                if (string.IsNullOrWhiteSpace(cliente.Cpfcnpj))
                {
                    ModelState.AddModelError("Cpfcnpj", "CNPJ é obrigatório para pessoa jurídica.");
                }
                else if (!ValidadorCPFCNPJ.ValidarCNPJ(cliente.Cpfcnpj))
                {
                    ModelState.AddModelError("Cpfcnpj", "CNPJ inválido.");
                }
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(cliente);
                    await _context.SaveChangesAsync();
                    TempData["SuccessMessage"] = "Cliente atualizado com sucesso!";
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!ClienteExists(cliente.IdCliente))
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
            return View(cliente);
        }

        // GET: Clientes/Delete/5
        [AuthorizePermission("excluir")]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var cliente = await _context.Clientes
                .FirstOrDefaultAsync(m => m.IdCliente == id);
            if (cliente == null)
            {
                return NotFound();
            }

            return View(cliente);
        }

        // POST: Clientes/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [AuthorizePermission("excluir")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var cliente = await _context.Clientes.FindAsync(id);
            if (cliente != null)
            {
                _context.Clientes.Remove(cliente);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Cliente excluído com sucesso!";
            }

            return RedirectToAction(nameof(Index));
        }

        private bool ClienteExists(int id)
        {
            return _context.Clientes.Any(e => e.IdCliente == id);
        }

        // POST: Clientes/BuscarCep
        [HttpPost]
        [Authorize]
        public async Task<IActionResult> BuscarCep([FromBody] string cep)
        {
            if (string.IsNullOrWhiteSpace(cep))
            {
                return BadRequest(new { success = false, message = "CEP não informado." });
            }

            // Remover caracteres não numéricos
            var cepLimpo = new string(cep.Where(char.IsDigit).ToArray());

            if (cepLimpo.Length != 8)
            {
                return BadRequest(new { success = false, message = "CEP inválido. Deve conter 8 dígitos." });
            }

            try
            {
                var cepData = await _nuvemFiscalService.BuscarCepAsync(cepLimpo);

                if (cepData == null)
                {
                    return NotFound(new { success = false, message = "CEP não encontrado." });
                }

                return Ok(new
                {
                    success = true,
                    data = new
                    {
                        cep = cepData.Cep,
                        logradouro = cepData.Logradouro ?? string.Empty,
                        complemento = cepData.Complemento ?? string.Empty,
                        bairro = cepData.Bairro ?? string.Empty,
                        cidade = cepData.Cidade ?? string.Empty,
                        uf = cepData.Uf ?? string.Empty,
                        ibge = cepData.Ibge ?? string.Empty
                    }
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = $"Erro ao buscar CEP: {ex.Message}" });
            }
        }
    }
}


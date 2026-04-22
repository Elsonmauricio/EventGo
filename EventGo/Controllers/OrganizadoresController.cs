using EventGo.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore; 
using EventGo.Models;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace EventGo.Controllers
{
    [Authorize] // Todas as ações exigem autenticação
    public class OrganizadoresController : Controller
    {
        private readonly ApplicationDbContext _context;

        public OrganizadoresController(ApplicationDbContext context)
        {
            _context = context;
        }

        // ==================== LISTAR TODOS OS ORGANIZADORES ====================
        // GET: Organizadores
        public async Task<IActionResult> Index()
        {
            var organizadores = await _context.Organizadores
                .Include(o => o.Eventos)
                .OrderBy(o => o.Nome)
                .ToListAsync();

            return View(organizadores);
        }

        // ==================== VER DETALHES DO ORGANIZADOR ====================
        // GET: Organizadores/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var organizador = await _context.Organizadores
                .Include(o => o.Eventos)
                    .ThenInclude(e => e.Local)
                .Include(o => o.Eventos)
                    .ThenInclude(e => e.Inscricoes)
                .FirstOrDefaultAsync(o => o.Id == id);

            if (organizador == null)
            {
                return NotFound();
            }

            // Estatísticas do organizador
            ViewBag.TotalEventos = organizador.Eventos.Count;
            ViewBag.EventosFuturos = organizador.Eventos.Count(e => e.Data > DateTime.Now);
            ViewBag.EventosPassados = organizador.Eventos.Count(e => e.Data <= DateTime.Now);
            ViewBag.TotalInscricoes = organizador.Eventos.Sum(e => e.Inscricoes.Count);

            return View(organizador);
        }

        // ==================== CRIAR NOVO ORGANIZADOR ====================
        // GET: Organizadores/Create
        [Authorize(Roles = "Admin")] // Só admins podem criar organizadores
        public IActionResult Create()
        {
            return View();
        }

        // POST: Organizadores/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Create([Bind("Nome,Email,Telefone,Empresa,Website,Bio")] Organizador organizador)
        {
            // Remover validações de navegação
            ModelState.Remove("Eventos");

            if (ModelState.IsValid)
            {
                try
                {
                    // Verificar se já existe organizador com o mesmo email
                    var existe = await _context.Organizadores
                        .AnyAsync(o => o.Email == organizador.Email);

                    if (existe)
                    {
                        ModelState.AddModelError("Email", "Já existe um organizador com este email.");
                        return View(organizador);
                    }

                    // Se o email do user logado for igual, associar automaticamente
                    if (User.Identity.Name == organizador.Email)
                    {
                        organizador.UserId = User.Identity.Name; // Ou o ID do Identity
                    }

                    organizador.DataRegisto = DateTime.Now;

                    _context.Add(organizador);
                    await _context.SaveChangesAsync();

                    TempData["Sucesso"] = "Organizador criado com sucesso!";
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", "Erro ao criar organizador: " + ex.Message);
                }
            }

            return View(organizador);
        }

        // ==================== EDITAR ORGANIZADOR ====================
        // GET: Organizadores/Edit/5
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var organizador = await _context.Organizadores.FindAsync(id);
            if (organizador == null)
            {
                return NotFound();
            }

            return View(organizador);
        }

        // POST: Organizadores/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Nome,Email,Telefone,Empresa,Website,Bio,DataRegisto,UserId")] Organizador organizador)
        {
            if (id != organizador.Id)
            {
                return NotFound();
            }

            // Remover validações de navegação
            ModelState.Remove("Eventos");

            if (ModelState.IsValid)
            {
                try
                {
                    // Verificar se email já existe (exceto o próprio)
                    var existe = await _context.Organizadores
                        .AnyAsync(o => o.Email == organizador.Email && o.Id != organizador.Id);

                    if (existe)
                    {
                        ModelState.AddModelError("Email", "Já existe um organizador com este email.");
                        return View(organizador);
                    }

                    _context.Update(organizador);
                    await _context.SaveChangesAsync();

                    TempData["Sucesso"] = "Organizador atualizado com sucesso!";
                    return RedirectToAction(nameof(Index));
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!OrganizadorExists(organizador.Id))
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
                    ModelState.AddModelError("", "Erro ao atualizar organizador: " + ex.Message);
                }
            }

            return View(organizador);
        }

        // ==================== ELIMINAR ORGANIZADOR ====================
        // GET: Organizadores/Delete/5
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var organizador = await _context.Organizadores
                .Include(o => o.Eventos)
                .FirstOrDefaultAsync(o => o.Id == id);

            if (organizador == null)
            {
                return NotFound();
            }

            // Verificar se tem eventos associados
            ViewBag.TemEventos = organizador.Eventos.Any();

            return View(organizador);
        }

        // POST: Organizadores/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var organizador = await _context.Organizadores
                .Include(o => o.Eventos)
                .FirstOrDefaultAsync(o => o.Id == id);

            if (organizador == null)
            {
                return NotFound();
            }

            try
            {
                // Verificar se tem eventos
                if (organizador.Eventos.Any())
                {
                    TempData["Erro"] = "Não é possível eliminar organizador com eventos associados. Transfira os eventos primeiro.";
                    return RedirectToAction(nameof(Delete), new { id });
                }

                _context.Organizadores.Remove(organizador);
                await _context.SaveChangesAsync();

                TempData["Sucesso"] = "Organizador eliminado com sucesso!";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                TempData["Erro"] = "Erro ao eliminar organizador: " + ex.Message;
                return RedirectToAction(nameof(Delete), new { id });
            }
        }

        // ==================== FUNÇÕES EXTRA ====================

        // GET: Organizadores/MeuPerfil
        public async Task<IActionResult> MeuPerfil()
        {
            // Assumindo que o email do user logado corresponde ao email do organizador
            var userEmail = User.Identity.Name;

            var organizador = await _context.Organizadores
                .Include(o => o.Eventos)
                    .ThenInclude(e => e.Local)
                .Include(o => o.Eventos)
                    .ThenInclude(e => e.Inscricoes)
                .FirstOrDefaultAsync(o => o.Email == userEmail || o.UserId == userEmail);

            if (organizador == null)
            {
                // Se não encontrar, permite criar perfil
                TempData["Info"] = "Ainda não tem perfil de organizador. Crie um agora.";
                return RedirectToAction("Create");
            }

            return View("Details", organizador);
        }

        // GET: Organizadores/Eventos/5
        public async Task<IActionResult> Eventos(int id)
        {
            var organizador = await _context.Organizadores
                .FirstOrDefaultAsync(o => o.Id == id);

            if (organizador == null)
            {
                return NotFound();
            }

            var eventos = await _context.Eventos
                .Include(e => e.Local)
                .Include(e => e.Inscricoes)
                .Where(e => e.OrganizadorId == id)
                .OrderBy(e => e.Data)
                .ToListAsync();

            ViewBag.Organizador = organizador;
            ViewBag.TotalEventos = eventos.Count;
            ViewBag.EventosFuturos = eventos.Count(e => e.Data > DateTime.Now);

            return View(eventos);
        }

        // GET: Organizadores/TopOrganizadores
        [AllowAnonymous] // Todos podem ver o ranking
        public async Task<IActionResult> TopOrganizadores()
        {
            var organizadores = await _context.Organizadores
                .Include(o => o.Eventos)
                    .ThenInclude(e => e.Inscricoes)
                .Select(o => new
                {
                    Organizador = o,
                    TotalEventos = o.Eventos.Count,
                    TotalInscricoes = o.Eventos.Sum(e => e.Inscricoes.Count),
                    EventosFuturos = o.Eventos.Count(e => e.Data > DateTime.Now)
                })
                .OrderByDescending(x => x.TotalEventos)
                .Take(10)
                .ToListAsync();

            return View(organizadores);
        }

        // GET: Organizadores/Estatisticas/5
        public async Task<IActionResult> Estatisticas(int id)
        {
            var organizador = await _context.Organizadores
                .Include(o => o.Eventos)
                    .ThenInclude(e => e.Inscricoes)
                .FirstOrDefaultAsync(o => o.Id == id);

            if (organizador == null)
            {
                return NotFound();
            }

            var estatisticas = new
            {
                Nome = organizador.Nome,
                TotalEventos = organizador.Eventos.Count,
                EventosFuturos = organizador.Eventos.Count(e => e.Data > DateTime.Now),
                EventosPassados = organizador.Eventos.Count(e => e.Data <= DateTime.Now),
                TotalInscricoes = organizador.Eventos.Sum(e => e.Inscricoes.Count),
                MediaInscricoesPorEvento = organizador.Eventos.Any()
                    ? organizador.Eventos.Average(e => e.Inscricoes.Count)
                    : 0,
                EventoMaisPopular = organizador.Eventos
                    .OrderByDescending(e => e.Inscricoes.Count)
                    .FirstOrDefault()?.Titulo,
                ProximoEvento = organizador.Eventos
                    .Where(e => e.Data > DateTime.Now)
                    .OrderBy(e => e.Data)
                    .FirstOrDefault()?.Titulo
            };

            return Json(estatisticas);
        }

        // POST: Organizadores/TransferirEventos
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> TransferirEventos(int organizadorOrigemId, int organizadorDestinoId)
        {
            try
            {
                var eventos = await _context.Eventos
                    .Where(e => e.OrganizadorId == organizadorOrigemId)
                    .ToListAsync();

                if (!eventos.Any())
                {
                    return Json(new { sucesso = false, mensagem = "Não há eventos para transferir." });
                }

                foreach (var evento in eventos)
                {
                    evento.OrganizadorId = organizadorDestinoId;
                }

                await _context.SaveChangesAsync();

                return Json(new
                {
                    sucesso = true,
                    mensagem = $"{eventos.Count} eventos transferidos com sucesso."
                });
            }
            catch (Exception ex)
            {
                return Json(new { sucesso = false, mensagem = "Erro: " + ex.Message });
            }
        }

        // GET: Organizadores/Disponiveis
        [AllowAnonymous]
        public async Task<IActionResult> Disponiveis()
        {
            var organizadores = await _context.Organizadores
                .Include(o => o.Eventos)
                .Where(o => o.Eventos.Any(e => e.Data > DateTime.Now))
                .OrderBy(o => o.Nome)
                .ToListAsync();

            ViewBag.Titulo = "Organizadores com Eventos Futuros";
            return View("Index", organizadores);
        }

        // ==================== FUNÇÕES AUXILIARES ====================
        private bool OrganizadorExists(int id)
        {
            return _context.Organizadores.Any(e => e.Id == id);
        }

        // GET: Organizadores/Search
        [AllowAnonymous]
        public async Task<IActionResult> Search(string termo)
        {
            if (string.IsNullOrEmpty(termo))
            {
                return RedirectToAction(nameof(Index));
            }

            var organizadores = await _context.Organizadores
                .Include(o => o.Eventos)
                .Where(o => o.Nome.Contains(termo) ||
                           o.Email.Contains(termo) ||
                           o.Empresa.Contains(termo))
                .OrderBy(o => o.Nome)
                .ToListAsync();

            ViewBag.TermoPesquisa = termo;
            ViewBag.Resultados = organizadores.Count;

            return View("Index", organizadores);
        }
    }
}
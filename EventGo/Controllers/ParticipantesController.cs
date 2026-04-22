using EventGo.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using EventGo.Models;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace EventGo.Controllers
{
    [Authorize] // Todas as ações exigem autenticação
    public class ParticipantesController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ParticipantesController(ApplicationDbContext context)
        {
            _context = context;
        }

        // ==================== LISTAR TODOS OS PARTICIPANTES ====================
        // GET: Participantes
        [Authorize(Roles = "Admin")] // Só admin pode ver lista de todos
        public async Task<IActionResult> Index()
        {
            var participantes = await _context.Participantes
                .Include(p => p.Inscricoes)
                    .ThenInclude(i => i.Evento)
                .OrderBy(p => p.Nome)
                .ToListAsync();

            return View(participantes);
        }

        // ==================== VER DETALHES DO PARTICIPANTE ====================
        // GET: Participantes/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var participante = await _context.Participantes
                .Include(p => p.Inscricoes)
                    .ThenInclude(i => i.Evento)
                        .ThenInclude(e => e.Local)
                .Include(p => p.Inscricoes)
                    .ThenInclude(i => i.Evento)
                        .ThenInclude(e => e.Organizador)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (participante == null)
            {
                return NotFound();
            }

            // Verificar se o utilizador logado é o próprio participante ou admin
            if (!User.IsInRole("Admin") && User.Identity.Name != participante.Email)
            {
                return Forbid(); // Não autorizado
            }

            // Estatísticas do participante
            ViewBag.TotalInscricoes = participante.Inscricoes.Count;
            ViewBag.InscricoesAtivas = participante.Inscricoes.Count(i => i.Status == "Ativa");
            ViewBag.InscricoesCanceladas = participante.Inscricoes.Count(i => i.Status == "Cancelada");
            ViewBag.EventosFuturos = participante.Inscricoes
                .Count(i => i.Evento.Data > DateTime.Now && i.Status == "Ativa");
            ViewBag.EventosPassados = participante.Inscricoes
                .Count(i => i.Evento.Data <= DateTime.Now && i.Status == "Ativa");

            return View(participante);
        }

        // ==================== CRIAR NOVO PARTICIPANTE ====================
        // GET: Participantes/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Participantes/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Nome,Email,Telefone,DataNascimento,Interesses")] Participante participante)
        {
            // Remover validações de navegação
            ModelState.Remove("Inscricoes");

            if (ModelState.IsValid)
            {
                try
                {
                    // Verificar se já existe participante com o mesmo email
                    var existe = await _context.Participantes
                        .AnyAsync(p => p.Email == participante.Email);

                    if (existe)
                    {
                        ModelState.AddModelError("Email", "Já existe um participante com este email.");
                        return View(participante);
                    }

                    // Associar ao user logado do Identity
                    participante.UserId = User.Identity.Name;
                    participante.DataRegisto = DateTime.Now;
                    participante.Ativo = true;

                    _context.Add(participante);
                    await _context.SaveChangesAsync();

                    TempData["Sucesso"] = "Perfil de participante criado com sucesso!";
                    return RedirectToAction("MeuPerfil");
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", "Erro ao criar participante: " + ex.Message);
                }
            }

            return View(participante);
        }

        // ==================== EDITAR PARTICIPANTE ====================
        // GET: Participantes/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var participante = await _context.Participantes.FindAsync(id);
            if (participante == null)
            {
                return NotFound();
            }

            // Verificar se o utilizador logado é o próprio participante ou admin
            if (!User.IsInRole("Admin") && User.Identity.Name != participante.Email)
            {
                return Forbid();
            }

            return View(participante);
        }

        // POST: Participantes/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Nome,Email,Telefone,DataNascimento,Interesses,Ativo,DataRegisto,UserId")] Participante participante)
        {
            if (id != participante.Id)
            {
                return NotFound();
            }

            // Verificar permissões novamente
            var participanteOriginal = await _context.Participantes.AsNoTracking().FirstOrDefaultAsync(p => p.Id == id);
            if (participanteOriginal == null)
            {
                return NotFound();
            }

            if (!User.IsInRole("Admin") && User.Identity.Name != participanteOriginal.Email)
            {
                return Forbid();
            }

            // Remover validações de navegação
            ModelState.Remove("Inscricoes");

            if (ModelState.IsValid)
            {
                try
                {
                    // Verificar se email já existe (exceto o próprio)
                    var existe = await _context.Participantes
                        .AnyAsync(p => p.Email == participante.Email && p.Id != participante.Id);

                    if (existe)
                    {
                        ModelState.AddModelError("Email", "Já existe um participante com este email.");
                        return View(participante);
                    }

                    // Manter dados originais que não devem ser alterados
                    participante.UserId = participanteOriginal.UserId;
                    participante.DataRegisto = participanteOriginal.DataRegisto;

                    _context.Update(participante);
                    await _context.SaveChangesAsync();

                    TempData["Sucesso"] = "Perfil atualizado com sucesso!";
                    return RedirectToAction("MeuPerfil");
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!ParticipanteExists(participante.Id))
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
                    ModelState.AddModelError("", "Erro ao atualizar perfil: " + ex.Message);
                }
            }

            return View(participante);
        }

        // ==================== ELIMINAR PARTICIPANTE ====================
        // GET: Participantes/Delete/5
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var participante = await _context.Participantes
                .Include(p => p.Inscricoes)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (participante == null)
            {
                return NotFound();
            }

            // Verificar se tem inscrições ativas
            ViewBag.TemInscricoesAtivas = participante.Inscricoes.Any(i => i.Status == "Ativa");

            return View(participante);
        }

        // POST: Participantes/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var participante = await _context.Participantes
                .Include(p => p.Inscricoes)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (participante == null)
            {
                return NotFound();
            }

            try
            {
                // Opção 1: Eliminar fisicamente (com todas as inscrições)
                if (participante.Inscricoes.Any())
                {
                    _context.Inscricoes.RemoveRange(participante.Inscricoes);
                }

                _context.Participantes.Remove(participante);
                await _context.SaveChangesAsync();

                TempData["Sucesso"] = "Participante eliminado com sucesso!";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                TempData["Erro"] = "Erro ao eliminar participante: " + ex.Message;
                return RedirectToAction(nameof(Delete), new { id });
            }
        }

        // ==================== FUNÇÕES EXTRA ====================

        // GET: Participantes/MeuPerfil
        public async Task<IActionResult> MeuPerfil()
        {
            var userEmail = User.Identity.Name;

            var participante = await _context.Participantes
                .Include(p => p.Inscricoes)
                    .ThenInclude(i => i.Evento)
                        .ThenInclude(e => e.Local)
                .Include(p => p.Inscricoes)
                    .ThenInclude(i => i.Evento)
                        .ThenInclude(e => e.Organizador)
                .FirstOrDefaultAsync(p => p.Email == userEmail || p.UserId == userEmail);

            if (participante == null)
            {
                TempData["Info"] = "Complete o seu perfil de participante.";
                return RedirectToAction("Create");
            }

            return View("Details", participante);
        }

        // GET: Participantes/MinhasInscricoes
        public async Task<IActionResult> MinhasInscricoes()
        {
            var userEmail = User.Identity.Name;

            var participante = await _context.Participantes
                .FirstOrDefaultAsync(p => p.Email == userEmail);

            if (participante == null)
            {
                return RedirectToAction("Create");
            }

            var inscricoes = await _context.Inscricoes
                .Include(i => i.Evento)
                    .ThenInclude(e => e.Local)
                .Include(i => i.Evento)
                    .ThenInclude(e => e.Organizador)
                .Where(i => i.ParticipanteId == participante.Id)
                .OrderBy(i => i.Evento.Data)
                .ToListAsync();

            ViewBag.Participante = participante;
            return View(inscricoes);
        }

        // GET: Participantes/EventosRecomendados
        public async Task<IActionResult> EventosRecomendados()
        {
            var userEmail = User.Identity.Name;

            var participante = await _context.Participantes
                .Include(p => p.Inscricoes)
                    .ThenInclude(i => i.Evento)
                .FirstOrDefaultAsync(p => p.Email == userEmail);

            if (participante == null)
            {
                return RedirectToAction("Create");
            }

            // Recomendação simples: eventos futuros com base nos interesses do participante
            var eventosRecomendados = await _context.Eventos
                .Include(e => e.Local)
                .Include(e => e.Organizador)
                .Include(e => e.Inscricoes)
                .Where(e => e.Data > DateTime.Now &&
                           e.Inscricoes.Count < e.VagasTotais && // Com vagas
                           !e.Inscricoes.Any(i => i.ParticipanteId == participante.Id)) // Não inscrito
                .OrderBy(e => e.Data)
                .Take(6)
                .ToListAsync();

            ViewBag.Participante = participante;
            return View(eventosRecomendados);
        }

        // POST: Participantes/CancelarInscricao
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CancelarInscricao(int inscricaoId)
        {
            var inscricao = await _context.Inscricoes
                .Include(i => i.Participante)
                .Include(i => i.Evento)
                .FirstOrDefaultAsync(i => i.Id == inscricaoId);

            if (inscricao == null)
            {
                return NotFound();
            }

            // Verificar se o participante logado é o dono da inscrição
            if (User.Identity.Name != inscricao.Participante.Email && !User.IsInRole("Admin"))
            {
                return Forbid();
            }

            // Verificar se evento ainda é futuro
            if (inscricao.Evento.Data <= DateTime.Now)
            {
                TempData["Erro"] = "Não é possível cancelar inscrição em eventos que já ocorreram.";
                return RedirectToAction("MinhasInscricoes");
            }

            try
            {
                // Soft delete - apenas muda o status
                inscricao.Status = "Cancelada";
                _context.Update(inscricao);

                // Ou hard delete - remove completamente
                // _context.Inscricoes.Remove(inscricao);

                await _context.SaveChangesAsync();

                TempData["Sucesso"] = "Inscrição cancelada com sucesso!";
                return RedirectToAction("MinhasInscricoes");
            }
            catch (Exception ex)
            {
                TempData["Erro"] = "Erro ao cancelar inscrição: " + ex.Message;
                return RedirectToAction("MinhasInscricoes");
            }
        }

        // GET: Participantes/Historico
        public async Task<IActionResult> Historico()
        {
            var userEmail = User.Identity.Name;

            var participante = await _context.Participantes
                .FirstOrDefaultAsync(p => p.Email == userEmail);

            if (participante == null)
            {
                return RedirectToAction("Create");
            }

            var historico = await _context.Inscricoes
                .Include(i => i.Evento)
                    .ThenInclude(e => e.Local)
                .Where(i => i.ParticipanteId == participante.Id &&
                           i.Evento.Data <= DateTime.Now)
                .OrderByDescending(i => i.Evento.Data)
                .ToListAsync();

            ViewBag.Participante = participante;
            ViewBag.TotalEventos = historico.Count;

            return View(historico);
        }

        // GET: Participantes/Ativos
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Ativos()
        {
            var participantesAtivos = await _context.Participantes
                .Include(p => p.Inscricoes)
                .Where(p => p.Ativo == true)
                .OrderBy(p => p.Nome)
                .ToListAsync();

            ViewBag.Titulo = "Participantes Ativos";
            return View("Index", participantesAtivos);
        }

        // POST: Participantes/ToggleStatus
        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> ToggleStatus(int id)
        {
            var participante = await _context.Participantes.FindAsync(id);
            if (participante == null)
            {
                return NotFound();
            }

            participante.Ativo = !participante.Ativo;
            await _context.SaveChangesAsync();

            return Json(new
            {
                sucesso = true,
                ativo = participante.Ativo,
                mensagem = participante.Ativo ? "Participante ativado" : "Participante desativado"
            });
        }

        // GET: Participantes/Estatisticas
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Estatisticas()
        {
            var totalParticipantes = await _context.Participantes.CountAsync();
            var participantesAtivos = await _context.Participantes.CountAsync(p => p.Ativo);
            var totalInscricoes = await _context.Inscricoes.CountAsync();
            var inscricoesAtivas = await _context.Inscricoes.CountAsync(i => i.Status == "Ativa");

            var estatisticas = new
            {
                TotalParticipantes = totalParticipantes,
                ParticipantesAtivos = participantesAtivos,
                ParticipantesInativos = totalParticipantes - participantesAtivos,
                TotalInscricoes = totalInscricoes,
                InscricoesAtivas = inscricoesAtivas,
                MediaInscricoesPorParticipante = totalParticipantes > 0
                    ? (double)totalInscricoes / totalParticipantes
                    : 0
            };

            return Json(estatisticas);
        }

        // ==================== FUNÇÕES AUXILIARES ====================
        private bool ParticipanteExists(int id)
        {
            return _context.Participantes.Any(e => e.Id == id);
        }

        // GET: Participantes/Search
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Search(string termo)
        {
            if (string.IsNullOrEmpty(termo))
            {
                return RedirectToAction(nameof(Index));
            }

            var participantes = await _context.Participantes
                .Include(p => p.Inscricoes)
                .Where(p => p.Nome.Contains(termo) ||
                           p.Email.Contains(termo) ||
                           p.Telefone.Contains(termo))
                .OrderBy(p => p.Nome)
                .ToListAsync();

            ViewBag.TermoPesquisa = termo;
            ViewBag.Resultados = participantes.Count;

            return View("Index", participantes);
        }
    }
}
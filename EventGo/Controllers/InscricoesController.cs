using EventGo.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using EventGo.Data; // ALTERA PARA O NAMESPACE DO TEU PROJETO
using EventGo.Models; // ALTERA PARA O NAMESPACE DO TEU PROJETO
using System;
using System.Linq;
using System.Threading.Tasks;

namespace EventGo.Controllers
{
    [Authorize] // Todas as ações exigem autenticação
    public class InscricoesController : Controller
    {
        private readonly ApplicationDbContext _context;

        public InscricoesController(ApplicationDbContext context)
        {
            _context = context;
        }

        // ==================== LISTAR TODAS AS INSCRIÇÕES ====================
        // GET: Inscricoes
        public async Task<IActionResult> Index()
        {
            var inscricoes = await _context.Inscricoes
                .Include(i => i.Participante)
                .Include(i => i.Evento)
                    .ThenInclude(e => e.Local)
                .Include(i => i.Evento)
                    .ThenInclude(e => e.Organizador)
                .OrderByDescending(i => i.DataInscricao)
                .ToListAsync();

            return View(inscricoes);
        }

        // ==================== DETALHES DA INSCRIÇÃO ====================
        // GET: Inscricoes/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var inscricao = await _context.Inscricoes
                .Include(i => i.Participante)
                .Include(i => i.Evento)
                    .ThenInclude(e => e.Local)
                .Include(i => i.Evento)
                    .ThenInclude(e => e.Organizador)
                .FirstOrDefaultAsync(i => i.Id == id);

            if (inscricao == null)
            {
                return NotFound();
            }

            return View(inscricao);
        }

        // ==================== INSCREVER PARTICIPANTE NUM EVENTO ====================
        // GET: Inscricoes/Create
        public IActionResult Create()
        {
            // Dropdowns para seleção
            ViewBag.EventoId = new SelectList(_context.Eventos
                .Where(e => e.Data > DateTime.Now) // Só eventos futuros
                .Include(e => e.Inscricoes), "Id", "Titulo", "Vagas");

            ViewBag.ParticipanteId = new SelectList(_context.Participantes, "Id", "Nome");

            return View();
        }

        // POST: Inscricoes/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("EventoId,ParticipanteId")] Inscricao inscricao)
        {
            // Remover validações de navegação
            ModelState.Remove("Evento");
            ModelState.Remove("Participante");

            if (ModelState.IsValid)
            {
                try
                {
                    // ========== VALIDAÇÕES IMPORTANTES ==========

                    // 1. Verificar se evento existe
                    var evento = await _context.Eventos
                        .Include(e => e.Inscricoes)
                        .FirstOrDefaultAsync(e => e.Id == inscricao.EventoId);

                    if (evento == null)
                    {
                        ModelState.AddModelError("EventoId", "Evento não encontrado.");
                        CarregarDropdowns();
                        return View(inscricao);
                    }

                    // 2. Verificar se é evento futuro
                    if (evento.Data <= DateTime.Now)
                    {
                        ModelState.AddModelError("EventoId", "Não é possível inscrever-se em eventos passados.");
                        CarregarDropdowns();
                        return View(inscricao);
                    }

                    // 3. Verificar vagas disponíveis
                    if (evento.Inscricoes.Count >= evento.VagasTotais)
                    {
                        TempData["Erro"] = "Evento sem vagas disponíveis!";
                        return RedirectToAction("Details", "Eventos", new { id = inscricao.EventoId });
                    }

                    // 4. Verificar se participante existe
                    var participante = await _context.Participantes
                        .FirstOrDefaultAsync(p => p.Id == inscricao.ParticipanteId);

                    if (participante == null)
                    {
                        ModelState.AddModelError("ParticipanteId", "Participante não encontrado.");
                        CarregarDropdowns();
                        return View(inscricao);
                    }

                    // 5. Verificar se já está inscrito
                    bool jaInscrito = await _context.Inscricoes
                        .AnyAsync(i => i.EventoId == inscricao.EventoId &&
                                      i.ParticipanteId == inscricao.ParticipanteId);

                    if (jaInscrito)
                    {
                        TempData["Erro"] = "Participante já está inscrito neste evento!";
                        return RedirectToAction("Details", "Eventos", new { id = inscricao.EventoId });
                    }

                    // ========== CRIAR INSCRIÇÃO ==========
                    inscricao.DataInscricao = DateTime.Now;
                    inscricao.Status = "Ativa"; // Podes criar um enum para status

                    _context.Add(inscricao);
                    await _context.SaveChangesAsync();

                    TempData["Sucesso"] = "Inscrição realizada com sucesso!";
                    return RedirectToAction("Details", "Eventos", new { id = inscricao.EventoId });
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", "Erro ao criar inscrição: " + ex.Message);
                }
            }

            CarregarDropdowns(inscricao.EventoId, inscricao.ParticipanteId);
            return View(inscricao);
        }

        // ==================== INSCREVER RÁPIDO (Diretamente do Evento) ====================
        // GET: Inscricoes/InscreverNoEvento/5
        public async Task<IActionResult> InscreverNoEvento(int eventoId)
        {
            var evento = await _context.Eventos
                .Include(e => e.Inscricoes)
                .FirstOrDefaultAsync(e => e.Id == eventoId);

            if (evento == null)
            {
                return NotFound();
            }

            // Verificar se evento é futuro
            if (evento.Data <= DateTime.Now)
            {
                TempData["Erro"] = "Não é possível inscrever-se em eventos passados.";
                return RedirectToAction("Details", "Eventos", new { id = eventoId });
            }

            // Verificar vagas
            if (evento.Inscricoes.Count >= evento.VagasTotais)
            {
                TempData["Erro"] = "Evento sem vagas disponíveis!";
                return RedirectToAction("Details", "Eventos", new { id = eventoId });
            }

            ViewBag.Evento = evento;
            ViewBag.ParticipanteId = new SelectList(_context.Participantes, "Id", "Nome");

            return View();
        }

        // POST: Inscricoes/InscreverNoEvento
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> InscreverNoEvento(int eventoId, int participanteId)
        {
            try
            {
                // Verificar se já está inscrito
                bool jaInscrito = await _context.Inscricoes
                    .AnyAsync(i => i.EventoId == eventoId && i.ParticipanteId == participanteId);

                if (jaInscrito)
                {
                    TempData["Erro"] = "Participante já está inscrito neste evento!";
                    return RedirectToAction("Details", "Eventos", new { id = eventoId });
                }

                var inscricao = new Inscricao
                {
                    EventoId = eventoId,
                    ParticipanteId = participanteId,
                    DataInscricao = DateTime.Now,
                    Status = "Ativa"
                };

                _context.Add(inscricao);
                await _context.SaveChangesAsync();

                TempData["Sucesso"] = "Inscrição realizada com sucesso!";
                return RedirectToAction("Details", "Eventos", new { id = eventoId });
            }
            catch (Exception ex)
            {
                TempData["Erro"] = "Erro ao realizar inscrição: " + ex.Message;
                return RedirectToAction("Details", "Eventos", new { id = eventoId });
            }
        }

        // ==================== CANCELAR INSCRIÇÃO ====================
        // POST: Inscricoes/Cancelar/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Cancelar(int id)
        {
            var inscricao = await _context.Inscricoes
                .Include(i => i.Evento)
                .FirstOrDefaultAsync(i => i.Id == id);

            if (inscricao == null)
            {
                return NotFound();
            }

            try
            {
                // Verificar se evento já passou
                if (inscricao.Evento.Data <= DateTime.Now)
                {
                    TempData["Erro"] = "Não é possível cancelar inscrição em eventos que já ocorreram.";
                    return RedirectToAction(nameof(Index));
                }

                // Opção 1: Remover completamente
                _context.Inscricoes.Remove(inscricao);

                // Opção 2: Ou apenas marcar como cancelada (soft delete)
                // inscricao.Status = "Cancelada";
                // _context.Update(inscricao);

                await _context.SaveChangesAsync();

                TempData["Sucesso"] = "Inscrição cancelada com sucesso!";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                TempData["Erro"] = "Erro ao cancelar inscrição: " + ex.Message;
                return RedirectToAction(nameof(Index));
            }
        }

        // ==================== MINHAS INSCRIÇÕES (para o participante logado) ====================
        // GET: Inscricoes/MinhasInscricoes
        public async Task<IActionResult> MinhasInscricoes()
        {
            // NOTA: Isto assume que o participante está autenticado com Identity
            // e que o email do participante corresponde ao email do user logado
            var userEmail = User.Identity.Name;

            var participante = await _context.Participantes
                .FirstOrDefaultAsync(p => p.Email == userEmail);

            if (participante == null)
            {
                // Se não encontrar participante, redireciona para criar perfil
                TempData["Info"] = "Precisa de criar um perfil de participante primeiro.";
                return RedirectToAction("Create", "Participantes");
            }

            var inscricoes = await _context.Inscricoes
                .Include(i => i.Evento)
                    .ThenInclude(e => e.Local)
                .Include(i => i.Evento)
                    .ThenInclude(e => e.Organizador)
                .Where(i => i.ParticipanteId == participante.Id)
                .OrderBy(i => i.Evento.Data)
                .ToListAsync();

            ViewBag.ParticipanteNome = participante.Nome;
            return View(inscricoes);
        }

        // ==================== INSCRIÇÕES POR EVENTO ====================
        // GET: Inscricoes/PorEvento/5
        public async Task<IActionResult> PorEvento(int eventoId)
        {
            var evento = await _context.Eventos
                .FirstOrDefaultAsync(e => e.Id == eventoId);

            if (evento == null)
            {
                return NotFound();
            }

            var inscricoes = await _context.Inscricoes
                .Include(i => i.Participante)
                .Where(i => i.EventoId == eventoId)
                .OrderBy(i => i.DataInscricao)
                .ToListAsync();

            ViewBag.Evento = evento;
            ViewBag.TotalInscricoes = inscricoes.Count;
            ViewBag.VagasRestantes = evento.VagasTotais - inscricoes.Count;

            return View(inscricoes);
        }

        // ==================== INSCRIÇÕES POR PARTICIPANTE ====================
        // GET: Inscricoes/PorParticipante/5
        public async Task<IActionResult> PorParticipante(int participanteId)
        {
            var participante = await _context.Participantes
                .FirstOrDefaultAsync(p => p.Id == participanteId);

            if (participante == null)
            {
                return NotFound();
            }

            var inscricoes = await _context.Inscricoes
                .Include(i => i.Evento)
                    .ThenInclude(e => e.Local)
                .Where(i => i.ParticipanteId == participanteId)
                .OrderBy(i => i.Evento.Data)
                .ToListAsync();

            ViewBag.Participante = participante;
            return View(inscricoes);
        }

        // ==================== VERIFICAR DISPONIBILIDADE ====================
        // GET: Inscricoes/VerificarVagas/5
        [AllowAnonymous] // Todos podem ver
        public async Task<IActionResult> VerificarVagas(int eventoId)
        {
            var evento = await _context.Eventos
                .Include(e => e.Inscricoes)
                .FirstOrDefaultAsync(e => e.Id == eventoId);

            if (evento == null)
            {
                return NotFound();
            }

            int inscritos = evento.Inscricoes.Count;
            int vagasDisponiveis = evento.VagasTotais - inscritos;

            return Json(new
            {
                eventoId = eventoId,
                totalVagas = evento.VagasTotais,
                inscritos = inscritos,
                vagasDisponiveis = vagasDisponiveis,
                temVagas = vagasDisponiveis > 0
            });
        }

        // ==================== LISTA DE ESPERA (EXTRA) ====================
        // GET: Inscricoes/ListaEspera/5
        public async Task<IActionResult> ListaEspera(int eventoId)
        {
            var evento = await _context.Eventos
                .Include(e => e.Inscricoes)
                    .ThenInclude(i => i.Participante)
                .FirstOrDefaultAsync(e => e.Id == eventoId);

            if (evento == null)
            {
                return NotFound();
            }

            // Se evento está cheio, mostra lista de espera
            if (evento.Inscricoes.Count >= evento.VagasTotais)
            {
                var listaEspera = await _context.Inscricoes
                    .Where(i => i.EventoId == eventoId && i.Status == "ListaEspera")
                    .OrderBy(i => i.DataInscricao)
                    .ToListAsync();

                ViewBag.Evento = evento;
                return View(listaEspera);
            }

            TempData["Info"] = "Evento ainda tem vagas disponíveis.";
            return RedirectToAction("Details", "Eventos", new { id = eventoId });
        }

        // ==================== FUNÇÕES AUXILIARES ====================
        private void CarregarDropdowns(object eventoSelecionado = null, object participanteSelecionado = null)
        {
            ViewBag.EventoId = new SelectList(_context.Eventos
                .Where(e => e.Data > DateTime.Now)
                .Include(e => e.Inscricoes)
                .Select(e => new {
                    e.Id,
                    e.Titulo,
                    VagasDisponiveis = e.VagasTotais - e.Inscricoes.Count
                })
                .ToList(), "Id", "Titulo", eventoSelecionado);

            ViewBag.ParticipanteId = new SelectList(_context.Participantes, "Id", "Nome", participanteSelecionado);
        }

        private bool InscricaoExists(int id)
        {
            return _context.Inscricoes.Any(e => e.Id == id);
        }
    }
}
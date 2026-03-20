using EventGo.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using EventGo.Data; // ALTERA PARA O NAMESPACE DO TEU PROJETO
using EventGo.Models; // ALTERA PARA O NAMESPACE DO TEU PROJETO
using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace EventGo.Controllers
{
    public class EventosController : Controller
    {
        private readonly ApplicationDbContext _context;

        // Construtor com injeção de dependência
        public EventosController(ApplicationDbContext context)
        {
            _context = context;
        }

        // ==================== LISTAR TODOS OS EVENTOS ====================
        // GET: Eventos
        public async Task<IActionResult> Index()
        {
            var eventos = await _context.Eventos
                .Include(e => e.Local)
                .Include(e => e.Organizador)
                .Include(e => e.Inscricoes)
                .OrderBy(e => e.Data)
                .ToListAsync();

            return View(eventos);
        }

        // ==================== VER DETALHES DE UM EVENTO ====================
        // GET: Eventos/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var evento = await _context.Eventos
                .Include(e => e.Local)
                .Include(e => e.Organizador)
                .Include(e => e.Inscricoes)
                    .ThenInclude(i => i.Participante)
                .FirstOrDefaultAsync(e => e.Id == id);

            if (evento == null)
            {
                return NotFound();
            }

            // Calcular vagas disponíveis
            ViewBag.VagasDisponiveis = evento.VagasTotais - evento.Inscricoes.Count;

            // Verificar se evento é futuro ou passado
            ViewBag.EventoFuturo = evento.Data > DateTime.Now;

            return View(evento);
        }

        // ==================== CRIAR NOVO EVENTO ====================
        // GET: Eventos/Create
        [Authorize] // Só utilizadores autenticados podem criar
        public IActionResult Create()
        {
            // Preparar dados para dropdowns
            ViewBag.OrganizadorId = new Microsoft.AspNetCore.Mvc.Rendering.SelectList(_context.Organizadores, "Id", "Nome");
            ViewBag.LocalId = new Microsoft.AspNetCore.Mvc.Rendering.SelectList(_context.Locais, "Id", "Nome");

            return View();
        }

        // POST: Eventos/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize]
        public async Task<IActionResult> Create([Bind("Titulo,Descricao,Data,VagasTotais,OrganizadorId,LocalId")] Evento evento)
        {
            // Remover validações de navegação que não vêm do formulário
            ModelState.Remove("Organizador");
            ModelState.Remove("Local");
            ModelState.Remove("Inscricoes");

            if (ModelState.IsValid)
            {
                try
                {
                    // Validar se a data é futura
                    if (evento.Data <= DateTime.Now)
                    {
                        ModelState.AddModelError("Data", "A data do evento deve ser futura.");

                        // Recarregar dropdowns
                        ViewBag.OrganizadorId = new Microsoft.AspNetCore.Mvc.Rendering.SelectList(_context.Organizadores, "Id", "Nome", evento.OrganizadorId);
                        ViewBag.LocalId = new Microsoft.AspNetCore.Mvc.Rendering.SelectList(_context.Locais, "Id", "Nome", evento.LocalId);

                        return View(evento);
                    }

                    _context.Add(evento);
                    await _context.SaveChangesAsync();

                    TempData["Sucesso"] = "Evento criado com sucesso!";
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", "Erro ao criar evento: " + ex.Message);
                }
            }

            // Se chegou aqui, algo falhou - recarregar dropdowns
            ViewBag.OrganizadorId = new Microsoft.AspNetCore.Mvc.Rendering.SelectList(_context.Organizadores, "Id", "Nome", evento.OrganizadorId);
            ViewBag.LocalId = new Microsoft.AspNetCore.Mvc.Rendering.SelectList(_context.Locais, "Id", "Nome", evento.LocalId);

            return View(evento);
        }

        // ==================== EDITAR EVENTO ====================
        // GET: Eventos/Edit/5
        [Authorize]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var evento = await _context.Eventos.FindAsync(id);
            if (evento == null)
            {
                return NotFound();
            }

            // Preparar dropdowns com o valor selecionado
            ViewBag.OrganizadorId = new Microsoft.AspNetCore.Mvc.Rendering.SelectList(_context.Organizadores, "Id", "Nome", evento.OrganizadorId);
            ViewBag.LocalId = new Microsoft.AspNetCore.Mvc.Rendering.SelectList(_context.Locais, "Id", "Nome", evento.LocalId);

            return View(evento);
        }

        // POST: Eventos/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Titulo,Descricao,Data,VagasTotais,OrganizadorId,LocalId")] Evento evento)
        {
            if (id != evento.Id)
            {
                return NotFound();
            }

            // Remover validações de navegação
            ModelState.Remove("Organizador");
            ModelState.Remove("Local");
            ModelState.Remove("Inscricoes");

            if (ModelState.IsValid)
            {
                try
                {
                    // Buscar evento original para verificar inscrições
                    var eventoOriginal = await _context.Eventos
                        .Include(e => e.Inscricoes)
                        .FirstOrDefaultAsync(e => e.Id == id);

                    if (eventoOriginal == null)
                    {
                        return NotFound();
                    }

                    // Validar se pode reduzir vagas (não pode ser menor que inscrições atuais)
                    if (evento.VagasTotais < eventoOriginal.Inscricoes.Count)
                    {
                        ModelState.AddModelError("VagasTotais",
                            $"Não pode reduzir vagas para {evento.VagasTotais}. Já existem {eventoOriginal.Inscricoes.Count} inscrições.");

                        // Recarregar dropdowns
                        ViewBag.OrganizadorId = new Microsoft.AspNetCore.Mvc.Rendering.SelectList(_context.Organizadores, "Id", "Nome", evento.OrganizadorId);
                        ViewBag.LocalId = new Microsoft.AspNetCore.Mvc.Rendering.SelectList(_context.Locais, "Id", "Nome", evento.LocalId);

                        return View(evento);
                    }

                    // Atualizar apenas os campos permitidos
                    eventoOriginal.Titulo = evento.Titulo;
                    eventoOriginal.Descricao = evento.Descricao;
                    eventoOriginal.Data = evento.Data;
                    eventoOriginal.VagasTotais = evento.VagasTotais;
                    eventoOriginal.OrganizadorId = evento.OrganizadorId;
                    eventoOriginal.LocalId = evento.LocalId;

                    _context.Update(eventoOriginal);
                    await _context.SaveChangesAsync();

                    TempData["Sucesso"] = "Evento atualizado com sucesso!";
                    return RedirectToAction(nameof(Index));
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!EventoExists(evento.Id))
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
                    ModelState.AddModelError("", "Erro ao atualizar evento: " + ex.Message);
                }
            }

            // Se chegou aqui, algo falhou
            ViewBag.OrganizadorId = new Microsoft.AspNetCore.Mvc.Rendering.SelectList(_context.Organizadores, "Id", "Nome", evento.OrganizadorId);
            ViewBag.LocalId = new Microsoft.AspNetCore.Mvc.Rendering.SelectList(_context.Locais, "Id", "Nome", evento.LocalId);

            return View(evento);
        }

        // ==================== ELIMINAR EVENTO ====================
        // GET: Eventos/Delete/5
        [Authorize]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var evento = await _context.Eventos
                .Include(e => e.Local)
                .Include(e => e.Organizador)
                .Include(e => e.Inscricoes)
                .FirstOrDefaultAsync(e => e.Id == id);

            if (evento == null)
            {
                return NotFound();
            }

            // Verificar se tem inscrições
            ViewBag.TemInscricoes = evento.Inscricoes.Any();

            return View(evento);
        }

        // POST: Eventos/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var evento = await _context.Eventos
                .Include(e => e.Inscricoes)
                .FirstOrDefaultAsync(e => e.Id == id);

            if (evento == null)
            {
                return NotFound();
            }

            try
            {
                // Remover inscrições associadas primeiro
                if (evento.Inscricoes.Any())
                {
                    _context.Inscricoes.RemoveRange(evento.Inscricoes);
                }

                _context.Eventos.Remove(evento);
                await _context.SaveChangesAsync();

                TempData["Sucesso"] = "Evento eliminado com sucesso!";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                TempData["Erro"] = "Erro ao eliminar evento: " + ex.Message;
                return RedirectToAction(nameof(Delete), new { id });
            }
        }

        // ==================== FUNÇÕES EXTRA ====================

        // GET: Eventos/Futuros
        public async Task<IActionResult> Futuros()
        {
            var eventosFuturos = await _context.Eventos
                .Include(e => e.Local)
                .Include(e => e.Organizador)
                .Where(e => e.Data > DateTime.Now)
                .OrderBy(e => e.Data)
                .ToListAsync();

            ViewBag.Titulo = "Eventos Futuros";
            return View("Index", eventosFuturos);
        }

        // GET: Eventos/Passados
        public async Task<IActionResult> Passados()
        {
            var eventosPassados = await _context.Eventos
                .Include(e => e.Local)
                .Include(e => e.Organizador)
                .Where(e => e.Data <= DateTime.Now)
                .OrderByDescending(e => e.Data)
                .ToListAsync();

            ViewBag.Titulo = "Eventos Passados";
            return View("Index", eventosPassados);
        }

        // GET: Eventos/ComVagas
        public async Task<IActionResult> ComVagas()
        {
            var eventos = await _context.Eventos
                .Include(e => e.Local)
                .Include(e => e.Organizador)
                .Include(e => e.Inscricoes)
                .Where(e => e.Data > DateTime.Now && e.Inscricoes.Count < e.VagasTotais)
                .OrderBy(e => e.Data)
                .ToListAsync();

            ViewBag.Titulo = "Eventos com Vagas Disponíveis";
            return View("Index", eventos);
        }

        // GET: Eventos/DoOrganizador/5
        public async Task<IActionResult> DoOrganizador(int id)
        {
            var organizador = await _context.Organizadores
                .FirstOrDefaultAsync(o => o.Id == id);

            if (organizador == null)
            {
                return NotFound();
            }

            var eventos = await _context.Eventos
                .Include(e => e.Local)
                .Where(e => e.OrganizadorId == id)
                .OrderBy(e => e.Data)
                .ToListAsync();

            ViewBag.Titulo = $"Eventos de {organizador.Nome}";
            ViewBag.OrganizadorNome = organizador.Nome;

            return View("Index", eventos);
        }

        // ==================== FUNÇÃO AUXILIAR ====================
        private bool EventoExists(int id)
        {
            return _context.Eventos.Any(e => e.Id == id);
        }
    }
}
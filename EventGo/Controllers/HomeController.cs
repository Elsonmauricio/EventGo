using EventGo.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using EventGo.Models;
using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace EventGo.Controllers
{
    public class HomeController : Controller
    {
        private readonly ApplicationDbContext _context;

        public HomeController(ApplicationDbContext context)
        {
            _context = context;
        }

        // ==================== PÁGINA INICIAL ====================
        // GET: Home
        public async Task<IActionResult> Index()
        {
            try
            {
                // Estatísticas gerais para o dashboard
                var totalEventos = await _context.Eventos.CountAsync();
                var totalEventosFuturos = await _context.Eventos
                    .CountAsync(e => e.Data > DateTime.Now);
                var totalOrganizadores = await _context.Organizadores.CountAsync();
                var totalParticipantes = await _context.Participantes.CountAsync();
                var totalInscricoes = await _context.Inscricoes.CountAsync();

                // Últimos eventos adicionados
                var ultimosEventos = await _context.Eventos
                    .Include(e => e.Local)
                    .Include(e => e.Organizador)
                    .Include(e => e.Inscricoes)
                    .Where(e => e.Data > DateTime.Now)
                    .OrderBy(e => e.Data)
                    .Take(6)
                    .ToListAsync();

                // Eventos mais populares (mais inscrições)
                var eventosPopulares = await _context.Eventos
                    .Include(e => e.Local)
                    .Include(e => e.Organizador)
                    .Include(e => e.Inscricoes)
                    .Where(e => e.Data > DateTime.Now)
                    .OrderByDescending(e => e.Inscricoes.Count)
                    .Take(3)
                    .ToListAsync();

                // Passar dados para a View
                ViewBag.TotalEventos = totalEventos;
                ViewBag.TotalEventosFuturos = totalEventosFuturos;
                ViewBag.TotalOrganizadores = totalOrganizadores;
                ViewBag.TotalParticipantes = totalParticipantes;
                ViewBag.TotalInscricoes = totalInscricoes;

                ViewBag.UltimosEventos = ultimosEventos;
                ViewBag.EventosPopulares = eventosPopulares;

                // Mensagem de boas-vindas conforme hora do dia
                var hora = DateTime.Now.Hour;
                string saudacao;
                if (hora < 12) saudacao = "Bom dia";
                else if (hora < 18) saudacao = "Boa tarde";
                else saudacao = "Boa noite";

                ViewBag.Saudacao = saudacao;

                // Verificar se user está autenticado
                if (User.Identity.IsAuthenticated)
                {
                    ViewBag.UserName = User.Identity.Name;
                    ViewBag.IsAdmin = User.IsInRole("Admin");
                }
            }
            catch (Exception ex)
            {
                // Log do erro (podes implementar um sistema de logging)
                Console.WriteLine($"Erro no Home Index: {ex.Message}");

                // Valores padrão em caso de erro
                ViewBag.TotalEventos = 0;
                ViewBag.TotalEventosFuturos = 0;
                ViewBag.TotalOrganizadores = 0;
                ViewBag.TotalParticipantes = 0;
                ViewBag.TotalInscricoes = 0;
                ViewBag.UltimosEventos = new List<Evento>();
                ViewBag.EventosPopulares = new List<Evento>();
                ViewBag.Saudacao = "Bem-vindo";
            }

            return View();
        }

        // ==================== PÁGINA SOBRE ====================
        // GET: Home/About
        public IActionResult About()
        {
            ViewBag.Message = "EventGo - Sistema de Gestão de Eventos";
            ViewBag.Description = "Plataforma completa para gestão de eventos, organizadores e participantes.";
            ViewBag.Version = "1.0.0";
            ViewBag.ReleaseDate = new DateTime(2024, 1, 1);

            return View();
        }

        // ==================== PÁGINA DE CONTACTOS ====================
        // GET: Home/Contact
        public IActionResult Contact()
        {
            ViewBag.Message = "Entre em contacto connosco";
            ViewBag.Email = "suporte@eventgo.pt";
            ViewBag.Telefone = "+351 123 456 789";
            ViewBag.Morada = "Rua da Tecnologia, 123 - Lisboa";

            return View();
        }

        // ==================== PÁGINA DE AJUDA/FAQ ====================
        // GET: Home/Help
        public IActionResult Help()
        {
            var faqs = new List<FAQ>
            {
                new FAQ {
                    Pergunta = "Como posso criar um evento?",
                    Resposta = "Para criar um evento, precisa primeiro de ter um perfil de organizador. Depois, no menu 'Eventos', clique em 'Criar Novo Evento'."
                },
                new FAQ {
                    Pergunta = "Como faço para me inscrever num evento?",
                    Resposta = "Navegue até ao evento desejado e clique no botão 'Inscrever-se'. Precisa de ter um perfil de participante."
                },
                new FAQ {
                    Pergunta = "Posso cancelar a minha inscrição?",
                    Resposta = "Sim, nas suas inscrições pode cancelar até 24h antes do evento."
                },
                new FAQ {
                    Pergunta = "Como sei se ainda há vagas?",
                    Resposta = "Na página do evento, pode ver o número de vagas disponíveis em tempo real."
                }
            };

            return View(faqs);
        }

        // ==================== POLÍTICA DE PRIVACIDADE ====================
        // GET: Home/Privacy
        public IActionResult Privacy()
        {
            return View();
        }

        // ==================== TERMOS DE USO ====================
        // GET: Home/Terms
        public IActionResult Terms()
        {
            return View();
        }

        // ==================== DASHBOARD (para users autenticados) ====================
        // GET: Home/Dashboard
        [Authorize]
        public async Task<IActionResult> Dashboard()
        {
            var userEmail = User.Identity.Name;

            // Dashboard diferente conforme o role
            if (User.IsInRole("Admin"))
            {
                return await AdminDashboard();
            }
            else if (User.IsInRole("Organizador"))
            {
                return await OrganizadorDashboard(userEmail);
            }
            else // Participante ou user normal
            {
                return await ParticipanteDashboard(userEmail);
            }
        }

        private async Task<IActionResult> AdminDashboard()
        {
            // Estatísticas avançadas para admin
            var stats = new
            {
                TotalEventos = await _context.Eventos.CountAsync(),
                EventosFuturos = await _context.Eventos.CountAsync(e => e.Data > DateTime.Now),
                EventosPassados = await _context.Eventos.CountAsync(e => e.Data <= DateTime.Now),
                TotalOrganizadores = await _context.Organizadores.CountAsync(),
                TotalParticipantes = await _context.Participantes.CountAsync(),
                TotalInscricoes = await _context.Inscricoes.CountAsync(),

                // Eventos por mês (últimos 6 meses)
                EventosPorMes = await _context.Eventos
                    .Where(e => e.Data > DateTime.Now.AddMonths(-6))
                    .GroupBy(e => new { e.Data.Year, e.Data.Month })
                    .Select(g => new {
                        Mes = $"{g.Key.Year}-{g.Key.Month}",
                        Total = g.Count()
                    })
                    .ToListAsync(),

                // Top 5 organizadores
                TopOrganizadores = await _context.Organizadores
                    .Include(o => o.Eventos)
                    .Select(o => new {
                        Nome = o.Nome,
                        TotalEventos = o.Eventos.Count
                    })
                    .OrderByDescending(o => o.TotalEventos)
                    .Take(5)
                    .ToListAsync(),

                // Inscrições por dia (últimos 30 dias)
                InscricoesRecentes = await _context.Inscricoes
                    .Where(i => i.DataInscricao > DateTime.Now.AddDays(-30))
                    .GroupBy(i => i.DataInscricao.Date)
                    .Select(g => new {
                        Data = g.Key,
                        Total = g.Count()
                    })
                    .OrderBy(g => g.Data)
                    .ToListAsync()
            };

            return View("AdminDashboard", stats);
        }

        private async Task<IActionResult> OrganizadorDashboard(string email)
        {
            var organizador = await _context.Organizadores
                .Include(o => o.Eventos)
                    .ThenInclude(e => e.Inscricoes)
                .Include(o => o.Eventos)
                    .ThenInclude(e => e.Local)
                .FirstOrDefaultAsync(o => o.Email == email);

            if (organizador == null)
            {
                return RedirectToAction("Create", "Organizadores");
            }

            var stats = new
            {
                Organizador = organizador,
                TotalEventos = organizador.Eventos.Count,
                EventosFuturos = organizador.Eventos.Count(e => e.Data > DateTime.Now),
                EventosPassados = organizador.Eventos.Count(e => e.Data <= DateTime.Now),
                TotalInscricoes = organizador.Eventos.Sum(e => e.Inscricoes.Count),

                PróximosEventos = organizador.Eventos
                    .Where(e => e.Data > DateTime.Now)
                    .OrderBy(e => e.Data)
                    .Take(5)
                    .ToList(),

                EventosComMaisInscricoes = organizador.Eventos
                    .OrderByDescending(e => e.Inscricoes.Count)
                    .Take(3)
                    .ToList()
            };

            return View("OrganizadorDashboard", stats);
        }

        private async Task<IActionResult> ParticipanteDashboard(string email)
        {
            var participante = await _context.Participantes
                .Include(p => p.Inscricoes)
                    .ThenInclude(i => i.Evento)
                        .ThenInclude(e => e.Local)
                .FirstOrDefaultAsync(p => p.Email == email);

            if (participante == null)
            {
                return RedirectToAction("Create", "Participantes");
            }

            var stats = new
            {
                Participante = participante,
                TotalInscricoes = participante.Inscricoes.Count,
                InscricoesAtivas = participante.Inscricoes.Count(i => i.Status == "Ativa"),

                PróximosEventos = participante.Inscricoes
                    .Where(i => i.Status == "Ativa" && i.Evento.Data > DateTime.Now)
                    .Select(i => i.Evento)
                    .OrderBy(e => e.Data)
                    .Take(5)
                    .ToList(),

                Historico = participante.Inscricoes
                    .Where(i => i.Evento.Data <= DateTime.Now)
                    .Select(i => i.Evento)
                    .OrderByDescending(e => e.Data)
                    .Take(5)
                    .ToList(),

                // Recomendações baseadas em eventos que já participou
                Recomendacoes = await GetRecomendacoes(participante)
            };

            return View("ParticipanteDashboard", stats);
        }

        private async Task<List<Evento>> GetRecomendacoes(Participante participante)
        {
            // Lógica simples de recomendação
            var eventosParticipados = participante.Inscricoes
                .Where(i => i.Status == "Ativa")
                .Select(i => i.Evento)
                .ToList();

            // Recomendar eventos futuros de organizadores que o participante já conhece
            var organizadoresConhecidos = eventosParticipados
                .Select(e => e.OrganizadorId)
                .Distinct()
                .ToList();

            var recomendacoes = await _context.Eventos
                .Include(e => e.Local)
                .Include(e => e.Organizador)
                .Where(e => e.Data > DateTime.Now &&
                           !participante.Inscricoes.Any(i => i.EventoId == e.Id) && // Não inscrito
                           (organizadoresConhecidos.Contains(e.OrganizadorId) || // Organizador conhecido
                            e.Inscricoes.Count < e.VagasTotais)) // Tem vagas
                .OrderBy(e => e.Data)
                .Take(6)
                .ToListAsync();

            return recomendacoes;
        }

        // ==================== PESQUISA GLOBAL ====================
        // GET: Home/Search
        public async Task<IActionResult> Search(string query)
        {
            if (string.IsNullOrWhiteSpace(query))
            {
                return RedirectToAction("Index");
            }

            var resultados = new
            {
                Eventos = await _context.Eventos
                    .Include(e => e.Local)
                    .Include(e => e.Organizador)
                    .Where(e => e.Titulo.Contains(query) ||
                               e.Descricao.Contains(query) ||
                               e.Local.Nome.Contains(query))
                    .Take(5)
                    .ToListAsync(),

                Organizadores = await _context.Organizadores
                    .Where(o => o.Nome.Contains(query) ||
                               o.Empresa.Contains(query))
                    .Take(5)
                    .ToListAsync(),

                Participantes = User.IsInRole("Admin") ?
                    await _context.Participantes
                        .Where(p => p.Nome.Contains(query))
                        .Take(5)
                        .ToListAsync() : null
            };

            ViewBag.Query = query;
            return View(resultados);
        }

        // ==================== ERROR HANDLING ====================
        // GET: Home/Error
        public IActionResult Error()
        {
            return View();
        }

        // GET: Home/AccessDenied
        public IActionResult AccessDenied()
        {
            return View();
        }

        // ==================== SITEMAP ====================
        // GET: Home/SiteMap
        public IActionResult SiteMap()
        {
            var links = new List<SitemapLink>
            {
                new SitemapLink { Url = "/", Text = "Home" },
                new SitemapLink { Url = "/Home/About", Text = "Sobre" },
                new SitemapLink { Url = "/Home/Contact", Text = "Contactos" },
                new SitemapLink { Url = "/Home/Help", Text = "Ajuda" },
                new SitemapLink { Url = "/Eventos", Text = "Eventos" },
                new SitemapLink { Url = "/Organizadores", Text = "Organizadores" },
                new SitemapLink { Url = "/Participantes", Text = "Participantes" },
                new SitemapLink { Url = "/Inscricoes", Text = "Inscrições" }
            };

            return View(links);
        }

        // ==================== STATUS/HEALTH CHECK ====================
        // GET: Home/Status
        [AllowAnonymous]
        public IActionResult Status()
        {
            var status = new
            {
                Application = "EventGo",
                Status = "Healthy",
                Timestamp = DateTime.Now,
                Database = _context.Database.CanConnect() ? "Connected" : "Disconnected",
                Environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production"
            };

            return Json(status);
        }
    }

    // ==================== CLASSES AUXILIARES ====================
    public class FAQ
    {
        public string Pergunta { get; set; }
        public string Resposta { get; set; }
    }

    public class SitemapLink
    {
        public string Url { get; set; }
        public string Text { get; set; }
    }
}
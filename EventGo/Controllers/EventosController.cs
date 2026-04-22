using Microsoft.AspNetCore.Mvc;

namespace EventGo.Controllers
{
    public class EventosController : Controller
    {
        // GET: Eventos/Criar
        public IActionResult Criar()
        {
            return View();
        }
    }
}
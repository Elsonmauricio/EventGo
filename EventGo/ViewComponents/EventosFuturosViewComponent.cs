using EventGo.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
public class EventosFuturosViewComponent : ViewComponent
{
    private readonly AppDbContext _context;

    public EventosFuturosViewComponent(AppDbContext context)
    {
        _context = context;
    }

    public async Task<IViewComponentResult> InvokeAsync()
    {
        var eventos = _context.Eventos
            .Where(e => e.Data > DateTime.Now)
            .ToList();

        return View(eventos);
    }
}
using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using projeto.Data; // Certifique-se de incluir o namespace do contexto
using projeto.Models;

namespace projeto.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly ApplicationDbContext _context;

        public HomeController(ILogger<HomeController> logger, ApplicationDbContext context)
        {
            _logger = logger;
            _context = context; // Injeta o contexto do banco de dados
        }

        public IActionResult Index()
        {
            var userEmail = HttpContext.Session.GetString("UserEmail");
            if (!string.IsNullOrEmpty(userEmail))
            {
                // Obter o utilizador a partir do e-mail
                var user = _context.Utilizador.FirstOrDefault(u => u.Email == userEmail);
                if (user != null)
                {
                    ViewData["UserPoints"] = user.Pontos; // Define os pontos no ViewData
                }
            }

            var leiloes = _context.Leiloes
                .Include(l => l.Item) // Inclui os itens relacionados
                .Where(l => l.DataFim > DateTime.Now) // Apenas leilões ativos
                .ToList();

            ViewData["Leiloes"] = leiloes;


            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}

using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using projeto.Data;
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
            _context = context;
        }

        public IActionResult Index()
        {
            //var userEmail = HttpContext.Session.GetString("UserEmail");
            //if (!string.IsNullOrEmpty(userEmail))
            //{
            //    var user = _context.Utilizador.FirstOrDefault(u => u.Email == userEmail);
            //    if (user != null)
            //    {
            //        ViewData["UserPoints"] = user.Pontos; 
            //    }
            //}

            //var leiloes = _context.Leiloes
            //    .Include(l => l.Item) 
            //    .Where(l => l.DataFim > DateTime.Now) 
            //    .ToList();

            //ViewData["Leiloes"] = leiloes;


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

        public IActionResult Terms()
        {
            return View();
        }
    }
}

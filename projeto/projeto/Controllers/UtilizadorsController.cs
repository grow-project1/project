using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using projeto.Data;
using projeto.Models;

namespace projeto.Controllers
{
    public class UtilizadorsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public UtilizadorsController(ApplicationDbContext context)
        {
            _context = context;
        }

        public IActionResult Register()
        {
            return View(); // Renderiza a mesma view de Create
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register([Bind("Nome, Email,Password")] Utilizador utilizador)
        {
            // Verifica se o e-mail e a senha estão preenchidos
            if (string.IsNullOrEmpty(utilizador.Email))
            {
                ModelState.AddModelError("Email", "O e-mail é obrigatório.");
            }


            if (string.IsNullOrEmpty(utilizador.Password))
            {
                ModelState.AddModelError("Password", "A senha é obrigatória.");
            }
            else if (utilizador.Password.Length < 6) // Exemplo de uma regra de validação para senha
            {
                ModelState.AddModelError("Password", "A senha deve ter pelo menos 6 caracteres.");
            }

            // Se o ModelState não for válido, retorna à view com as mensagens de erro
            if (!ModelState.IsValid)
            {
                return View(utilizador);
            }

            // Verifica se o e-mail já está em uso
            var userExists = await _context.Utilizador.AnyAsync(u => u.Email == utilizador.Email);
            if (userExists)
            {
                ModelState.AddModelError("Email", "O e-mail já está em uso.");
                return View(utilizador);
            }

            // Adiciona o novo utilizador ao banco de dados
            _context.Add(utilizador);
            await _context.SaveChangesAsync();

            // Mensagem de sucesso
            TempData["Success"] = "Conta criada com sucesso! Faça login.";

            // Redireciona para a página de Login
            return RedirectToAction("Login", "Utilizadors");
        }




        // GET: Login
        public IActionResult Login()
        {
            return View();
        }

        // POST: Login
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginModel loginModel)
        {
            if (ModelState.IsValid)
            {
                var utilizador = await _context.Utilizador
                    .FirstOrDefaultAsync(u => u.Email == loginModel.Email && u.Password == loginModel.Password);

                if (utilizador != null)
                {
                    // Armazena o e-mail do usuário na sessão
                    HttpContext.Session.SetString("UserEmail", utilizador.Email);

                    HttpContext.Session.SetString("UserNome", utilizador.Nome);

                    TempData["Success"] = "Login realizado com sucesso!";
                    return RedirectToAction("Index", "Home");
                }

                ModelState.AddModelError(string.Empty, "Invalid crentials");
            }
            return View(loginModel);
        }


        public IActionResult Logout()
        {
            // Remove os dados da sessão
            HttpContext.Session.Clear();
            TempData["Success"] = "Logout realizado com sucesso!";
            return RedirectToAction("Login");
        }




        // GET: Utilizadors
        public async Task<IActionResult> Index()
        {
            return View(await _context.Utilizador.ToListAsync());
        }

        // GET: Utilizadors/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var utilizador = await _context.Utilizador
                .FirstOrDefaultAsync(m => m.UtilizadorId == id);
            if (utilizador == null)
            {
                return NotFound();
            }

            return View(utilizador);
        }

        // GET: Utilizadors/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Utilizadors/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("UtilizadorId,Email,Password")] Utilizador utilizador)
        {
            if (ModelState.IsValid)
            {
                _context.Add(utilizador);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(utilizador);
        }

        // GET: Utilizadors/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                Console.WriteLine("erro");
                return NotFound();
            }


            Console.WriteLine("no edit");
            var utilizador = await _context.Utilizador.FindAsync(id);
            if (utilizador == null)
            {
                return NotFound();
            }
            return View(utilizador);
        }

        // POST: Utilizadors/Edit/5
        // POST: Utilizadors/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("UtilizadorId,Nome")] Utilizador utilizador)
        {
            if (id != utilizador.UtilizadorId)
            {

                Console.WriteLine("2");
                return NotFound();
            }

            Console.WriteLine("1");

            if (ModelState.IsValid)
            {
                try
                {
                    // A busca do utilizador é necessária para garantir que o email não seja alterado
                    var existingUtilizador = await _context.Utilizador.FindAsync(id);
                    if (existingUtilizador != null)
                    {

                        Console.WriteLine("existeeee");
                        // Atualiza apenas o Nome
                        existingUtilizador.Nome = utilizador.Nome;

                        _context.Update(existingUtilizador);
                        await _context.SaveChangesAsync();
                    }
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!UtilizadorExists(utilizador.UtilizadorId))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }

                // Redireciona para o perfil após a edição
                return RedirectToAction("Profile", new { id = utilizador.UtilizadorId });
            }

            return View(utilizador);
        }



        // GET: Utilizadors/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var utilizador = await _context.Utilizador
                .FirstOrDefaultAsync(m => m.UtilizadorId == id);
            if (utilizador == null)
            {
                return NotFound();
            }

            return View(utilizador);
        }

        // POST: Utilizadors/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var utilizador = await _context.Utilizador.FindAsync(id);
            if (utilizador != null)
            {
                _context.Utilizador.Remove(utilizador);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool UtilizadorExists(int id)
        {
            return _context.Utilizador.Any(e => e.UtilizadorId == id);
        }

        // GET: Profile
        public IActionResult Profile()
        {
            var userEmail = HttpContext.Session.GetString("UserEmail");

            if (string.IsNullOrEmpty(userEmail))
            {
                return RedirectToAction("Login", "Utilizadors");
            }

            var utilizador = _context.Utilizador.FirstOrDefault(u => u.Email == userEmail);

            if (utilizador == null)
            {
                return NotFound();
            }

            return View(utilizador); // Passa o utilizador para a view
        }
    }
}

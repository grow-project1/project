using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.CodeAnalysis.Scripting;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
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

        // Método Register (GET)
        public IActionResult Register()
        {
            return View();
        }

        // Método Register (POST)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register([Bind("Nome,Email,Password")] Utilizador utilizador)
        {
            // Validações adicionais
            if (string.IsNullOrEmpty(utilizador.Nome))
            {
                ModelState.AddModelError("Nome", "O nome é obrigatório.");
            }

            if (string.IsNullOrEmpty(utilizador.Email))
            {
                ModelState.AddModelError("Email", "O e-mail é obrigatório.");
            }

            if (string.IsNullOrEmpty(utilizador.Password))
            {
                ModelState.AddModelError("Password", "A senha é obrigatória.");
            }
            else if (utilizador.Password.Length < 6)
            {
                ModelState.AddModelError("Password", "A senha deve ter pelo menos 6 caracteres.");
            }

            if (!ModelState.IsValid)
            {
                return View(utilizador);
            }

            // Verifica se o e-mail já existe no banco
            var userExists = await _context.Utilizador.AnyAsync(u => u.Email == utilizador.Email);
            if (userExists)
            {
                ModelState.AddModelError("Email", "Este e-mail já está cadastrado.");
                return View(utilizador);
            }

            // Hash da senha
            utilizador.Password = BCrypt.Net.BCrypt.HashPassword(utilizador.Password);

            // Adiciona o utilizador ao banco
            _context.Add(utilizador);
            await _context.SaveChangesAsync();

            // Mensagem de sucesso
            TempData["Success"] = "Conta criada com sucesso! Faça login para acessar sua conta.";

            return RedirectToAction("Login");
        }

        // Método Login (GET)
        public IActionResult Login()
        {
            return View();
        }

        // Método Login (POST)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginModel loginModel)
        {
            if (ModelState.IsValid)
            {
                var utilizador = await _context.Utilizador
                    .FirstOrDefaultAsync(u => u.Email == loginModel.Email);

                if (utilizador != null && BCrypt.Net.BCrypt.Verify(loginModel.Password, utilizador.Password))
                {
                    HttpContext.Session.SetString("UserEmail", utilizador.Email);
                    HttpContext.Session.SetString("UserNome", utilizador.Nome);

                    TempData["Success"] = "Login realizado com sucesso!";
                    return RedirectToAction("Index", "Home");
                }

                ModelState.AddModelError(string.Empty, "Invalid credentials");
            }
            return View(loginModel);
        }

        // Método Logout
        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            Response.Headers["Cache-Control"] = "no-cache, no-store, must-revalidate";
            Response.Headers["Pragma"] = "no-cache";
            Response.Headers["Expires"] = "0";
            TempData["Success"] = "Logout realizado com sucesso!";
            return RedirectToAction("Login");
        }

        // Método Profile
        public IActionResult Profile()
        {
            var userEmail = HttpContext.Session.GetString("UserEmail");
         
            if (string.IsNullOrEmpty(userEmail))
            {
                return RedirectToAction("Login");
            }

            var utilizador = _context.Utilizador.FirstOrDefault(u => u.Email == userEmail);

            if (utilizador == null)
            {
                return NotFound();
            }

            return View(utilizador); // Passa o utilizador para a view
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
                return NotFound();
            }

            var utilizador = await _context.Utilizador.FindAsync(id);
            if (utilizador == null)
            {
                return NotFound();
            }
            return View(utilizador);
        }

        // POST: Utilizadors/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("UtilizadorId,Email, Nome, Password")] Utilizador utilizador)
        {
            
            if (id != utilizador.UtilizadorId)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {



                    _context.Update(utilizador);
                    await _context.SaveChangesAsync();


                    TempData["Message"] = "Alterações realizadas com sucesso!";
                }
                catch (DbUpdateConcurrencyException)
                {
                    TempData["Message"] = "Alterações realizadas com su!";
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
                return RedirectToAction(nameof(Index));
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

        
    }
}

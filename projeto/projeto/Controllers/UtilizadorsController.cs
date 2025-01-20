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

            _context.Add(utilizador);
            await _context.SaveChangesAsync();

            await RegisterLog("Novo utilizador registrado.", utilizador.UtilizadorId, true);

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
                var utilizador = await _context.Utilizador.FirstOrDefaultAsync(u => u.Email == loginModel.Email);

                if (utilizador != null)
                {
                    // Verifica se a conta está bloqueada
                    if (utilizador.EstadoConta == EstadoConta.Bloqueada)
                    {
                        // Verifica se já passaram 20 segundos desde o bloqueio
                        var logBloqueio = await _context.LogUtilizadores
                            .Where(log => log.Utilizador.UtilizadorId == utilizador.UtilizadorId &&
                                          log.LogMessage.Contains("foi bloqueada"))
                            .OrderByDescending(log => log.LogDataLogin)
                            .FirstOrDefaultAsync();

                        if (logBloqueio != null && logBloqueio.LogDataLogin.AddSeconds(20) <= DateTime.Now)
                        {
                            utilizador.EstadoConta = EstadoConta.Ativa;
                            _context.Update(utilizador);
                            await _context.SaveChangesAsync();

                            TempData["Info"] = "Sua conta foi desbloqueada automaticamente. Tente novamente.";
                        }
                        else
                        {
                            ModelState.AddModelError(string.Empty, "A sua conta está bloqueada. Tente novamente mais tarde.");
                            return View(loginModel);
                        }
                    }

                    // Verifica a senha
                    if (BCrypt.Net.BCrypt.Verify(loginModel.Password, utilizador.Password))
                    {
                        // Registra um log de sucesso
                        await RegisterLog("Login bem-sucedido.", utilizador.UtilizadorId, true);

                        HttpContext.Session.SetString("UserEmail", utilizador.Email);
                        HttpContext.Session.SetString("UserNome", utilizador.Nome);

                        TempData["Success"] = "Login realizado com sucesso!";
                        return RedirectToAction("Index", "Home");
                    }
                    else
                    {
                        // Registra um log de falha
                        await RegisterLog("Tentativa de login falhada.", utilizador.UtilizadorId, false);

                        var vinteSegundosAtras = DateTime.Now.AddSeconds(-20);

                        var falhasRecentes = await _context.LogUtilizadores
                            .Where(log => log.Utilizador.UtilizadorId == utilizador.UtilizadorId &&
                                          !log.IsLoginSuccess &&
                                          log.LogDataLogin >= vinteSegundosAtras)
                            .CountAsync();

                        if (falhasRecentes >= 3)
                        {
                            utilizador.EstadoConta = EstadoConta.Bloqueada;
                            _context.Update(utilizador);
                            await _context.SaveChangesAsync();

                            await RegisterLog("A conta foi bloqueada devido a múltiplas tentativas falhadas.", utilizador.UtilizadorId, false);

                            ModelState.AddModelError(string.Empty, "A sua conta foi bloqueada devido a várias tentativas falhadas. Tente novamente mais tarde.");
                            return View(loginModel);
                        }
                        else
                        {
                            ModelState.AddModelError(string.Empty, $"Credenciais inválidas. Tentativas restantes antes de bloqueio: {3 - falhasRecentes}");
                        }
                    }
                }
                else
                {
                    ModelState.AddModelError(string.Empty, "E-mail ou senha incorretos.");
                }
            }
            return View(loginModel);
        }

        // Método Logout
        public async Task<IActionResult> LogoutAsync()
        {
            var userEmail = HttpContext.Session.GetString("UserEmail");
            var utilizador = _context.Utilizador.FirstOrDefault(u => u.Email == userEmail);

            if (utilizador != null)
            {
                await RegisterLog("Utilizador efetuou logout.", utilizador.UtilizadorId, true);
            }

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

                    await RegisterLog("Utilizador atualizou o perfil.", utilizador.UtilizadorId, true);

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

        // Método para "Esqueci Minha Senha" - POST
        // Método ForgotPassword (GET)
        public IActionResult ForgotPassword()
        {
            return View();
        }

        // Método ForgotPassword (POST)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ForgotPassword(string email)
        {
            if (string.IsNullOrEmpty(email))
            {
                ModelState.AddModelError("Email", "O e-mail é obrigatório.");
                return View();
            }

            var utilizador = await _context.Utilizador.FirstOrDefaultAsync(u => u.Email == email);
            if (utilizador == null)
            {
                ModelState.AddModelError("Email", "E-mail não encontrado.");
                return View();
            }

            var random = new Random();
            int verificationCode = random.Next(100000, 999999);

            var verificationModel = new VerificationModel
            {
                VerificationCode = verificationCode
            };

            _context.VerificationModel.Add(verificationModel);
            await _context.SaveChangesAsync();

            await RegisterLog("O código de verificação é " + verificationCode + ".", utilizador.UtilizadorId, true);
            TempData["Info"] = $"O código de verificação é {verificationCode}.";

            // Armazena o e-mail na sessão para validação posterior
            HttpContext.Session.SetString("ResetEmail", email);

            return RedirectToAction("VerificationCode");
        }

        // Método VerificationCode (GET)
        public IActionResult VerificationCode()
        {
            return View();
        }

        // Método VerificationCode (POST)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> VerificationCode(int verificationCode)
        {
            var email = HttpContext.Session.GetString("ResetEmail");
            if (string.IsNullOrEmpty(email))
            {
                TempData["Error"] = "Sessão expirada. Tente novamente.";
                return RedirectToAction("ForgotPassword");
            }

            var verification = await _context.VerificationModel
                .OrderByDescending(v => v.RequestTime)
                .FirstOrDefaultAsync(v => v.VerificationCode == verificationCode);

            if (verification == null)
            {
                TempData["Error"] = "Código de verificação inválido.";
                return View();
            }

            // Código válido, redireciona para a redefinição de senha
            return RedirectToAction("ResetPassword");
        }

        // Método para "Redefinir Senha" - GET
        public IActionResult ResetPassword()
        {
            // Verifica se o usuário está tentando redefinir a senha
            var email = HttpContext.Session.GetString("ResetEmail");
            if (string.IsNullOrEmpty(email))
            {
                return RedirectToAction("ForgotPassword");
            }

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetPassword(string newPassword)
        {
            // Obtém o e-mail armazenado na sessão
            var email = HttpContext.Session.GetString("ResetEmail");

            // Verifica se o e-mail está disponível
            if (string.IsNullOrEmpty(email))
            {
                TempData["Error"] = "Sessão expirada. Tente novamente.";
                return RedirectToAction("ForgotPassword");
            }

            // Valida a nova senha
            if (string.IsNullOrEmpty(newPassword))
            {
                ModelState.AddModelError("newPassword", "A nova senha é obrigatória.");
            }
            else if (newPassword.Length < 6)
            {
                ModelState.AddModelError("newPassword", "A nova senha deve ter pelo menos 6 caracteres.");
            }

            // Caso existam erros de validação
            if (!ModelState.IsValid)
            {
                return View();
            }

            // Busca o utilizador pelo e-mail
            var utilizador = await _context.Utilizador.FirstOrDefaultAsync(u => u.Email == email);

            if (utilizador != null)
            {
                // Atualiza a senha com o hash
                utilizador.Password = BCrypt.Net.BCrypt.HashPassword(newPassword);

                // Atualiza o utilizador no banco de dados
                _context.Utilizador.Update(utilizador);
                await _context.SaveChangesAsync();

                // Registra o log
                await RegisterLog("Senha redefinida pelo utilizador.", utilizador.UtilizadorId, true);

                // Exibe a mensagem de sucesso e redireciona para a página de Login
                TempData["Success"] = "Senha redefinida com sucesso! Faça login com a nova senha.";
                return RedirectToAction("Login");
            }

            // Caso o utilizador não seja encontrado
            TempData["Error"] = "Utilizador não encontrado.";
            return View();
        }

        private async Task RegisterLog(string logMessage, int utilizadorId, bool isLoginSuccess)
        {
            var utilizador = await _context.Utilizador.FindAsync(utilizadorId);

            if (utilizador != null)
            {
                var log = new LogUtilizador
                {
                    Utilizador = utilizador,
                    LogMessage = logMessage,
                    LogDataLogin = DateTime.Now,
                    LogUtilizadorEmail = utilizador.Email,
                    IsLoginSuccess = isLoginSuccess
                };

                _context.LogUtilizadores.Add(log);
                await _context.SaveChangesAsync();
            }
        }

    }
}

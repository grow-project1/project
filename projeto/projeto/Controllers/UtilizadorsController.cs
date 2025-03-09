
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using projeto.Data;
using projeto.Models;

namespace projeto.Controllers
{
    public class UtilizadorsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _webHostEnvironment;
        private readonly IConfiguration _configuration;

        public UtilizadorsController(ApplicationDbContext context, IWebHostEnvironment webHostEnvironment, IConfiguration configuration)
        {
            _context = context;
            _webHostEnvironment = webHostEnvironment;
            _configuration = configuration;
        }

        public IActionResult ToggleLanguage()
        {
            // Obtém o idioma atual do cookie (se não existir, assume "en")
            string currentLanguage = Request.Cookies["language"] ?? "en";

            // Alterna entre "en" e "pt"
            string newLanguage = currentLanguage == "en" ? "pt" : "en";

            // Define o novo idioma no cookie com validade de 1 ano
            CookieOptions options = new CookieOptions
            {
                Expires = DateTime.UtcNow.AddYears(1),
                HttpOnly = false // Permite acesso pelo JavaScript
            };

            Response.Cookies.Append("language", newLanguage, options);

            // Redireciona de volta para a página atual
            return Redirect(Request.Headers["Referer"].ToString());
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
                ModelState.AddModelError("Nome", "Name is required");
            }

            if (string.IsNullOrEmpty(utilizador.Email))
            {
                ModelState.AddModelError("Email", "Email is required");
            }

            if (string.IsNullOrEmpty(utilizador.Password))
            {
                ModelState.AddModelError("Password", "Password is required");
            }
            else if (utilizador.Password.Length < 6)
            {
                ModelState.AddModelError("Password", "Password must be at least 6 characters");
            }

            if (!ModelState.IsValid)
            {
                return View(utilizador);
            }

            var userExists = await _context.Utilizador.AnyAsync(u => u.Email == utilizador.Email);
            if (userExists)
            {
                ModelState.AddModelError("Email", "This email is alredy registered");
                return View(utilizador);
            }

            utilizador.Password = BCrypt.Net.BCrypt.HashPassword(utilizador.Password);

            _context.Add(utilizador);
            await _context.SaveChangesAsync();

            await RegisterLog("Novo utilizador registrado.", utilizador.UtilizadorId, true);

            TempData["Success"] = "Account successfully created! Please log in to access your account";

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

                            TempData["Info"] = "Your account was automatically unlocked. Try again.";
                        }
                        else
                        {
                            ModelState.AddModelError(string.Empty, "Your account is locked. Try again later.");
                            return View(loginModel);
                        }
                    }

                    if (BCrypt.Net.BCrypt.Verify(loginModel.Password, utilizador.Password))
                    {
                        await RegisterLog("Login bem-sucedido.", utilizador.UtilizadorId, true);

                        HttpContext.Session.SetString("UserEmail", utilizador.Email);
                        HttpContext.Session.SetString("UserNome", utilizador.Nome);

                        return RedirectToAction("Index", "Leilaos");
                    }
                    else
                    {
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

                            ModelState.AddModelError(string.Empty, "Your account was locked due to multiple failed attempts. Try again later");
                            return View(loginModel);
                        }
                        else
                        {
                            ModelState.AddModelError(string.Empty, $"Invalid credentials. Remaining attempts before lock: {3 - falhasRecentes}");
                        }
                    }
                }
                else
                {
                    ModelState.AddModelError(string.Empty, "Email or password is incorrect");
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
            TempData["Success"] = "Logout successuful!";
            return RedirectToAction("Login");
        }

        public async Task<IActionResult> ProfileAsync()
        {
            var userEmail = HttpContext.Session.GetString("UserEmail");

            var user = await _context.Utilizador.FirstOrDefaultAsync(u => u.Email == userEmail);

            ViewData["UserPoints"] = user?.Pontos;

            if (string.IsNullOrEmpty(userEmail))
            {
                return RedirectToAction("Login");
            }

            if (user == null)
            {
                return NotFound();
            }

            return View(user); 
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
        public async Task<IActionResult> Edit(int id, [Bind("UtilizadorId,Nome,Morada,CodigoPostal,Pais,Telemovel")] Utilizador utilizador)
        {
            if (id != utilizador.UtilizadorId)
            {
                return NotFound();
            }

            var utilizadorExistente = await _context.Utilizador.FindAsync(id);
            if (utilizadorExistente == null)
            {
                return NotFound();
            }

            utilizadorExistente.Nome = utilizador.Nome;
            utilizadorExistente.Morada = utilizador.Morada;
            utilizadorExistente.CodigoPostal = utilizador.CodigoPostal;
            utilizadorExistente.Pais = utilizador.Pais;
            utilizadorExistente.Telemovel = utilizador.Telemovel;

            try
            {
                _context.Update(utilizadorExistente);
                await _context.SaveChangesAsync();

                // Adiciona mensagem de sucesso
                TempData["Success"] = "Perfil atualizado com sucesso!";

                return RedirectToAction("Profile", new { id = utilizadorExistente.UtilizadorId });
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
                ModelState.AddModelError("Email", "Email is required.");
                return View();
            }

            var utilizador = await _context.Utilizador.FirstOrDefaultAsync(u => u.Email == email);
            if (utilizador == null)
            {
                ModelState.AddModelError("Email", "Email not found.");
                return View();
            }

            var random = new Random();
            int verificationCode = random.Next(100000, 999999);

            var verificationModel = new VerificationModel
            {
                VerificationCode = verificationCode
            };

            var emailSender = new EmailSender(_configuration); 
            string subject = "Código de Verificação";
            string message = $"Seu código de verificação é: {verificationCode}";
            await emailSender.SendEmailAsync(email, subject, message);


            _context.VerificationModel.Add(verificationModel);
            await _context.SaveChangesAsync();

            await RegisterLog("O código de verificação é " + verificationCode + ".", utilizador.UtilizadorId, true);

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
                TempData["Error"] = "Session expired. Please try again.";
                return RedirectToAction("ForgotPassword");
            }

            var verification = await _context.VerificationModel
                .OrderByDescending(v => v.RequestTime)
                .FirstOrDefaultAsync(v => v.VerificationCode == verificationCode);

            if (verification == null)
            {
                TempData["Error"] = "Invalid code";
                return View();
            }

            // Código válido, redireciona para a redefinição de senha
            return RedirectToAction("ResetPassword");
        }

        // Método para "Redefinir Senha" - GET
        public IActionResult ResetPassword()
        {
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
            var email = HttpContext.Session.GetString("ResetEmail");

            if (string.IsNullOrEmpty(email))
            {
                TempData["Error"] = "Session expired. Please try again.";
                return RedirectToAction("ForgotPassword");
            }

            if (string.IsNullOrEmpty(newPassword))
            {
                ModelState.AddModelError("newPassword", "New password is required.");
            }
            else if (newPassword.Length < 6)
            {
                ModelState.AddModelError("newPassword", "Password must have at least 6 characters.");
            }

            var utilizador = await _context.Utilizador.FirstOrDefaultAsync(u => u.Email == email);

            if (utilizador == null)
            {
                TempData["Error"] = "User not found";
                return View();
            }

            // 🔴 Verificar se a nova palavra-passe é igual à atual
            if (BCrypt.Net.BCrypt.Verify(newPassword, utilizador.Password))
            {
                ModelState.AddModelError("newPassword", "The new password cannot be the same as the current password.");
            }

            if (!ModelState.IsValid)
            {
                return View();
            }

            // Se passou nas verificações, atualiza a palavra-passe
            utilizador.Password = BCrypt.Net.BCrypt.HashPassword(newPassword);
            _context.Utilizador.Update(utilizador);
            await _context.SaveChangesAsync();

            await RegisterLog("Senha redefinida pelo utilizador.", utilizador.UtilizadorId, true);

            TempData["Success"] = "Password successfully reset! Please log in with the new password.";
            return RedirectToAction("Login");
        }


        public IActionResult ConfirmPassword(int id)
        {
            var utilizador = _context.Utilizador.Find(id);
            if (utilizador == null)
            {
                return NotFound();
            }

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ConfirmPassword(int id, string currentPassword)
        {
            var utilizador = await _context.Utilizador.FindAsync(id);
            if (utilizador == null)
            {
                ModelState.AddModelError("confirmPassword", "User not found");
                return View();
            }

            // Verificar se a senha atual está correta
            if (!BCrypt.Net.BCrypt.Verify(currentPassword, utilizador.Password))
            {
                ModelState.AddModelError("confirmPassword", "Invalid password");
                return View();
            }

            // Redireciona para a página de atualização da nova senha
            return RedirectToAction("UpdatePassword", new { id = utilizador.UtilizadorId });
        }

        public IActionResult UpdatePassword(int id)
        {
            var utilizador = _context.Utilizador.Find(id);
            if (utilizador == null)
            {
                return NotFound();
            }

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdatePassword(int id, string newPassword, string confirmPassword)
        {
            // Verifica se a nova senha tem pelo menos 6 caracteres
            if (string.IsNullOrEmpty(newPassword) || newPassword.Length < 6)
            {
                ModelState.AddModelError("newPassword", "New passwrod must have atleast 6 characters");
            }

            // Verifica se a confirmação da senha coincide com a nova senha
            if (newPassword != confirmPassword)
            {
                ModelState.AddModelError("confirmPassword", "The passwords do not match");
            }

            if (!ModelState.IsValid)
            {
                return View();
            }

            var utilizador = await _context.Utilizador.FindAsync(id);
            if (utilizador == null)
            {
                ModelState.AddModelError(string.Empty, "User not found");
                return View();
            }

            // Atualiza a senha com o hash
            utilizador.Password = BCrypt.Net.BCrypt.HashPassword(newPassword);
            await _context.SaveChangesAsync();

            TempData["Message"] = "Password successfully reset!";
            return RedirectToAction("Profile", new { id = utilizador.UtilizadorId });
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

        public IActionResult EditAvatar(int id)
        {
            var utilizador = _context.Utilizador.Find(id);
            if (utilizador == null)
            {
                return NotFound();
            }

            // Diretório onde os avatares estão armazenados
            string avatarDirectory = Path.Combine(_webHostEnvironment.WebRootPath, "images");
            var avatars = Directory.GetFiles(avatarDirectory, "avatar*.png") // Filtra arquivos que começam com "avatar"
                                   .Select(Path.GetFileName)
                                   .ToList();

            ViewBag.AvatarList = avatars;
            return View(utilizador);
        }

        [HttpPost]
        public IActionResult EditAvatar(int id, string selectedAvatar)
        {
            var utilizador = _context.Utilizador.Find(id);
            if (utilizador == null)
            {
                return NotFound();
            }

            if (!string.IsNullOrEmpty(selectedAvatar))
            {
                utilizador.ImagePath = "~/images/" + selectedAvatar;
                _context.Update(utilizador);
                _context.SaveChanges();
            }

            return RedirectToAction("Profile");
        }
    }
}

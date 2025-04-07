
// ========================================================
// Autores: Samuel Alves, Rodrigo Baião, Isidoro Ornelas, Miguel Pinto
// Projeto: Sistema de Gestão de Utilizadores
// Descrição: Controlador responsável pela gestão de utilizadores, incluindo registo, login, alteração de idioma e perfil.
// ========================================================

using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using projeto.Data;
using projeto.Models;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Identity;
using Stripe;
using Microsoft.VisualStudio.Web.CodeGenerators.Mvc.Templates.BlazorIdentity.Pages.Manage;
using System.Globalization;

namespace projeto.Controllers
{
    // ========================================================
    // Autor: Samuel Alves
    // Controlador responsável pela gestão de utilizadores
    // ========================================================
    public class UtilizadorsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _webHostEnvironment;
        private readonly IConfiguration _configuration;
        private readonly IEmailSender _emailSender;

        // ========================================================
        // Autor: Rodrigo Baião
        // Construtor do controlador, injeta as dependências necessárias
        // ========================================================
        public UtilizadorsController(ApplicationDbContext context, IWebHostEnvironment webHostEnvironment, IConfiguration configuration, IEmailSender emailSender)
        {
            _context = context;
            _webHostEnvironment = webHostEnvironment;
            _configuration = configuration;
            _emailSender = emailSender;
        }

        // ========================================================
        // Autor: Isidoro Ornelas
        // Método para alternar o idioma do site
        // ========================================================
        public IActionResult ToggleLanguage(string language)
        {
            if (language == "en" || language == "pt")
            {
                // Definindo o idioma no cookie
                Response.Cookies.Append("language", language, new CookieOptions
                {
                    Expires = DateTime.Now.AddYears(1), // O cookie expira em 1 ano
                    IsEssential = true // Marcar o cookie como essencial
                });
            }

            // Redirecionando para a página inicial ou para a página atual
            return Redirect(Request.Headers["Referer"].ToString());
        }

        // ========================================================
        // Autor: Miguel Pinto
        // Método GET para registo de um novo utilizador
        // ========================================================
        public IActionResult Register()
        {
            return View();
        }

        // ========================================================
        // Autor: Samuel Alves
        // Método POST para registo de um novo utilizador
        // ========================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register([Bind("Nome,Email,Password")] Utilizador utilizador, bool acceptedTerms)
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
            else if (!IsPasswordStrong(utilizador.Password))
            {
                ModelState.AddModelError("Password", "Password must have at least 6 characters, one uppercase letter, one number, and one special character.");
            }

            // Verifica se os termos foram aceitos
            if (!acceptedTerms)
            {
                ModelState.AddModelError(string.Empty, "You must accept the Terms and Conditions.");
            }

            if (!ModelState.IsValid)
            {
                return View(utilizador);
            }

            var userExists = await _context.Utilizador.AnyAsync(u => u.Email == utilizador.Email);
            if (userExists)
            {
                ModelState.AddModelError("Email", "This email is already registered");
                return View(utilizador);
            }

            // Gera o código de verificação
            var random = new Random();
            int verificationCode = random.Next(100000, 999999);

            // Envia email com o código
            string subject = "Código de Verificação - Confirmação de Registo";
            string message = $"Seu código de verificação é: {verificationCode}";
            await _emailSender.SendEmailAsync(utilizador.Email, subject, message);

            // Guarda tudo na sessão (ou TempData)
            HttpContext.Session.SetString("PendingRegName", utilizador.Nome);
            HttpContext.Session.SetString("PendingRegEmail", utilizador.Email);
            HttpContext.Session.SetString("PendingRegPassword", utilizador.Password);

            // Armazena também o code
            HttpContext.Session.SetString("PendingRegCode", verificationCode.ToString());
            Console.WriteLine($"*Codigo no controller*: {verificationCode}");

            TempData["Info"] = "We sent a verification code to your email. Please confirm.";
            return RedirectToAction("ConfirmRegistration");
        }

        // ========================================================
        // Autor: Rodrigo Baião
        // Método GET para confirmar o registo com o código de verificação
        // ========================================================
        public IActionResult ConfirmRegistration()
        {
            return View();
        }

        // ========================================================
        // Autor: Isidoro Ornelas
        // Método POST para confirmar o registo após verificação do código
        // ========================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ConfirmRegistration(int verificationCode)
        {
            // Resgata dados da sessão
            var pendingName = HttpContext.Session.GetString("PendingRegName");
            var pendingEmail = HttpContext.Session.GetString("PendingRegEmail");
            var pendingPass = HttpContext.Session.GetString("PendingRegPassword");
            int? storedCode = int.Parse(HttpContext.Session.GetString("PendingRegCode"));

            if (string.IsNullOrEmpty(pendingEmail) || storedCode == null)
            {
                // Sessão expirou, ou o user já foi criado
                TempData["Error"] = "Session expired. Please register again.";
                return RedirectToAction("Register");
            }

            // Verifica o code
            if (verificationCode != storedCode.Value)
            {
                TempData["Error"] = "Invalid code. Please try again.";
                return View();
            }

            // OK: Criar o utilizador no DB
            var utilizador = new Utilizador
            {
                Nome = pendingName,
                Email = pendingEmail,
                Password = BCrypt.Net.BCrypt.HashPassword(pendingPass),
            };

            _context.Utilizador.Add(utilizador);
            await _context.SaveChangesAsync();

            // Limpa a sessão
            HttpContext.Session.Remove("PendingRegName");
            HttpContext.Session.Remove("PendingRegEmail");
            HttpContext.Session.Remove("PendingRegPassword");
            HttpContext.Session.Remove("PendingRegCode");

            TempData["Success"] = "Account confirmed! Please log in.";
            return RedirectToAction("Login");
        }

        // ========================================================
        // Autor: Miguel Pinto
        // Função para validar a força da senha
        // ========================================================
        private bool IsPasswordStrong(string password)
        {
            return password.Length >= 6 &&
                   password.Any(char.IsUpper) &&
                   password.Any(char.IsDigit) &&
                   password.Any(ch => !char.IsLetterOrDigit(ch));
        }

        // ========================================================
        // Autor: Samuel Alves
        // Método GET para login de utilizador
        // ========================================================
        public IActionResult Login()
        {
            return View();
        }

        // ========================================================
        // Autor: Rodrigo Baião
        // Método POST para login de utilizador
        // ========================================================
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

        // ========================================================
        // Autor: Isidoro Ornelas
        // Método para fazer logout do utilizador
        // ========================================================
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
        // ========================================================
        // Autor: Samuel Alves
        // GET: ProfileAsync
        // Descrição: Exibe o perfil do utilizador e verifica se o utilizador está logado.
        // ========================================================
        public async Task<IActionResult> ProfileAsync()
        {
            // Obtém o e-mail do utilizador da sessão
            var userEmail = HttpContext.Session.GetString("UserEmail");

            // Busca o utilizador no banco de dados utilizando o e-mail da sessão
            var user = await _context.Utilizador.FirstOrDefaultAsync(u => u.Email == userEmail);

            // Adiciona os pontos do utilizador à ViewData para poder acessá-los no frontend
            ViewData["UserPoints"] = user?.Pontos;

            // Se o e-mail não for encontrado, redireciona para a página de login
            if (string.IsNullOrEmpty(userEmail))
            {
                return RedirectToAction("Login");
            }

            // Se o utilizador não for encontrado, retorna um erro 404
            if (user == null)
            {
                return NotFound();
            }

            // Exibe a view de perfil do utilizador
            return View(user);
        }

        // ========================================================
        // Autor: Samuel Alves
        // GET: Index
        // Descrição: Lista todos os utilizadores.
        // ========================================================
        public async Task<IActionResult> Index()
        {
            // Exibe a lista de todos os utilizadores
            return View(await _context.Utilizador.ToListAsync());
        }

        // ========================================================
        // Autor: Samuel Alves
        // GET: Details
        // Descrição: Exibe os detalhes do utilizador selecionado.
        // ========================================================
        public async Task<IActionResult> Details(int? id)
        {
            // Se o ID for nulo, retorna um erro 404
            if (id == null)
            {
                return NotFound();
            }

            // Busca o utilizador pelo ID fornecido
            var utilizador = await _context.Utilizador
                .FirstOrDefaultAsync(m => m.UtilizadorId == id);

            // Se o utilizador não for encontrado, retorna um erro 404
            if (utilizador == null)
            {
                return NotFound();
            }

            // Exibe a view com os detalhes do utilizador
            return View(utilizador);
        }

        // ========================================================
        // Autor: Miguel Pinto
        // GET: Create
        // Descrição: Exibe a view para criar um novo utilizador.
        // ========================================================
        public IActionResult Create()
        {
            // Exibe a view de criação de um novo utilizador
            return View();
        }

        // ========================================================
        // Autor: Miguel Pinto
        // POST: Create
        // Descrição: Cria um novo utilizador no banco de dados.
        // ========================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("UtilizadorId,Email,Password")] Utilizador utilizador)
        {
            // Se o modelo for válido, adiciona o utilizador no banco de dados e salva as alterações
            if (ModelState.IsValid)
            {
                _context.Add(utilizador);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }

            // Se o modelo não for válido, exibe a view novamente com os erros
            return View(utilizador);
        }

        // ========================================================
        // Autor: Isidoro Ornelas
        // GET: Edit
        // Descrição: Exibe a view para editar os dados do utilizador.
        // ========================================================
        public async Task<IActionResult> Edit(int? id)
        {
            // Obtém o e-mail do utilizador da sessão
            var userEmail = HttpContext.Session.GetString("UserEmail");

            // Busca o utilizador no banco de dados utilizando o e-mail da sessão
            var user = await _context.Utilizador.FirstOrDefaultAsync(u => u.Email == userEmail);

            // Adiciona os pontos do utilizador à ViewData para poder acessá-los no frontend
            ViewData["UserPoints"] = user?.Pontos;

            // Se o ID for nulo, retorna um erro 404
            if (id == null)
            {
                return NotFound();
            }

            // Busca o utilizador pelo ID fornecido
            var utilizador = await _context.Utilizador.FindAsync(id);

            // Se o utilizador não for encontrado, retorna um erro 404
            if (utilizador == null)
            {
                return NotFound();
            }

            // Exibe a view de edição de dados do utilizador
            return View(utilizador);
        }

        // ========================================================
        // Autor:  Isidoro Ornelas
        // POST: Edit
        // Descrição: Atualiza os dados do utilizador no banco de dados.
        // ========================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("UtilizadorId,Nome,Morada,CodigoPostal,Pais,Telemovel")] Utilizador utilizador)
        {
            // Se o ID não corresponder ao utilizador fornecido, retorna um erro 404
            if (id != utilizador.UtilizadorId)
            {
                return NotFound();
            }

            // Busca o utilizador existente no banco de dados
            var utilizadorExistente = await _context.Utilizador.FindAsync(id);

            // Se o utilizador não for encontrado, retorna um erro 404
            if (utilizadorExistente == null)
            {
                return NotFound();
            }

            // Atualiza os dados do utilizador existente com os novos dados
            utilizadorExistente.Nome = utilizador.Nome;
            utilizadorExistente.Morada = utilizador.Morada;
            utilizadorExistente.CodigoPostal = utilizador.CodigoPostal;
            utilizadorExistente.Pais = utilizador.Pais;
            utilizadorExistente.Telemovel = utilizador.Telemovel;

            try
            {
                // Atualiza o utilizador no banco de dados e salva as alterações
                _context.Update(utilizadorExistente);
                await _context.SaveChangesAsync();

                // Exibe uma mensagem de sucesso e redireciona para o perfil do utilizador
                TempData["SuccessMessage"] = "Profile saved";
                return RedirectToAction("Profile", new { id = utilizadorExistente.UtilizadorId });
            }
            catch (DbUpdateConcurrencyException)
            {
                // Se ocorrer um erro de concorrência, retorna um erro 404
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

        // ========================================================
        // Autor: Rodrigo Baião
        // GET: Delete
        // Descrição: Exibe a view para deletar o utilizador.
        // ========================================================
        public async Task<IActionResult> Delete(int? id)
        {
            // Se o ID for nulo, retorna um erro 404
            if (id == null)
            {
                return NotFound();
            }

            // Busca o utilizador pelo ID fornecido
            var utilizador = await _context.Utilizador
                .FirstOrDefaultAsync(m => m.UtilizadorId == id);

            // Se o utilizador não for encontrado, retorna um erro 404
            if (utilizador == null)
            {
                return NotFound();
            }

            // Exibe a view de confirmação de exclusão do utilizador
            return View(utilizador);
        }

        // ========================================================
        // Autor: Rodrigo Baião
        // POST: Delete
        // Descrição: Deleta o utilizador do banco de dados.
        // ========================================================
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            // Busca o utilizador pelo ID fornecido
            var utilizador = await _context.Utilizador.FindAsync(id);

            // Se o utilizador for encontrado, remove-o do banco de dados
            if (utilizador != null)
            {
                _context.Utilizador.Remove(utilizador);
            }

            // Salva as alterações no banco de dados e redireciona para a lista de utilizadores
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        // ========================================================
        // Autor: Miguel Pinto
        // Método para verificar se o utilizador existe
        // ========================================================
        private bool UtilizadorExists(int id)
        {
            return _context.Utilizador.Any(e => e.UtilizadorId == id);
        }

        // ========================================================
        // Autor: Miguel Pinto
        // GET: ForgotPassword
        // Descrição: Exibe a view para recuperação de senha.
        // ========================================================
        public IActionResult ForgotPassword()
        {
            // Exibe a view de recuperação de senha
            return View();
        }

        // ========================================================
        // Autor: Miguel Pinto
        // POST: ForgotPassword
        // Descrição: Envia um código de verificação para o e-mail do utilizador para recuperação de senha.
        // ========================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ForgotPassword(string email)
        {
            // Se o e-mail estiver vazio, adiciona um erro no modelo
            if (string.IsNullOrEmpty(email))
            {
                ModelState.AddModelError("Email", "Email is required.");
                return View();
            }

            // Busca o utilizador no banco de dados utilizando o e-mail fornecido
            var utilizador = await _context.Utilizador.FirstOrDefaultAsync(u => u.Email == email);

            // Se o utilizador não for encontrado, adiciona um erro no modelo
            if (utilizador == null)
            {
                ModelState.AddModelError("Email", "Email not found.");
                return View();
            }

            // Gera um código de verificação aleatório
            var random = new Random();
            int verificationCode = random.Next(100000, 999999);

            // Cria um modelo de verificação e envia o código de verificação por e-mail
            var verificationModel = new VerificationModel
            {
                VerificationCode = verificationCode
            };

            string subject = "Código de Verificação";
            string message = $"Seu código de verificação é: {verificationCode}";
            await _emailSender.SendEmailAsync(email, subject, message);

            // Adiciona o modelo de verificação ao banco de dados
            _context.VerificationModel.Add(verificationModel);
            await _context.SaveChangesAsync();

            // Registra o log com o código de verificação
            await RegisterLog("O código de verificação é " + verificationCode + ".", utilizador.UtilizadorId, true);

            // Armazena o e-mail na sessão e redireciona para a página de verificação do código
            HttpContext.Session.SetString("ResetEmail", email);
            return RedirectToAction("VerificationCode");
        }

        // ========================================================
        // Autor: Rodrigo Baião
        // GET: VerificationCode
        // Descrição: Exibe a view para inserir o código de verificação enviado por e-mail.
        // ========================================================
        public IActionResult VerificationCode()
        {
            // Exibe a view de verificação do código
            return View();
        }

        // ========================================================
        // Autor: Rodrigo Baião
        // POST: VerificationCode
        // Descrição: Valida o código de verificação inserido pelo utilizador.
        // ========================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> VerificationCode(int verificationCode)
        {
            // Obtém o e-mail armazenado na sessão
            var email = HttpContext.Session.GetString("ResetEmail");

            // Se o e-mail estiver vazio, informa que a sessão expirou
            if (string.IsNullOrEmpty(email))
            {
                TempData["Error"] = "Session expired. Please try again.";
                return RedirectToAction("ForgotPassword");
            }

            // Busca o código de verificação no banco de dados
            var verification = await _context.VerificationModel
                .OrderByDescending(v => v.RequestTime)
                .FirstOrDefaultAsync(v => v.VerificationCode == verificationCode);

            // Se o código não for encontrado, exibe um erro
            if (verification == null)
            {
                TempData["Error"] = "Invalid code";
                return View();
            }

            // Redireciona para a página de redefinição de senha
            return RedirectToAction("ResetPassword");
        }

        // ========================================================
        // Autor: Samuel Alves
        // Descrição: Exibe a view para redefinir a senha do utilizador.
        // ========================================================
        public IActionResult ResetPassword()
        {
            // Obtém o e-mail da sessão
            var email = HttpContext.Session.GetString("ResetEmail");

            // Se o e-mail estiver vazio, redireciona para a página de recuperação de senha
            if (string.IsNullOrEmpty(email))
            {
                return RedirectToAction("ForgotPassword");
            }

            // Exibe a view para redefinir a senha
            return View();
        }
        // ========================================================
        // Autor: Samuel Alves
        // Método para redefinir a senha do utilizador
        // ========================================================
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

            // Verifica se a password foi preenchida (mas sem adicionar erro manual)
            if (!ModelState.IsValid)
            {
                return View(); // ASP.NET já adicionou "The Password field is required."
            }

            if (!IsPasswordStrong(newPassword))
            {
                ModelState.AddModelError("newPassword", "Password must have at least 6 characters, one uppercase letter, one number, and one special character.");
                return View();
            }

            var utilizador = await _context.Utilizador.FirstOrDefaultAsync(u => u.Email == email);

            if (utilizador == null)
            {
                TempData["Error"] = "User not found";
                return View();
            }

            if (BCrypt.Net.BCrypt.Verify(newPassword, utilizador.Password))
            {
                ModelState.AddModelError("newPassword", "The new password cannot be the same as the current password.");
                return View();
            }

            utilizador.Password = BCrypt.Net.BCrypt.HashPassword(newPassword);
            _context.Utilizador.Update(utilizador);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Password successfully reset! Please log in with the new password.";
            return RedirectToAction("Login");
        }

        // ========================================================
        // Autor: Samuel Alves
        // Método para confirmar a senha do utilizador antes de permitir a atualização
        // ========================================================
        public async Task<IActionResult> ConfirmPasswordAsync(int id)
        {
            var utilizador = _context.Utilizador.Find(id);
            var userEmail = HttpContext.Session.GetString("UserEmail");

            var user = await _context.Utilizador.FirstOrDefaultAsync(u => u.Email == userEmail);

            ViewData["UserPoints"] = user?.Pontos;

            if (utilizador == null)
            {
                return NotFound();
            }

            return View();
        }

        // ========================================================
        // Autor: Samuel Alves
        // Método para validar a senha atual antes de permitir a mudança
        // ========================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ConfirmPassword(int id, string currentPassword)
        {
            var utilizador = await _context.Utilizador.FindAsync(id);

            var userEmail = HttpContext.Session.GetString("UserEmail");

            var user = await _context.Utilizador.FirstOrDefaultAsync(u => u.Email == userEmail);

            ViewData["UserPoints"] = user?.Pontos;

            if (utilizador == null)
            {
                ModelState.AddModelError("confirmPassword", "User not found");
                return View();
            }

            if (!BCrypt.Net.BCrypt.Verify(currentPassword, utilizador.Password))
            {
                ModelState.AddModelError("confirmPassword", "Invalid password");
                return View();
            }

            TempData["SuccessMessage"] = "Password changed successfully!";

            return RedirectToAction("UpdatePassword", new { id = utilizador.UtilizadorId });
        }

        // ========================================================
        // Autor: Samuel Alves
        // Método para exibir o formulário de atualização de senha
        // ========================================================
        public IActionResult UpdatePassword(int id)
        {
            var utilizador = _context.Utilizador.Find(id);
            if (utilizador == null)
            {
                return NotFound();
            }

            return View();
        }

        // ========================================================
        // Autor: Samuel Alves
        // Método para processar a atualização de senha
        // ========================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdatePassword(int id, string newPassword, string confirmPassword)
        {
            // Verifica se a nova senha tem pelo menos 6 caracteres
            if (string.IsNullOrEmpty(newPassword) || newPassword.Length < 6)
            {
                ModelState.AddModelError("newPassword", "New password must have at least 6 characters");
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

        // ========================================================
        // Autor: Miguel Pinto
        // Método para registrar logs de atividade do utilizador
        // ========================================================
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
        // ========================================================
        // Autor: Idioro Ornelas
        // Método para editar o avatar do utilizador
        // ========================================================
        public async Task<IActionResult> EditAvatarAsync(int id)
        {
            var utilizador = _context.Utilizador.Find(id);
            var userEmail = HttpContext.Session.GetString("UserEmail");

            var user = await _context.Utilizador.FirstOrDefaultAsync(u => u.Email == userEmail);

            ViewData["UserPoints"] = user?.Pontos;

            if (utilizador == null)
            {
                return NotFound();
            }

            string avatarDirectory = Path.Combine(_webHostEnvironment.WebRootPath, "images");
            var avatars = Directory.GetFiles(avatarDirectory, "avatar*.png")
                                   .Select(Path.GetFileName)
                                   .ToList();

            TempData["SuccessMessage"] = "Avatar updated successfully!";

            ViewBag.AvatarList = avatars;
            return View(utilizador);
        }

        // ========================================================
        // Autor: Isidoro Ornelas
        // Método para processar a edição do avatar do utilizador
        // ========================================================
        [HttpPost]
        public async Task<IActionResult> EditAvatarAsync(int id, string selectedAvatar)
        {
            var userEmail = HttpContext.Session.GetString("UserEmail");

            var user = await _context.Utilizador.FirstOrDefaultAsync(u => u.Email == userEmail);

            ViewData["UserPoints"] = user?.Pontos;

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

        // ========================================================
        // Autor: Isidoro Ornelas
        // Método para visualizar os pagamentos do utilizador
        // ========================================================
        [HttpGet]
        public async Task<IActionResult> Pagamentos()
        {
            var userEmail = HttpContext.Session.GetString("UserEmail");
            var user = await _context.Utilizador.FirstOrDefaultAsync(u => u.Email == userEmail);

            ViewData["UserPoints"] = user?.Pontos;
            if (user == null)
            {
                return RedirectToAction("Login", "Utilizadors");
            }

            var meusLeiloesGanhos = await _context.Leiloes
            .Where(l => l.VencedorId == user.UtilizadorId)
            .Include(l => l.Item)
            .OrderBy(l => l.Pago)
            .ToListAsync();

            var meusLeiloes = await _context.Leiloes
                .Where(l => l.UtilizadorId == user.UtilizadorId)
                .Include(l => l.Item)
                .Include(l => l.Vencedor)
                .OrderBy(l => l.Pago)
                .ToListAsync();

            var viewModel = new PagamentosViewModel
            {
                LeiloesGanhos = meusLeiloesGanhos,
                MeusLeiloes = meusLeiloes
            };

            return View(viewModel);
        }

        // ========================================================
        // Autor: Isidoro Ornelas
        // Método para exibir os detalhes do pagamento do leilão
        // ========================================================
        public async Task<IActionResult> PagamentoDetalhes(int leilaoId)
        {
            var userEmail = HttpContext.Session.GetString("UserEmail");
            var utilizador = await _context.Utilizador.FirstOrDefaultAsync(u => u.Email == userEmail);

            ViewData["UserPoints"] = utilizador?.Pontos;

            var leilao = await _context.Leiloes
                .Include(l => l.Item)
                .Where(l => l.LeilaoId == leilaoId && !l.Pago)
                .FirstOrDefaultAsync();

            if (leilao == null)
            {
                TempData["PaymentError"] = "Leilão não encontrado ou já pago.";
                return RedirectToAction("Pagamentos");
            }

            var descontosDisponiveis = await _context.DescontoResgatado
                    .Include(d => d.Desconto)
                    .Where(d => d.UtilizadorId == utilizador.UtilizadorId && !d.Usado && d.DataValidade >= DateTime.Now)
                    .ToListAsync();

            var viewModel = new PagamentoDetalhesViewModel
            {
                Leilao = leilao,
                Utilizador = utilizador,
                DescontosDisponiveis = descontosDisponiveis
            };

            return View(viewModel);
        }

        // ========================================================
        // Autor: Miguel Pinto
        // Método para processar o pagamento do leilão
        // ========================================================
        [HttpPost]
        public async Task<IActionResult> ProcessarPagamento([FromBody] PagamentoRequest request)
        {
            var stripeOptions = new RequestOptions
            {
                ApiKey = "sk_test_51R3dfgFTcoPiNF4z1IEVgmdqMmjYVS9RRjLuBFybWNHH8nmBmgQDOia2BAWMBMbJZXjkMxlzdDiUCTou1B0BIJO600KNSfV6pO"
            };

            try
            {
                var paymentIntentService = new PaymentIntentService();
                var paymentIntent = await paymentIntentService.CreateAsync(new PaymentIntentCreateOptions
                {
                    Amount = (long)(request.Valor * 100),
                    Currency = "eur",
                    PaymentMethod = request.PaymentMethodId,
                    Confirm = true,
                    AutomaticPaymentMethods = new PaymentIntentAutomaticPaymentMethodsOptions
                    {
                        Enabled = true,
                        AllowRedirects = "never"
                    }
                }, stripeOptions);

                var leilao = await _context.Leiloes.FindAsync(request.LeilaoId);
                leilao.Pago = true;

                var desconto = await _context.DescontoResgatado.FirstOrDefaultAsync(d => d.DescontoResgatadoId == request.DescontoUsadoId);

                var fullName = request.FullName;
                var address = request.Address;
                var city = request.City;
                var postalCode = request.PostalCode;
                var country = request.Country;
                var phone = request.Phone;

                var userEmail = HttpContext.Session.GetString("UserEmail");

                string subject = "O pagamento foi bem sucedido";

                string message = @"
            <html>
            <body style='font-family: Arial, sans-serif; color: #333;'>
                <h3 style='color: #28a745;'>Pagamento Realizado com Sucesso</h3>
                <p><strong>Detalhes da Compra:</strong></p>
                <table style='width: 100%; border: 1px solid #ddd; border-collapse: collapse;'>
                    <tr>
                        <td style='padding: 10px; font-weight: bold;'>Valor Total:</td>
                        <td style='padding: 10px;'>" + request.Valor.ToString("C") + @"</td>
                    </tr>
                    <tr>
                        <td style='padding: 10px; font-weight: bold;'>Nome:</td>
                        <td style='padding: 10px;'>" + fullName + @"</td>
                    </tr>
                    <tr>
                        <td style='padding: 10px; font-weight: bold;'>Endereço:</td>
                        <td style='padding: 10px;'>" + address + @", " + city + @", " + postalCode + @", " + country + @"</td>
                    </tr>
                    <tr>
                        <td style='padding: 10px; font-weight: bold;'>Telefone:</td>
                        <td style='padding: 10px;'>" + phone + @"</td>
                    </tr>
                </table>
                 </body>
            </html>
            ";

                // Enviar o e-mail
                await _emailSender.SendEmailAsync(userEmail, subject, message);

                var vendedor = await _context.Utilizador.FindAsync(leilao.VencedorId);

                if (vendedor != null && !string.IsNullOrEmpty(vendedor.Email))
                {
                    // Mensagem para o vendedor
                    string vendedorMessage = @"
        <html>
        <body style='font-family: Arial, sans-serif; color: #333;'>
            <h3 style='color: #28a745;'>Pagamento Recebido para o Seu Leilão</h3>
            <p><strong>Detalhes do Pagamento:</strong></p>
            <table style='width: 100%; border: 1px solid #ddd; border-collapse: collapse;'>
                <tr>
                    <td style='padding: 10px; font-weight: bold;'>Valor Total:</td>
                    <td style='padding: 10px;'>" + request.Valor.ToString("C") + @"</td>
                </tr>
                <tr>
                    <td style='padding: 10px; font-weight: bold;'>Nome do Comprador:</td>
                    <td style='padding: 10px;'>" + fullName + @"</td>
                </tr>
                <tr>
                    <td style='padding: 10px; font-weight: bold;'>Endereço de Entrega:</td>
                    <td style='padding: 10px;'>" + address + @", " + city + @", " + postalCode + @", " + country + @"</td>
                </tr>
                <tr>
                    <td style='padding: 10px; font-weight: bold;'>Telefone:</td>
                    <td style='padding: 10px;'>" + phone + @"</td>
                </tr>
            </table>
        </body>
        </html>
    ";

                    // Enviar o e-mail para o vendedor
                    await _emailSender.SendEmailAsync(vendedor.Email, "Pagamento Recebido para o Seu Leilão", vendedorMessage);
                }

                if (desconto != null)
                {
                    var descontoValor = _context.Desconto
                        .Where(d => d.DescontoId == desconto.DescontoId)
                        .Select(d => d.Valor)
                        .FirstOrDefault();
                    await GerarFatura(leilao.LeilaoId, userEmail, request.NIF, true, descontoValor, request.Address, request.PostalCode, request.Country);
                    desconto.Usado = true;
                    _context.DescontoResgatado.Update(desconto);
                }
                else
                {
                    await GerarFatura(leilao.LeilaoId, userEmail, request.NIF, false, 0.0, request.Address, request.PostalCode, request.Country);
                }

                await _context.SaveChangesAsync();

                TempData["PaymentSuccess"] = "Payment successfully completed!";

                return Json(new { success = true, message = "Payment successfully completed!" });
            }
            catch (StripeException ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        // ========================================================
        // Autor: Rodrigo Baião
        // Método para gerar fatura
        // ========================================================
        public async Task<IActionResult> GerarFatura(int leilaoId, string email, string nif, bool desconto, double valor, string rua, string codigoPostal, string pais)
        {
            // Obter o leilão com base no ID, incluindo detalhes do item e do vencedor
            var leilao = _context.Leiloes
                .Include(l => l.Item)
                .Include(l => l.Vencedor)
                .FirstOrDefault(l => l.LeilaoId == leilaoId);

            // Verificar se o leilão existe, se o pagamento foi realizado e se há um vencedor
            if (leilao == null || !leilao.Pago || leilao.Vencedor == null)
            {
                return Content("Erro: Leilão não encontrado ou pagamento não confirmado.");
            }

            // Definir a taxa de IVA
            decimal taxaIva = 0.23m;

            // Calcular o valor final e o valor base sem IVA
            decimal valorFinal = Convert.ToDecimal(leilao.ValorAtualLance);
            decimal valorBaseSemIva = valorFinal / (1 + taxaIva);
            decimal iva = valorFinal - valorBaseSemIva;

            // Calcular o valor com desconto, caso aplicável
            decimal valorComDesconto = valorBaseSemIva;
            if (desconto)
            {
                decimal descontoDecimal = Convert.ToDecimal(valor) / 100;
                valorComDesconto -= valorComDesconto * descontoDecimal;
            }

            // Calcular o total com IVA
            decimal totalComIva = valorComDesconto * (1 + taxaIva);

            // Criar a fatura com as informações calculadas
            var fatura = new Fatura
            {
                Id = leilao.LeilaoId,
                Numero = "FT" + leilao.LeilaoId.ToString("D5"),
                Data = DateTime.Now,
                NomeComprador = leilao.Vencedor.Nome,
                NIF = nif,
                ItemLeiloado = leilao.Item.Titulo,
                ValorBase = valorComDesconto,
                IVA = iva,
                TotalComIVA = totalComIva,
                Desconto = desconto ? valorBaseSemIva - valorComDesconto : 0,
                Rua = rua,
                CodigoPostal = codigoPostal,
                Pais = pais
            };

            // Gerar o PDF da fatura usando o serviço de PDF
            var pdfService = new PdfService();
            byte[] pdfFatura = pdfService.GerarFaturaPDF(fatura);

            // Enviar um email com a fatura anexada
            var emailSender = new EmailSender(_configuration);
            await emailSender.SendEmailWithAttachmentAsync(
                email,
                "Fatura #" + fatura.Numero,
                "<p>Segue em anexo a sua fatura do leilão.</p>",
                pdfFatura,
                "Fatura_" + fatura.Numero + ".pdf"
            );

            // Retornar uma resposta de sucesso
            return Content("Fatura enviada com sucesso!");
        }

        // Classe para receber os dados de pagamento de um leilão
        public class PagamentoRequest
        {
            public string PaymentMethodId { get; set; }
            public int LeilaoId { get; set; }
            public decimal Valor { get; set; }
            public int? DescontoUsadoId { get; set; }
            public string FullName { get; set; }
            public string Address { get; set; }
            public string City { get; set; }
            public string PostalCode { get; set; }
            public string Country { get; set; }
            public string Phone { get; set; }
            public string NIF { get; set; }
        }


        // ========================================================
        // Autor: Miguel Pinto
        // Método para cancelar a conta
        // ========================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(string confirmCancel)
        {
            var emailSender = new EmailSender(_configuration);

            // Obter o email do usuário a partir da sessão
            var userEmail = HttpContext.Session.GetString("UserEmail");
            if (userEmail == null)
            {
                TempData["Error"] = "Você deve estar logado.";
                return RedirectToAction("Login", "Utilizadors");
            }

            // Obter o usuário com base no email
            var utilizador = await _context.Utilizador
                .FirstOrDefaultAsync(u => u.Email == userEmail);

            // Verificar se o usuário existe
            if (utilizador == null)
            {
                TempData["Error"] = "Usuário não encontrado.";
                return RedirectToAction("Index", "Home");
            }

            // Exigir que o usuário digite "Cancel" para confirmar a exclusão da conta
            if (confirmCancel?.Trim().ToLower() != "cancel")
            {
                TempData["Error"] = "Para confirmar o cancelamento da conta, digite 'Cancel'.";
                return RedirectToAction("Profile", new { id = utilizador.UtilizadorId });
            }

            // Verificar se o usuário tem leilões ativos
            var leiloesAtivos = await _context.Leiloes
                .Where(l => l.UtilizadorId == utilizador.UtilizadorId
                            && l.EstadoLeilao == EstadoLeilao.Disponivel)
                .ToListAsync();

            if (leiloesAtivos.Any())
            {
                TempData["Error"] = "Você não pode cancelar sua conta enquanto tiver leilões ativos.";
                return RedirectToAction("Profile", new { id = utilizador.UtilizadorId });
            }

            // Verificar se o usuário tem leilões ganhos e não pagos
            var pendentes = await _context.Leiloes
                .Where(l => l.VencedorId == utilizador.UtilizadorId && !l.Pago)
                .ToListAsync();

            if (pendentes.Any())
            {
                TempData["Error"] = "Você tem pagamentos pendentes. Não é possível cancelar a conta até que todos os pagamentos sejam quitados.";
                return RedirectToAction("Profile", new { id = utilizador.UtilizadorId });
            }

            // Verificar se o usuário tem licitações em leilões ativos
            var licitacoesAtivas = await _context.Licitacoes
                .Include(l => l.Leilao)
                .Where(l => l.UtilizadorId == utilizador.UtilizadorId
                            && l.Leilao.EstadoLeilao == EstadoLeilao.Disponivel)
                .ToListAsync();

            if (licitacoesAtivas.Any())
            {
                TempData["Error"] = "Você não pode cancelar sua conta enquanto tiver licitações ativas.";
                return RedirectToAction("Profile", new { id = utilizador.UtilizadorId });
            }

            // Se passou por todas as verificações, pode remover a conta do usuário
            _context.Utilizador.Remove(utilizador);
            await _context.SaveChangesAsync();
            HttpContext.Session.Clear();

            // Enviar um email de confirmação de cancelamento
            string assunto = "Conta cancelada com sucesso - Grow";
            string mensagem = $"<h2>Olá {utilizador.Nome},</h2>" +
                              "<p>A sua conta na plataforma <strong>Grow</strong> foi cancelada com sucesso.</p>" +
                              "<p>Agradecemos a sua participação e esperamos vê-lo novamente no futuro.</p>" +
                              "<p>Se esta ação não foi realizada por si ou se mudou de ideias, entre em contacto connosco através do nosso site.</p>" +
                              "<br /><p>Cumprimentos,<br/>Equipa Grow</p>";

            await emailSender.SendEmailAsync(utilizador.Email, assunto, mensagem);

            TempData["Success"] = "A sua conta foi cancelada com sucesso.";
            return RedirectToAction("Index", "Leilaos");
        }

    }
}


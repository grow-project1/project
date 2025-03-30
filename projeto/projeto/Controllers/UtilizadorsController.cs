﻿
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using projeto.Data;
using projeto.Models;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Identity;
using Stripe;
using Microsoft.VisualStudio.Web.CodeGenerators.Mvc.Templates.BlazorIdentity.Pages.Manage;

namespace projeto.Controllers
{
    public class UtilizadorsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _webHostEnvironment;
        private readonly IConfiguration _configuration;
        private readonly IEmailSender _emailSender;

        public UtilizadorsController(ApplicationDbContext context, IWebHostEnvironment webHostEnvironment, IConfiguration configuration, IEmailSender emailSender)
        {
            _context = context;
            _webHostEnvironment = webHostEnvironment;
            _configuration = configuration;
            _emailSender = emailSender;
        }

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

        // Método Register (GET)
        public IActionResult Register()
        {
            return View();
        }

        // Método Register (POST)
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
            // Atenção: Sessão não deve armazenar strings muito grandes. Aqui é pequeno, deve servir.
            HttpContext.Session.SetString("PendingRegName", utilizador.Nome);
            HttpContext.Session.SetString("PendingRegEmail", utilizador.Email);
            HttpContext.Session.SetString("PendingRegPassword", utilizador.Password);

            // Armazena também o code
            HttpContext.Session.SetString("PendingRegCode", verificationCode.ToString());
            Console.WriteLine($"*Codigo no controller*: {verificationCode}");


            TempData["Info"] = "We sent a verification code to your email. Please confirm.";
            return RedirectToAction("ConfirmRegistration");
        }


        // GET
        public IActionResult ConfirmRegistration()
        {
            return View();
        }

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

        // Função para validar a força da senha
        private bool IsPasswordStrong(string password)
        {
            return password.Length >= 6 &&
                   password.Any(char.IsUpper) &&
                   password.Any(char.IsDigit) &&
                   password.Any(ch => !char.IsLetterOrDigit(ch));
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
            var userEmail = HttpContext.Session.GetString("UserEmail");

            var user = await _context.Utilizador.FirstOrDefaultAsync(u => u.Email == userEmail);

            ViewData["UserPoints"] = user?.Pontos;

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


                TempData["SuccessMessage"] = "Profile saved";

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
            // Em vez de criar new EmailSender(...) aqui, usa _emailSender
            string subject = "Código de Verificação";
            string message = $"Seu código de verificação é: {verificationCode}";
            await _emailSender.SendEmailAsync(email, subject, message);


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

            // Verifica se a password foi preenchida (mas sem adicionar erro manual)
            if (!ModelState.IsValid)
            {
                return View(); // ASP.NET já adicionou "The
                               // Password field is required."
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

        public async Task<IActionResult> PagamentoDetalhes(int leilaoId)
        {

            var userEmail = HttpContext.Session.GetString("UserEmail");
            var utilizador = await _context.Utilizador.FirstOrDefaultAsync(u => u.Email == userEmail);

            ViewData["UserPoints"] = utilizador?.Pontos;

            var leilao = await _context.Leiloes
                .Include(l => l.Item) // Certifique-se de incluir o item associado ao leilão
                .Where(l => l.LeilaoId == leilaoId && !l.Pago) // Filtro para leilão não pago
                .FirstOrDefaultAsync(); // Obtém o primeiro (e único) leilão ou null

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
                    await GerarFatura(leilao.LeilaoId, userEmail, request.NIF, true, descontoValor);
                    desconto.Usado = true;
                    _context.DescontoResgatado.Update(desconto);
                    
                }
                else
                {
                    await GerarFatura(leilao.LeilaoId, userEmail, request.NIF, false,0.0);
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

        public async Task<IActionResult> GerarFatura(int leilaoId, string email, string nif, bool desconto, double valor)
        {
            var leilao = _context.Leiloes
                .Include(l => l.Item)
                .Include(l => l.Vencedor)
                .FirstOrDefault(l => l.LeilaoId == leilaoId);

            if (leilao == null || !leilao.Pago || leilao.Vencedor == null)
            {
                return Content("Erro: Leilão não encontrado ou pagamento não confirmado.");
            }

            decimal taxaIva = 0.23m;
            decimal valorFinal = Convert.ToDecimal(leilao.ValorAtualLance);
            decimal valorBaseSemIva = valorFinal / (1 + taxaIva);
            decimal iva = valorFinal - valorBaseSemIva;
            decimal valorComDesconto = valorBaseSemIva;
            if (desconto)
            {
                decimal descontoDecimal = Convert.ToDecimal(valor) / 100;
                valorComDesconto -= valorComDesconto * descontoDecimal; 
            }
            decimal totalComIva = valorComDesconto * (1 + taxaIva);

     
            // Criar fatura
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
            };

            // Gerar PDF
            var pdfService = new PdfService();
            byte[] pdfFatura = pdfService.GerarFaturaPDF(fatura);

            // Enviar Email com anexo
            var emailSender = new EmailSender(_configuration);
            await emailSender.SendEmailWithAttachmentAsync(
                email,
                "Fatura #" + fatura.Numero,
                "<p>Segue em anexo a sua fatura do leilão.</p>",
                pdfFatura,
                "Fatura_" + fatura.Numero + ".pdf"
            );

            return Content("Fatura enviada com sucesso!");
        }



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

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(string confirmCancel)
        {
            var emailSender = new EmailSender(_configuration);

            var userEmail = HttpContext.Session.GetString("UserEmail");
            if (userEmail == null)
            {
                TempData["Error"] = "You must be logged in.";
                return RedirectToAction("Login", "Utilizadors");
            }

            var utilizador = await _context.Utilizador
                .FirstOrDefaultAsync(u => u.Email == userEmail);

            if (utilizador == null)
            {
                TempData["Error"] = "User not found.";
                return RedirectToAction("Index", "Home");
            }

            // Exigir que o user digite "Cancel"
            if (confirmCancel?.Trim().ToLower() != "cancel")
            {
                TempData["Error"] = "To confirm account cancellation, please type 'Cancel'.";
                return RedirectToAction("Profile", new { id = utilizador.UtilizadorId });
            }

            // 1) Verificar se o user tem leilões ativos (como antes)
            var leiloesAtivos = await _context.Leiloes
                .Where(l => l.UtilizadorId == utilizador.UtilizadorId
                            && l.EstadoLeilao == EstadoLeilao.Disponivel)
                .ToListAsync();

            if (leiloesAtivos.Any())
            {
                TempData["Error"] = "You cannot cancel your account while you have auctions.";
                return RedirectToAction("Profile", new { id = utilizador.UtilizadorId });
            }

            // 2) Verificar se o user tem leilões ganhos e não pagos (como antes)
            var pendentes = await _context.Leiloes
                .Where(l => l.VencedorId == utilizador.UtilizadorId && !l.Pago)
                .ToListAsync();

            if (pendentes.Any())
            {
                TempData["Error"] = "You have pending payments. You cannot cancel your account until all payments are settled.";
                return RedirectToAction("Profile", new { id = utilizador.UtilizadorId });
            }

            // 3) NOVO: Verificar se o user tem licitações em leilões ativos
            var licitacoesAtivas = await _context.Licitacoes
                .Include(l => l.Leilao)
                .Where(l => l.UtilizadorId == utilizador.UtilizadorId
                            && l.Leilao.EstadoLeilao == EstadoLeilao.Disponivel)
                .ToListAsync();

            if (licitacoesAtivas.Any())
            {
                TempData["Error"] = "You cannot cancel your account while you have active bids.";
                return RedirectToAction("Profile", new { id = utilizador.UtilizadorId });
            }

            // Se passou de todas as verificações, pode remover
            _context.Utilizador.Remove(utilizador);
            await _context.SaveChangesAsync();
            HttpContext.Session.Clear();

            // Envia email de confirmação
            string assunto = "Conta cancelada com sucesso - Grow";
            string mensagem = $"<h2>Olá {utilizador.Nome},</h2>" +
                              "<p>A sua conta na plataforma <strong>Grow</strong> foi cancelada com sucesso.</p>" +
                              "<p>Agradecemos a sua participação e esperamos vê-lo novamente no futuro.</p>" +
                              "<p>Se esta ação não foi realizada por si ou se mudou de ideias, entre em contacto connosco através do nosso site.</p>" +
                              "<br /><p>Cumprimentos,<br/>Equipa Grow</p>";

            await emailSender.SendEmailAsync(utilizador.Email, assunto, mensagem);

            TempData["Success"] = "Your account has been successfully cancelled.";
            return RedirectToAction("Index", "Leilaos");
        }
    }
}


using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Moq;
using Xunit;
using projeto.Controllers;
using projeto.Data;
using projeto.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Http.HttpResults;


namespace growTests.NotificationsTests
{
    public class RegisterNotificationsTests
    {
        private readonly ApplicationDbContext _dbContext;
        private readonly Mock<IConfiguration> _mockConfig;
        private readonly FakeEmailSender _fakeEmailSender;
        private readonly UtilizadorsController _controller;
        private readonly DefaultHttpContext _httpContext;

        public RegisterNotificationsTests()
        {
            // 1) Configura DB em memória (GUID para isolar cada teste)
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;
            _dbContext = new ApplicationDbContext(options);

            // 2) Mock da IConfiguration (se for necessário em algum ponto)
            _mockConfig = new Mock<IConfiguration>();

            // 3) FakeEmailSender (não envia email real, só registra as chamadas)
            _fakeEmailSender = new FakeEmailSender();

            // 4) Cria o Controller
            _controller = new UtilizadorsController(
                _dbContext,
                webHostEnvironment: null,    // IWebHostEnvironment (não usado neste teste)
                _mockConfig.Object,          // IConfiguration mockada
                _fakeEmailSender             // IEmailSender fake
            );

            // 5) Adiciona HttpContext com MockSession
            _httpContext = new DefaultHttpContext
            {
                Session = new MockSession()
            };
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = _httpContext
            };

            // 6) Configura TempData para não ser null

            _controller.TempData = new TempDataDictionary(
           _httpContext,
           Mock.Of<ITempDataProvider>() // usando Moq
       );
        }

        [Fact]
        public async Task Register_ValidData_ShouldSendEmailAndRedirectToConfirm()
        {
            // Arrange
            var novoUtilizador = new Utilizador
            {
                Nome = "TesteNome",
                Email = "teste1@example.com",
                Password = "Pa$$w0rd"
            };

            // Act
            var result = await _controller.Register(novoUtilizador, true);

            // Assert
            // Verifica se redirecionou para ConfirmRegistration
            var redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("ConfirmRegistration", redirectResult.ActionName);

            // Verifica se um email foi "enviado"
            Assert.Single(_fakeEmailSender.SentEmails);
            var sentEmail = _fakeEmailSender.SentEmails.First();
            Assert.Equal("teste1@example.com", sentEmail.To);
            Assert.Contains("Código de Verificação", sentEmail.Subject);
            // (Opcional) Poderia verificar se o corpo contém o código de 6 dígitos

            // Verifica se a Session guardou o código
            int pendingCode = int.Parse(_httpContext.Session.GetString("PendingRegCode"));
            Assert.NotNull(pendingCode);

            // O utilizador ainda não deve estar na base de dados (ele só entra após ConfirmRegistration)
            var dbUser = await _dbContext.Utilizador
                .FirstOrDefaultAsync(u => u.Email == "teste1@example.com");
            Assert.Null(dbUser);
        }

        [Fact]
        public async Task Register_ExistingEmail_ShouldReturnViewAndErrorInModelState()
        {
            // Arrange
            // Adiciona um utilizador manualmente à base
            var existingUser = new Utilizador
            {
                Nome = "Existente",
                Email = "existe@example.com",
                Password = "Teste"
            };
            _dbContext.Utilizador.Add(existingUser);
            await _dbContext.SaveChangesAsync();

            // Tenta registar outro com o mesmo email
            var novoUtilizador = new Utilizador
            {
                Nome = "Novo",
                Email = "existe@example.com",
                Password = "Pa$$w0rd"
            };

            // Act
            var result = await _controller.Register(novoUtilizador, true);

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.False(viewResult.ViewData.ModelState.IsValid);
            Assert.True(viewResult.ViewData.ModelState.ContainsKey("Email"),
                "Deveria ter um erro no campo 'Email'.");
        }

        [Fact]
        public async Task ConfirmRegistration_ValidCode_ShouldCreateUser()
        {
            // Arrange
            var utilizador = new Utilizador
            {
                UtilizadorId = 1,
                Nome = "User2",
                Email = "user2@example.com",
                Password = "Abc123!"
            };

            // Primeiro, chama o Register (POST) e valida se deu redirect para ConfirmRegistration
            var registerResult = await _controller.Register(utilizador, true);
            var redirectToConfirm = Assert.IsType<RedirectToActionResult>(registerResult);
            Assert.Equal("ConfirmRegistration", redirectToConfirm.ActionName);

            // Pega o código gerado na sessão
            int? codeGerado = int.Parse(_httpContext.Session.GetString("PendingRegCode"));
            Assert.NotNull(codeGerado);
            Console.WriteLine($"*Codigo no teste*: {codeGerado}");

            // Act: agora chama ConfirmRegistration com o código correto
            var confirmResult = await _controller.ConfirmRegistration(codeGerado.Value);

            // Assert

            // Verifica se o utilizador foi criado no DB e se a password foi hasheada

            var dbUser = await (from u in _dbContext.Utilizador
                                where u.Email == "user2@example.com"
                                select u)
                               .FirstOrDefaultAsync();

            Assert.NotNull(dbUser);
            Assert.Equal("User2", dbUser.Nome);
            Assert.NotEqual("Abc123!", dbUser.Password); // confirmando que foi hasheada

            // Session deve ter sido limpa (exemplo: PendingRegEmail)
            Assert.Null(_httpContext.Session.GetString("PendingRegEmail"));
        }


        [Fact]
        public async Task ConfirmRegistration_InvalidCode_ShouldReturnViewWithError()
        {
            // Arrange
            // Simula o Register para povoar a sessão com o code
            var utilizador = new Utilizador
            {
                Nome = "User3",
                Email = "user3@example.com",
                Password = "Abc123!"
            };
            await _controller.Register(utilizador, true);
            // Mas enviaremos um código diferente
            var invalidCode = 111111; // certamente diferente do gerado

            // Act
            var result = await _controller.ConfirmRegistration(invalidCode);

            // Assert
            // Deve voltar para a View (não redirecionar)
            var viewResult = Assert.IsType<ViewResult>(result);

            // Verifica se o TempData["Error"] contém a mensagem de código inválido
            Assert.True(_controller.TempData.ContainsKey("Error"));
            Assert.Equal("Invalid code. Please try again.", _controller.TempData["Error"]);

            // Garante que o user NÃO foi criado no DB
            var dbUser = await _dbContext.Utilizador
                .FirstOrDefaultAsync(u => u.Email == "user3@example.com");
            Assert.Null(dbUser);
        }
    }
}

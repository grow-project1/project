using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Moq;
using growTests.Controllers;
using growTests.Data;
using growTests.Models;
using growTests;
using System.Text;
using Microsoft.Extensions.Configuration;

namespace growTests
{
    public class RegisterNotificationsTests
    {
        private readonly ApplicationDbContext _dbContext;
        private readonly Mock<IConfiguration> _mockConfig;
        private readonly FakeEmailSender _fakeEmailSender;
        private readonly UtilizadorsController _controller;

        public RegisterNotificationsTests()
        {
            // 1) DB InMemory
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;
            _dbContext = new ApplicationDbContext(options);

            // 2) Mock da IConfiguration
            _mockConfig = new Mock<IConfiguration>();

            // 3) FakeEmailSender
            _fakeEmailSender = new FakeEmailSender();

            // 4) Controller injetando as dependências
            _controller = new UtilizadorsController(
                _dbContext,
                null,           // IWebHostEnvironment
                _mockConfig.Object,
                _fakeEmailSender
            );

            // 5) MockSession para evitar erro de "Session not configured"
            var defaultHttpContext = new DefaultHttpContext
            {
                Session = new MockSession()
            };
            _controller.ControllerContext = new Microsoft.AspNetCore.Mvc.ControllerContext
            {
                HttpContext = defaultHttpContext
            };
        }

        [Fact]
        public async Task Register_ValidData_SendsVerificationEmail_AndStoresInSession()
        {
            // Arrange
            var newUser = new Utilizador
            {
                Nome = "Test User",
                Email = "testuser@email.com",
                Password = "Passw0rd!"
            };

            // Act
            var result = await _controller.Register(newUser) as Microsoft.AspNetCore.Mvc.RedirectToActionResult;

            // Assert
            // 1. Verifica se redirecionou para "ConfirmRegistration"
            Assert.NotNull(result);
            Assert.Equal("ConfirmRegistration", result.ActionName);

            // 2. Verifica se a FakeEmailSender enviou o e-mail

            Assert.Contains(_fakeEmailSender.SentEmails, mail =>
                mail.To == newUser.Email
                && mail.Subject.Contains("Confirmação de Registo")
                // Podes verificar Body se quiseres "Seu código de verificação é:"
                && mail.Body.Contains("Seu código de verificação é:")
            );

            // 3. Verifica se sessão guardou PendingRegName, PendingRegEmail, etc.
            var session = _controller.HttpContext.Session;
            Assert.Equal(newUser.Nome, session.GetString("PendingRegName"));
            Assert.Equal(newUser.Email, session.GetString("PendingRegEmail"));
            Assert.Equal(newUser.Password, session.GetString("PendingRegPassword"));
            Assert.NotNull(session.GetInt32("PendingRegCode")); // Verifica se há um code

            // 4. Confirma que ainda não existe no DB
            var userInDB = _dbContext.Utilizador.FirstOrDefault(u => u.Email == newUser.Email);
            Assert.Null(userInDB);
        }

        [Fact]
        public async Task ConfirmRegistration_ValidCode_CreatesUserInDB()
        {
            // Arrange
            // Simula que o utilizador acabou de vir do Register
            var pendingName = "PendingUser";
            var pendingEmail = "pending@teste.com";
            var pendingPass = "Abc123!";
            var code = 123456;

            // Seta na sessão
            var session = _controller.HttpContext.Session;
            session.SetString("PendingRegName", pendingName);
            session.SetString("PendingRegEmail", pendingEmail);
            session.SetString("PendingRegPassword", pendingPass);
            session.SetInt32("PendingRegCode", code);

            // Act
            // Chamamos ConfirmRegistration com o código correto
            var result = await _controller.ConfirmRegistration(code) as Microsoft.AspNetCore.Mvc.RedirectToActionResult;

            // Assert
            // 1. Verifica se redirecionou para "Login"
            Assert.NotNull(result);
            Assert.Equal("Login", result.ActionName);

            // 2. Verifica se o utilizador foi criado no DB
            var userInDB = _dbContext.Utilizador.FirstOrDefault(u => u.Email == pendingEmail);
            Assert.NotNull(userInDB);
            Assert.Equal(pendingName, userInDB.Nome);
            // Verifica se a password foi hasheada
            Assert.True(BCrypt.Net.BCrypt.Verify(pendingPass, userInDB.Password));

            // 3. Verifica se limpou a sessão
            Assert.Null(session.GetString("PendingRegName"));
            Assert.Null(session.GetString("PendingRegEmail"));
            Assert.Null(session.GetString("PendingRegPassword"));
            Assert.Null(session.GetInt32("PendingRegCode"));
        }
    }
}

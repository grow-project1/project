using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Moq;
using Xunit;
using growTests.Controllers;
using growTests.Data;
using growTests.Models;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Http;
using growTests;

namespace growTests
{
    public class ForgotPasswordTests
    {
        private readonly ApplicationDbContext _dbContext;
        private readonly Mock<IConfiguration> _mockConfig; // Mock da IConfiguration
        private readonly FakeEmailSender _fakeEmailSender; // Fake para não ligar a SMTP
        private readonly UtilizadorsController _controller;

        public ForgotPasswordTests()
        {
            // 1) Configura DB em memória
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            _dbContext = new ApplicationDbContext(options);

            // 2) Mock da IConfiguration (caso precises de chaves para algo)
            _mockConfig = new Mock<IConfiguration>();

            // 3) FakeEmailSender para não tentar SMTP real
            _fakeEmailSender = new FakeEmailSender();

            // 4) Cria o Controller com injeção do fake
            _controller = new UtilizadorsController(
                _dbContext,
                null,                // IWebHostEnvironment (não usas no teste)
                _mockConfig.Object,  // IConfiguration mockado
                _fakeEmailSender     // IEmailSender fake
            );
        }

        [Fact]
        public async Task ForgotPassword_EmptyEmail_ShouldReturnErrorInModelState()
        {
            // Arrange
            string emptyEmail = "";

            // Act
            var result = await _controller.ForgotPassword(emptyEmail) as ViewResult;

            // Assert
            Assert.NotNull(result);
            Assert.True(result.ViewData.ModelState.ContainsKey("Email"));
        }

        [Fact]
        public async Task ForgotPassword_InvalidEmail_ShouldReturnModelError()
        {
            // Arrange
            string invalidEmail = "naoexiste@email.com";

            // Act
            var result = await _controller.ForgotPassword(invalidEmail) as ViewResult;



            // Assert
            Assert.NotNull(result);
            Assert.True(result.ViewData.ModelState.ContainsKey("Email"));
            Assert.Contains("Email not found.",
                result.ViewData.ModelState["Email"].Errors.First().ErrorMessage);
        }

        [Fact]
        public async Task ForgotPassword_ValidEmail_ShouldGenerateVerificationCodeAndRedirect()
        {
            // Arrange
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;
            var db = new ApplicationDbContext(options);

            var fakeEmailSender = new FakeEmailSender();
            var mockConfig = new Mock<IConfiguration>();

            var controller = new UtilizadorsController(
                db,
                null,
                mockConfig.Object,
                fakeEmailSender
            );

            //  Adiciona um HttpContext com Session mock
            var defaultHttpContext = new DefaultHttpContext
            {
                Session = new MockSession() // Implementa ISession
            };
            controller.ControllerContext = new ControllerContext
            {
                HttpContext = defaultHttpContext
            };

            // Adicionar utilizador
            var validEmail = "teste@teste.com";
            var utilizador = new Utilizador
            {
                UtilizadorId = 1,
                Nome = "User Test",
                Email = validEmail,
                Password = "dummy"
            };
            db.Utilizador.Add(utilizador);
            await db.SaveChangesAsync();

            // Act
            var result = await controller.ForgotPassword(validEmail);

            // Assert
            var redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("VerificationCode", redirectResult.ActionName);

            // Verifica se criou VerificationModel
            var verification = await db.VerificationModel.FirstOrDefaultAsync();
            Assert.NotNull(verification);
            Assert.InRange(verification.VerificationCode, 100000, 999999);

            // Se quiseres até podes verificar se a Session guardou algo
            Assert.True(defaultHttpContext.Session.TryGetValue("ResetEmail", out var resetEmailBytes));
            var resetEmail = System.Text.Encoding.UTF8.GetString(resetEmailBytes);
            Assert.Equal(validEmail, resetEmail);
        }


    }
}

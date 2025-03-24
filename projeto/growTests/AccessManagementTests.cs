using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Moq;
using projeto.Controllers;
using projeto.Data;
using projeto.Models;
using Microsoft.Extensions.Configuration;


namespace growTests
{
    public class AccessManagementTests
    {
        private readonly ApplicationDbContext _dbContext;
        private readonly Mock<IConfiguration> _mockConfig;
        private readonly FakeEmailSender _fakeEmailSender;
        private readonly UtilizadorsController _controller;
        private readonly DefaultHttpContext _httpContext;

        public AccessManagementTests()
        {
            // Configura o banco de dados em memória
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;
            _dbContext = new ApplicationDbContext(options);

            // Configura o mock da IConfiguration e o FakeEmailSender
            _mockConfig = new Mock<IConfiguration>();
            _fakeEmailSender = new FakeEmailSender();

            // Cria o controller injetando as dependências necessárias
            _controller = new UtilizadorsController(
                _dbContext,
                webHostEnvironment: null, 
                _mockConfig.Object,
                _fakeEmailSender
            );

            // Configura o HttpContext com uma sessão fake
            _httpContext = new DefaultHttpContext
            {
                Session = new MockSession()
            };
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = _httpContext
            };

            // Configura o TempData para não ser nulo
            _controller.TempData = new TempDataDictionary(
                _httpContext,
                Mock.Of<ITempDataProvider>()
            );
        }

        [Fact]
        public async Task Login_ValidCredentials_ShouldRedirectToIndex()
        {
            // Arrange: cria um usuário no banco de dados
            var user = new Utilizador
            {
                Nome = "Test User",
                Email = "test@example.com",
                Password = BCrypt.Net.BCrypt.HashPassword("Test123!")
            };
            _dbContext.Utilizador.Add(user);
            await _dbContext.SaveChangesAsync();

            // Prepara o modelo de login com as credenciais corretas
            var loginModel = new LoginModel
            {
                Email = "test@example.com",
                Password = "Test123!"
            };

            // Act
            var result = await _controller.Login(loginModel);

            // Assert: verifica se ocorreu o redirecionamento para a ação "Index" do controller "Leilaos"
            var redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Index", redirectResult.ActionName);

        }

        [Fact]
        public async Task Login_InvalidPassword_ShouldReturnViewWithError()
        {
            // Arrange: cria um usuário no banco de dados
            var user = new Utilizador
            {
                Nome = "Test User",
                Email = "test2@example.com",
                Password = BCrypt.Net.BCrypt.HashPassword("CorrectPass1!")
            };
            _dbContext.Utilizador.Add(user);
            await _dbContext.SaveChangesAsync();

            // Prepara o modelo de login com senha incorreta
            var loginModel = new LoginModel
            {
                Email = "test2@example.com",
                Password = "WrongPass!"
            };

            // Act
            var result = await _controller.Login(loginModel);

            // Assert: espera-se que retorne uma View com erros no ModelState
            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.False(viewResult.ViewData.ModelState.IsValid);
            Assert.True(viewResult.ViewData.ModelState[string.Empty].Errors.Any(e =>
                e.ErrorMessage.Contains("Invalid credentials") ||
                e.ErrorMessage.Contains("remaining")));
        }

        [Fact]
        public async Task Login_AccountLockoutAfterThreeFailures_ShouldLockAccount()
        {
            // Arrange: Cria um usuário de teste com senha correta e conta ativa
            var testUser = new Utilizador
            {
                Nome = "LockoutUser",
                Email = "lockout@example.com",
                Password = BCrypt.Net.BCrypt.HashPassword("CorrectPass1!"),
                EstadoConta = EstadoConta.Ativa
            };
            _dbContext.Utilizador.Add(testUser);
            await _dbContext.SaveChangesAsync();

            // Cria um modelo de login com senha errada
            var loginModel = new LoginModel
            {
                Email = "lockout@example.com",
                Password = "WrongPassword!"
            };

            // Act: Simula três tentativas de login com senha incorreta
            for (int i = 0; i < 3; i++)
            {
                // Chamamos o método Login para simular uma tentativa de acesso falhada
                var result = await _controller.Login(loginModel);
                // Limpa o ModelState para simular uma nova requisição (o ModelState não persiste entre requests)
                _controller.ModelState.Clear();
            }

            // Assert: Após três falhas, a conta deve estar bloqueada (EstadoConta == Bloqueada)
            var userFromDb = await _dbContext.Utilizador.FirstOrDefaultAsync(u => u.Email == "lockout@example.com");
            Assert.Equal(EstadoConta.Bloqueada, userFromDb.EstadoConta);
        }


        [Fact]
        public async Task Register_TermsNotAccepted_ShouldReturnViewWithError()
        {
            // Arrange: cria um novo usuário com dados válidos, mas sem aceitar os termos
            var novoUtilizador = new Utilizador
            {
                Nome = "TesteUser",
                Email = "testTerms@example.com",
                Password = "ValidPass1!"
            };

            // Act: chama o método Register com acceptedTerms == false
            var result = await _controller.Register(novoUtilizador, false);

            // Assert: espera-se que o retorno seja uma ViewResult e que o ModelState contenha um erro relacionado à não aceitação dos termos
            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.False(viewResult.ViewData.ModelState.IsValid);
            Assert.True(viewResult.ViewData.ModelState[string.Empty].Errors.Any(e =>
                e.ErrorMessage.Contains("accept the Terms and Conditions")));
        }


    }
}

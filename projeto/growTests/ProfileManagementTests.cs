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
    public class ProfileManagementTests
    {
        private readonly ApplicationDbContext _dbContext;
        private readonly UtilizadorsController _controller;
        private readonly DefaultHttpContext _httpContext;
        private readonly Mock<IConfiguration> _mockConfig;
        private readonly FakeEmailSender _fakeEmailSender;

        public ProfileManagementTests()
        {
            // Configuração da DB em memória para isolar os testes
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;
            _dbContext = new ApplicationDbContext(options);

            _mockConfig = new Mock<IConfiguration>();
            _fakeEmailSender = new FakeEmailSender();

            // Cria o controller com os mocks necessários
            _controller = new UtilizadorsController(_dbContext, webHostEnvironment: null, _mockConfig.Object, _fakeEmailSender);

            // Configura o HttpContext com uma sessão fake
            _httpContext = new DefaultHttpContext { Session = new MockSession() };
            _controller.ControllerContext = new ControllerContext { HttpContext = _httpContext };

            // Configura o TempData
            _controller.TempData = new TempDataDictionary(_httpContext, Mock.Of<ITempDataProvider>());
        }

        [Fact]
        public async Task ProfileAsync_ReturnsViewWithUser_WhenUserIsLoggedIn()
        {
            // Arrange: Adiciona um utilizador à DB e simula sessão de login
            var user = new Utilizador { UtilizadorId = 1, Nome = "Test User", Email = "test@example.com", Password = "123Pass:" };
            _dbContext.Utilizador.Add(user);
            await _dbContext.SaveChangesAsync();
            _httpContext.Session.SetString("UserEmail", user.Email);

            // Act
            var result = await _controller.ProfileAsync();

            // Assert: Verifica se o resultado é uma View com o modelo correto
            var viewResult = Assert.IsType<ViewResult>(result);
            var model = Assert.IsType<Utilizador>(viewResult.Model);
            Assert.Equal("Test User", model.Nome);
        }

        [Fact]
        public async Task ProfileAsync_RedirectsToLogin_WhenNoUserInSession()
        {
            // Arrange: Remove qualquer informação de sessão
            _httpContext.Session.Remove("UserEmail");

            // Act
            var result = await _controller.ProfileAsync();

            // Assert: Verifica que redireciona para Login
            var redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Login", redirectResult.ActionName);
        }

        [Fact]
        public async Task Edit_GET_ReturnsViewWithUser_WhenUserExists()
        {
            // Arrange: Cria um utilizador e simula sessão
            var user = new Utilizador { UtilizadorId = 2, Nome = "Edit Test", Email = "edit@example.com", Password = "123Pass:" };
            _dbContext.Utilizador.Add(user);
            await _dbContext.SaveChangesAsync();
            _httpContext.Session.SetString("UserEmail", user.Email);

            // Act: Chama o método Edit (GET)
            var result = await _controller.Edit(2);

            // Assert: Verifica que a View é retornada com o modelo correto
            var viewResult = Assert.IsType<ViewResult>(result);
            var model = Assert.IsType<Utilizador>(viewResult.Model);
            Assert.Equal("Edit Test", model.Nome);
        }

        [Fact]
        public async Task Edit_POST_UpdatesUserProfile_Successfully()
        {
            // Arrange: Adiciona um utilizador com dados antigos
            var user = new Utilizador { UtilizadorId = 3, Nome = "Old Name", Email = "old@example.com", Morada = "Old Address", Password = "OldPass123:" };
            _dbContext.Utilizador.Add(user);
            await _dbContext.SaveChangesAsync();
            _httpContext.Session.SetString("UserEmail", user.Email);

            // Dados atualizados que serão enviados pelo POST
            var updatedUser = new Utilizador
            {
                UtilizadorId = 3,
                Nome = "New Name",
                Morada = "New Address",
                CodigoPostal = "12345",
                Pais = "Country",
                Telemovel = "123456789"
            };

            // Act: Chama o método Edit (POST)
            var result = await _controller.Edit(3, updatedUser);

            // Assert: Verifica que o redirecionamento ocorreu para a página de perfil e que os dados foram atualizados
            var redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Profile", redirectResult.ActionName);

            var userFromDb = await _dbContext.Utilizador.FindAsync(3);
            Assert.Equal("New Name", userFromDb.Nome);
            Assert.Equal("New Address", userFromDb.Morada);
            Assert.Equal("12345", userFromDb.CodigoPostal);
        }

        [Fact]
        public async Task UpdatePassword_POST_ReturnsError_WhenPasswordsDoNotMatch()
        {
            // Arrange: Adiciona um utilizador e simula sessão
            var user = new Utilizador { UtilizadorId = 4, Nome = "Password Test", Email = "pass@example.com", Password = BCrypt.Net.BCrypt.HashPassword("OldPass123!") };
            _dbContext.Utilizador.Add(user);
            await _dbContext.SaveChangesAsync();
            _httpContext.Session.SetString("UserEmail", user.Email);

            // Act: Chama UpdatePassword com senhas que não conferem
            var result = await _controller.UpdatePassword(4, "NewPass123!", "DifferentPass!");

            // Assert: O ModelState deverá ser inválido
            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.False(viewResult.ViewData.ModelState.IsValid);
            Assert.True(viewResult.ViewData.ModelState.ContainsKey("confirmPassword"));
        }

        [Fact]
        public async Task UpdatePassword_POST_UpdatesPasswordSuccessfully()
        {
            // Arrange: Cria um utilizador e armazena o hash da senha antiga
            var oldPassword = "OldPass123!";
            var newPassword = "NewPass123!";
            var user = new Utilizador { UtilizadorId = 5, Nome = "Password Change", Email = "changepass@example.com", Password = BCrypt.Net.BCrypt.HashPassword(oldPassword) };
            _dbContext.Utilizador.Add(user);
            await _dbContext.SaveChangesAsync();
            _httpContext.Session.SetString("UserEmail", user.Email);

            // Act: Chama UpdatePassword com senhas corretas
            var result = await _controller.UpdatePassword(5, newPassword, newPassword);

            // Assert: Verifica o redirecionamento e que a senha foi alterada
            var redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Profile", redirectResult.ActionName);

            var updatedUser = await _dbContext.Utilizador.FindAsync(5);
            Assert.True(BCrypt.Net.BCrypt.Verify(newPassword, updatedUser.Password));
        }
    }
}

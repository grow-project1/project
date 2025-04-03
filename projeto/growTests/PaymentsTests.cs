using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.EntityFrameworkCore;
using Moq;
using projeto.Controllers;
using projeto.Data;
using projeto.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;
using static projeto.Controllers.UtilizadorsController;
using Microsoft.Extensions.Configuration;


namespace growTests
{
    public class PaymentsTests
    {
        private readonly ApplicationDbContext _dbContext;
        private readonly UtilizadorsController _controller;
        private readonly DefaultHttpContext _httpContext;
        private readonly FakeEmailSender _fakeEmailSender;
        private readonly Mock<IConfiguration> _mockConfig;

        public PaymentsTests()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;
            _dbContext = new ApplicationDbContext(options);

            _mockConfig = new Mock<IConfiguration>();
            _fakeEmailSender = new FakeEmailSender();

            _controller = new UtilizadorsController(
                _dbContext,
                webHostEnvironment: null,
                _mockConfig.Object,
                _fakeEmailSender
            );

            _httpContext = new DefaultHttpContext { Session = new MockSession() };
            _controller.ControllerContext = new ControllerContext { HttpContext = _httpContext };
            _controller.TempData = new TempDataDictionary(_httpContext, Mock.Of<ITempDataProvider>());
        }

        [Fact]
        public async Task Pagamentos_RedirectsToLogin_WhenUserIsNotLoggedIn()
        {
            // Arrange
            _httpContext.Session.Remove("UserEmail");

            // Act
            var result = await _controller.Pagamentos();

            // Assert
            var redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Login", redirectResult.ActionName);
        }

        [Fact]
        public async Task Pagamentos_ReturnsViewWithLeiloesGanhosAndMeusLeiloes_WhenUserIsLoggedIn()
        {
            // Arrange
            var user = new Utilizador
            {
                UtilizadorId = 1,
                Nome = "Test User",
                Email = "test@example.com",
                Pontos = 100,
                Password = "hashedpassword" // Adiciona um valor qualquer
            };

            _dbContext.Utilizador.Add(user);
            await _dbContext.SaveChangesAsync();

            // Adiciona um leilão ganho, associado ao utilizador
            var leilaoGanho = new Leilao { LeilaoId = 1, VencedorId = user.UtilizadorId, Pago = false, Item = new Item() };
            _dbContext.Leiloes.Add(leilaoGanho);

            // Adiciona um leilão "meu" (leilão em que o utilizador participa mas não é o vencedor)
            var meuLeilao = new Leilao { LeilaoId = 2, UtilizadorId = user.UtilizadorId, Pago = false, Item = new Item() };
            _dbContext.Leiloes.Add(meuLeilao);

            await _dbContext.SaveChangesAsync();

            // Verifica se os leilões foram salvos corretamente no banco de dados
            var leilaoGanhoNoDb = await _dbContext.Leiloes.FirstOrDefaultAsync(l => l.LeilaoId == 1);
            var meuLeilaoNoDb = await _dbContext.Leiloes.FirstOrDefaultAsync(l => l.LeilaoId == 2);

            Assert.NotNull(leilaoGanhoNoDb);  // Verifica se o leilão ganho foi adicionado
            Assert.Equal(user.UtilizadorId, leilaoGanhoNoDb.VencedorId);  // Verifica o VencedorId

            Assert.NotNull(meuLeilaoNoDb);  // Verifica se o "meu leilão" foi adicionado
            Assert.Equal(user.UtilizadorId, meuLeilaoNoDb.UtilizadorId);  // Verifica o UtilizadorId

            // Simula o login do utilizador
            _httpContext.Session.SetString("UserEmail", user.Email);

            // Act
            var result = await _controller.Pagamentos();

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            var model = Assert.IsType<PagamentosViewModel>(viewResult.Model);

            // Verifica se a coleção de leilões ganhos não está vazia
            Assert.NotEmpty(model.LeiloesGanhos);
            Assert.Single(model.LeiloesGanhos);  // Verifica que há apenas um leilão ganho

            // Verifica se a coleção de "meus leilões" não está vazia
            Assert.NotEmpty(model.MeusLeiloes);
            Assert.Single(model.MeusLeiloes);  // Verifica que há apenas um leilão do utilizador (caso tenhas apenas um leilão "meu" no teste)
        }



    }
}

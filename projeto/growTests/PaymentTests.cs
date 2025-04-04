using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Moq;
using projeto.Controllers;
using projeto.Data;
using projeto.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
namespace growTests
{
    public class PaymentTests
    {
        private readonly ApplicationDbContext _dbContext;
        private readonly Mock<IConfiguration> _mockConfig;
        private readonly FakeEmailSender _fakeEmailSender;
        private readonly UtilizadorsController _controller;
        private readonly DefaultHttpContext _httpContext;

        public PaymentTests()
        {
            // 1) Configuração do DbContext in-memory (GUID para isolar cada teste)
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;
            _dbContext = new ApplicationDbContext(options);

            // 2) Mock de IConfiguration (caso seja necessário)
            _mockConfig = new Mock<IConfiguration>();

            // 3) FakeEmailSender (para não disparar emails reais)
            _fakeEmailSender = new FakeEmailSender();

            // 4) Cria o Controller (Note que passamos 'null' para IWebHostEnvironment
            //    pois as actions de pagamento não usam esse ambiente – ajuste se preciso)
            _controller = new UtilizadorsController(
                _dbContext,
                webHostEnvironment: null, 
                _mockConfig.Object,
                _fakeEmailSender
            );

            // 5) Cria HttpContext com MockSession
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
                Mock.Of<ITempDataProvider>()
            );
        }

        [Fact]
        public async Task Pagamentos_UserNotLoggedIn_ShouldRedirectToLogin()
        {
            // Arrange: não setamos nada na sessão => userEmail = null

            // Act
            var result = await _controller.Pagamentos();

            // Assert
            var redirect = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Login", redirect.ActionName);
            Assert.Equal("Utilizadors", redirect.ControllerName);
        }

        [Fact]
        public async Task Pagamentos_UserLoggedIn_ShouldReturnViewWithViewModel()
        {
            // Arrange
            // 1) Criar usuário no DB
            var user = new Utilizador
            {
                Nome = "user",
                Email = "user@example.com",
                Password = "Pa$$w0rd"
            };

            _dbContext.Utilizador.Add(user);
            await _dbContext.SaveChangesAsync();

            // 2) Simular login
            _httpContext.Session.SetString("UserEmail", user.Email);

            // 3) Criar alguns leilões de teste (ganhos + meus leilões)
            var leilaoGanho = new Leilao
            {
                LeilaoId = 1,
                VencedorId = user.UtilizadorId,
                Pago = false,
                Item = new Item { Titulo = "Leilão Ganho" },
                EstadoLeilao = EstadoLeilao.Encerrado
            };
            var leilaoMeu = new Leilao
            {
                LeilaoId = 2,
                UtilizadorId = user.UtilizadorId,
                Pago = false,
                Item = new Item { Titulo = "Meu Leilão" },
                EstadoLeilao = EstadoLeilao.Encerrado
            };

            _dbContext.Leiloes.AddRange(leilaoGanho, leilaoMeu);
            await _dbContext.SaveChangesAsync();

            // Act
            var result = await _controller.Pagamentos();

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            var model = Assert.IsType<PagamentosViewModel>(viewResult.Model);

            Assert.Single(model.LeiloesGanhos);
            Assert.Single(model.MeusLeiloes);

            Assert.Equal("Leilão Ganho", model.LeiloesGanhos.First().Item.Titulo);
            Assert.Equal("Meu Leilão", model.MeusLeiloes.First().Item.Titulo);
        }


        [Fact]
        public async Task PagamentoDetalhes_LeilaoInexistenteOuPago_ShouldRedirectWithError()
        {
            // Arrange
            // 1) Usuario no DB + login
            var user = new Utilizador
            {
                Nome = "user",
                Email = "user@example.com",
                Password = "Pa$$w0rd"
            };
            _dbContext.Utilizador.Add(user);
            await _dbContext.SaveChangesAsync();

            _httpContext.Session.SetString("UserEmail", user.Email);

            // 2) Leilão já pago
            var leilaoPago = new Leilao
            {
                LeilaoId = 99,
                VencedorId = user.UtilizadorId,
                Pago = true,  // <--- já pago
                Item = new Item { Titulo = "Leilão já pago" }
            };
            _dbContext.Leiloes.Add(leilaoPago);
            await _dbContext.SaveChangesAsync();

            // Act
            var result = await _controller.PagamentoDetalhes(99);

            // Assert
            var redirect = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Pagamentos", redirect.ActionName);
            Assert.True(_controller.TempData.ContainsKey("PaymentError"));
        }

        [Fact]
        public async Task PagamentoDetalhes_LeilaoNaoPagoDeveMostrarView()
        {
            // Arrange
            // 1) Usuário e login
            var user = new Utilizador
            {
                Nome = "user",
                Email = "user@example.com",
                Password = "Pa$$w0rd"
            };
            _dbContext.Utilizador.Add(user);
            await _dbContext.SaveChangesAsync();

            _httpContext.Session.SetString("UserEmail", user.Email);

            // 2) Leilao não pago
            var leilao = new Leilao
            {
                LeilaoId = 111,
                VencedorId = user.UtilizadorId,
                Pago = false,
                Item = new Item { Titulo = "Leilão a pagar" }
            };
            _dbContext.Leiloes.Add(leilao);

            // 3) Descontos disponíveis
            var descontoResgatado = new DescontoResgatado
            {
                DescontoResgatadoId = 1,
                UtilizadorId = user.UtilizadorId,
                DataResgate = DateTime.Now.AddDays(-2),
                DataValidade = DateTime.Now.AddDays(5),
                Usado = false,
                Desconto = new Desconto
                {
                    DescontoId = 100,
                    Descricao = "10% off",
                    Valor = 10
                }
            };
            _dbContext.DescontoResgatado.Add(descontoResgatado);

            await _dbContext.SaveChangesAsync();

            // Act
            var result = await _controller.PagamentoDetalhes(111);

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            var model = Assert.IsType<PagamentoDetalhesViewModel>(viewResult.Model);

            Assert.Equal(111, model.Leilao.LeilaoId);
            Assert.NotNull(model.DescontosDisponiveis);
            Assert.Single(model.DescontosDisponiveis);
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

    }
}

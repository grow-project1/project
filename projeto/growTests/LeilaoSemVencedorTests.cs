using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Xunit;
using Moq;
using growTests.Controllers;
using growTests.Data;
using growTests.Models;
using Microsoft.AspNetCore.Http;
using growTests;

namespace growTests
{
    public class LeilaoSemVencedorTests
    {
        private readonly ApplicationDbContext _dbContext;
        private readonly Mock<IConfiguration> _mockConfig;
        private readonly FakeEmailSender _fakeEmailSender;
        private readonly LeilaosController _controller;

        public LeilaoSemVencedorTests()
        {
            // 1) Configurar DB in-memory
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;
            _dbContext = new ApplicationDbContext(options);

            // 2) Mock da IConfiguration
            _mockConfig = new Mock<IConfiguration>();

            // 3) FakeEmailSender
            _fakeEmailSender = new FakeEmailSender();

            // 4)  controller injetando o FakeEmailSender
            _controller = new LeilaosController(
                _dbContext,
                null,               // IWebHostEnvironment (não usado no teste)
                _mockConfig.Object, // IConfiguration mockado
                _fakeEmailSender
            );

            // 5)  MockSession para evitar erro de "Session has not been configured"
            var defaultHttpContext = new DefaultHttpContext
            {
                Session = new MockSession() // MockSession
            };
            _controller.ControllerContext = new Microsoft.AspNetCore.Mvc.ControllerContext
            {
                HttpContext = defaultHttpContext
            };
        }

        [Fact]
        public async Task LeilaoSemVencedor_QuandoEncerrado_EnviaEmailParaLeiloeiro()
        {
            // Arrange
            // 1. Criar um leiloeiro (dono)
            var dono = new Utilizador
            {
                UtilizadorId = 1,
                Nome = "Leiloeiro Teste",
                Email = "dono@teste.com",
                Password = "password"
            };
            _dbContext.Utilizador.Add(dono);

            // 2. Criar item
            var item = new Item
            {
                ItemId = 10,
                Titulo = "Item sem Lances",
                Descricao = "Teste",
                PrecoInicial = 100,
                Categoria = Categoria.Tecnologia
            };
            _dbContext.Itens.Add(item);
            await _dbContext.SaveChangesAsync();

            // 3. Criar Leilao sem licitações e com DataFim já no passado
            var leilao = new Leilao
            {
                LeilaoId = 100,
                ItemId = item.ItemId,
                Item = item,
                DataInicio = DateTime.Now.AddDays(-5),
                DataFim = DateTime.Now.AddDays(-1), // já expirou
                UtilizadorId = dono.UtilizadorId,
                ValorIncrementoMinimo = 10,
                ValorAtualLance = 0,
                EstadoLeilao = EstadoLeilao.Disponivel
            };
            _dbContext.Leiloes.Add(leilao);
            await _dbContext.SaveChangesAsync();

            // Act
            // Chamamos Index(...) que possui a lógica de encerramento
            await _controller.Index(null, null, null, null);

            // Assert
            // Verifica se o leilão foi encerrado
            var leilaoAtualizado = await _dbContext.Leiloes.FindAsync(leilao.LeilaoId);
            Assert.Equal(EstadoLeilao.Encerrado, leilaoAtualizado.EstadoLeilao);

            // Verifica se não há vencedor
            Assert.Null(leilaoAtualizado.Vencedor);

            // confirmar o email e o seu conteudo

            Assert.Contains(_fakeEmailSender.SentEmails, mail =>
                mail.To == "dono@teste.com" &&
                mail.Subject.Contains("terminou sem licitações") &&
                mail.Body.Contains("não obteve nenhuma licitação")
            );
        }
    }
}

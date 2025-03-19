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

namespace growTests
{
    public class AuctionsNotificationsTests
    {
        private readonly ApplicationDbContext _dbContext;
        private readonly Mock<IConfiguration> _mockConfig;
        private readonly FakeEmailSender _fakeEmailSender;
        private readonly LeilaosController _controller;

        public AuctionsNotificationsTests()
        {
            // 1) DB InMemory
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;
            _dbContext = new ApplicationDbContext(options);

            // 2) Mock da IConfiguration
            _mockConfig = new Mock<IConfiguration>();

            // 3) FakeEmailSender (armazena ou simula envio)
            _fakeEmailSender = new FakeEmailSender();

            // 4) Criar o controller e injetar dependências
            _controller = new LeilaosController(
                _dbContext,
                null,              // IWebHostEnvironment
                _mockConfig.Object,
                _fakeEmailSender
            );

            // 5) MockSession para evitar erro "Session not configured"
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
        public async Task AuctionEndsWithoutWinner_SendsEmailToOwner()
        {
            // Arrange
            // Dono do leilão (sem licitações)
            var owner = new Utilizador
            {
                UtilizadorId = 1,
                Nome = "Leiloeiro Teste",
                Email = "dono@teste.com",
                Password = "password"
            };
            _dbContext.Utilizador.Add(owner);

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

            // Leilão que já expirou
            var leilao = new Leilao
            {
                LeilaoId = 100,
                ItemId = item.ItemId,
                Item = item,
                DataInicio = DateTime.Now.AddDays(-5),
                DataFim = DateTime.Now.AddDays(-1),
                UtilizadorId = owner.UtilizadorId,
                ValorIncrementoMinimo = 10,
                ValorAtualLance = 0,
                EstadoLeilao = EstadoLeilao.Disponivel
            };
            _dbContext.Leiloes.Add(leilao);
            await _dbContext.SaveChangesAsync();

            // Act
            // Chama o método Index(...) que contém a lógica de encerramento
            await _controller.Index(null, null, null, null);

            // Assert
            // Verifica se o leilão foi encerrado
            var updatedAuction = await _dbContext.Leiloes.FindAsync(leilao.LeilaoId);
            Assert.Equal(EstadoLeilao.Encerrado, updatedAuction.EstadoLeilao);

            // Verifica se não tem vencedor
            Assert.Null(updatedAuction.Vencedor);

            // Verifica se o FakeEmailSender enviou email de "sem licitações" para o dono
            Assert.Contains(_fakeEmailSender.SentEmails, mail =>
                mail.To == "dono@teste.com" &&
                mail.Subject.Contains("terminou sem licitações") &&
                mail.Body.Contains("não obteve nenhuma licitação")
            );
        }

        [Fact]
        public async Task AuctionEndsWithWinner_SendsEmailToOwner()
        {
            // Arrange
            // (A) Dono (owner) do leilão
            var owner = new Utilizador
            {
                UtilizadorId = 2,
                Nome = "Dono do Leilao",
                Email = "owner@teste.com",
                Password = "passwordUser"
            };
            _dbContext.Utilizador.Add(owner);

            // (B) Vencedor (winner) - mas não vamos associar ainda
            var winner = new Utilizador
            {
                UtilizadorId = 3,
                Nome = "Comprador",
                Email = "winner@teste.com",
                Password= "passwordWinner"

            };
            _dbContext.Utilizador.Add(winner);
            await _dbContext.SaveChangesAsync();

            // (C) Item
            var item = new Item
            {
                ItemId = 20,
                Titulo = "Item Com Lances",
                Descricao = "Teste item com lances",
                PrecoInicial = 200,
                Categoria = Categoria.Tecnologia
            };
            _dbContext.Itens.Add(item);
            await _dbContext.SaveChangesAsync();

            // (D) Leilão que já expirou
            var leilao = new Leilao
            {
                LeilaoId = 200,
                ItemId = item.ItemId,
                Item = item,
                DataInicio = DateTime.Now.AddDays(-5),
                DataFim = DateTime.Now.AddDays(-1),  // Expirado
                UtilizadorId = owner.UtilizadorId,
                ValorIncrementoMinimo = 10,
                ValorAtualLance = 0,
                EstadoLeilao = EstadoLeilao.Disponivel
            };
            _dbContext.Leiloes.Add(leilao);
            await _dbContext.SaveChangesAsync();

            // (E) Cria uma licitação para "winner"
            var licitacao = new Licitacao
            {
                LeilaoId = leilao.LeilaoId,
                UtilizadorId = winner.UtilizadorId,
                ValorLicitacao = 300,
                DataLicitacao = DateTime.Now
            };
            _dbContext.Licitacoes.Add(licitacao);
            await _dbContext.SaveChangesAsync();

            // Act
            await _controller.Index(null, null, null, null);

            // Assert
            // Busca o leilão atualizado
            var updatedAuction = await _dbContext.Leiloes.FindAsync(leilao.LeilaoId);
            Assert.Equal(EstadoLeilao.Encerrado, updatedAuction.EstadoLeilao);

            // Verifica se tem vencedor
            Assert.Equal("Comprador", updatedAuction.Vencedor);

            // Verifica se um e-mail foi enviado ao DONO do leilão
            Assert.Contains(_fakeEmailSender.SentEmails, mail =>
                mail.To == "owner@teste.com" &&
                mail.Subject.Contains("foi vendido!") &&     // "O seu leilão {titulo} foi vendido!"
                mail.Body.Contains("foi vendido por 300€")   // p.ex. "foi vendido por {licitacao.ValorLicitacao}"
            );
        }

        [Fact]
        public async Task AuctionEndsWithWinner_SendsEmailToWinner()
        {
            // Arrange
            var owner = new Utilizador
            {
                UtilizadorId = 4,
                Nome = "Dono",
                Email = "owner2@teste.com",
                Password = "passwordOwner2"
            };
            _dbContext.Utilizador.Add(owner);

            var winner = new Utilizador
            {
                UtilizadorId = 5,
                Nome = "Winner2",
                Email = "winner2@teste.com",
                Password = "passordWinner"
            };
            _dbContext.Utilizador.Add(winner);
            await _dbContext.SaveChangesAsync();

            var item = new Item
            {
                ItemId = 30,
                Titulo = "Item Com Lances 2",
                Descricao = "Teste item2 com lances",
                PrecoInicial = 150,
                Categoria = Categoria.Tecnologia
            };
            _dbContext.Itens.Add(item);
            await _dbContext.SaveChangesAsync();

            var leilao = new Leilao
            {
                LeilaoId = 300,
                ItemId = item.ItemId,
                Item = item,
                DataInicio = DateTime.Now.AddDays(-2),
                DataFim = DateTime.Now.AddDays(-1),
                UtilizadorId = owner.UtilizadorId,
                ValorIncrementoMinimo = 10,
                ValorAtualLance = 0,
                EstadoLeilao = EstadoLeilao.Disponivel
            };
            _dbContext.Leiloes.Add(leilao);
            await _dbContext.SaveChangesAsync();

            var licitacao = new Licitacao
            {
                LeilaoId = leilao.LeilaoId,
                UtilizadorId = winner.UtilizadorId,
                ValorLicitacao = 250,
                DataLicitacao = DateTime.Now
            };
            _dbContext.Licitacoes.Add(licitacao);
            await _dbContext.SaveChangesAsync();

            // Act
            await _controller.Index(null, null, null, null);

            // Assert
            var updatedAuction = await _dbContext.Leiloes.FindAsync(leilao.LeilaoId);
            Assert.Equal(EstadoLeilao.Encerrado, updatedAuction.EstadoLeilao);
            Assert.Equal("Winner2", updatedAuction.Vencedor);

            // Confirma se o e-mail chegou ao vencedor
            Assert.Contains(_fakeEmailSender.SentEmails, mail =>
                mail.To == "winner2@teste.com" &&
                mail.Subject.Contains("Parabéns! Ganhaste o leilão") &&
                mail.Body.Contains("Você venceu o leilão do item <strong>Item Com Lances 2</strong>")
            );
        }
    }
}

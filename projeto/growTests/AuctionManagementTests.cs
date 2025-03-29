using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Moq;
using projeto.Controllers;
using projeto.Data;
using projeto.Models;
using Microsoft.Extensions.Configuration;
using growTests.TestsHelpers;
using Microsoft.AspNetCore.Hosting;
namespace growTests
{
    public class AuctionManagementTests
    {
        private readonly ApplicationDbContext _dbContext;
        private readonly LeilaosController _controller;
        private readonly DefaultHttpContext _httpContext;
        private readonly Mock<IConfiguration> _mockConfig;
        private readonly FakeEmailSender _fakeEmailSender;

        public AuctionManagementTests()
        {
            // Configuração do banco de dados em memória
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

            _dbContext = new ApplicationDbContext(options);

            // Mock de IConfiguration e EmailSender
            _mockConfig = new Mock<IConfiguration>();
            _fakeEmailSender = new FakeEmailSender();

            // Criar mock de IWebHostEnvironment
            var mockEnv = new Mock<IWebHostEnvironment>();
            // Defina qualquer caminho que faça sentido nos seus testes.
            // Pode ser algo como Path.GetTempPath() ou "C:\Test\wwwroot" etc.
            mockEnv.Setup(e => e.WebRootPath).Returns(Path.GetTempPath());

            // Criar o Controller
            _controller = new LeilaosController(
                _dbContext,
                mockEnv.Object,        // <<--- Em vez de 'null'
                _mockConfig.Object,
                _fakeEmailSender
            );

            // Configurar a sessão fake
            _httpContext = new DefaultHttpContext
            {
                Session = new MockSession()
            };
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = _httpContext
            };

            // Configurar TempData
            _controller.TempData = new TempDataDictionary(_httpContext, Mock.Of<ITempDataProvider>());
        }

        [Fact]
        public async Task Index_FilterByCategory_ShouldReturnCorrectAuctions()
        {
            // Arrange: Cria dois leilões com categorias diferentes
            var auction1 = new Leilao
            {
                LeilaoId = 1,
                UtilizadorId = 1,
                EstadoLeilao = EstadoLeilao.Disponivel,
                DataFim = DateTime.Now.AddDays(1),
                Item = new Item { Titulo = "Auction 1", Categoria = Categoria.Tecnologia, PrecoInicial = 100, Sustentavel = false }
            };
            var auction2 = new Leilao
            {
                LeilaoId = 2,
                UtilizadorId = 1,
                EstadoLeilao = EstadoLeilao.Disponivel,
                DataFim = DateTime.Now.AddDays(1),
                Item = new Item { Titulo = "Auction 2", Categoria = Categoria.Moda, PrecoInicial = 200, Sustentavel = false }
            };
            _dbContext.Leiloes.AddRange(auction1, auction2);
            await _dbContext.SaveChangesAsync();

            // Act: Chama o método Index filtrando pela categoria "Eletronicos"
            var result = await _controller.Index("Tecnologia", null, null, null);
            var viewResult = Assert.IsType<ViewResult>(result);
            var model = Assert.IsAssignableFrom<System.Collections.Generic.List<Leilao>>(viewResult.Model);

            // Assert: Somente o auction1 deve estar presente
            Assert.Single(model);
            Assert.Equal("Auction 1", model.First().Item.Titulo);
        }

        [Fact]
        public async Task Details_AuctionClosed_ShouldIncludeBids()
        {
            // Arrange: Cria um leilão expirado com licitações
            var auction = new Leilao
            {
                LeilaoId = 3,
                UtilizadorId = 1,
                EstadoLeilao = EstadoLeilao.Disponivel,
                DataFim = DateTime.Now.AddDays(-1), // Já expirado
                Item = new Item { Titulo = "Closed Auction", Categoria = Categoria.Moda, PrecoInicial = 150, Sustentavel = false }
            };
            _dbContext.Leiloes.Add(auction);
            _dbContext.Licitacoes.Add(new Licitacao
            {
                LeilaoId = 3,
                UtilizadorId = 2,
                ValorLicitacao = 180,
                DataLicitacao = DateTime.Now.AddDays(-2)
            });
            await _dbContext.SaveChangesAsync();

            // Act: Chama o método Details
            var result = await _controller.Details(3);
            var viewResult = Assert.IsType<ViewResult>(result);
            var model = Assert.IsType<Leilao>(viewResult.Model);

            // Assert: Como o leilão está encerrado, a lista de licitações deve estar populada
            Assert.NotNull(model.Licitacoes);
            Assert.Single(model.Licitacoes);
        }
        [Fact]
        public async Task Create_ValidAuction_ShouldCreateAuctionAndRedirect()
        {
            // 1) Criar um utilizador e salvar no DB de teste
            var user = new Utilizador
            {
                UtilizadorId = 1,
                Nome = "Test User",
                Email = "user@example.com",
                Password = "12345"
            };
            _dbContext.Utilizador.Add(user);
            await _dbContext.SaveChangesAsync();

            // 2) Simular que esse usuário está logado, guardando o email na sessão
            _httpContext.Session.SetString("UserEmail", user.Email);

            // 3) Criar um ficheiro fake (FakeFormFile)
            var stream = new MemoryStream(new byte[1024]); // 1KB
            var fakeFile = new FakeFormFile(stream, stream.Length, "file", "test.jpg", "image/jpeg");

            // 4) Montar o Leilao com dados válidos
            var newAuction = new Leilao
            {
                // O controller vai sobrescrever UtilizadorId
                // mas não tem problema definir aqui por clareza
                UtilizadorId = user.UtilizadorId,
                DataFim = DateTime.Now.AddDays(1),
                ValorIncrementoMinimo = 10,
                Item = new Item
                {
                    Titulo = "New Auction",
                    Descricao = "Description",
                    PrecoInicial = 50,
                    Categoria = Categoria.Lazer,
                    Sustentavel = false,
                    fotoo = fakeFile
                }
            };

            // 5) Chamar o método Create do controller
            var result = await _controller.Create(newAuction);

            // 6) Verificar se o ActionResult é um RedirectToActionResult -> "Index"
            var redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Index", redirectResult.ActionName);

            // 7) Verificar se realmente foi criado na base de dados
            var createdAuction = await _dbContext.Leiloes
                .Include(l => l.Item)
                .FirstOrDefaultAsync(a => a.Item.Titulo == "New Auction");

            Assert.NotNull(createdAuction);
            Assert.Equal("New Auction", createdAuction.Item.Titulo);
        }



        [Fact]
        public async Task RecolocarLeilao_InvalidDate_ShouldReturnViewWithError()
        {
            // 1) Criar e salvar usuário
            var user = new Utilizador
            {
                UtilizadorId = 1,
                Nome = "TestUser",
                Email = "user@example.com",
                Password = "Pass"
            };
            _dbContext.Utilizador.Add(user);

            // 2) Criar e salvar um leilão para testarmos a recolocação
            var auction = new Leilao
            {
                LeilaoId = 5,
                UtilizadorId = user.UtilizadorId, // ou outro ID, mas coerente
                EstadoLeilao = EstadoLeilao.Disponivel,
                DataFim = DateTime.Now.AddDays(2),
                ValorIncrementoMinimo = 15,
                Item = new Item
                {
                    Titulo = "Auction to repost",
                    Descricao = "Desc",
                    PrecoInicial = 80,
                    Categoria = Categoria.Tecnologia,
                    Sustentavel = false
                }
            };
            _dbContext.Leiloes.Add(auction);

            // Salva tudo no DB in-memory
            await _dbContext.SaveChangesAsync();

            // 3) Simular que o usuário está logado via sessão
            _httpContext.Session.SetString("UserEmail", "user@example.com");

            // 4) Montar um "updatedAuction" com data inválida (passado)
            var updatedAuction = new Leilao
            {
                LeilaoId = 5,
                DataFim = DateTime.Now.AddDays(-1), // inválido
                ValorIncrementoMinimo = 15,
                Item = new Item
                {
                    Titulo = "Auction to repost",
                    Descricao = "Desc updated",
                    PrecoInicial = 80,
                    Categoria = Categoria.Tecnologia,
                    Sustentavel = false
                }
            };

            // 5) Chamar a Action
            var result = await _controller.RecolocarLeilao(5, novaFoto: null, updatedAuction);

            // 6) Verificar se é ViewResult e se tem erro no ModelState
            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.False(viewResult.ViewData.ModelState.IsValid);  // pois deve ter o erro
            Assert.True(viewResult.ViewData.ModelState.ContainsKey("DataFim"));
        }


        [Fact]
        public async Task FazerLicitacao_ValidBid_ShouldAddBidAndUpdateAuction()
        {
            // Arrange: Cria um leilão sem licitações
            var auction = new Leilao
            {
                LeilaoId = 6,
                UtilizadorId = 1,
                EstadoLeilao = EstadoLeilao.Disponivel,
                DataFim = DateTime.Now.AddDays(1),
                ValorIncrementoMinimo = 10,
                Item = new Item { Titulo = "Bidding Auction", Descricao = "Desc", PrecoInicial = 100, Categoria = Categoria.Tecnologia, Sustentavel = false }
            };
            _dbContext.Leiloes.Add(auction);
            await _dbContext.SaveChangesAsync();

            // Simula o usuário logado e cria o usuário que fará a licitação
            _httpContext.Session.SetString("UserEmail", "bidder@example.com");
            var bidder = new Utilizador
            {
                UtilizadorId = 2,
                Nome = "Bidder",
                Email = "bidder@example.com",
                Password = "Password" // valor simples para teste
            };
            _dbContext.Utilizador.Add(bidder);
            await _dbContext.SaveChangesAsync();

            // Act: Faça uma licitação válida (valor = PrecoInicial + incremento)
            double valorLicitacao = 110;
            var result = await _controller.FazerLicitacao(6, valorLicitacao);

            // Assert: Verifica se a licitação foi adicionada e se o leilão foi atualizado
            var bid = await _dbContext.Licitacoes.FirstOrDefaultAsync(l => l.LeilaoId == 6 && l.UtilizadorId == 2);
            Assert.NotNull(bid);
            Assert.Equal(110, bid.ValorLicitacao);

            var updatedAuction = await _dbContext.Leiloes.FindAsync(6);
            Assert.Equal(110, updatedAuction.ValorAtualLance);
        }

        [Fact]
        public async Task AtualizarEstadoLeiloes_ShouldCloseExpiredAuctions()
        {

            // Arrange: Cria um leilão expirado
            var auction = new Leilao
            {
                LeilaoId = 7,
                UtilizadorId = 1,
                EstadoLeilao = EstadoLeilao.Disponivel,
                DataFim = DateTime.Now.AddHours(-1), // Já expirou
                ValorIncrementoMinimo = 10,
                Item = new Item
                {
                    Titulo = "Expired Auction",
                    Descricao = "Desc",
                    PrecoInicial = 100,
                    Categoria = Categoria.Moda,
                    Sustentavel = false
                },
                Licitacoes = new List<Licitacao>() // Inicializa a lista para evitar NullReferenceException
            };

            var bid = new Licitacao
            {
                LeilaoId = auction.LeilaoId,
                ValorLicitacao = auction.Item.PrecoInicial + 10,
                DataLicitacao = DateTime.Now,
                UtilizadorId = 1 // ou o id de um usuário válido
            };

            auction.Licitacoes.Add(bid);
            _dbContext.Leiloes.Add(auction);
            await _dbContext.SaveChangesAsync();

            // Act: Chama AtualizarEstadoLeiloes
            var result = await _controller.AtualizarEstadoLeiloes();
            var redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Index", redirectResult.ActionName);

            // Assert: Verifica que o leilão foi fechado
            var updatedAuction = await _dbContext.Leiloes.FindAsync(7);
            Assert.Equal(EstadoLeilao.Encerrado, updatedAuction.EstadoLeilao);
        }

        [Fact]
        public async Task MyAuctions_ShouldReturnAuctionsForLoggedUser()
        {
            // Arrange: Crie dois leilões para o usuário com email "user@example.com"
            var auction1 = new Leilao { LeilaoId = 8, UtilizadorId = 1, EstadoLeilao = EstadoLeilao.Disponivel, DataFim = DateTime.Now.AddDays(1), ValorIncrementoMinimo = 10, Item = new Item { Titulo = "User Auction 1", Descricao = "Desc", PrecoInicial = 100, Categoria = Categoria.Tecnologia, Sustentavel = false } };
            var auction2 = new Leilao { LeilaoId = 9, UtilizadorId = 1, EstadoLeilao = EstadoLeilao.Disponivel, DataFim = DateTime.Now.AddDays(2), ValorIncrementoMinimo = 15, Item = new Item { Titulo = "User Auction 2", Descricao = "Desc", PrecoInicial = 150, Categoria = Categoria.Moda, Sustentavel = false } };
            _dbContext.Leiloes.AddRange(auction1, auction2);
            await _dbContext.SaveChangesAsync();

            // Simula o usuário logado
            _httpContext.Session.SetString("UserEmail", "user@example.com");
            var user = new Utilizador { UtilizadorId = 1, Nome = "User", Email = "user@example.com", Password = "Test" };
            _dbContext.Utilizador.Add(user);
            await _dbContext.SaveChangesAsync();

            // Act: Chama MyAuctions
            var result = await _controller.MyAuctions();
            var viewResult = Assert.IsType<ViewResult>(result);
            var model = Assert.IsAssignableFrom<System.Collections.Generic.List<Leilao>>(viewResult.Model);

            // Assert: Verifica que os leilões retornados pertencem ao usuário
            Assert.Equal(2, model.Count);
            Assert.All(model, a => Assert.Equal(1, a.UtilizadorId));
        }

        [Fact]
        public async Task MyBids_ShouldReturnBidsForLoggedUser()
        {
            // Arrange
            var user = new Utilizador
            {
                UtilizadorId = 2,
                Nome = "Bidder",
                Email = "bidder@example.com",
                Password = "Test"
            };
            _dbContext.Utilizador.Add(user);

            // Precisamos criar também os Leilões com ID=10 e ID=11
            var leilao10 = new Leilao
            {
                LeilaoId = 10,
                UtilizadorId = 99, // qualquer usuário
                DataFim = DateTime.Now.AddDays(1),
                Item = new Item { Titulo = "Test Leilao 10", PrecoInicial = 100, Categoria = Categoria.Moda }
            };
            var leilao11 = new Leilao
            {
                LeilaoId = 11,
                UtilizadorId = 99,
                DataFim = DateTime.Now.AddDays(1),
                Item = new Item { Titulo = "Test Leilao 11", PrecoInicial = 150, Categoria = Categoria.Tecnologia }
            };

            _dbContext.Leiloes.Add(leilao10);
            _dbContext.Leiloes.Add(leilao11);

            // Salva tudo de uma só vez aqui
            await _dbContext.SaveChangesAsync();

            // Agora sim adicionamos as licitações
            _dbContext.Licitacoes.Add(new Licitacao
            {
                LeilaoId = 10,
                UtilizadorId = 2,
                ValorLicitacao = 120,
                DataLicitacao = DateTime.Now
            });
            _dbContext.Licitacoes.Add(new Licitacao
            {
                LeilaoId = 11,
                UtilizadorId = 2,
                ValorLicitacao = 130,
                DataLicitacao = DateTime.Now
            });
            await _dbContext.SaveChangesAsync();

            // Simula sessão do usuário logado
            _httpContext.Session.SetString("UserEmail", "bidder@example.com");

            // Act
            var result = await _controller.MyBids();

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            var model = Assert.IsAssignableFrom<List<Licitacao>>(viewResult.Model);

            Assert.Equal(2, model.Count);
            Assert.All(model, b => Assert.Equal(2, b.UtilizadorId));
        }


        [Fact]
        public async Task TopAuctions_ShouldReturnAuctionsSortedByBidCount()
        {
            // Arrange: Crie três leilões com diferentes números de licitações
            var auction1 = new Leilao { LeilaoId = 12, UtilizadorId = 1, EstadoLeilao = EstadoLeilao.Disponivel, DataFim = DateTime.Now.AddDays(1), ValorIncrementoMinimo = 10, Item = new Item { Titulo = "Auction A", Descricao = "Desc", PrecoInicial = 100, Categoria = Categoria.Tecnologia, Sustentavel = false } };
            var auction2 = new Leilao { LeilaoId = 13, UtilizadorId = 1, EstadoLeilao = EstadoLeilao.Disponivel, DataFim = DateTime.Now.AddDays(1), ValorIncrementoMinimo = 10, Item = new Item { Titulo = "Auction B", Descricao = "Desc", PrecoInicial = 200, Categoria = Categoria.Moda, Sustentavel = false } };
            var auction3 = new Leilao { LeilaoId = 14, UtilizadorId = 1, EstadoLeilao = EstadoLeilao.Disponivel, DataFim = DateTime.Now.AddDays(1), ValorIncrementoMinimo = 10, Item = new Item { Titulo = "Auction C", Descricao = "Desc", PrecoInicial = 300, Categoria = Categoria.Moda, Sustentavel = false } };

            _dbContext.Leiloes.AddRange(auction1, auction2, auction3);
            await _dbContext.SaveChangesAsync();

            // Adicione licitações: auction1 com 1 bid, auction2 com 3 bids, auction3 com 2 bids.
            _dbContext.Licitacoes.Add(new Licitacao { LeilaoId = 12, UtilizadorId = 2, ValorLicitacao = 110, DataLicitacao = DateTime.Now });
            _dbContext.Licitacoes.Add(new Licitacao { LeilaoId = 13, UtilizadorId = 2, ValorLicitacao = 210, DataLicitacao = DateTime.Now });
            _dbContext.Licitacoes.Add(new Licitacao { LeilaoId = 13, UtilizadorId = 3, ValorLicitacao = 220, DataLicitacao = DateTime.Now });
            _dbContext.Licitacoes.Add(new Licitacao { LeilaoId = 13, UtilizadorId = 4, ValorLicitacao = 230, DataLicitacao = DateTime.Now });
            _dbContext.Licitacoes.Add(new Licitacao { LeilaoId = 14, UtilizadorId = 2, ValorLicitacao = 310, DataLicitacao = DateTime.Now });
            _dbContext.Licitacoes.Add(new Licitacao { LeilaoId = 14, UtilizadorId = 3, ValorLicitacao = 320, DataLicitacao = DateTime.Now });
            await _dbContext.SaveChangesAsync();

            // Act: Chama TopAuctions
            var result = await _controller.TopAuctions();
            var viewResult = Assert.IsType<ViewResult>(result);
            var model = Assert.IsAssignableFrom<System.Collections.Generic.List<Leilao>>(viewResult.Model);

            // Assert: Verifica a ordem (auction2 deve ter mais bids que auction3, e auction1 a última)
            Assert.Equal(3, model.Count);
            Assert.Equal("Auction B", model[0].Item.Titulo);
            Assert.Equal("Auction C", model[1].Item.Titulo);
            Assert.Equal("Auction A", model[2].Item.Titulo);
        }

        /////
        [Fact]
        public async Task Delete_AuctionWithBidsAndNotPaid_ShouldNotDeleteAuction()
        {
            // Arrange: Cria um leilão com uma licitação e que ainda não foi pago
            var auction = new Leilao
            {
                LeilaoId = 10,
                UtilizadorId = 1,
                EstadoLeilao = EstadoLeilao.Disponivel,
                DataFim = DateTime.Now.AddDays(1),
                ValorIncrementoMinimo = 10,
                Pago = false,
                Item = new Item
                {
                    Titulo = "Auction With Bid",
                    Descricao = "Test Auction",
                    PrecoInicial = 100,
                    Categoria = Categoria.Moda,
                    Sustentavel = false
                },
                Licitacoes = new List<Licitacao>()
            };

            // Adiciona uma licitação válida
            auction.Licitacoes.Add(new Licitacao
            {
                LeilaoId = auction.LeilaoId,
                UtilizadorId = 2,
                ValorLicitacao = 110,
                DataLicitacao = DateTime.Now
            });

            _dbContext.Leiloes.Add(auction);
            await _dbContext.SaveChangesAsync();

            // Act: Tenta eliminar o leilão
            var result = await _controller.DeleteConfirmed(auction.LeilaoId);

            // Assert: Verifica que o TempData contém a mensagem de erro e o leilão ainda existe
            Assert.True(_controller.TempData.ContainsKey("Error"));
            var existingAuction = await _dbContext.Leiloes.FindAsync(auction.LeilaoId);
            Assert.NotNull(existingAuction);
        }

        [Fact]
        public async Task FazerLicitacao_InvalidBid_ShouldReturnError()
        {
            // Arrange: Cria um leilão com um lance inicial
            var auction = new Leilao
            {
                LeilaoId = 20,
                UtilizadorId = 1,
                EstadoLeilao = EstadoLeilao.Disponivel,
                DataFim = DateTime.Now.AddDays(1),
                ValorIncrementoMinimo = 10,
                Item = new Item
                {
                    Titulo = "Auction For Bid Test",
                    Descricao = "Test Description",
                    PrecoInicial = 100,
                    Categoria = Categoria.Moda,
                    Sustentavel = false
                },
                Licitacoes = new List<Licitacao>()
            };

            // Não adiciona nenhum lance para que o lance mínimo seja o preço inicial
            _dbContext.Leiloes.Add(auction);
            await _dbContext.SaveChangesAsync();

            // Simula um usuário autenticado
            string testEmail = "bidder@example.com";
            var user = new Utilizador { UtilizadorId = 2, Nome = "Bidder", Email = testEmail, Password = "Dummy" };
            _dbContext.Utilizador.Add(user);
            await _dbContext.SaveChangesAsync();
            _controller.ControllerContext.HttpContext.Session.SetString("UserEmail", testEmail);

            // Act: Tenta fazer uma licitação com valor abaixo do mínimo (por exemplo, 105, quando o mínimo deveria ser 100 + 10 = 110)
            var result = await _controller.FazerLicitacao(auction.LeilaoId, 105.0);

            // Assert: Verifica que há uma mensagem de erro em TempData e que o lance não foi adicionado
            Assert.True(_controller.TempData.ContainsKey("Error"));
            var updatedAuction = await _dbContext.Leiloes
                                   .Include(a => a.Licitacoes)
                                   .FirstOrDefaultAsync(a => a.LeilaoId == auction.LeilaoId);
            Assert.Empty(updatedAuction.Licitacoes);
        }


    }
}

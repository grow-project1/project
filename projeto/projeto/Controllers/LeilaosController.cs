using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.Web.CodeGenerators.Mvc.Templates.BlazorIdentity.Pages.Manage;
using projeto.Data;
using projeto.Models;
using Microsoft.AspNetCore.Identity.UI.Services;

namespace projeto.Controllers
{
    public class LeilaosController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _env;
        private readonly IConfiguration _config;
        private readonly IEmailSender _emailSender;

        // ========================================================
        // Autor: Rodrigo Baião
        // Descrição: Construtor que inicializa o controlador de leilões
        // ========================================================
        public LeilaosController(ApplicationDbContext context, IWebHostEnvironment env, IConfiguration config, IEmailSender emailSender)
        {
            _context = context;
            _env = env;
            _config = config;
            _emailSender = emailSender;
        }

        // ========================================================
        // Autor: Samuel Alves
        // Descrição: Método para mostrar os leilões disponíveis com filtros
        // ========================================================
        public async Task<IActionResult> Index(string categorias, string tempo, double? min, double? max)
        {
            var userEmail = HttpContext.Session.GetString("UserEmail");
            var user = await _context.Utilizador.FirstOrDefaultAsync(u => u.Email == userEmail);

            ViewData["UserPoints"] = user?.Pontos;
            ViewData["Logged"] = user != null;
            ViewData["UserId"] = user?.UtilizadorId;

            var categoriasEnum = Enum.GetValues(typeof(Categoria)).Cast<Categoria>().ToList();
            ViewData["Categorias"] = categoriasEnum;

            var query = _context.Leiloes.Include(l => l.Item).AsQueryable();

            if (!string.IsNullOrEmpty(categorias))
            {
                var categoriasSelecionadas = categorias.Split(',').Select(c => Enum.Parse<Categoria>(c)).ToList();
                query = query.Where(l => categoriasSelecionadas.Contains(l.Item.Categoria));
            }

            if (!string.IsNullOrEmpty(tempo))
            {
                var agora = DateTime.Now;
                var filtrosTempo = tempo.Split(',').Select(int.Parse).ToList();

                query = query.Where(l => filtrosTempo.Any(t => l.DataFim <= agora.AddDays(t)));
            }

            if (min.HasValue)
                query = query.Where(l => l.ValorAtualLance >= min.Value);
            if (max.HasValue)
                query = query.Where(l => l.ValorAtualLance <= max.Value);

            var leiloes = await query.ToListAsync();

            var leilaoIds = leiloes.Select(l => l.LeilaoId).ToList();
            var maioresLances = await _context.Licitacoes
                .Where(l => leilaoIds.Contains(l.LeilaoId))
                .GroupBy(l => l.LeilaoId)
                .Select(g => new { LeilaoId = g.Key, MaiorLance = g.Max(l => (double?)l.ValorLicitacao) })
                .ToListAsync();

            foreach (var leilao in leiloes)
            {
                leilao.ValorAtualLance = maioresLances.FirstOrDefault(l => l.LeilaoId == leilao.LeilaoId)?.MaiorLance ?? leilao.Item.PrecoInicial;
            }

            foreach (var leilao in leiloes.Where(l => DateTime.Now > l.DataFim && l.EstadoLeilao != EstadoLeilao.Encerrado))
            {
                leilao.EstadoLeilao = EstadoLeilao.Encerrado;
                var licitacaoVencedora = await _context.Licitacoes
                    .Where(l => l.LeilaoId == leilao.LeilaoId)
                    .OrderByDescending(l => l.ValorLicitacao)
                    .FirstOrDefaultAsync();

                var leiloeiro = await _context.Utilizador.FindAsync(leilao.UtilizadorId);

                if (licitacaoVencedora != null)
                {
                    var vencedor = await _context.Utilizador.FindAsync(licitacaoVencedora.UtilizadorId);

                    if (vencedor != null)
                    {
                        vencedor.Pontos += leilao.Item.Sustentavel ? 50 : 20;
                        _context.Update(vencedor);
                        leilao.Vencedor = vencedor;

                        string subject = $"Parabéns! Ganhaste o leilão {leilao.Item.Titulo}";
                        string message = $"<h2>Parabéns, {vencedor.Nome}!</h2>" +
                                           $"<p>Você venceu o leilão do item <strong>{leilao.Item.Titulo}</strong> pelo valor de {licitacaoVencedora.ValorLicitacao}€.</p>" +
                                           "<p>Entre na <a href='https://projeto-grow-2025.azurewebsites.net/' target='_blank' style='color: blue; text-decoration: underline;'>GROW</a> para proceder à entrega do item.</p>";

                        await _emailSender.SendEmailAsync(vencedor.Email, subject, message);

                        if (leiloeiro != null)
                        {
                            string subjectLeiloeiro = $"O seu leilão {leilao.Item.Titulo} foi vendido!";
                            string messageLeiloeiro = $"<h2>O seu leilão foi concluído com sucesso!</h2>" +
                                                      $"<p>O item <strong>{leilao.Item.Titulo}</strong> foi vendido por {licitacaoVencedora.ValorLicitacao}€.</p>" +
                                                      $"<p>O vencedor foi: <strong>{vencedor.Nome}</strong></p>" +
                                                      "<p>Entre na <a href='https://projeto-grow-2025.azurewebsites.net/' target='_blank' style='color: blue; text-decoration: underline;'>GROW</a> para coordenar a entrega do item.</p>";

                            await _emailSender.SendEmailAsync(leiloeiro.Email, subjectLeiloeiro, messageLeiloeiro);
                        }
                    }
                }
                else
                {
                    leilao.Vencedor = null;
                    if (leiloeiro != null)
                    {
                        string subjectLeiloeiroSemLicitacoes = $"O seu leilão {leilao.Item.Titulo} terminou sem licitações";
                        string messageLeiloeiroSemLicitacoes = $"<h2>O seu leilão chegou ao fim, mas não obteve nenhuma licitação.</h2>" +
                                                              $"<p>Infelizmente, o item <strong>{leilao.Item.Titulo}</strong> não recebeu lances durante o período do leilão.</p>" +
                                                              "<p>Considere repostar o item ou ajustar o preço inicial para atrair mais interessados.</p>" +
                                                              "<p>Você pode gerir os seus leilões na <a href='https://projeto-grow-2025.azurewebsites.net/' target='_blank' style='color: blue; text-decoration: underline;'>GROW</a> indo ao seu perfil e aos seus leilões para recolocar o seu leilão.</p>";

                        await _emailSender.SendEmailAsync(leiloeiro.Email, subjectLeiloeiroSemLicitacoes, messageLeiloeiroSemLicitacoes);
                    }
                }
                leilao.EstadoLeilao = EstadoLeilao.Encerrado;
                _context.Update(leilao);
            }

            await _context.SaveChangesAsync();
            return View(leiloes);
        }

        // ========================================================
        // Autor: Isidoro Ornelas
        // Descrição: Método para visualizar detalhes de um leilão
        // ========================================================
        // GET: Leilaos/Details/5
        public async Task<IActionResult> Details(int id)
        {
            var userEmail = HttpContext.Session.GetString("UserEmail");
            var user = await _context.Utilizador.FirstOrDefaultAsync(u => u.Email == userEmail);
            ViewData["UserPoints"] = user?.Pontos;

            if (user != null)
            {
                ViewData["UserId"] = user.UtilizadorId;
            }

            var leilao = await _context.Leiloes
                .Include(l => l.Item)
                .FirstOrDefaultAsync(l => l.LeilaoId == id);

            if (leilao == null)
            {
                return NotFound();
            }

            if (leilao.EstadoLeilao == EstadoLeilao.Encerrado)
            {
                leilao.Licitacoes = await _context.Licitacoes
                    .Where(l => l.LeilaoId == id)
                    .OrderByDescending(l => l.ValorLicitacao)
                    .ToListAsync();
            }

            return View(leilao);
        }

        // ========================================================
        // Autor: Samuel Alves
        // Descrição: Método para criar um novo leilão
        // ========================================================
        // GET: Leilaos/Create
        public async Task<IActionResult> Create()
        {
            ViewData["Categorias"] = Enum.GetValues(typeof(Categoria))
                                 .Cast<Categoria>()
                                 .Select(c => new SelectListItem
                                 {
                                     Value = c.ToString(),
                                     Text = c.ToString()
                                 }).ToList();

            var userEmail = HttpContext.Session.GetString("UserEmail");

            var user = await _context.Utilizador.FirstOrDefaultAsync(u => u.Email == userEmail);

            ViewData["UserPoints"] = user?.Pontos;

            if (user == null)
            {
                return RedirectToAction("Login", "Utilizadors");
            }

            return View();
        }


        // ========================================================
        // Autor: Samuel Alves
        // Método para criar um leilão (POST)
        // ========================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Leilao leilao)
        {
            // Obtém o e-mail do utilizador logado a partir da sessão
            var userEmail = HttpContext.Session.GetString("UserEmail");
            var user = await _context.Utilizador.FirstOrDefaultAsync(u => u.Email == userEmail);

            // Verifica se o utilizador está autenticado
            if (user == null)
            {
                return RedirectToAction("Login", "Utilizadors"); // Se não estiver autenticado, redireciona para login
            }

            // Verifica se a foto do item foi fornecida
            if (leilao.Item.fotoo == null || leilao.Item.fotoo.Length == 0)
            {
                ModelState.AddModelError("Item.fotoo", "A foto é obrigatória.");
                return View(leilao); // Se não houver foto, retorna a view com o erro
            }

            leilao.UtilizadorId = user.UtilizadorId; // Atribui o ID do utilizador ao leilão

            // Processa a foto do item, se fornecida
            if (leilao.Item.fotoo != null && leilao.Item.fotoo.Length > 0)
            {
                // Define o diretório onde a foto será salva
                string folder = "leilao/fotos/";
                string fileName = Guid.NewGuid().ToString() + Path.GetExtension(leilao.Item.fotoo.FileName); // Gera um nome único para a foto
                string serverFolder = Path.Combine(_env.WebRootPath, folder);

                // Verifica se o diretório de destino existe, se não, cria-o
                if (!Directory.Exists(serverFolder))
                {
                    Directory.CreateDirectory(serverFolder);
                }

                // Define o caminho completo do arquivo
                string filePath = Path.Combine(serverFolder, fileName);

                // Verifica se o tamanho do arquivo excede o limite de 5MB
                if (leilao.Item.fotoo.Length > 5 * 1024 * 1024)
                {
                    ModelState.AddModelError("Item.fotoo", "Size is too big");
                    return View(leilao); // Se a foto for muito grande, retorna à view com o erro
                }

                // Verifica se a extensão do arquivo é permitida
                var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif" };
                if (!allowedExtensions.Contains(Path.GetExtension(leilao.Item.fotoo.FileName).ToLower()))
                {
                    ModelState.AddModelError("Item.fotoo", "Only extensions (.jpg, .jpeg, .png, .gif) allowed.");
                    return View(leilao); // Se a extensão não for permitida, retorna à view com o erro
                }

                // Salva a foto no servidor
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await leilao.Item.fotoo.CopyToAsync(stream); // Copia a foto para o servidor
                }

                // Atribui a URL da foto ao item
                leilao.Item.FotoUrl = "/" + folder + fileName;
            }

            _context.Add(leilao); // Adiciona o leilão ao contexto do banco de dados
            await _context.SaveChangesAsync(); // Salva as alterações no banco de dados

            TempData["Success"] = "Auction created"; // Mensagem de sucesso

            return RedirectToAction(nameof(Index)); // Redireciona para a página de índice (listar leilões)
        }


        // ========================================================
        // Autor: Rodrigo Baião
        // Método para editar um leilão (GET)
        // ========================================================
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound(); // Se o ID não for fornecido, retorna erro
            }

            var leilao = await _context.Leiloes.FindAsync(id); // Encontra o leilão pelo ID
            if (leilao == null)
            {
                return NotFound(); // Se o leilão não for encontrado, retorna erro
            }
            ViewData["ItemId"] = new SelectList(_context.Itens, "ItemId", "ItemId", leilao.ItemId); // Preenche a lista de itens disponíveis
            return View(leilao); // Retorna a view com o leilão encontrado
        }

        // ========================================================
        // Autor: Rodrigo Baião
        // Método para editar um leilão (POST)
        // ========================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("LeilaoId,ItemId,DataInicio,DataFim,ValorIncrementoMinimo,Vencedor")] Leilao leilao)
        {
            if (id != leilao.LeilaoId)
            {
                return NotFound(); // Se o ID não corresponder ao leilão, retorna erro
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(leilao); // Atualiza o leilão no banco de dados
                    await _context.SaveChangesAsync(); // Salva as alterações
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!LeilaoExists(leilao.LeilaoId))
                    {
                        return NotFound(); // Se o leilão não existir, retorna erro
                    }
                    else
                    {
                        throw; // Lança erro em caso de falha na atualização
                    }
                }
                return RedirectToAction(nameof(Index)); // Redireciona para a página de índice
            }
            ViewData["ItemId"] = new SelectList(_context.Itens, "ItemId", "ItemId", leilao.ItemId); // Preenche a lista de itens
            return View(leilao); // Retorna à view com o leilão atualizado
        }


        // ========================================================
        // Autor: Rodrigo Baião
        // Método para excluir um leilão (GET)
        // ========================================================
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound(); // Se o ID não for fornecido, retorna erro
            }

            var leilao = await _context.Leiloes
                .Include(l => l.Item) // Inclui informações do item
                .FirstOrDefaultAsync(m => m.LeilaoId == id); // Encontra o leilão pelo ID
            if (leilao == null)
            {
                return NotFound(); // Se o leilão não for encontrado, retorna erro
            }

            return View(leilao); // Retorna a view com o leilão encontrado
        }

        // ========================================================
        // Autor: Rodrigo Baião
        // Método para excluir um leilão (POST)
        // ========================================================
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var leilao = await _context.Leiloes
                .Include(l => l.Item)
                .Include(l => l.Licitacoes) // Inclui as licitações relacionadas
                .FirstOrDefaultAsync(l => l.LeilaoId == id); // Encontra o leilão pelo ID

            if (leilao == null)
            {
                TempData["Error"] = "Leilão não encontrado."; // Mensagem de erro
                return RedirectToAction("MyAuctions"); // Redireciona para a página de leilões do utilizador
            }

            // Verifica se existem licitações ativas
            bool temLicitacoes = leilao.Licitacoes != null && leilao.Licitacoes.Any();

            // Se houver licitações ativas e o leilão ainda não foi pago, não pode ser excluído
            if (temLicitacoes && !leilao.Pago)
            {
                TempData["Error"] = "Não pode eliminar um leilão com licitações ativas que ainda não foi pago.";
                return RedirectToAction("MyAuctions"); // Redireciona para a página de leilões do utilizador
            }

            // Se o leilão não tiver licitações ou já foi pago, procede à exclusão
            if (leilao.Item != null)
            {
                _context.Itens.Remove(leilao.Item); // Remove o item associado ao leilão
            }

            _context.Leiloes.Remove(leilao); // Remove o leilão
            await _context.SaveChangesAsync(); // Salva as alterações no banco de dados

            TempData["Success"] = "Leilão eliminado com sucesso."; // Mensagem de sucesso
            return RedirectToAction("MyAuctions"); // Redireciona para a página de leilões do utilizador
        }

        // ========================================================
        // Autor: Samuel Alves
        // Verifica se o leilão existe no banco de dados
        // ========================================================
        private bool LeilaoExists(int id)
        {
            return _context.Leiloes.Any(e => e.LeilaoId == id); // Verifica se o leilão existe
        }

        // ========================================================
        // Autor: Samuel Alves
        // Método para fazer uma licitação em um leilão (POST)
        // ========================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> FazerLicitacao(int leilaoId, double valorLicitacao)
        {
            var userEmail = HttpContext.Session.GetString("UserEmail"); // Obtém o e-mail do utilizador logado
            var user = await _context.Utilizador.FirstOrDefaultAsync(u => u.Email == userEmail);

            if (user == null)
            {
                return RedirectToAction("Login", "Utilizadors"); // Se o utilizador não estiver autenticado, redireciona para login
            }

            var leilao = await _context.Leiloes
                .Include(l => l.Licitacoes) // Inclui as licitações do leilão
                .Include(l => l.Item) // Inclui o item do leilão
                .FirstOrDefaultAsync(l => l.LeilaoId == leilaoId); // Encontra o leilão pelo ID

            if (leilao == null)
            {
                return NotFound(); // Se o leilão não for encontrado, retorna erro
            }

            // Verifica se o leilão está encerrado ou se a data de fim já passou
            if (leilao.EstadoLeilao == EstadoLeilao.Encerrado || DateTime.Now > leilao.DataFim)
            {
                TempData["Error"] = "Este leilão já terminou e não aceita mais lances.";
                return RedirectToAction("Index", "Leilaos"); // Redireciona para a página de leilões
            }

            // Calcula o lance mínimo necessário
            double lanceMinimo = leilao.Licitacoes.Any()
                ? leilao.Licitacoes.Max(l => l.ValorLicitacao)
                : leilao.Item.PrecoInicial;

            double valorNecessario = lanceMinimo + leilao.ValorIncrementoMinimo; // Incremento mínimo

            if (valorLicitacao < valorNecessario)
            {
                TempData["Error"] = $"O lance deve ser igual ou superior a {valorNecessario:C2}.";
                return RedirectToAction("Index", "Leilaos"); // Redireciona com erro se o valor for muito baixo
            }

            var licitacao = new Licitacao
            {
                LeilaoId = leilaoId,
                UtilizadorId = user.UtilizadorId,
                ValorLicitacao = valorLicitacao,
                DataLicitacao = DateTime.Now // Define a data e hora da licitação
            };

            _context.Licitacoes.Add(licitacao); // Adiciona a licitação ao banco de dados
            user.Pontos += 1; // Adiciona pontos ao utilizador
            _context.Update(user); // Atualiza os pontos do utilizador no banco de dados

            // Atualiza o valor atual do lance
            leilao.ValorAtualLance = leilao.Licitacoes.Count > 0
                ? leilao.Licitacoes.Max(x => x.ValorLicitacao)
                : leilao.Item.PrecoInicial;

            _context.Update(leilao); // Atualiza o leilão com o novo valor do lance

            await _context.SaveChangesAsync(); // Salva as alterações no banco de dados

            return RedirectToAction("Index", "Leilaos"); // Redireciona para a página de leilões
        }

        // ========================================================
        // Autor: Samuel Alves
        // Fazer licitacao detalhes
        // ========================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> FazerLicitacaoDetails(int leilaoId, double valorLicitacao)
        {
            // Obtém o e-mail do usuário atual da sessão
            var userEmail = HttpContext.Session.GetString("UserEmail");
            var user = await _context.Utilizador.FirstOrDefaultAsync(u => u.Email == userEmail);

            // Se o usuário não estiver autenticado, redireciona para a página de login
            if (user == null)
            {
                return RedirectToAction("Login", "Utilizadors");
            }

            // Obtém os detalhes do leilão
            var leilao = await _context.Leiloes
                .Include(l => l.Licitacoes)
                .Include(l => l.Item)
                .FirstOrDefaultAsync(l => l.LeilaoId == leilaoId);

            // Se o leilão não for encontrado, retorna erro 404
            if (leilao == null)
            {
                return NotFound();
            }

            // Verifica se o usuário está tentando licitar no seu próprio leilão
            if (leilao.UtilizadorId == user.UtilizadorId)
            {
                TempData["BidError"] = "You cannot place bids on your own auction.";
                return RedirectToAction("Details", new { id = leilaoId });
            }

            // Verifica se o leilão está encerrado ou a data final já passou
            if (leilao.EstadoLeilao == EstadoLeilao.Encerrado || DateTime.Now > leilao.DataFim)
            {
                TempData["BidError"] = "This auction has already ended and no longer accepts bids.";
                return RedirectToAction("Details", new { id = leilaoId });
            }

            // Calcula o valor mínimo necessário para a nova licitação
            double lanceMinimo = leilao.Licitacoes.Any()
                ? leilao.Licitacoes.Max(l => l.ValorLicitacao)
                : leilao.Item.PrecoInicial;

            double valorNecessario = lanceMinimo + leilao.ValorIncrementoMinimo;

            // Verifica se o valor da licitação é inferior ao valor necessário
            if (valorLicitacao < valorNecessario)
            {
                TempData["BidError"] = $"The bid must be equal or higher than {valorNecessario:C2}.";
                return RedirectToAction("Details", new { id = leilaoId });
            }

            // Cria uma nova licitação
            var licitacao = new Licitacao
            {
                LeilaoId = leilaoId,
                UtilizadorId = user.UtilizadorId,
                ValorLicitacao = valorLicitacao,
                DataLicitacao = DateTime.Now
            };

            TempData["BidSuccess"] = "Bid placed successfully!";

            // Adiciona a nova licitação ao banco de dados e atualiza os pontos do usuário
            _context.Licitacoes.Add(licitacao);
            user.Pontos += 1;
            _context.Update(user);
            await _context.SaveChangesAsync();

            // Atualiza o valor atual do leilão com a maior licitação
            leilao = await _context.Leiloes
                .Include(l => l.Licitacoes)
                .Include(l => l.Item)
                .FirstOrDefaultAsync(l => l.LeilaoId == leilaoId);

            if (leilao != null)
            {
                leilao.ValorAtualLance = leilao.Licitacoes.Max(l => l.ValorLicitacao);
                _context.Update(leilao);
                await _context.SaveChangesAsync();
            }

            TempData["Success"] = "Successful bid!";
            return RedirectToAction("Details", new { id = leilaoId });
        }

        // ========================================================
        // Autor: Samuel Alves
        // Atualiza estado dos leilões
        // ========================================================
        public async Task<IActionResult> AtualizarEstadoLeiloes()
        {
            // Obtém todos os leilões
            var leiloes = await _context.Leiloes.ToListAsync();

            foreach (var leilao in leiloes)
            {
                // Verifica se o leilão terminou
                if (DateTime.Now > leilao.DataFim)
                {
                    if (leilao.EstadoLeilao == EstadoLeilao.Disponivel)
                    {
                        leilao.EstadoLeilao = EstadoLeilao.Encerrado;

                        // Obtém o vencedor do leilão (a maior licitação)
                        var vencedor = leilao.Licitacoes.OrderByDescending(l => l.ValorLicitacao).FirstOrDefault();
                        if (vencedor != null)
                        {
                            // Lógica para atualizar o vencedor, se necessário
                            // (exemplo: leilao.VencedorId = vencedor.UtilizadorId)
                        }
                    }
                }

                _context.Update(leilao);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction("Index");
        }

        // ========================================================
        // Autor: Samuel Alves
        // Éxibe os meus leilões
        // ========================================================
        public async Task<IActionResult> MyAuctions(int page = 1)
        {
            int pageSize = 3;
            var userEmail = HttpContext.Session.GetString("UserEmail");
            var user = await _context.Utilizador.FirstOrDefaultAsync(u => u.Email == userEmail);

            ViewData["UserPoints"] = user?.Pontos;

            // Verifica se o usuário está autenticado
            if (user == null)
            {
                return RedirectToAction("Login", "Utilizadors");
            }

            // Obtém o total de leilões do usuário e paginando os resultados
            var totalLeiloes = await _context.Leiloes.CountAsync(l => l.UtilizadorId == user.UtilizadorId);
            var leiloes = await _context.Leiloes
                .Where(l => l.UtilizadorId == user.UtilizadorId)
                .Include(l => l.Item)
                .OrderBy(l => l.DataFim)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = (int)Math.Ceiling((double)totalLeiloes / pageSize);

            return View(leiloes);
        }

        // ========================================================
        // Autor: Isidoro Ornelas
        // Exibe as minhas licitações
        // ========================================================
        public async Task<IActionResult> MyBids()
        {
            var userEmail = HttpContext.Session.GetString("UserEmail");
            var user = await _context.Utilizador.FirstOrDefaultAsync(u => u.Email == userEmail);

            ViewData["UserPoints"] = user?.Pontos;

            // Verifica se o usuário está autenticado
            if (user == null)
            {
                return RedirectToAction("Login", "Utilizadors");
            }

            // Obtém as licitações feitas pelo usuário, ordenadas pela data
            var meusLances = await _context.Licitacoes
                .Where(l => l.UtilizadorId == user.UtilizadorId)
                .Include(l => l.Leilao)
                .ThenInclude(l => l.Item)
                .OrderByDescending(l => l.DataLicitacao)
                .ToListAsync();

            return View(meusLances);
        }

        // ========================================================
        // Autor: Isidoro Ornelas
        // Recoloca o leilão
        // ========================================================
        [HttpGet]
        [Route("Leiloes/RecolocarLeilao/{id}")]
        public async Task<IActionResult> RecolocarLeilao(int id)
        {
            var userEmail = HttpContext.Session.GetString("UserEmail");
            var user = await _context.Utilizador.FirstOrDefaultAsync(u => u.Email == userEmail);

            // Verifica se o usuário está autenticado
            if (user == null)
            {
                return RedirectToAction("Login", "Utilizadors");
            }

            // Obtém os detalhes do leilão para recolocar
            var leilao = await _context.Leiloes
                .Include(l => l.Item)
                .FirstOrDefaultAsync(l => l.LeilaoId == id);

            if (leilao == null)
            {
                return NotFound();
            }

            ViewData["Categorias"] = Enum.GetValues(typeof(Categoria))
                .Cast<Categoria>()
                .Select(c => new SelectListItem
                {
                    Value = c.ToString(),
                    Text = c.ToString()
                }).ToList();

            ViewData["UserPoints"] = user.Pontos;
            CarregarCategorias();

            return View(leilao);
        }

        private void CarregarCategorias()
        {
            ViewBag.Categorias = new SelectList(Enum.GetValues(typeof(Categoria)));
        }

        // ========================================================
        // Autor: Isidoro Ornelas
        // Recoloca o leilão
        // ========================================================
        [HttpPost]
        [Route("Leiloes/RecolocarLeilao/{id}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RecolocarLeilao(int id, IFormFile novaFoto, [Bind("LeilaoId, DataFim, ValorIncrementoMinimo, Item")] Leilao leilaoAtualizado)
        {
            var userEmail = HttpContext.Session.GetString("UserEmail");
            var user = await _context.Utilizador.FirstOrDefaultAsync(u => u.Email == userEmail);

            // Verifica se o usuário está autenticado
            if (user == null)
            {
                return RedirectToAction("Login", "Utilizadors");
            }

            var leilao = await _context.Leiloes
                .Include(l => l.Item)
                .FirstOrDefaultAsync(l => l.LeilaoId == id);

            if (leilao == null)
            {
                return NotFound();
            }

            // Verifica se a data de término do leilão é válida
            if (leilaoAtualizado.DataFim <= DateTime.Now)
            {
                ModelState.AddModelError("DataFim", "Data Invalida.");
                return View(leilao);
            }

            // Atualiza os dados do leilão
            leilao.DataFim = leilaoAtualizado.DataFim;
            leilao.ValorIncrementoMinimo = leilaoAtualizado.ValorIncrementoMinimo;
            leilao.EstadoLeilao = EstadoLeilao.Disponivel;

            leilao.Item.Titulo = leilaoAtualizado.Item.Titulo;
            leilao.Item.Descricao = leilaoAtualizado.Item.Descricao;
            leilao.Item.PrecoInicial = leilaoAtualizado.Item.PrecoInicial;
            leilao.Item.Categoria = leilaoAtualizado.Item.Categoria;

            // Lógica para processar a foto enviada
            if (novaFoto != null && novaFoto.Length > 0)
            {
                string folder = "leilao/fotos/";
                string fileName = Guid.NewGuid().ToString() + Path.GetExtension(novaFoto.FileName);
                string serverFolder = Path.Combine(_env.WebRootPath, folder);

                if (!Directory.Exists(serverFolder))
                {
                    Directory.CreateDirectory(serverFolder);
                }

                string filePath = Path.Combine(serverFolder, fileName);

                // Verifica se o arquivo tem um tamanho maior que 5MB
                if (novaFoto.Length > 5 * 1024 * 1024)
                {
                    ModelState.AddModelError("Item.fotoo", "O tamanho do ficheiro é demasiado grande.");
                    return View(leilao);
                }

                // Verifica se a extensão do arquivo é permitida
                var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif" };
                if (!allowedExtensions.Contains(Path.GetExtension(novaFoto.FileName).ToLower()))
                {
                    ModelState.AddModelError("Item.fotoo", "Apenas ficheiros .jpg, .jpeg, .png, .gif são permitidos.");
                    return View(leilao);
                }

                // Salva a nova foto
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await novaFoto.CopyToAsync(stream);
                }

                // Atualiza a URL da foto
                leilao.Item.FotoUrl = "/" + folder + fileName;
            }

            CarregarCategorias();

            // Salva as alterações no banco de dados
            _context.Update(leilao);
            await _context.SaveChangesAsync();

            return RedirectToAction("MyAuctions");
        }

        // ========================================================
        // Autor: Miguel Pinto
        // Exibe o top 3 leilões
        // ========================================================
        [HttpGet]
        [Route("[controller]/TopAuctions")]
        public async Task<IActionResult> TopAuctions()
        {
            var userEmail = HttpContext.Session.GetString("UserEmail");
            var user = await _context.Utilizador.FirstOrDefaultAsync(u => u.Email == userEmail);

            ViewData["UserPoints"] = user?.Pontos;

            // Obtém os 3 leilões mais populares (com mais licitações)
            var topLeiloes = await _context.Leiloes
                .Include(l => l.Item)
                .Include(l => l.Licitacoes)
                .Where(l => l.EstadoLeilao == EstadoLeilao.Disponivel)
                .OrderByDescending(l => l.Licitacoes.Count)
                .Take(3)
                .ToListAsync();

            return View(topLeiloes);
        }

    }
}


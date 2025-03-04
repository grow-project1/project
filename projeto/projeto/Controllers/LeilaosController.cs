using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using projeto.Data;
using projeto.Models;

namespace projeto.Controllers
{
    public class LeilaosController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _env;

        public LeilaosController(ApplicationDbContext context, IWebHostEnvironment env)
        {
            _context = context;
            _env = env;
        }

        // GET: Leilaos
        public async Task<IActionResult> Index()
        {
            var userEmail = HttpContext.Session.GetString("UserEmail");

            var user = await _context.Utilizador.FirstOrDefaultAsync(u => u.Email == userEmail);

            ViewData["UserPoints"] = user?.Pontos;
            ViewData["Logged"] = user != null;
            ViewData["UserId"] = user?.UtilizadorId;

            var categorias = Enum.GetValues(typeof(Categoria)).Cast<Categoria>().ToList();
            ViewData["Categorias"] = categorias;

            // Obter todos os leilões com os itens associados
            var leiloes = await _context.Leiloes
                .Include(l => l.Item)  // Inclui o item associado ao leilão
                .ToListAsync();

            // Para cada leilão, calculamos o maior lance
            foreach (var leilao in leiloes)
            {
                // Obtém o maior valor de licitação para o leilão
                var maiorLance = await _context.Licitacoes
                    .Where(l => l.LeilaoId == leilao.LeilaoId)
                    .MaxAsync(l => (double?)l.ValorLicitacao);

                // Se houver licitações, o maior lance será o maior valor de licitação,
                // caso contrário, usamos o preço inicial do item
                leilao.ValorAtualLance = maiorLance ?? leilao.Item.PrecoInicial;
            }

            foreach (var leilao in leiloes)
            {
                if (DateTime.Now > leilao.DataFim && leilao.EstadoLeilao != EstadoLeilao.Encerrado)
                {
                    leilao.EstadoLeilao = EstadoLeilao.Encerrado;

                    var licitacaoVencedora = await _context.Licitacoes
                        .Where(l => l.LeilaoId == leilao.LeilaoId)  // Filtra pela ID do leilão
                        .OrderByDescending(l => l.ValorLicitacao)  // Ordena pelo valor da licitação
                        .FirstOrDefaultAsync();  // Pega a licitação com o maior valor

                    if (licitacaoVencedora != null)
                    {
                        var vencedor = await _context.Utilizador.FindAsync(licitacaoVencedora.UtilizadorId);

                        if (vencedor != null)
                        {
                            // Verifica se o item do leilão é sustentável
                            bool isSustentavel = leilao.Item.Sustentavel;

                            // Dá os pontos ao vencedor
                            vencedor.Pontos += isSustentavel ? 50 : 20;
                            _context.Update(vencedor);

                            // Define o vencedor no leilão
                            leilao.Vencedor = vencedor.Nome;
                        }
                    }


                    _context.Update(leilao);
                }
            }
            await _context.SaveChangesAsync();
            return View(leiloes);
        }


        // GET: Leilaos/Details/5
        public async Task<IActionResult> Details(int id)
        {
            var leilao = await _context.Leiloes
                .Include(l => l.Item)
                .FirstOrDefaultAsync(l => l.LeilaoId == id);

            if (leilao == null)
            {
                return NotFound();
            }

            // Verifica se o leilão está encerrado
            if (leilao.EstadoLeilao == EstadoLeilao.Encerrado)
            {
                // Busca as licitações para o leilão se ele estiver encerrado
                leilao.Licitacoes = await _context.Licitacoes
                    .Where(l => l.LeilaoId == id)
                    .OrderByDescending(l => l.ValorLicitacao)  // Ordena por valor de licitação
                    .ToListAsync();
            }

            return View(leilao);  // Passa o leilão com as licitações (se encerrado) para a view
        }




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

            if (user == null) {
                return RedirectToAction("Login", "Utilizadors");  // Altere "Account" para o nome do controlador de login

            }

            return View();
        }

        // POST: Leilaos/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Leilao leilao)
        {
            var userEmail = HttpContext.Session.GetString("UserEmail");
            var user = await _context.Utilizador.FirstOrDefaultAsync(u => u.Email == userEmail);

            // Verifica se o usuário está logado
            if (user == null)
            {
                return RedirectToAction("Login", "Utilizadors");  // Redireciona para a página de login
            }

            // Associa o leilão ao usuário logado
            leilao.UtilizadorId = user.UtilizadorId;

            // Verifica se o arquivo FotoFile foi enviado
            if (leilao.Item.fotoo != null && leilao.Item.fotoo.Length > 0)
            {
                // Define o diretório para salvar a imagem
                string folder = "leilao/fotos/";
                string fileName = Guid.NewGuid().ToString() + Path.GetExtension(leilao.Item.fotoo.FileName);  // Gera um nome único com a extensão do arquivo
                string serverFolder = Path.Combine(_env.WebRootPath, folder);

                // Cria o diretório de uploads se não existir
                if (!Directory.Exists(serverFolder))
                {
                    Directory.CreateDirectory(serverFolder);
                }

                // Caminho final do arquivo no servidor
                string filePath = Path.Combine(serverFolder, fileName);

                // Verifica o tamanho máximo do arquivo (exemplo: 5 MB)
                if (leilao.Item.fotoo.Length > 5 * 1024 * 1024)  // 5MB
                {
                    ModelState.AddModelError("Item.fotoo", "O arquivo de imagem é muito grande. O tamanho máximo permitido é 5MB.");
                    return View(leilao);  // Retorna à página de criação com o erro
                }

                // Verifica o tipo de arquivo (apenas imagens)
                var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif" };
                if (!allowedExtensions.Contains(Path.GetExtension(leilao.Item.fotoo.FileName).ToLower()))
                {
                    ModelState.AddModelError("Item.fotoo", "Apenas arquivos de imagem (.jpg, .jpeg, .png, .gif) são permitidos.");
                    return View(leilao);  // Retorna à página de criação com o erro
                }

                // Salva o arquivo no servidor
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await leilao.Item.fotoo.CopyToAsync(stream);
                }

                // Atribui o caminho da imagem ao campo FotoUrl
                leilao.Item.FotoUrl = "/" + folder + fileName;
            }

            // Adiciona o leilão no contexto
            _context.Add(leilao);
            await _context.SaveChangesAsync();  // Salva no banco de dados

            return RedirectToAction(nameof(Index));  // Redireciona para a lista de leilões
        }





        // GET: Leilaos/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var leilao = await _context.Leiloes.FindAsync(id);
            if (leilao == null)
            {
                return NotFound();
            }
            ViewData["ItemId"] = new SelectList(_context.Itens, "ItemId", "ItemId", leilao.ItemId);
            return View(leilao);
        }

        // POST: Leilaos/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("LeilaoId,ItemId,DataInicio,DataFim,ValorIncrementoMinimo,Vencedor")] Leilao leilao)
        {
            if (id != leilao.LeilaoId)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(leilao);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!LeilaoExists(leilao.LeilaoId))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }
            ViewData["ItemId"] = new SelectList(_context.Itens, "ItemId", "ItemId", leilao.ItemId);
            return View(leilao);
        }

        // GET: Leilaos/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var leilao = await _context.Leiloes
                .Include(l => l.Item)
                .FirstOrDefaultAsync(m => m.LeilaoId == id);
            if (leilao == null)
            {
                return NotFound();
            }

            return View(leilao);
        }

        // POST: Leilaos/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var leilao = await _context.Leiloes
                .Include(l => l.Item) // Inclui o item associado
                .FirstOrDefaultAsync(l => l.LeilaoId == id);

            if (leilao != null)
            {
                if (leilao.Item != null)
                {
                    _context.Itens.Remove(leilao.Item); // Remove o item antes do leilão
                }
                _context.Leiloes.Remove(leilao);
                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Index));
        }


        private bool LeilaoExists(int id)
        {
            return _context.Leiloes.Any(e => e.LeilaoId == id);
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> FazerLicitacao(int leilaoId, double valorLicitacao)
        {
            var userEmail = HttpContext.Session.GetString("UserEmail");
            var user = await _context.Utilizador.FirstOrDefaultAsync(u => u.Email == userEmail);

            if (user == null)
            {
                return RedirectToAction("Login", "Utilizadors");
            }

            var leilao = await _context.Leiloes
                .Include(l => l.Licitacoes)
                .Include(l => l.Item) // Para obter o preço inicial
                .FirstOrDefaultAsync(l => l.LeilaoId == leilaoId);

            if (leilao == null)
            {
                return NotFound();
            }

            // Verificar se o leilão já foi encerrado
            if (leilao.EstadoLeilao == EstadoLeilao.Encerrado || DateTime.Now > leilao.DataFim)
            {
                TempData["Error"] = "Este leilão já foi finalizado e não aceita mais licitações.";
                return RedirectToAction("Index", "Leilaos"); // Volta para a Home
            }

            // Determinar o valor mínimo para a nova licitação
            double lanceMinimo = leilao.Licitacoes.Any()
                ? leilao.Licitacoes.Max(l => l.ValorLicitacao)
                : leilao.Item.PrecoInicial;

            double valorNecessario = lanceMinimo + leilao.ValorIncrementoMinimo;

            // Se a licitação for menor que o valor necessário, exibe erro
            if (valorLicitacao < valorNecessario)
            {
                TempData["Error"] = $"O lance deve ser maior ou igual a {valorNecessario:C2}.";
                return RedirectToAction("Index", "Leilaos"); // Volta para a Home Page
            }

            // Criar e salvar a nova licitação
            var licitacao = new Licitacao
            {
                LeilaoId = leilaoId,
                UtilizadorId = user.UtilizadorId,
                ValorLicitacao = valorLicitacao,
                DataLicitacao = DateTime.Now
            };

            _context.Licitacoes.Add(licitacao);
            user.Pontos += 1;
            _context.Update(user);

            await _context.SaveChangesAsync();

            TempData["Success"] = "Lance realizado com sucesso!";
            return RedirectToAction("Index", "Leilaos"); // Volta para a Home Page
        }



        public async Task<IActionResult> AtualizarEstadoLeiloes()
        {
            // Buscar todos os leilões
            var leiloes = await _context.Leiloes.ToListAsync();

            foreach (var leilao in leiloes)
            {
                if (DateTime.Now > leilao.DataFim)
                {
                    // Se o leilão terminou, atualiza o estado para "Encerrado"
                    if (leilao.EstadoLeilao == EstadoLeilao.Disponivel)
                    {
                        leilao.EstadoLeilao = EstadoLeilao.Encerrado;

                        // Se o leilão tiver licitações, podemos determinar o vencedor
                        var vencedor = leilao.Licitacoes.OrderByDescending(l => l.ValorLicitacao).FirstOrDefault();
                        if (vencedor != null)
                        {
                            // Aqui você pode, por exemplo, atualizar o campo VencedorId ou fazer outra lógica
                            // Para o momento, vamos manter a informação apenas no leilão.
                        }
                    }
                }

                // Salvar a atualização do leilão
                _context.Update(leilao);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction("Index"); // Redireciona para a página inicial após a atualização
        }

    }
}

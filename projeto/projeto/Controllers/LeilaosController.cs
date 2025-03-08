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

            var leiloes = await _context.Leiloes
                .Include(l => l.Item)  
                .ToListAsync();

            foreach (var leilao in leiloes)
            {
                var maiorLance = await _context.Licitacoes
                    .Where(l => l.LeilaoId == leilao.LeilaoId)
                    .MaxAsync(l => (double?)l.ValorLicitacao);

                leilao.ValorAtualLance = maiorLance ?? leilao.Item.PrecoInicial;
            }

            foreach (var leilao in leiloes)
            {
                if (DateTime.Now > leilao.DataFim && leilao.EstadoLeilao != EstadoLeilao.Encerrado)
                {
                    leilao.EstadoLeilao = EstadoLeilao.Encerrado;

                    var licitacaoVencedora = await _context.Licitacoes
                        .Where(l => l.LeilaoId == leilao.LeilaoId) 
                        .OrderByDescending(l => l.ValorLicitacao) 
                        .FirstOrDefaultAsync();

                    if (licitacaoVencedora != null)
                    {
                        var vencedor = await _context.Utilizador.FindAsync(licitacaoVencedora.UtilizadorId);

                        if (vencedor != null)
                        {
                            bool isSustentavel = leilao.Item.Sustentavel;

                            vencedor.Pontos += isSustentavel ? 50 : 20;
                            _context.Update(vencedor);

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

            if (leilao.EstadoLeilao == EstadoLeilao.Encerrado)
            {
                leilao.Licitacoes = await _context.Licitacoes
                    .Where(l => l.LeilaoId == id)
                    .OrderByDescending(l => l.ValorLicitacao) 
                    .ToListAsync();
            }

            return View(leilao);  
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
                return RedirectToAction("Login", "Utilizadors"); 

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

            if (user == null)
            {
                return RedirectToAction("Login", "Utilizadors"); 
            }

           

            leilao.UtilizadorId = user.UtilizadorId;

            if (leilao.Item.fotoo != null && leilao.Item.fotoo.Length > 0)
            {
                string folder = "leilao/fotos/";
                string fileName = Guid.NewGuid().ToString() + Path.GetExtension(leilao.Item.fotoo.FileName);  
                string serverFolder = Path.Combine(_env.WebRootPath, folder);

                if (!Directory.Exists(serverFolder))
                {
                    Directory.CreateDirectory(serverFolder);
                }

                string filePath = Path.Combine(serverFolder, fileName);

                if (leilao.Item.fotoo.Length > 5 * 1024 * 1024)  
                {
                    ModelState.AddModelError("Item.fotoo", "Size is to big");
                    return View(leilao); 
                }

                var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif" };
                if (!allowedExtensions.Contains(Path.GetExtension(leilao.Item.fotoo.FileName).ToLower()))
                {
                    ModelState.AddModelError("Item.fotoo", "Only extensions (.jpg, .jpeg, .png, .gif) allowed.");
                    return View(leilao); 
                }

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await leilao.Item.fotoo.CopyToAsync(stream);
                }

                leilao.Item.FotoUrl = "/" + folder + fileName;
            }

            _context.Add(leilao);
            await _context.SaveChangesAsync(); 

            return RedirectToAction(nameof(Index));  
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
                .Include(l => l.Item) 
                .FirstOrDefaultAsync(l => l.LeilaoId == id);

            if (leilao != null)
            {
                if (leilao.Item != null)
                {
                    _context.Itens.Remove(leilao.Item);
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
                .Include(l => l.Item) 
                .FirstOrDefaultAsync(l => l.LeilaoId == leilaoId);

            if (leilao == null)
            {
                return NotFound();
            }

            if (leilao.EstadoLeilao == EstadoLeilao.Encerrado || DateTime.Now > leilao.DataFim)
            {
                TempData["Error"] = "This auction has already ended and no longer accepts bids..";
                return RedirectToAction("Index", "Leilaos"); 
            }

            double lanceMinimo = leilao.Licitacoes.Any()
                ? leilao.Licitacoes.Max(l => l.ValorLicitacao)
                : leilao.Item.PrecoInicial;

            double valorNecessario = lanceMinimo + leilao.ValorIncrementoMinimo;

            if (valorLicitacao < valorNecessario)
            {
                TempData["Error"] = $"The bid must be higher than {valorNecessario:C2}.";
                return RedirectToAction("Index", "Leilaos"); 
            }

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

            TempData["Success"] = "Sucessfull bid!";
            return RedirectToAction("Index", "Leilaos"); 
        }

        public async Task<IActionResult> AtualizarEstadoLeiloes()
        {
            var leiloes = await _context.Leiloes.ToListAsync();

            foreach (var leilao in leiloes)
            {
                if (DateTime.Now > leilao.DataFim)
                {
                    if (leilao.EstadoLeilao == EstadoLeilao.Disponivel)
                    {
                        leilao.EstadoLeilao = EstadoLeilao.Encerrado;

                        var vencedor = leilao.Licitacoes.OrderByDescending(l => l.ValorLicitacao).FirstOrDefault();
                        if (vencedor != null)
                        {
                            // Aqui você pode, por exemplo, atualizar o campo VencedorId ou fazer outra lógica
                            // Para o momento, vamos manter a informação apenas no leilão.
                        }
                    }
                }

                _context.Update(leilao);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction("Index");
        }

        public async Task<IActionResult> MyAuctions()
        {
            var userEmail = HttpContext.Session.GetString("UserEmail");
            var user = await _context.Utilizador.FirstOrDefaultAsync(u => u.Email == userEmail);

            if (user == null)
            {
                return RedirectToAction("Login", "Utilizadors");
            }

            var userAuctions = await _context.Leiloes
                .Include(l => l.Item)
                .Where(l => l.UtilizadorId == user.UtilizadorId)
                .ToListAsync();

            return View(userAuctions);
        }
    }
}

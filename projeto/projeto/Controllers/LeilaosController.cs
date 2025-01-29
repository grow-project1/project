using System;
using System.Collections.Generic;
using System.Linq;
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

            var applicationDbContext = _context.Leiloes.Include(l => l.Item);
            return View(await applicationDbContext.ToListAsync());
        }

        // GET: Leilaos/Details/5
        public async Task<IActionResult> Details(int? id)
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

        // GET: Leilaos/Create
        public async Task<IActionResult> Create()
        {
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
    }
}

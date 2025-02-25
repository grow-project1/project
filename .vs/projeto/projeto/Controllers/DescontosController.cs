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
    public class DescontosController : Controller
    {
        private readonly ApplicationDbContext _context;

        public DescontosController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Descontos
        public async Task<IActionResult> Index()
        {
            var userEmail = HttpContext.Session.GetString("UserEmail");
            var user =  await _context.Utilizador.FirstOrDefaultAsync(u => u.Email == userEmail);

            ViewData["UserPoints"] = user?.Pontos;
            return View(await _context.Desconto.ToListAsync());
        }

        // GET: Descontos/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var desconto = await _context.Desconto
                .FirstOrDefaultAsync(m => m.DescontoId == id);
            if (desconto == null)
            {
                return NotFound();
            }

            return View(desconto);
        }

        // GET: Descontos/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Descontos/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("DescontoId,Descricao,Valor,DataObtencao,DataFim")] Desconto desconto)
        {
            if (ModelState.IsValid)
            {
                _context.Add(desconto);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(desconto);
        }

        // GET: Descontos/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var desconto = await _context.Desconto.FindAsync(id);
            if (desconto == null)
            {
                return NotFound();
            }
            return View(desconto);
        }

        // POST: Descontos/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("DescontoId,Descricao,Valor,DataObtencao,DataFim")] Desconto desconto)
        {
            if (id != desconto.DescontoId)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(desconto);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!DescontoExists(desconto.DescontoId))
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
            return View(desconto);
        }

        // GET: Descontos/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var desconto = await _context.Desconto
                .FirstOrDefaultAsync(m => m.DescontoId == id);
            if (desconto == null)
            {
                return NotFound();
            }

            return View(desconto);
        }

        // POST: Descontos/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var desconto = await _context.Desconto.FindAsync(id);
            if (desconto != null)
            {
                _context.Desconto.Remove(desconto);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool DescontoExists(int id)
        {
            return _context.Desconto.Any(e => e.DescontoId == id);
        }





        public async Task<IActionResult> RedeemDesconto(int id)
        {
            var userEmail = HttpContext.Session.GetString("UserEmail");
            if (string.IsNullOrEmpty(userEmail))
            {
                TempData["ErrorMessage"] = "Você precisa estar logado para resgatar um desconto.";
                return RedirectToAction(nameof(Index));
            }

            // Obter o utilizador
            var user = await _context.Utilizador.FirstOrDefaultAsync(u => u.Email == userEmail);
            if (user == null)
            {
                TempData["ErrorMessage"] = "Usuário não encontrado.";
                return RedirectToAction(nameof(Index));
            }

            // Obter o desconto da loja
            var desconto = await _context.Desconto.FirstOrDefaultAsync(d => d.DescontoId == id);
            if (desconto == null || desconto.IsLoja == false)
            {
                TempData["ErrorMessage"] = "Desconto não encontrado ou não disponível para resgate.";
                return RedirectToAction(nameof(Index));
            }

            // Verificar se o utilizador tem pontos suficientes
            if (user.Pontos < desconto.PontosNecessarios)
            {
                TempData["ErrorMessage"] = "Você não tem pontos suficientes para resgatar este desconto.";
                return RedirectToAction(nameof(Index));
            }

            // Deduzir pontos e criar instância do desconto resgatado
            user.Pontos -= desconto.PontosNecessarios;

            // Criar registro de desconto resgatado
            var descontoResgatado = new DescontoResgatado
            {
                DescontoId = desconto.DescontoId,
                UtilizadorId = user.UtilizadorId,
                DataResgate = DateTime.Now,
                DataValidade = DateTime.Now.AddMonths(1) // Exemplo: validade de 1 mês
            };

            _context.DescontoResgatado.Add(descontoResgatado);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Desconto resgatado com sucesso!";
            return RedirectToAction(nameof(Index));
        }





        public async Task<IActionResult> MeusDescontos()
        {
            var userEmail = HttpContext.Session.GetString("UserEmail");

            var user = await _context.Utilizador.FirstOrDefaultAsync(u => u.Email == userEmail);

            ViewData["UserPoints"] = user?.Pontos;

            if (string.IsNullOrEmpty(userEmail))
            {
                // Redireciona para login se o usuário não estiver logado
                return RedirectToAction("Login", "Utilizadors");
            }

            if (user == null)
            {
                return NotFound();
            }

            // Buscar os descontos resgatados pelo usuário
            var descontosResgatados = await _context.DescontoResgatado
                .Where(dr => dr.UtilizadorId == user.UtilizadorId)
                .Include(dr => dr.Desconto) // Incluir o desconto associado
                .ToListAsync();

            return View(descontosResgatados);
        }


    }
}

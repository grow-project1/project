// ========================================================
// Autores: Samuel Alves, Rodrigo Baião, Isidoro Ornelas, Miguel Pinto
// Projeto: Sistema de Gestão de Descontos
// Descrição: Controlador responsável pela gestão de descontos
// ========================================================

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

        // ========================================================
        // Autor: Samuel Alves
        // GET: Descontos
        // Descrição: Lista todos os descontos disponíveis.
        // ========================================================
        public async Task<IActionResult> Index()
        {
            var userEmail = HttpContext.Session.GetString("UserEmail");
            var user = await _context.Utilizador.FirstOrDefaultAsync(u => u.Email == userEmail);

            ViewData["UserPoints"] = user?.Pontos;
            return View(await _context.Desconto.ToListAsync());
        }

        // ========================================================
        // Autor: Rodrigo Baião
        // GET: Descontos/Details/5
        // Descrição: Mostra os detalhes de um desconto específico.
        // ========================================================
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var desconto = await _context.Desconto.FirstOrDefaultAsync(m => m.DescontoId == id);
            if (desconto == null) return NotFound();

            return View(desconto);
        }

        // ========================================================
        // Autor: Isidoro Ornelas
        // GET: Descontos/Create
        // Descrição: Apresenta o formulário para criar um novo desconto.
        // ========================================================
        public IActionResult Create()
        {
            return View();
        }

        // ========================================================
        // Autor: Miguel Pinto
        // POST: Descontos/Create
        // Descrição: Cria um novo desconto com os dados fornecidos.
        // ========================================================
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

        // ========================================================
        // Autor: Samuel Alves
        // GET: Descontos/Edit/5
        // Descrição: Apresenta o formulário de edição de um desconto.
        // ========================================================
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var desconto = await _context.Desconto.FindAsync(id);
            if (desconto == null) return NotFound();

            return View(desconto);
        }

        // ========================================================
        // Autor: Rodrigo Baião
        // POST: Descontos/Edit/5
        // Descrição: Guarda as alterações feitas num desconto.
        // ========================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("DescontoId,Descricao,Valor,DataObtencao,DataFim")] Desconto desconto)
        {
            if (id != desconto.DescontoId) return NotFound();

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

        // ========================================================
        // Autor: Isidoro Ornelas
        // GET: Descontos/Delete/5
        // Descrição: Mostra a confirmação de remoção de um desconto.
        // ========================================================
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var desconto = await _context.Desconto.FirstOrDefaultAsync(m => m.DescontoId == id);
            if (desconto == null) return NotFound();

            return View(desconto);
        }

        // ========================================================
        // Autor: Miguel Pinto
        // POST: Descontos/Delete/5
        // Descrição: Remove um desconto da base de dados.
        // ========================================================
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

        // ========================================================
        // Autor: Samuel Alves
        // Método auxiliar
        // Descrição: Verifica se um desconto existe com o ID fornecido.
        // ========================================================
        private bool DescontoExists(int id)
        {
            return _context.Desconto.Any(e => e.DescontoId == id);
        }

        // ========================================================
        // Autor: Rodrigo Baião
        // Método: RedeemDesconto
        // Descrição: Permite a um utilizador resgatar um desconto.
        // ========================================================
        public async Task<IActionResult> RedeemDesconto(int id)
        {
            var userEmail = HttpContext.Session.GetString("UserEmail");
            if (string.IsNullOrEmpty(userEmail))
            {
                TempData["ErrorMessage"] = "You need to be logged to redeem a discount.";
                return RedirectToAction(nameof(Index));
            }

            var user = await _context.Utilizador.FirstOrDefaultAsync(u => u.Email == userEmail);
            if (user == null)
            {
                TempData["ErrorMessage"] = "User not found.";
                return RedirectToAction(nameof(Index));
            }

            var desconto = await _context.Desconto.FirstOrDefaultAsync(d => d.DescontoId == id);
            if (desconto == null || desconto.IsLoja == false)
            {
                TempData["ErrorMessage"] = "Not able for redeem.";
                return RedirectToAction(nameof(Index));
            }

            if (user.Pontos < desconto.PontosNecessarios)
            {
                TempData["ErrorMessage"] = "You don't have enough points to redeem the discount.";
                return RedirectToAction(nameof(Index));
            }

            user.Pontos -= desconto.PontosNecessarios;

            var descontoResgatado = new DescontoResgatado
            {
                DescontoId = desconto.DescontoId,
                UtilizadorId = user.UtilizadorId,
                DataResgate = DateTime.Now,
                DataValidade = DateTime.Now.AddMonths(1),
                Usado = false
            };

            _context.DescontoResgatado.Add(descontoResgatado);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Discount redeemed!";
            return RedirectToAction(nameof(Index));
        }

        // ========================================================
        // Autor: Isidoro Ornelas
        // Método: MeusDescontos
        // Descrição: Mostra os descontos resgatados pelo utilizador.
        // ========================================================
        public async Task<IActionResult> MeusDescontos()
        {
            var userEmail = HttpContext.Session.GetString("UserEmail");

            var user = await _context.Utilizador.FirstOrDefaultAsync(u => u.Email == userEmail);

            ViewData["UserPoints"] = user?.Pontos;

            if (string.IsNullOrEmpty(userEmail))
            {
                return RedirectToAction("Login", "Utilizadors");
            }

            if (user == null)
            {
                return NotFound();
            }

            var descontosResgatados = await _context.DescontoResgatado
                .Where(dr => dr.UtilizadorId == user.UtilizadorId)
                .Include(dr => dr.Desconto)
                .ToListAsync();

            return View(descontosResgatados);
        }
    }
}

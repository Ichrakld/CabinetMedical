using GestionCabinetMedical.Models;
using GestionCabinetMedical.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace GestionCabinetMedical.Controllers
{
    [Authorize(Roles = "ADMIN,SECRETAIRE")]
    public class RessourcesMedicalesController : Controller
    {
        private readonly BdCabinetMedicalContext _context;

        public RessourcesMedicalesController(BdCabinetMedicalContext context)
        {
            _context = context;
        }

        // ============================================================
        // GET: RessourceMedicales - WITH PAGINATION AND STOCK FILTERS
        // ============================================================
        public async Task<IActionResult> Index(
            string? searchTerm,
            string? stockFilter,
            string? sortBy = "nom",
            int page = 1,
            int pageSize = 10)
        {
            // Validate page size
            if (!new[] { 5, 10, 25, 50 }.Contains(pageSize))
                pageSize = 10;

            var query = _context.RessourceMedicales.AsQueryable();

            // Text search filtering
            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                var term = searchTerm.ToLower();
                query = query.Where(r => r.Nom != null && r.Nom.ToLower().Contains(term));
            }

            // Stock filtering
            if (!string.IsNullOrWhiteSpace(stockFilter))
            {
                switch (stockFilter.ToLower())
                {
                    case "faible":
                        query = query.Where(r => r.Quantite < 10);
                        break;
                    case "normal":
                        query = query.Where(r => r.Quantite >= 10);
                        break;
                        // "tous" - no filter
                }
            }

            // Calculate statistics (before pagination)
            var allRessources = await _context.RessourceMedicales.ToListAsync();

            var stats = new
            {
                Total = allRessources.Count,
                StockFaible = allRessources.Count(r => r.Quantite < 10),
                QuantiteTotale = allRessources.Sum(r => r.Quantite)
            };

            // Sorting
            query = sortBy?.ToLower() switch
            {
                "quantite" => query.OrderBy(r => r.Quantite),
                "quantite-desc" => query.OrderByDescending(r => r.Quantite),
                _ => query.OrderBy(r => r.Nom)
            };

            // Pagination
            var paginatedList = await PaginatedList<RessourceMedicale>.CreateAsync(query, page, pageSize);

            var viewModel = new RessourceMedicaleIndexViewModel
            {
                RessourceMedicales = paginatedList,
                TotalRessources = stats.Total,
                RessourcesStockFaible = stats.StockFaible,
                QuantiteTotale = stats.QuantiteTotale,
                SearchTerm = searchTerm,
                StockFilter = stockFilter,
                SortBy = sortBy,
                PageSize = pageSize
            };

            return View(viewModel);
        }

        // GET: RessourceMedicales/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var ressourceMedicale = await _context.RessourceMedicales
                .FirstOrDefaultAsync(m => m.Id == id);
            if (ressourceMedicale == null)
            {
                return NotFound();
            }

            return View(ressourceMedicale);
        }

        // GET: RessourceMedicales/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: RessourceMedicales/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,Nom,Quantite")] RessourceMedicale ressourceMedicale)
        {
            if (ModelState.IsValid)
            {
                _context.Add(ressourceMedicale);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Ressource médicale ajoutée avec succès !";
                return RedirectToAction(nameof(Index));
            }
            return View(ressourceMedicale);
        }

        // GET: RessourceMedicales/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var ressourceMedicale = await _context.RessourceMedicales.FindAsync(id);
            if (ressourceMedicale == null)
            {
                return NotFound();
            }
            return View(ressourceMedicale);
        }

        // POST: RessourceMedicales/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Nom,Quantite")] RessourceMedicale ressourceMedicale)
        {
            if (id != ressourceMedicale.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(ressourceMedicale);
                    await _context.SaveChangesAsync();
                    TempData["Success"] = "Ressource médicale modifiée avec succès !";
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!RessourceMedicaleExists(ressourceMedicale.Id))
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
            return View(ressourceMedicale);
        }

        // GET: RessourceMedicales/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var ressourceMedicale = await _context.RessourceMedicales
                .FirstOrDefaultAsync(m => m.Id == id);
            if (ressourceMedicale == null)
            {
                return NotFound();
            }

            return View(ressourceMedicale);
        }

        // POST: RessourceMedicales/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var ressourceMedicale = await _context.RessourceMedicales.FindAsync(id);
            if (ressourceMedicale != null)
            {
                _context.RessourceMedicales.Remove(ressourceMedicale);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Ressource médicale supprimée avec succès !";
            }
            return RedirectToAction(nameof(Index));
        }

        private bool RessourceMedicaleExists(int id)
        {
            return _context.RessourceMedicales.Any(e => e.Id == id);
        }
    }
}
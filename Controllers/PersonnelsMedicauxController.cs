using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using GestionCabinetMedical.Models;
using GestionCabinetMedical.ViewModels;
using Microsoft.AspNetCore.Authorization;

namespace GestionCabinetMedical.Controllers
{
    [Authorize(Roles = "ADMIN")]
    public class PersonnelsMedicauxController : Controller
    {
        private readonly BdCabinetMedicalContext _context;

        public PersonnelsMedicauxController(BdCabinetMedicalContext context)
        {
            _context = context;
        }

        // ============================================================
        // GET: PersonnelsMedicaux - WITH PAGINATION AND FILTERS
        // ============================================================
        public async Task<IActionResult> Index(
            string? searchTerm,
            string? fonction,
            string? sortBy = "nom",
            int page = 1,
            int pageSize = 10)
        {
            // Validate page size
            if (!new[] { 5, 10, 25, 50 }.Contains(pageSize))
                pageSize = 10;

            var query = _context.PersonnelMedicals.AsQueryable();

            // Text search filtering
            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                var term = searchTerm.ToLower();
                query = query.Where(p =>
                    (p.Nom != null && p.Nom.ToLower().Contains(term)) ||
                    (p.Fonction != null && p.Fonction.ToLower().Contains(term))
                );
            }

            // Function filtering
            if (!string.IsNullOrWhiteSpace(fonction))
            {
                query = query.Where(p => p.Fonction == fonction);
            }

            // Calculate statistics (before pagination)
            var allPersonnel = await _context.PersonnelMedicals.ToListAsync();

            var stats = new
            {
                Total = allPersonnel.Count,
                FonctionsDistinctes = allPersonnel.Select(p => p.Fonction).Distinct().Count()
            };

            // Get list of functions for filter dropdown
            var fonctions = await _context.PersonnelMedicals
                .Where(p => !string.IsNullOrEmpty(p.Fonction))
                .Select(p => p.Fonction)
                .Distinct()
                .OrderBy(f => f)
                .ToListAsync();

            // Sorting
            query = sortBy?.ToLower() switch
            {
                "fonction" => query.OrderBy(p => p.Fonction).ThenBy(p => p.Nom),
                _ => query.OrderBy(p => p.Nom)
            };

            // Pagination
            var paginatedList = await PaginatedList<PersonnelMedical>.CreateAsync(query, page, pageSize);

            var viewModel = new PersonnelMedicalIndexViewModel
            {
                PersonnelMedicals = paginatedList,
                TotalPersonnel = stats.Total,
                FonctionsDistinctes = stats.FonctionsDistinctes,
                SearchTerm = searchTerm,
                Fonction = fonction,
                SortBy = sortBy,
                PageSize = pageSize,
                Fonctions = fonctions
            };

            return View(viewModel);
        }

        // GET: PersonnelsMedicaux/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var personnelMedical = await _context.PersonnelMedicals
                .FirstOrDefaultAsync(m => m.Id == id);

            if (personnelMedical == null) return NotFound();

            return View(personnelMedical);
        }

        // GET: PersonnelsMedicaux/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: PersonnelsMedicaux/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,Nom,Fonction")] PersonnelMedical personnelMedical)
        {
            if (ModelState.IsValid)
            {
                _context.Add(personnelMedical);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Personnel médical ajouté avec succès !";
                return RedirectToAction(nameof(Index));
            }
            return View(personnelMedical);
        }

        // GET: PersonnelsMedicaux/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var personnelMedical = await _context.PersonnelMedicals.FindAsync(id);
            if (personnelMedical == null) return NotFound();

            return View(personnelMedical);
        }

        // POST: PersonnelsMedicaux/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Nom,Fonction")] PersonnelMedical personnelMedical)
        {
            if (id != personnelMedical.Id) return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(personnelMedical);
                    await _context.SaveChangesAsync();
                    TempData["Success"] = "Personnel médical modifié avec succès !";
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!PersonnelMedicalExists(personnelMedical.Id)) return NotFound();
                    else throw;
                }
                return RedirectToAction(nameof(Index));
            }
            return View(personnelMedical);
        }

        // GET: PersonnelsMedicaux/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var personnelMedical = await _context.PersonnelMedicals
                .FirstOrDefaultAsync(m => m.Id == id);

            if (personnelMedical == null) return NotFound();

            return View(personnelMedical);
        }

        // POST: PersonnelsMedicaux/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var personnelMedical = await _context.PersonnelMedicals.FindAsync(id);
            if (personnelMedical != null)
            {
                _context.PersonnelMedicals.Remove(personnelMedical);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Personnel médical supprimé avec succès !";
            }
            return RedirectToAction(nameof(Index));
        }

        private bool PersonnelMedicalExists(int id)
        {
            return _context.PersonnelMedicals.Any(e => e.Id == id);
        }
    }
}
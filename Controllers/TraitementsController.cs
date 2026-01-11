using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using GestionCabinetMedical.Models;
using GestionCabinetMedical.ViewModels;
using Microsoft.AspNetCore.Authorization;

namespace GestionCabinetMedical.Controllers
{
    [Authorize(Roles = "ADMIN,MEDECIN")]
    public class TraitementsController : Controller
    {
        private readonly BdCabinetMedicalContext _context;

        public TraitementsController(BdCabinetMedicalContext context)
        {
            _context = context;
        }

        // ============================================================
        // GET: Traitements - WITH PAGINATION AND FILTERS
        // ============================================================
        public async Task<IActionResult> Index(
            string? searchTerm,
            string? sortBy = "recent",
            int page = 1,
            int pageSize = 10)
        {
            // Validate page size
            if (!new[] { 5, 10, 25, 50 }.Contains(pageSize))
                pageSize = 10;

            // Base query with all necessary includes
            var query = _context.Traitements
                .Include(t => t.Consultation)
                    .ThenInclude(c => c.DossierMedical)
                        .ThenInclude(d => d.Patient)
                            .ThenInclude(p => p.IdNavigation)
                .AsQueryable();

            // Text search filtering
            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                var term = searchTerm.ToLower();
                query = query.Where(t =>
                    (t.TypeTraitement != null && t.TypeTraitement.ToLower().Contains(term)) ||
                    (t.Consultation != null && t.Consultation.DossierMedical != null &&
                     t.Consultation.DossierMedical.Patient != null &&
                     t.Consultation.DossierMedical.Patient.IdNavigation != null &&
                     (t.Consultation.DossierMedical.Patient.IdNavigation.Nom.ToLower().Contains(term) ||
                      t.Consultation.DossierMedical.Patient.IdNavigation.Prenom.ToLower().Contains(term)))
                );
            }

            // Calculate statistics (before pagination)
            var allTraitements = await _context.Traitements
                .Include(t => t.Consultation)
                .ToListAsync();

            var dateLimite30j = DateTime.Now.AddDays(-30);
            var stats = new
            {
                Total = allTraitements.Count,
                Recents = allTraitements.Count(t =>
                    t.Consultation != null && t.Consultation.DateConsultation >= dateLimite30j)
            };

            // Sorting
            query = sortBy?.ToLower() switch
            {
                "recent" => query.OrderByDescending(t => t.Consultation != null ? t.Consultation.DateConsultation : DateTime.MinValue),
                "ancien" => query.OrderBy(t => t.Consultation != null ? t.Consultation.DateConsultation : DateTime.MinValue),
                "type" => query.OrderBy(t => t.TypeTraitement),
                "patient" => query.OrderBy(t => t.Consultation != null && t.Consultation.DossierMedical != null &&
                    t.Consultation.DossierMedical.Patient != null && t.Consultation.DossierMedical.Patient.IdNavigation != null ?
                    t.Consultation.DossierMedical.Patient.IdNavigation.Nom : ""),
                _ => query.OrderByDescending(t => t.NumPro)
            };

            // Pagination
            var paginatedList = await PaginatedList<Traitement>.CreateAsync(query, page, pageSize);

            var viewModel = new TraitementIndexViewModel
            {
                Traitements = paginatedList,
                TotalTraitements = stats.Total,
                TraitementsRecents = stats.Recents,
                SearchTerm = searchTerm,
                SortBy = sortBy,
                PageSize = pageSize
            };

            return View(viewModel);
        }

        // GET: Traitements/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var traitement = await _context.Traitements
                .Include(t => t.Consultation)
                    .ThenInclude(c => c.DossierMedical)
                        .ThenInclude(d => d.Patient)
                            .ThenInclude(p => p.IdNavigation)
                .FirstOrDefaultAsync(m => m.NumPro == id);

            if (traitement == null) return NotFound();

            return View(traitement);
        }

        // GET: Traitements/Create
        public IActionResult Create()
        {
            PopulateDropdowns();
            return View();
        }

        // POST: Traitements/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("NumPro,TypeTraitement,ConsultationId")] Traitement traitement)
        {
            // FIX: Remove validation for navigation property
            ModelState.Remove("Consultation");

            if (ModelState.IsValid)
            {
                _context.Add(traitement);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Traitement ajouté avec succès !";
                return RedirectToAction(nameof(Index));
            }
            PopulateDropdowns(traitement.ConsultationId);
            return View(traitement);
        }

        // GET: Traitements/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var traitement = await _context.Traitements.FindAsync(id);
            if (traitement == null) return NotFound();

            PopulateDropdowns(traitement.ConsultationId);
            return View(traitement);
        }

        // POST: Traitements/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("NumPro,TypeTraitement,ConsultationId")] Traitement traitement)
        {
            if (id != traitement.NumPro) return NotFound();

            // FIX: Remove validation for navigation property
            ModelState.Remove("Consultation");

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(traitement);
                    await _context.SaveChangesAsync();
                    TempData["Success"] = "Traitement modifié avec succès !";
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!TraitementExists(traitement.NumPro)) return NotFound();
                    else throw;
                }
                return RedirectToAction(nameof(Index));
            }
            PopulateDropdowns(traitement.ConsultationId);
            return View(traitement);
        }

        // GET: Traitements/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var traitement = await _context.Traitements
                .Include(t => t.Consultation)
                    .ThenInclude(c => c.DossierMedical)
                        .ThenInclude(d => d.Patient)
                            .ThenInclude(p => p.IdNavigation)
                .FirstOrDefaultAsync(m => m.NumPro == id);

            if (traitement == null) return NotFound();

            return View(traitement);
        }

        // POST: Traitements/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var traitement = await _context.Traitements.FindAsync(id);
            if (traitement != null)
            {
                _context.Traitements.Remove(traitement);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Traitement supprimé avec succès !";
            }
            return RedirectToAction(nameof(Index));
        }

        private bool TraitementExists(int id)
        {
            return _context.Traitements.Any(e => e.NumPro == id);
        }

        // HELPER METHOD pour afficher "Consultation N°X - Date - Patient"
        private void PopulateDropdowns(object selectedConsultation = null)
        {
            var consultationsQuery = _context.Consultations
                .Include(c => c.DossierMedical)
                    .ThenInclude(d => d.Patient)
                        .ThenInclude(p => p.IdNavigation)
                .Select(c => new {
                    NumDetail = c.NumDetail,
                    DisplayText = "Cons. N°" + c.NumDetail +
                          " (" + c.DateConsultation.ToShortDateString() + ") - " +
                          c.DossierMedical.Patient.IdNavigation.Nom + " " +
                          c.DossierMedical.Patient.IdNavigation.Prenom

                })
                .ToList();

            ViewData["ConsultationId"] = new SelectList(consultationsQuery, "NumDetail", "DisplayText", selectedConsultation);
        }
    }
}
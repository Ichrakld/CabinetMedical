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
    [Authorize(Roles = "ADMIN,MEDECIN")]
    public class ConsultationsController : Controller
    {
        private readonly BdCabinetMedicalContext _context;

        public ConsultationsController(BdCabinetMedicalContext context)
        {
            _context = context;
        }

        // GET: Consultations - AVEC PAGINATION
        public async Task<IActionResult> Index(
            string searchTerm,
            DateTime? dateDebut,
            DateTime? dateFin,
            int? dossierId,
            string sortBy = "date",
            string sortOrder = "desc",
            int page = 1,
            int pageSize = 10)
        {
            // Valider la taille de page
            if (!new[] { 5, 10, 25, 50 }.Contains(pageSize))
                pageSize = 10;

            var query = _context.Consultations
                .Include(c => c.DossierMedical)
                    .ThenInclude(d => d.Patient)
                        .ThenInclude(p => p.IdNavigation)
                .Include(c => c.Traitements)
                .AsQueryable();

            // Filtrage par recherche textuelle
            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                var term = searchTerm.ToLower();
                query = query.Where(c =>
                    (c.Diagnostic != null && c.Diagnostic.ToLower().Contains(term)) ||
                    (c.Notes != null && c.Notes.ToLower().Contains(term)) ||
                    (c.DossierMedical.Patient.IdNavigation.Nom != null &&
                     c.DossierMedical.Patient.IdNavigation.Nom.ToLower().Contains(term)) ||
                    (c.DossierMedical.Patient.IdNavigation.Prenom != null &&
                     c.DossierMedical.Patient.IdNavigation.Prenom.ToLower().Contains(term))
                );
            }

            // Filtrage par dates
            if (dateDebut.HasValue)
                query = query.Where(c => c.DateConsultation.Date >= dateDebut.Value.Date);

            if (dateFin.HasValue)
                query = query.Where(c => c.DateConsultation.Date <= dateFin.Value.Date);

            // Filtrage par dossier
            if (dossierId.HasValue)
                query = query.Where(c => c.DossierMedicalId == dossierId.Value);

            // Statistiques (avant pagination)
            var totalConsultations = await _context.Consultations.CountAsync();
            var consultationsAujourdhui = await _context.Consultations
                .CountAsync(c => c.DateConsultation.Date == DateTime.Today);
            var avecDiagnostic = await _context.Consultations
                .CountAsync(c => !string.IsNullOrEmpty(c.Diagnostic));
            var totalTraitements = await _context.Consultations
                .SelectMany(c => c.Traitements)
                .CountAsync();

            // Tri
            query = (sortBy?.ToLower(), sortOrder?.ToLower()) switch
            {
                ("date", "asc") => query.OrderBy(c => c.DateConsultation),
                ("date", "desc") => query.OrderByDescending(c => c.DateConsultation),
                ("patient", "asc") => query.OrderBy(c => c.DossierMedical.Patient.IdNavigation.Nom),
                ("patient", "desc") => query.OrderByDescending(c => c.DossierMedical.Patient.IdNavigation.Nom),
                ("dossier", "asc") => query.OrderBy(c => c.DossierMedicalId),
                ("dossier", "desc") => query.OrderByDescending(c => c.DossierMedicalId),
                _ => query.OrderByDescending(c => c.DateConsultation)
            };

            // Pagination
            var paginatedList = await PaginatedList<Consultation>.CreateAsync(query, page, pageSize);

            // Liste des dossiers pour le filtre
            var dossiers = await _context.DossierMedicals
                .Include(d => d.Patient)
                    .ThenInclude(p => p.IdNavigation)
                .Select(d => new
                {
                    d.NumDossier,
                    PatientNom = d.Patient.IdNavigation.Nom + " " + d.Patient.IdNavigation.Prenom
                })
                .ToListAsync();

            ViewBag.Dossiers = new SelectList(dossiers, "NumDossier", "PatientNom");
            ViewBag.SearchTerm = searchTerm;
            ViewBag.DateDebut = dateDebut;
            ViewBag.DateFin = dateFin;
            ViewBag.DossierId = dossierId;
            ViewBag.SortBy = sortBy;
            ViewBag.SortOrder = sortOrder;
            ViewBag.PageSize = pageSize;

            // Statistiques
            ViewBag.TotalConsultations = totalConsultations;
            ViewBag.ConsultationsAujourdhui = consultationsAujourdhui;
            ViewBag.AvecDiagnostic = avecDiagnostic;
            ViewBag.TotalTraitements = totalTraitements;

            return View(paginatedList);
        }

        // GET: Consultations/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var consultation = await _context.Consultations
                .Include(c => c.DossierMedical)
                    .ThenInclude(d => d.Patient)
                        .ThenInclude(p => p.IdNavigation)
                .Include(c => c.DossierMedical)
                    .ThenInclude(d => d.Medecin)
                        .ThenInclude(m => m.IdNavigation)
                .Include(c => c.Traitements)
                .FirstOrDefaultAsync(m => m.NumDetail == id);

            if (consultation == null) return NotFound();

            return View(consultation);
        }

        // GET: Consultations/Create
        public IActionResult Create(int? dossierId)
        {
            PopulateDropdowns(dossierId);

            var consultation = new Consultation
            {
                DateConsultation = DateTime.Now
            };

            return View(consultation);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("NumDetail,DateConsultation,Diagnostic,Notes,DossierMedicalId")] Consultation consultation)
        {
            ModelState.Remove("DossierMedical");

            if (ModelState.IsValid)
            {
                _context.Add(consultation);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Consultation créée avec succès.";
                return RedirectToAction(nameof(Index));
            }
            PopulateDropdowns(consultation.DossierMedicalId);
            return View(consultation);
        }

        // GET: Consultations/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var consultation = await _context.Consultations
                .Include(c => c.Traitements)
                .FirstOrDefaultAsync(c => c.NumDetail == id);

            if (consultation == null) return NotFound();

            PopulateDropdowns(consultation.DossierMedicalId);
            return View(consultation);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("NumDetail,DateConsultation,Diagnostic,Notes,DossierMedicalId")] Consultation consultation)
        {
            if (id != consultation.NumDetail) return NotFound();

            ModelState.Remove("DossierMedical");

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(consultation);
                    await _context.SaveChangesAsync();
                    TempData["SuccessMessage"] = "Consultation modifiée avec succès.";
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!ConsultationExists(consultation.NumDetail)) return NotFound();
                    else throw;
                }
                return RedirectToAction(nameof(Index));
            }
            PopulateDropdowns(consultation.DossierMedicalId);
            return View(consultation);
        }

        // GET: Consultations/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var consultation = await _context.Consultations
                .Include(c => c.DossierMedical)
                    .ThenInclude(d => d.Patient)
                        .ThenInclude(p => p.IdNavigation)
                .Include(c => c.Traitements)
                .FirstOrDefaultAsync(m => m.NumDetail == id);

            if (consultation == null) return NotFound();

            return View(consultation);
        }

        // POST: Consultations/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var consultation = await _context.Consultations
                .Include(c => c.Traitements)
                .FirstOrDefaultAsync(c => c.NumDetail == id);

            if (consultation != null)
            {
                if (consultation.Traitements?.Any() == true)
                {
                    _context.Traitements.RemoveRange(consultation.Traitements);
                }

                _context.Consultations.Remove(consultation);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Consultation supprimée avec succès.";
            }
            return RedirectToAction(nameof(Index));
        }

        private bool ConsultationExists(int id)
        {
            return _context.Consultations.Any(e => e.NumDetail == id);
        }

        private void PopulateDropdowns(object selectedDossier = null)
        {
            var dossierQuery = _context.DossierMedicals
                .Include(d => d.Patient)
                    .ThenInclude(p => p.IdNavigation)
                .Select(d => new {
                    NumDossier = d.NumDossier,
                    DisplayText = "Dossier N°" + d.NumDossier + " - " + d.Patient.IdNavigation.Nom + " " + d.Patient.IdNavigation.Prenom
                })
                .ToList();

            ViewData["DossierMedicalId"] = new SelectList(dossierQuery, "NumDossier", "DisplayText", selectedDossier);
        }
    }
}
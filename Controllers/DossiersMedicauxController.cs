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
    public class DossiersMedicauxController : Controller
    {
        private readonly BdCabinetMedicalContext _context;

        public DossiersMedicauxController(BdCabinetMedicalContext context)
        {
            _context = context;
        }

        // GET: DossiersMedicaux - AVEC PAGINATION
        public async Task<IActionResult> Index(
            string searchTerm,
            int? medecinId,
            int? patientId,
            bool? avecAllergies,
            string sortBy = "numero",
            string sortOrder = "desc",
            int page = 1,
            int pageSize = 10)
        {
            // Validation taille de page
            if (!new[] { 5, 10, 25, 50 }.Contains(pageSize))
                pageSize = 10;

            var query = _context.DossierMedicals
                .Include(d => d.Medecin)
                    .ThenInclude(m => m.IdNavigation)
                .Include(d => d.Patient)
                    .ThenInclude(p => p.IdNavigation)
                .Include(d => d.Consultations)
                .AsQueryable();

            // Filtrage par recherche textuelle
            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                var term = searchTerm.ToLower();
                query = query.Where(d =>
                    (d.Patient.IdNavigation.Nom != null && d.Patient.IdNavigation.Nom.ToLower().Contains(term)) ||
                    (d.Patient.IdNavigation.Prenom != null && d.Patient.IdNavigation.Prenom.ToLower().Contains(term)) ||
                    (d.GroupeSanguin != null && d.GroupeSanguin.ToLower().Contains(term)) ||
                    (d.Allergies != null && d.Allergies.ToLower().Contains(term))
                );
            }

            // Filtrage par médecin
            if (medecinId.HasValue)
                query = query.Where(d => d.MedecinId == medecinId.Value);

            // Filtrage par patient
            if (patientId.HasValue)
                query = query.Where(d => d.PatientId == patientId.Value);

            // Filtrage par allergies
            if (avecAllergies.HasValue && avecAllergies.Value)
                query = query.Where(d => !string.IsNullOrEmpty(d.Allergies));

            // Statistiques (avant pagination)
            var totalDossiers = await _context.DossierMedicals.CountAsync();
            var avecAllergiesCount = await _context.DossierMedicals.CountAsync(d => !string.IsNullOrEmpty(d.Allergies));
            var avecAntecedents = await _context.DossierMedicals.CountAsync(d => !string.IsNullOrEmpty(d.AntecedentsMedicaux));
            var totalConsultations = await _context.Consultations.CountAsync();

            // Tri
            query = (sortBy?.ToLower(), sortOrder?.ToLower()) switch
            {
                ("numero", "asc") => query.OrderBy(d => d.NumDossier),
                ("numero", "desc") => query.OrderByDescending(d => d.NumDossier),
                ("patient", "asc") => query.OrderBy(d => d.Patient.IdNavigation.Nom),
                ("patient", "desc") => query.OrderByDescending(d => d.Patient.IdNavigation.Nom),
                ("medecin", "asc") => query.OrderBy(d => d.Medecin.IdNavigation.Nom),
                ("medecin", "desc") => query.OrderByDescending(d => d.Medecin.IdNavigation.Nom),
                _ => query.OrderByDescending(d => d.NumDossier)
            };

            // Pagination
            var paginatedList = await PaginatedList<DossierMedical>.CreateAsync(query, page, pageSize);

            // Listes pour les filtres
            var medecins = await _context.Medecins
                .Include(m => m.IdNavigation)
                .Select(m => new { m.Id, NomComplet = "Dr. " + m.IdNavigation.Nom + " " + m.IdNavigation.Prenom })
                .ToListAsync();

            ViewBag.Medecins = new SelectList(medecins, "Id", "NomComplet");
            ViewBag.SearchTerm = searchTerm;
            ViewBag.MedecinId = medecinId;
            ViewBag.PatientId = patientId;
            ViewBag.AvecAllergiesFiltre = avecAllergies;  // Pour le filtre (bool)
            ViewBag.SortBy = sortBy;
            ViewBag.SortOrder = sortOrder;

            // Statistiques
            ViewBag.TotalDossiers = totalDossiers;
            ViewBag.AvecAllergies = avecAllergiesCount;  // Pour la statistique (int)
            ViewBag.AvecAntecedents = avecAntecedents;
            ViewBag.TotalConsultations = totalConsultations;

            return View(paginatedList);
        }

        // GET: DossiersMedicaux/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var dossierMedical = await _context.DossierMedicals
                .Include(d => d.Medecin)
                    .ThenInclude(m => m.IdNavigation)
                .Include(d => d.Patient)
                    .ThenInclude(p => p.IdNavigation)
                .Include(d => d.Consultations)
                    .ThenInclude(c => c.Traitements)
                .FirstOrDefaultAsync(m => m.NumDossier == id);

            if (dossierMedical == null)
            {
                return NotFound();
            }

            return View(dossierMedical);
        }

        // GET: DossiersMedicaux/Create
        public IActionResult Create(int? patientId)
        {
            PopulateDropdowns(selectedPatient: patientId);
            return View();
        }

        // POST: DossiersMedicaux/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("NumDossier,GroupeSanguin,Allergies,AntecedentsMedicaux,PatientId,MedecinId")] DossierMedical dossierMedical)
        {
            ModelState.Remove("Patient");
            ModelState.Remove("Medecin");

            if (ModelState.IsValid)
            {
                _context.Add(dossierMedical);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Dossier médical créé avec succès.";
                return RedirectToAction(nameof(Index));
            }

            PopulateDropdowns(dossierMedical.MedecinId, dossierMedical.PatientId);
            return View(dossierMedical);
        }

        // GET: DossiersMedicaux/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var dossierMedical = await _context.DossierMedicals.FindAsync(id);
            if (dossierMedical == null)
            {
                return NotFound();
            }

            PopulateDropdowns(dossierMedical.MedecinId, dossierMedical.PatientId);
            return View(dossierMedical);
        }

        // POST: DossiersMedicaux/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("NumDossier,GroupeSanguin,Allergies,AntecedentsMedicaux,PatientId,MedecinId")] DossierMedical dossierMedical)
        {
            if (id != dossierMedical.NumDossier)
            {
                return NotFound();
            }

            ModelState.Remove("Patient");
            ModelState.Remove("Medecin");

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(dossierMedical);
                    await _context.SaveChangesAsync();
                    TempData["SuccessMessage"] = "Dossier médical modifié avec succès.";
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!DossierMedicalExists(dossierMedical.NumDossier))
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

            PopulateDropdowns(dossierMedical.MedecinId, dossierMedical.PatientId);
            return View(dossierMedical);
        }

        // GET: DossiersMedicaux/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var dossierMedical = await _context.DossierMedicals
                .Include(d => d.Medecin)
                    .ThenInclude(m => m.IdNavigation)
                .Include(d => d.Patient)
                    .ThenInclude(p => p.IdNavigation)
                .Include(d => d.Consultations)
                    .ThenInclude(c => c.Traitements)
                .FirstOrDefaultAsync(m => m.NumDossier == id);

            if (dossierMedical == null)
            {
                return NotFound();
            }

            return View(dossierMedical);
        }

        // POST: DossiersMedicaux/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var dossierMedical = await _context.DossierMedicals
                .Include(d => d.Consultations)
                    .ThenInclude(c => c.Traitements)
                .FirstOrDefaultAsync(d => d.NumDossier == id);

            if (dossierMedical != null)
            {
                // Supprimer les traitements de toutes les consultations
                foreach (var consultation in dossierMedical.Consultations ?? Enumerable.Empty<Consultation>())
                {
                    if (consultation.Traitements?.Any() == true)
                    {
                        _context.Traitements.RemoveRange(consultation.Traitements);
                    }
                }

                // Supprimer les consultations
                if (dossierMedical.Consultations?.Any() == true)
                {
                    _context.Consultations.RemoveRange(dossierMedical.Consultations);
                }

                _context.DossierMedicals.Remove(dossierMedical);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Dossier médical supprimé avec succès.";
            }

            return RedirectToAction(nameof(Index));
        }

        private bool DossierMedicalExists(int id)
        {
            return _context.DossierMedicals.Any(e => e.NumDossier == id);
        }

        // HELPER METHOD to load names into dropdowns
        private void PopulateDropdowns(object selectedMedecin = null, object selectedPatient = null)
        {
            var medecins = _context.Medecins
                .Include(m => m.IdNavigation)
                .AsEnumerable()
                .Select(m => new {
                    m.Id,
                    NomComplet = $"Dr. {m.IdNavigation.Nom} {m.IdNavigation.Prenom} ({m.Specialite})"
                });

            var patients = _context.Patients
                .Include(p => p.IdNavigation)
                .AsEnumerable()
                .Select(p => new {
                    p.Id,
                    NomComplet = $"{p.IdNavigation.Nom} {p.IdNavigation.Prenom} ({p.NumSecuriteSociale})"
                });

            ViewData["MedecinId"] = new SelectList(medecins, "Id", "NomComplet", selectedMedecin);
            ViewData["PatientId"] = new SelectList(patients, "Id", "NomComplet", selectedPatient);
        }
    }
}
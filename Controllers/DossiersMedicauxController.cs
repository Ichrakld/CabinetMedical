using GestionCabinetMedical.Models;
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

        // GET: DossiersMedicaux
        public async Task<IActionResult> Index()
        {
            var dossiers = await _context.DossierMedicals
                .Include(d => d.Medecin)
                    .ThenInclude(m => m.IdNavigation)
                .Include(d => d.Patient)
                    .ThenInclude(p => p.IdNavigation)
                .Include(d => d.Consultations)
                .OrderByDescending(d => d.NumDossier)
                .ToListAsync();

            return View(dossiers);
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
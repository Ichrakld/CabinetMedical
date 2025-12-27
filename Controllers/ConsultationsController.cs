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
    public class ConsultationsController : Controller
    {
        private readonly BdCabinetMedicalContext _context;

        public ConsultationsController(BdCabinetMedicalContext context)
        {
            _context = context;
        }

        // GET: Consultations
        public async Task<IActionResult> Index()
        {
            // On charge le Dossier, PUIS le Patient du dossier, PUIS le User du Patient
            var consultations = _context.Consultations
                .Include(c => c.DossierMedical)
                    .ThenInclude(d => d.Patient)
                        .ThenInclude(p => p.IdNavigation);

            return View(await consultations.ToListAsync());
        }

        // GET: Consultations/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var consultation = await _context.Consultations
                .Include(c => c.DossierMedical)
                    .ThenInclude(d => d.Patient)
                        .ThenInclude(p => p.IdNavigation)
                .FirstOrDefaultAsync(m => m.NumDetail == id);

            if (consultation == null) return NotFound();

            return View(consultation);
        }

        // GET: Consultations/Create
        public IActionResult Create()
        {
            PopulateDropdowns();
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("NumDetail,Diagnostic,DateConsultation,DossierMedicalId")] Consultation consultation)
        {
            // FIX: Remove validation for the navigation property
            ModelState.Remove("DossierMedical");

            if (ModelState.IsValid)
            {
                _context.Add(consultation);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            PopulateDropdowns(consultation.DossierMedicalId);
            return View(consultation);
        }

        // GET: Consultations/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var consultation = await _context.Consultations.FindAsync(id);
            if (consultation == null) return NotFound();

            PopulateDropdowns(consultation.DossierMedicalId);
            return View(consultation);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("NumDetail,Diagnostic,DateConsultation,DossierMedicalId")] Consultation consultation)
        {
            if (id != consultation.NumDetail) return NotFound();

            // FIX: Remove validation here too
            ModelState.Remove("DossierMedical");

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(consultation);
                    await _context.SaveChangesAsync();
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
                .FirstOrDefaultAsync(m => m.NumDetail == id);

            if (consultation == null) return NotFound();

            return View(consultation);
        }

        // POST: Consultations/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var consultation = await _context.Consultations.FindAsync(id);
            if (consultation != null)
            {
                _context.Consultations.Remove(consultation);
            }
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool ConsultationExists(int id)
        {
            return _context.Consultations.Any(e => e.NumDetail == id);
        }

        // HELPER METHOD pour afficher "Dossier N°X - Nom Patient"
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
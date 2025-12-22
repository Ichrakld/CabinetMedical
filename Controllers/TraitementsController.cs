using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using GestionCabinetMedical.Models;
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

        // GET: Traitements
        public async Task<IActionResult> Index()
        {
            // On charge la Consultation, puis le Dossier, puis le Patient pour avoir le contexte complet
            var traitements = _context.Traitements
                .Include(t => t.Consultation)
                    .ThenInclude(c => c.DossierMedical)
                        .ThenInclude(d => d.Patient)
                            .ThenInclude(p => p.IdNavigation);

            return View(await traitements.ToListAsync());
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
            if (ModelState.IsValid)
            {
                _context.Add(traitement);
                await _context.SaveChangesAsync();
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

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(traitement);
                    await _context.SaveChangesAsync();
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
            }
            await _context.SaveChangesAsync();
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
                                  " (" + (c.DateConsultation.HasValue ? c.DateConsultation.Value.ToShortDateString() : "Date N/A") + ") - " +
                                  c.DossierMedical.Patient.IdNavigation.Nom + " " + c.DossierMedical.Patient.IdNavigation.Prenom
                })
                .ToList();

            ViewData["ConsultationId"] = new SelectList(consultationsQuery, "NumDetail", "DisplayText", selectedConsultation);
        }
    }
}
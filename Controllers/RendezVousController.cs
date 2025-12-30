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
    [Authorize(Roles = "ADMIN,MEDECIN,SECRETAIRE,PATIENT")]
    public class RendezVousController : Controller
    {
        private readonly BdCabinetMedicalContext _context;

        public RendezVousController(BdCabinetMedicalContext context)
        {
            _context = context;
        }

        // GET: RendezVous
        public async Task<IActionResult> Index()
        {
            var rendezVous = _context.RendezVous
                .Include(r => r.Medecin)
                    .ThenInclude(m => m.IdNavigation)
                .Include(r => r.Patient)
                    .ThenInclude(p => p.IdNavigation);

            return View(await rendezVous.ToListAsync());
        }

        // GET: RendezVous/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var rendezVou = await _context.RendezVous
                .Include(r => r.Medecin)
                    .ThenInclude(m => m.IdNavigation)
                .Include(r => r.Patient)
                    .ThenInclude(p => p.IdNavigation)
                .FirstOrDefaultAsync(m => m.NumCom == id);

            if (rendezVou == null) return NotFound();

            return View(rendezVou);
        }

        // GET: RendezVous/Create
        public IActionResult Create()
        {
            PopulateDropdowns();
            return View();
        }

        // POST: RendezVous/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("NumCom,DateHeure,Statut,MedecinId,PatientId")] RendezVou rendezVou)
        {
            // Retirer la validation des propriétés de navigation
            ModelState.Remove("Medecin");
            ModelState.Remove("Patient");

            // VALIDATION CÔTÉ SERVEUR : Vérifier que la date est dans le futur
            if (rendezVou.DateHeure <= DateTime.Now)
            {
                ModelState.AddModelError("DateHeure", "La date et l'heure du rendez-vous doivent être supérieures à maintenant.");
            }

            if (ModelState.IsValid)
            {
                _context.Add(rendezVou);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Rendez-vous créé avec succès !";
                return RedirectToAction(nameof(Index));
            }

            PopulateDropdowns(rendezVou.MedecinId, rendezVou.PatientId);
            return View(rendezVou);
        }

        // GET: RendezVous/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var rendezVou = await _context.RendezVous.FindAsync(id);
            if (rendezVou == null) return NotFound();

            PopulateDropdowns(rendezVou.MedecinId, rendezVou.PatientId);
            return View(rendezVou);
        }

        // POST: RendezVous/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("NumCom,DateHeure,Statut,MedecinId,PatientId")] RendezVou rendezVou)
        {
            if (id != rendezVou.NumCom) return NotFound();

            // Retirer la validation des propriétés de navigation
            ModelState.Remove("Medecin");
            ModelState.Remove("Patient");

            // VALIDATION CÔTÉ SERVEUR : Vérifier que la date est dans le futur
            if (rendezVou.DateHeure <= DateTime.Now)
            {
                ModelState.AddModelError("DateHeure", "La date et l'heure du rendez-vous doivent être supérieures à maintenant.");
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(rendezVou);
                    await _context.SaveChangesAsync();
                    TempData["Success"] = "Rendez-vous modifié avec succès !";
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!RendezVouExists(rendezVou.NumCom)) return NotFound();
                    else throw;
                }
                return RedirectToAction(nameof(Index));
            }
            PopulateDropdowns(rendezVou.MedecinId, rendezVou.PatientId);
            return View(rendezVou);
        }

        // GET: RendezVous/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var rendezVou = await _context.RendezVous
                .Include(r => r.Medecin)
                    .ThenInclude(m => m.IdNavigation)
                .Include(r => r.Patient)
                    .ThenInclude(p => p.IdNavigation)
                .FirstOrDefaultAsync(m => m.NumCom == id);

            if (rendezVou == null) return NotFound();

            return View(rendezVou);
        }

        // POST: RendezVous/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var rendezVou = await _context.RendezVous.FindAsync(id);
            if (rendezVou != null)
            {
                _context.RendezVous.Remove(rendezVou);
            }
            await _context.SaveChangesAsync();
            TempData["Success"] = "Rendez-vous supprimé avec succès !";
            return RedirectToAction(nameof(Index));
        }

        private bool RendezVouExists(int id)
        {
            return _context.RendezVous.Any(e => e.NumCom == id);
        }

        // HELPER METHOD pour les listes déroulantes avec Noms
        private void PopulateDropdowns(object selectedMedecin = null, object selectedPatient = null)
        {
            var medecinsQuery = _context.Medecins
                .Include(m => m.IdNavigation)
                .Select(m => new {
                    Id = m.Id,
                    NomComplet = "Dr. " + m.IdNavigation.Nom + " " + m.IdNavigation.Prenom + " (" + m.Specialite + ")"
                }).ToList();

            var patientsQuery = _context.Patients
                .Include(p => p.IdNavigation)
                .Select(p => new {
                    Id = p.Id,
                    NomComplet = p.IdNavigation.Nom + " " + p.IdNavigation.Prenom
                }).ToList();

            ViewData["MedecinId"] = new SelectList(medecinsQuery, "Id", "NomComplet", selectedMedecin);
            ViewData["PatientId"] = new SelectList(patientsQuery, "Id", "NomComplet", selectedPatient);
        }
    }
}
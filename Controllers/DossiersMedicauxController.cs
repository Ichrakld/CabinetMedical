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
            // FIX 1: Added .ThenInclude to load the User info (Nom/Prenom) for both Medecin and Patient
            var dossiers = _context.DossierMedicals
                .Include(d => d.Medecin)
                    .ThenInclude(m => m.IdNavigation)
                .Include(d => d.Patient)
                    .ThenInclude(p => p.IdNavigation);

            return View(await dossiers.ToListAsync());
        }

        // GET: DossiersMedicaux/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            // FIX 1: Applied here as well so Details view shows names
            var dossierMedical = await _context.DossierMedicals
                .Include(d => d.Medecin)
                    .ThenInclude(m => m.IdNavigation)
                .Include(d => d.Patient)
                    .ThenInclude(p => p.IdNavigation)
                .FirstOrDefaultAsync(m => m.NumDossier == id);

            if (dossierMedical == null)
            {
                return NotFound();
            }

            return View(dossierMedical);
        }

        // GET: DossiersMedicaux/Create
        public IActionResult Create()
        {
            // FIX 2: Project data to get Full Names for the Dropdown list
            PopulateDropdowns();
            return View();
        }

        // POST: DossiersMedicaux/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("NumDossier,GroupeSanguin,PatientId,MedecinId")] DossierMedical dossierMedical)
        {
            // IMPORTANT : On retire la validation des objets de navigation.
            // Le formulaire n'envoie que les IDs (PatientId, MedecinId), donc les objets complets sont null.
            // Sans ces deux lignes, ModelState.IsValid sera toujours faux et rien ne s'enregistrera.
            ModelState.Remove("Patient");
            ModelState.Remove("Medecin");

            if (ModelState.IsValid)
            {
                _context.Add(dossierMedical);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }

            // Si le formulaire est invalide (ex: champ manquant), on recharge les listes déroulantes
            // pour que l'utilisateur puisse corriger sans tout perdre.
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

            // FIX 2: Populate dropdowns with names
            PopulateDropdowns(dossierMedical.MedecinId, dossierMedical.PatientId);
            return View(dossierMedical);
        }

        // POST: DossiersMedicaux/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("NumDossier,GroupeSanguin,PatientId,MedecinId")] DossierMedical dossierMedical)
        {
            if (id != dossierMedical.NumDossier)
            {
                return NotFound();
            }

            // CORRECTION : On retire les objets de navigation de la validation
            // car le formulaire n'envoie que les IDs (PatientId, MedecinId)
            ModelState.Remove("Patient");
            ModelState.Remove("Medecin");

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(dossierMedical);
                    await _context.SaveChangesAsync();
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

            // Si ça échoue encore, on recharge les listes
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

            // FIX 1: Applied here so Delete confirmation shows who is being deleted
            var dossierMedical = await _context.DossierMedicals
                .Include(d => d.Medecin)
                    .ThenInclude(m => m.IdNavigation)
                .Include(d => d.Patient)
                    .ThenInclude(p => p.IdNavigation)
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
            var dossierMedical = await _context.DossierMedicals.FindAsync(id);
            if (dossierMedical != null)
            {
                _context.DossierMedicals.Remove(dossierMedical);
            }

            await _context.SaveChangesAsync();
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
                .AsEnumerable() // Évite la projection SQL complexe
                .Select(m => new {
                    m.Id,
                    NomComplet = $"Dr. {m.IdNavigation.Nom} {m.IdNavigation.Prenom} ({m.Specialite})"
                });

            var patients = _context.Patients
                .Include(p => p.IdNavigation)
                .AsEnumerable()
                .Select(p => new {
                    p.Id,
                    NomComplet = $"{p.IdNavigation.Nom} {p.IdNavigation.Prenom}"
                });

            ViewData["MedecinId"] = new SelectList(medecins, "Id", "NomComplet", selectedMedecin);
            ViewData["PatientId"] = new SelectList(patients, "Id", "NomComplet", selectedPatient);
        }
    }
}
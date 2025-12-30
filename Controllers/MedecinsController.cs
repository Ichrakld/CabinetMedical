using GestionCabinetMedical.Models;
using GestionCabinetMedical.Areas.Identity.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace GestionCabinetMedical.Controllers
{
    [Authorize(Roles = "ADMIN")]
    public class MedecinsController : Controller
    {
        private readonly BdCabinetMedicalContext _context;
        private readonly UserManager<Userper> _userManager;

        public MedecinsController(BdCabinetMedicalContext context, UserManager<Userper> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // GET: Medecins
        public async Task<IActionResult> Index()
        {
            var bdCabinetMedicalContext = _context.Medecins.Include(m => m.IdNavigation);
            return View(await bdCabinetMedicalContext.ToListAsync());
        }

        // GET: Medecins/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            // Charger le médecin avec toutes ses relations
            var medecin = await _context.Medecins
                .Include(m => m.IdNavigation)
                .Include(m => m.RendezVous)
                    .ThenInclude(r => r.Patient)
                        .ThenInclude(p => p.IdNavigation)
                .Include(m => m.DossierMedicals)
                    .ThenInclude(d => d.Patient)
                        .ThenInclude(p => p.IdNavigation)
                .Include(m => m.DossierMedicals)
                    .ThenInclude(d => d.Consultations)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (medecin == null)
            {
                return NotFound();
            }

            // Statistiques du médecin
            ViewBag.TotalRdv = medecin.RendezVous?.Count ?? 0;
            ViewBag.RdvAujourdHui = medecin.RendezVous?.Count(r => r.DateHeure.Date == DateTime.Today && r.Statut != "Annulé") ?? 0;
            ViewBag.RdvAVenir = medecin.RendezVous?.Count(r => r.DateHeure > DateTime.Now && r.Statut != "Annulé") ?? 0;
            ViewBag.RdvCeMois = medecin.RendezVous?.Count(r => r.DateHeure.Month == DateTime.Now.Month && r.DateHeure.Year == DateTime.Now.Year) ?? 0;

            ViewBag.TotalPatients = medecin.DossierMedicals?.Select(d => d.PatientId).Distinct().Count() ?? 0;
            ViewBag.TotalDossiers = medecin.DossierMedicals?.Count ?? 0;
            ViewBag.TotalConsultations = medecin.DossierMedicals?.Sum(d => d.Consultations?.Count ?? 0) ?? 0;

            // Prochains RDV (5)
            ViewBag.ProchainsRdv = medecin.RendezVous?
                .Where(r => r.DateHeure > DateTime.Now && r.Statut != "Annulé")
                .OrderBy(r => r.DateHeure)
                .Take(5)
                .ToList();

            // RDV d'aujourd'hui
            ViewBag.RdvDuJour = medecin.RendezVous?
                .Where(r => r.DateHeure.Date == DateTime.Today && r.Statut != "Annulé")
                .OrderBy(r => r.DateHeure)
                .ToList();

            // Patients récents (5)
            ViewBag.PatientsRecents = medecin.DossierMedicals?
                .OrderByDescending(d => d.NumDossier)
                .Select(d => d.Patient)
                .Where(p => p != null)
                .DistinctBy(p => p.Id)
                .Take(5)
                .ToList();

            return View(medecin);
        }

        // GET: Medecins/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Medecins/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Medecin medecin)
        {
            if (ModelState.IsValid)
            {
                // ===============================================
                // ÉTAPE 1 : Créer l'utilisateur dans ASP.NET Identity
                // ===============================================
                var identityUser = new Userper
                {
                    UserName = medecin.IdNavigation.Email,
                    Email = medecin.IdNavigation.Email,
                    Nom = $"Dr. {medecin.IdNavigation.Nom} {medecin.IdNavigation.Prenom}",
                    EmailConfirmed = true,
                    PhoneNumber = medecin.IdNavigation.Telephone
                };

                // Créer l'utilisateur avec le mot de passe fourni
                var result = await _userManager.CreateAsync(identityUser, medecin.IdNavigation.MotDePasse);

                if (result.Succeeded)
                {
                    // Assigner le rôle MEDECIN
                    await _userManager.AddToRoleAsync(identityUser, "MEDECIN");

                    // ===============================================
                    // ÉTAPE 2 : Créer dans la base métier
                    // ===============================================
                    if (medecin.IdNavigation != null)
                    {
                        medecin.IdNavigation.EstActif = true;
                    }

                    _context.Add(medecin);
                    await _context.SaveChangesAsync();

                    TempData["Success"] = $"Dr. {medecin.IdNavigation.Nom} créé avec succès ! Il peut maintenant se connecter avec l'email: {medecin.IdNavigation.Email}";
                    return RedirectToAction(nameof(Index));
                }
                else
                {
                    // Afficher les erreurs de création Identity
                    foreach (var error in result.Errors)
                    {
                        ModelState.AddModelError(string.Empty, error.Description);
                    }
                }
            }

            return View(medecin);
        }

        // GET: Medecins/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var medecin = await _context.Medecins
                .Include(m => m.IdNavigation)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (medecin == null)
            {
                return NotFound();
            }
            return View(medecin);
        }

        // POST: Medecins/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Medecin medecin)
        {
            if (id != medecin.Id)
            {
                return NotFound();
            }

            ModelState.Remove("IdNavigation.MotDePasse");
            ModelState.Remove("IdNavigation.Admin");
            ModelState.Remove("IdNavigation.Medecin");
            ModelState.Remove("IdNavigation.Patient");
            ModelState.Remove("IdNavigation.Secretaire");

            if (ModelState.IsValid)
            {
                try
                {
                    var medecinExist = await _context.Medecins
                        .Include(m => m.IdNavigation)
                        .FirstOrDefaultAsync(m => m.Id == id);

                    if (medecinExist == null)
                    {
                        return NotFound();
                    }

                    // Récupérer l'ancien email
                    var ancienEmail = medecinExist.IdNavigation.Email;

                    // Mettre à jour la base métier
                    medecinExist.Specialite = medecin.Specialite;
                    medecinExist.IdNavigation.Nom = medecin.IdNavigation.Nom;
                    medecinExist.IdNavigation.Prenom = medecin.IdNavigation.Prenom;
                    medecinExist.IdNavigation.Email = medecin.IdNavigation.Email;
                    medecinExist.IdNavigation.Telephone = medecin.IdNavigation.Telephone;

                    // ===============================================
                    // SYNCHRONISER avec ASP.NET Identity
                    // ===============================================
                    var identityUser = await _userManager.FindByEmailAsync(ancienEmail);
                    if (identityUser != null)
                    {
                        identityUser.Email = medecin.IdNavigation.Email;
                        identityUser.UserName = medecin.IdNavigation.Email;
                        identityUser.Nom = $"Dr. {medecin.IdNavigation.Nom} {medecin.IdNavigation.Prenom}";
                        identityUser.PhoneNumber = medecin.IdNavigation.Telephone;
                        await _userManager.UpdateAsync(identityUser);
                    }

                    _context.Update(medecinExist);
                    await _context.SaveChangesAsync();

                    TempData["Success"] = "Médecin modifié avec succès !";
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!MedecinExists(medecin.Id))
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

            return View(medecin);
        }

        // GET: Medecins/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var medecin = await _context.Medecins
                .Include(m => m.IdNavigation)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (medecin == null)
            {
                return NotFound();
            }

            return View(medecin);
        }

        // POST: Medecins/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var medecin = await _context.Medecins
                .Include(m => m.IdNavigation)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (medecin != null)
            {
                // ===============================================
                // SUPPRIMER aussi l'utilisateur ASP.NET Identity
                // ===============================================
                var identityUser = await _userManager.FindByEmailAsync(medecin.IdNavigation.Email);
                if (identityUser != null)
                {
                    await _userManager.DeleteAsync(identityUser);
                }

                // Supprimer de la base métier
                var utilisateur = await _context.Utilisateurs.FindAsync(id);
                if (utilisateur != null)
                {
                    _context.Utilisateurs.Remove(utilisateur);
                }
            }

            await _context.SaveChangesAsync();
            TempData["Success"] = "Médecin supprimé avec succès !";
            return RedirectToAction(nameof(Index));
        }

        private bool MedecinExists(int id)
        {
            return _context.Medecins.Any(e => e.Id == id);
        }
    }
}
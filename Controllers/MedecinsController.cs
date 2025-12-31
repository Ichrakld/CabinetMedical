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

            ViewBag.ProchainsRdv = medecin.RendezVous?
                .Where(r => r.DateHeure > DateTime.Now && r.Statut != "Annulé")
                .OrderBy(r => r.DateHeure)
                .Take(5)
                .ToList();

            ViewBag.RdvDuJour = medecin.RendezVous?
                .Where(r => r.DateHeure.Date == DateTime.Today && r.Statut != "Annulé")
                .OrderBy(r => r.DateHeure)
                .ToList();

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
                // 1. Créer l'utilisateur dans ASP.NET Identity
                var identityUser = new Userper
                {
                    UserName = medecin.IdNavigation.Email,
                    Email = medecin.IdNavigation.Email,
                    Nom = $"Dr. {medecin.IdNavigation.Nom} {medecin.IdNavigation.Prenom}",
                    EmailConfirmed = true,
                    PhoneNumber = medecin.IdNavigation.Telephone
                };

                var result = await _userManager.CreateAsync(identityUser, medecin.IdNavigation.MotDePasse);

                if (result.Succeeded)
                {
                    await _userManager.AddToRoleAsync(identityUser, "MEDECIN");

                    // 2. Créer dans la base métier
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

                    var ancienEmail = medecinExist.IdNavigation.Email;

                    // Mettre à jour la base métier
                    medecinExist.Specialite = medecin.Specialite;
                    medecinExist.IdNavigation.Nom = medecin.IdNavigation.Nom;
                    medecinExist.IdNavigation.Prenom = medecin.IdNavigation.Prenom;
                    medecinExist.IdNavigation.Email = medecin.IdNavigation.Email;
                    medecinExist.IdNavigation.Telephone = medecin.IdNavigation.Telephone;

                    // Synchroniser avec ASP.NET Identity
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

            // Compter les données associées
            var rdvCount = await _context.RendezVous.CountAsync(r => r.MedecinId == id);
            var dossierCount = await _context.DossierMedicals.CountAsync(d => d.MedecinId == id);

            ViewBag.RdvCount = rdvCount;
            ViewBag.DossierCount = dossierCount;

            return View(medecin);
        }

        // POST: Medecins/Delete/5 - CORRIGÉ AVEC SUPPRESSION IDENTITY
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                var medecin = await _context.Medecins
                    .Include(m => m.IdNavigation)
                    .FirstOrDefaultAsync(m => m.Id == id);

                if (medecin != null)
                {
                    var email = medecin.IdNavigation.Email;

                    // 1. Supprimer les notifications liées aux RDV du médecin
                    var rdvIds = await _context.RendezVous
                        .Where(r => r.MedecinId == id)
                        .Select(r => r.NumCom)
                        .ToListAsync();

                    if (rdvIds.Any())
                    {
                        var notifications = await _context.Notifications
                            .Where(n => rdvIds.Contains(n.RendezVousId ?? 0))
                            .ToListAsync();

                        if (notifications.Any())
                        {
                            _context.Notifications.RemoveRange(notifications);
                        }
                    }

                    // 2. Supprimer les notifications de l'utilisateur
                    var userNotifications = await _context.Notifications
                        .Where(n => n.UserId == id)
                        .ToListAsync();

                    if (userNotifications.Any())
                    {
                        _context.Notifications.RemoveRange(userNotifications);
                    }

                    // 3. Supprimer les RDV du médecin
                    var rendezVous = await _context.RendezVous
                        .Where(r => r.MedecinId == id)
                        .ToListAsync();

                    if (rendezVous.Any())
                    {
                        _context.RendezVous.RemoveRange(rendezVous);
                    }

                    // 4. Gérer les dossiers médicaux (réassigner ou supprimer)
                    var dossiers = await _context.DossierMedicals
                        .Where(d => d.MedecinId == id)
                        .Include(d => d.Consultations)
                            .ThenInclude(c => c.Traitements)
                        .ToListAsync();

                    foreach (var dossier in dossiers)
                    {
                        // Supprimer les traitements
                        foreach (var consultation in dossier.Consultations)
                        {
                            if (consultation.Traitements?.Any() == true)
                            {
                                _context.Traitements.RemoveRange(consultation.Traitements);
                            }
                        }

                        // Supprimer les consultations
                        if (dossier.Consultations?.Any() == true)
                        {
                            _context.Consultations.RemoveRange(dossier.Consultations);
                        }
                    }

                    // Supprimer les dossiers
                    if (dossiers.Any())
                    {
                        _context.DossierMedicals.RemoveRange(dossiers);
                    }

                    await _context.SaveChangesAsync();

                    // 5. Supprimer le médecin
                    _context.Medecins.Remove(medecin);
                    await _context.SaveChangesAsync();

                    // 6. Supprimer l'utilisateur de la base métier
                    var utilisateur = await _context.Utilisateurs.FindAsync(id);
                    if (utilisateur != null)
                    {
                        _context.Utilisateurs.Remove(utilisateur);
                        await _context.SaveChangesAsync();
                    }

                    // 7. IMPORTANT: Supprimer l'utilisateur ASP.NET Identity
                    var identityUser = await _userManager.FindByEmailAsync(email);
                    if (identityUser != null)
                    {
                        await _userManager.DeleteAsync(identityUser);
                    }

                    await transaction.CommitAsync();
                    TempData["Success"] = "Médecin et toutes ses données associées supprimés avec succès !";
                }
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                TempData["Error"] = $"Erreur lors de la suppression : {ex.Message}";
            }

            return RedirectToAction(nameof(Index));
        }

        private bool MedecinExists(int id)
        {
            return _context.Medecins.Any(e => e.Id == id);
        }
    }
}
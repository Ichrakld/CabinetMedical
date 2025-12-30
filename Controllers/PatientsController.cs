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
    [Authorize(Roles = "ADMIN,MEDECIN,SECRETAIRE")]
    public class PatientsController : Controller
    {
        private readonly BdCabinetMedicalContext _context;
        private readonly UserManager<Userper> _userManager;

        public PatientsController(BdCabinetMedicalContext context, UserManager<Userper> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // GET: Patients
        public async Task<IActionResult> Index()
        {
            var bdCabinetMedicalContext = _context.Patients.Include(p => p.IdNavigation);
            return View(await bdCabinetMedicalContext.ToListAsync());
        }

        // GET: Patients/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            // Charger le patient avec TOUTES ses relations
            var patient = await _context.Patients
                .Include(p => p.IdNavigation)
                .Include(p => p.RendezVous)
                    .ThenInclude(r => r.Medecin)
                        .ThenInclude(m => m.IdNavigation)
                .Include(p => p.DossierMedicals)
                    .ThenInclude(d => d.Medecin)
                        .ThenInclude(m => m.IdNavigation)
                .Include(p => p.DossierMedicals)
                    .ThenInclude(d => d.Consultations)
                        .ThenInclude(c => c.Traitements)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (patient == null)
            {
                return NotFound();
            }

            // Statistiques du patient
            ViewBag.TotalRdv = patient.RendezVous?.Count ?? 0;
            ViewBag.RdvAVenir = patient.RendezVous?.Count(r => r.DateHeure > DateTime.Now && r.Statut != "Annulé") ?? 0;
            ViewBag.RdvPasses = patient.RendezVous?.Count(r => r.DateHeure <= DateTime.Now) ?? 0;
            ViewBag.RdvAnnules = patient.RendezVous?.Count(r => r.Statut == "Annulé") ?? 0;

            ViewBag.TotalDossiers = patient.DossierMedicals?.Count ?? 0;
            ViewBag.TotalConsultations = patient.DossierMedicals?.Sum(d => d.Consultations?.Count ?? 0) ?? 0;
            ViewBag.TotalTraitements = patient.DossierMedicals?
                .SelectMany(d => d.Consultations ?? Enumerable.Empty<Consultation>())
                .Sum(c => c.Traitements?.Count ?? 0) ?? 0;

            // Prochain RDV
            ViewBag.ProchainRdv = patient.RendezVous?
                .Where(r => r.DateHeure > DateTime.Now && r.Statut != "Annulé")
                .OrderBy(r => r.DateHeure)
                .FirstOrDefault();

            // Dernier RDV
            ViewBag.DernierRdv = patient.RendezVous?
                .Where(r => r.DateHeure <= DateTime.Now)
                .OrderByDescending(r => r.DateHeure)
                .FirstOrDefault();

            // Dernière consultation
            ViewBag.DerniereConsultation = patient.DossierMedicals?
                .SelectMany(d => d.Consultations ?? Enumerable.Empty<Consultation>())
                .OrderByDescending(c => c.DateConsultation)
                .FirstOrDefault();

            // Médecins traitants (distincts)
            ViewBag.MedecinsTraitants = patient.DossierMedicals?
                .Select(d => d.Medecin)
                .Where(m => m != null)
                .DistinctBy(m => m.Id)
                .ToList();

            // Calcul de l'âge si date de naissance disponible
            // ViewBag.Age = CalculerAge(patient.DateNaissance);

            return View(patient);
        }

        // GET: Patients/Create
        public IActionResult Create()
        {
            var patient = new Patient
            {
                IdNavigation = new Utilisateur
                {
                    EstActif = true
                }
            };

            return View(patient);
        }

        // POST: Patients/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Patient patient)
        {
            if (ModelState.IsValid)
            {
                // ===============================================
                // ÉTAPE 1 : Créer l'utilisateur dans ASP.NET Identity
                // ===============================================
                var identityUser = new Userper
                {
                    UserName = patient.IdNavigation.Email,
                    Email = patient.IdNavigation.Email,
                    Nom = $"{patient.IdNavigation.Nom} {patient.IdNavigation.Prenom}",
                    EmailConfirmed = true,
                    PhoneNumber = patient.IdNavigation.Telephone
                };

                // Créer l'utilisateur avec le mot de passe fourni
                var result = await _userManager.CreateAsync(identityUser, patient.IdNavigation.MotDePasse);

                if (result.Succeeded)
                {
                    // Assigner le rôle PATIENT
                    await _userManager.AddToRoleAsync(identityUser, "PATIENT");

                    // ===============================================
                    // ÉTAPE 2 : Créer l'entrée dans la table Utilisateur 
                    // et Patient de votre base métier
                    // ===============================================
                    _context.Add(patient);
                    await _context.SaveChangesAsync();

                    TempData["Success"] = $"Patient {patient.IdNavigation.Nom} créé avec succès ! Il peut maintenant se connecter.";
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

            return View(patient);
        }

        // GET: Patients/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var patient = await _context.Patients
                .Include(p => p.IdNavigation)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (patient == null)
            {
                return NotFound();
            }

            return View(patient);
        }

        // POST: Patients/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Patient patient)
        {
            if (id != patient.Id)
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
                    var patientExist = await _context.Patients
                        .Include(p => p.IdNavigation)
                        .FirstOrDefaultAsync(p => p.Id == id);

                    if (patientExist == null)
                    {
                        return NotFound();
                    }

                    // Récupérer l'ancien email avant modification
                    var ancienEmail = patientExist.IdNavigation.Email;

                    // Mettre à jour les champs
                    patientExist.NumSecuriteSociale = patient.NumSecuriteSociale;
                    patientExist.IdNavigation.Nom = patient.IdNavigation.Nom;
                    patientExist.IdNavigation.Prenom = patient.IdNavigation.Prenom;
                    patientExist.IdNavigation.Email = patient.IdNavigation.Email;
                    patientExist.IdNavigation.Telephone = patient.IdNavigation.Telephone;
                    patientExist.IdNavigation.EstActif = patient.IdNavigation.EstActif;

                    // ===============================================
                    // SYNCHRONISER avec ASP.NET Identity si l'email a changé
                    // ===============================================
                    var identityUser = await _userManager.FindByEmailAsync(ancienEmail);
                    if (identityUser != null)
                    {
                        identityUser.Email = patient.IdNavigation.Email;
                        identityUser.UserName = patient.IdNavigation.Email;
                        identityUser.Nom = $"{patient.IdNavigation.Nom} {patient.IdNavigation.Prenom}";
                        identityUser.PhoneNumber = patient.IdNavigation.Telephone;
                        await _userManager.UpdateAsync(identityUser);
                    }

                    _context.Update(patientExist);
                    await _context.SaveChangesAsync();

                    TempData["Success"] = "Patient modifié avec succès !";
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!PatientExists(patient.Id))
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

            return View(patient);
        }

        // GET: Patients/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var patient = await _context.Patients
                .Include(p => p.IdNavigation)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (patient == null)
            {
                return NotFound();
            }

            // Compter les dépendances pour informer l'utilisateur
            var rdvCount = await _context.RendezVous.CountAsync(r => r.PatientId == id);
            var dossierCount = await _context.DossierMedicals.CountAsync(d => d.PatientId == id);

            ViewBag.RdvCount = rdvCount;
            ViewBag.DossierCount = dossierCount;

            return View(patient);
        }

        // POST: Patients/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            // Utiliser une transaction pour garantir l'intégrité
            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                var patient = await _context.Patients
                    .Include(p => p.IdNavigation)
                    .FirstOrDefaultAsync(p => p.Id == id);

                if (patient != null)
                {
                    // ===============================================
                    // ÉTAPE 1 : Récupérer les DossierMedicals du patient
                    // ===============================================
                    var dossierIds = await _context.DossierMedicals
                        .Where(d => d.PatientId == id)
                        .Select(d => d.NumDossier)
                        .ToListAsync();

                    if (dossierIds.Any())
                    {
                        // ===============================================
                        // ÉTAPE 2 : Récupérer les Consultations de ces dossiers
                        // ===============================================
                        var consultationIds = await _context.Consultations
                            .Where(c => dossierIds.Contains(c.DossierMedicalId))
                            .Select(c => c.NumDetail)
                            .ToListAsync();

                        if (consultationIds.Any())
                        {
                            // ===============================================
                            // ÉTAPE 3 : Supprimer les Traitements de ces consultations
                            // ===============================================
                            var traitements = await _context.Traitements
                                .Where(t => consultationIds.Contains(t.ConsultationId))
                                .ToListAsync();

                            if (traitements.Any())
                            {
                                _context.Traitements.RemoveRange(traitements);
                                await _context.SaveChangesAsync();
                            }

                            // ===============================================
                            // ÉTAPE 4 : Supprimer les Consultations
                            // ===============================================
                            var consultations = await _context.Consultations
                                .Where(c => dossierIds.Contains(c.DossierMedicalId))
                                .ToListAsync();

                            _context.Consultations.RemoveRange(consultations);
                            await _context.SaveChangesAsync();
                        }

                        // ===============================================
                        // ÉTAPE 5 : Supprimer les DossierMedicals
                        // ===============================================
                        var dossiers = await _context.DossierMedicals
                            .Where(d => d.PatientId == id)
                            .ToListAsync();

                        _context.DossierMedicals.RemoveRange(dossiers);
                        await _context.SaveChangesAsync();
                    }

                    // ===============================================
                    // ÉTAPE 6 : Supprimer les Notifications liées aux RendezVous du patient
                    // ===============================================
                    var rdvIds = await _context.RendezVous
                        .Where(r => r.PatientId == id)
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
                            await _context.SaveChangesAsync();
                        }
                    }

                    // ===============================================
                    // ÉTAPE 7 : Supprimer les RendezVous du patient
                    // ===============================================
                    var rendezVous = await _context.RendezVous
                        .Where(r => r.PatientId == id)
                        .ToListAsync();

                    if (rendezVous.Any())
                    {
                        _context.RendezVous.RemoveRange(rendezVous);
                        await _context.SaveChangesAsync();
                    }

                    // ===============================================
                    // ÉTAPE 8 : Supprimer le Patient
                    // ===============================================
                    _context.Patients.Remove(patient);
                    await _context.SaveChangesAsync();

                    // ===============================================
                    // ÉTAPE 9 : Supprimer l'Utilisateur (base métier)
                    // ===============================================
                    var utilisateur = await _context.Utilisateurs.FindAsync(id);
                    if (utilisateur != null)
                    {
                        _context.Utilisateurs.Remove(utilisateur);
                        await _context.SaveChangesAsync();
                    }

                    // ===============================================
                    // ÉTAPE 10 : Supprimer l'utilisateur ASP.NET Identity
                    // ===============================================
                    var identityUser = await _userManager.FindByEmailAsync(patient.IdNavigation.Email);
                    if (identityUser != null)
                    {
                        await _userManager.DeleteAsync(identityUser);
                    }
                }

                // Valider la transaction
                await transaction.CommitAsync();

                TempData["Success"] = "Patient et toutes ses données associées supprimés avec succès !";
            }
            catch (Exception ex)
            {
                // Annuler la transaction en cas d'erreur
                await transaction.RollbackAsync();
                TempData["Error"] = $"Erreur lors de la suppression : {ex.Message}";
            }

            return RedirectToAction(nameof(Index));
        }

        private bool PatientExists(int id)
        {
            return _context.Patients.Any(e => e.Id == id);
        }
    }
}
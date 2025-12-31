using GestionCabinetMedical.Models;
using GestionCabinetMedical.Areas.Identity.Data;
using GestionCabinetMedical.ViewModels;
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

        // ============================================================
        // GET: Patients - MODIFIÉ avec filtres, recherche et statistiques
        // ============================================================
        public async Task<IActionResult> Index(
            string? searchTerm,
            string? statut,
            string? sortBy = "nom",
            int page = 1)
        {
            var pageSize = 10;
            var query = _context.Patients
                .Include(p => p.IdNavigation)
                .Include(p => p.RendezVous)
                .Include(p => p.DossierMedicals)
                .AsQueryable();

            // ===============================================
            // Filtrage par recherche textuelle
            // ===============================================
            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                var term = searchTerm.ToLower();
                query = query.Where(p =>
                    (p.IdNavigation.Nom != null && p.IdNavigation.Nom.ToLower().Contains(term)) ||
                    (p.IdNavigation.Prenom != null && p.IdNavigation.Prenom.ToLower().Contains(term)) ||
                    (p.IdNavigation.Email != null && p.IdNavigation.Email.ToLower().Contains(term)) ||
                    (p.IdNavigation.Telephone != null && p.IdNavigation.Telephone.Contains(term)) ||
                    (p.NumSecuriteSociale != null && p.NumSecuriteSociale.Contains(term))
                );
            }

            // ===============================================
            // Filtrage par statut (basé sur EstActif)
            // ===============================================
            if (!string.IsNullOrWhiteSpace(statut))
            {
                bool estActif = statut.ToLower() == "actif";
                query = query.Where(p => p.IdNavigation.EstActif == estActif);
            }

            // ===============================================
            // Calcul des statistiques (avant pagination)
            // ===============================================
            var allPatients = await _context.Patients
                .Include(p => p.IdNavigation)
                .Include(p => p.RendezVous)
                .ToListAsync();

            var dateLimite30j = DateTime.Now.AddDays(-30);
            var stats = new
            {
                Total = allPatients.Count,
                Actifs = allPatients.Count(p => p.IdNavigation?.EstActif == true),
                Inactifs = allPatients.Count(p => p.IdNavigation?.EstActif != true),
                AvecRdvRecent = allPatients.Count(p =>
                    p.RendezVous != null &&
                    p.RendezVous.Any(r => r.DateHeure >= dateLimite30j))
            };

            // ===============================================
            // Tri
            // ===============================================
            query = sortBy?.ToLower() switch
            {
                "rdv" => query.OrderByDescending(p =>
                    p.RendezVous.OrderByDescending(r => r.DateHeure).FirstOrDefault().DateHeure),
                "nss" => query.OrderBy(p => p.NumSecuriteSociale),
                _ => query.OrderBy(p => p.IdNavigation.Nom).ThenBy(p => p.IdNavigation.Prenom)
            };

            // ===============================================
            // Pagination
            // ===============================================
            var totalItems = await query.CountAsync();
            var totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);
            page = Math.Max(1, Math.Min(page, Math.Max(1, totalPages)));

            var patients = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(p => new PatientListItem
                {
                    Id = p.Id,
                    Nom = p.IdNavigation.Nom,
                    Prenom = p.IdNavigation.Prenom,
                    Email = p.IdNavigation.Email,
                    Telephone = p.IdNavigation.Telephone,
                    NumSecuriteSociale = p.NumSecuriteSociale,
                    EstActif = p.IdNavigation.EstActif,
                    DernierRdv = p.RendezVous
                        .OrderByDescending(r => r.DateHeure)
                        .Select(r => (DateTime?)r.DateHeure)
                        .FirstOrDefault(),
                    NombreRdv = p.RendezVous.Count,
                    NombreDossiers = p.DossierMedicals.Count,
                    IdNavigation = p.IdNavigation
                })
                .ToListAsync();

            var viewModel = new PatientIndexViewModel
            {
                Patients = patients,
                TotalPatients = stats.Total,
                PatientsActifs = stats.Actifs,
                PatientsInactifs = stats.Inactifs,
                PatientsAvecRdvRecent = stats.AvecRdvRecent,
                SearchTerm = searchTerm,
                Statut = statut,
                SortBy = sortBy,
                CurrentPage = page,
                PageSize = pageSize,
                TotalPages = totalPages
            };

            return View(viewModel);
        }

        // ============================================================
        // Les autres actions restent INCHANGÉES
        // ============================================================

        // GET: Patients/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

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

            ViewBag.ProchainRdv = patient.RendezVous?
                .Where(r => r.DateHeure > DateTime.Now && r.Statut != "Annulé")
                .OrderBy(r => r.DateHeure)
                .FirstOrDefault();

            ViewBag.DernierRdv = patient.RendezVous?
                .Where(r => r.DateHeure <= DateTime.Now)
                .OrderByDescending(r => r.DateHeure)
                .FirstOrDefault();

            ViewBag.DerniereConsultation = patient.DossierMedicals?
                .SelectMany(d => d.Consultations ?? Enumerable.Empty<Consultation>())
                .OrderByDescending(c => c.DateConsultation)
                .FirstOrDefault();

            ViewBag.MedecinsTraitants = patient.DossierMedicals?
                .Select(d => d.Medecin)
                .Where(m => m != null)
                .DistinctBy(m => m.Id)
                .ToList();

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
                var identityUser = new Userper
                {
                    UserName = patient.IdNavigation.Email,
                    Email = patient.IdNavigation.Email,
                    Nom = $"{patient.IdNavigation.Nom} {patient.IdNavigation.Prenom}",
                    EmailConfirmed = true,
                    PhoneNumber = patient.IdNavigation.Telephone
                };

                var result = await _userManager.CreateAsync(identityUser, patient.IdNavigation.MotDePasse);

                if (result.Succeeded)
                {
                    await _userManager.AddToRoleAsync(identityUser, "PATIENT");

                    _context.Add(patient);
                    await _context.SaveChangesAsync();

                    TempData["Success"] = $"Patient {patient.IdNavigation.Nom} créé avec succès ! Il peut maintenant se connecter.";
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

                    var ancienEmail = patientExist.IdNavigation.Email;

                    patientExist.NumSecuriteSociale = patient.NumSecuriteSociale;
                    patientExist.IdNavigation.Nom = patient.IdNavigation.Nom;
                    patientExist.IdNavigation.Prenom = patient.IdNavigation.Prenom;
                    patientExist.IdNavigation.Email = patient.IdNavigation.Email;
                    patientExist.IdNavigation.Telephone = patient.IdNavigation.Telephone;
                    patientExist.IdNavigation.EstActif = patient.IdNavigation.EstActif;

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
            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                var patient = await _context.Patients
                    .Include(p => p.IdNavigation)
                    .FirstOrDefaultAsync(p => p.Id == id);

                if (patient != null)
                {
                    var dossierIds = await _context.DossierMedicals
                        .Where(d => d.PatientId == id)
                        .Select(d => d.NumDossier)
                        .ToListAsync();

                    if (dossierIds.Any())
                    {
                        var consultationIds = await _context.Consultations
                            .Where(c => dossierIds.Contains(c.DossierMedicalId))
                            .Select(c => c.NumDetail)
                            .ToListAsync();

                        if (consultationIds.Any())
                        {
                            var traitements = await _context.Traitements
                                .Where(t => consultationIds.Contains(t.ConsultationId))
                                .ToListAsync();

                            if (traitements.Any())
                            {
                                _context.Traitements.RemoveRange(traitements);
                                await _context.SaveChangesAsync();
                            }

                            var consultations = await _context.Consultations
                                .Where(c => dossierIds.Contains(c.DossierMedicalId))
                                .ToListAsync();

                            _context.Consultations.RemoveRange(consultations);
                            await _context.SaveChangesAsync();
                        }

                        var dossiers = await _context.DossierMedicals
                            .Where(d => d.PatientId == id)
                            .ToListAsync();

                        _context.DossierMedicals.RemoveRange(dossiers);
                        await _context.SaveChangesAsync();
                    }

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

                    var rendezVous = await _context.RendezVous
                        .Where(r => r.PatientId == id)
                        .ToListAsync();

                    if (rendezVous.Any())
                    {
                        _context.RendezVous.RemoveRange(rendezVous);
                        await _context.SaveChangesAsync();
                    }

                    _context.Patients.Remove(patient);
                    await _context.SaveChangesAsync();

                    var utilisateur = await _context.Utilisateurs.FindAsync(id);
                    if (utilisateur != null)
                    {
                        _context.Utilisateurs.Remove(utilisateur);
                        await _context.SaveChangesAsync();
                    }

                    var identityUser = await _userManager.FindByEmailAsync(patient.IdNavigation.Email);
                    if (identityUser != null)
                    {
                        await _userManager.DeleteAsync(identityUser);
                    }
                }

                await transaction.CommitAsync();

                TempData["Success"] = "Patient et toutes ses données associées supprimés avec succès !";
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                TempData["Error"] = $"Erreur lors de la suppression : {ex.Message}";
            }

            return RedirectToAction(nameof(Index));
        }

        // ============================================================
        // AJAX: Toggle patient status (activer/désactiver)
        // ============================================================
        [HttpPost]
        public async Task<IActionResult> ToggleStatus(int id)
        {
            var patient = await _context.Patients
                .Include(p => p.IdNavigation)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (patient == null) return NotFound();

            patient.IdNavigation.EstActif = !patient.IdNavigation.EstActif;
            await _context.SaveChangesAsync();

            return Json(new
            {
                success = true,
                estActif = patient.IdNavigation.EstActif,
                message = patient.IdNavigation.EstActif ? "Patient activé" : "Patient désactivé"
            });
        }

        private bool PatientExists(int id)
        {
            return _context.Patients.Any(e => e.Id == id);
        }
    }
}
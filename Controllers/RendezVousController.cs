using GestionCabinetMedical.Models;
using GestionCabinetMedical.Helpers;
using GestionCabinetMedical.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using ClosedXML.Excel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Security.Claims;

namespace GestionCabinetMedical.Controllers
{
    [Authorize(Roles = "ADMIN,MEDECIN,SECRETAIRE,PATIENT")]
    public class RendezVousController : Controller
    {
        private readonly BdCabinetMedicalContext _context;
        private readonly ICrudNotificationHelper _notificationHelper;
        private readonly INotificationService _notificationService;
        private const int PageSize = 10;

        public RendezVousController(
            BdCabinetMedicalContext context,
            ICrudNotificationHelper notificationHelper,
            INotificationService notificationService)
        {
            _context = context;
            _notificationHelper = notificationHelper;
            _notificationService = notificationService;
        }

        // ============================================================
        // Helper : Récupérer l'ID utilisateur connecté
        // ============================================================
        private async Task<int> GetCurrentUserIdAsync()
        {
            var email = User.FindFirst(ClaimTypes.Email)?.Value
                     ?? User.FindFirst(ClaimTypes.Name)?.Value;

            if (string.IsNullOrEmpty(email))
                return 0;

            var utilisateur = await _context.Utilisateurs
                .FirstOrDefaultAsync(u => u.Email == email);

            return utilisateur?.Id ?? 0;
        }

        // ============================================================
        // Helper : Vérifier si l'utilisateur est un patient
        // ============================================================
        private bool IsPatient()
        {
            return User.IsInRole("PATIENT") && !User.IsInRole("ADMIN") && !User.IsInRole("MEDECIN") && !User.IsInRole("SECRETAIRE");
        }

        // ============================================================
        // Helper : Vérifier conflit de RDV (même médecin ou patient à la même heure)
        // ============================================================
        private async Task<(bool hasConflict, string message)> CheckRdvConflictAsync(int medecinId, int patientId, DateTime dateHeure, int? excludeRdvId = null)
        {
            // Définir une plage de tolérance (par exemple, 30 minutes avant/après)
            var startTime = dateHeure.AddMinutes(-29);
            var endTime = dateHeure.AddMinutes(29);

            // Vérifier conflit pour le médecin
            var medecinConflict = await _context.RendezVous
                .Where(r => r.MedecinId == medecinId
                         && r.DateHeure >= startTime
                         && r.DateHeure <= endTime
                         && r.Statut != "Annulé"
                         && (excludeRdvId == null || r.NumCom != excludeRdvId))
                .Include(r => r.Patient).ThenInclude(p => p.IdNavigation)
                .FirstOrDefaultAsync();

            if (medecinConflict != null)
            {
                var patientNom = $"{medecinConflict.Patient?.IdNavigation?.Nom} {medecinConflict.Patient?.IdNavigation?.Prenom}";
                return (true, $"Le médecin a déjà un rendez-vous à {medecinConflict.DateHeure:HH:mm} avec {patientNom}. Veuillez choisir un autre créneau.");
            }

            // Vérifier conflit pour le patient
            var patientConflict = await _context.RendezVous
                .Where(r => r.PatientId == patientId
                         && r.DateHeure >= startTime
                         && r.DateHeure <= endTime
                         && r.Statut != "Annulé"
                         && (excludeRdvId == null || r.NumCom != excludeRdvId))
                .Include(r => r.Medecin).ThenInclude(m => m.IdNavigation)
                .FirstOrDefaultAsync();

            if (patientConflict != null)
            {
                var medecinNom = $"Dr. {patientConflict.Medecin?.IdNavigation?.Nom}";
                return (true, $"Le patient a déjà un rendez-vous à {patientConflict.DateHeure:HH:mm} avec {medecinNom}. Veuillez choisir un autre créneau.");
            }

            return (false, string.Empty);
        }

        // ============================================================
        // GET: RendezVous avec filtres et pagination
        // RESTRICTION: Les patients ne voient que leurs propres RDV
        // ============================================================
        public async Task<IActionResult> Index(
            string? search,
            string? statut,
            string? dateDebut,
            string? dateFin,
            int? medecinId,
            string? periode,
            string? sortBy,
            string? sortOrder,
            int page = 1)
        {
            var query = _context.RendezVous
                .Include(r => r.Medecin).ThenInclude(m => m.IdNavigation)
                .Include(r => r.Patient).ThenInclude(p => p.IdNavigation)
                .AsQueryable();

            // ========== RESTRICTION PATIENT ==========
            if (IsPatient())
            {
                var currentUserId = await GetCurrentUserIdAsync();
                query = query.Where(r => r.PatientId == currentUserId);
            }

            // Filtre par période rapide
            var today = DateTime.Today;
            if (!string.IsNullOrEmpty(periode))
            {
                switch (periode)
                {
                    case "aujourd'hui":
                        query = query.Where(r => r.DateHeure.Date == today);
                        break;
                    case "semaine":
                        var startOfWeek = today.AddDays(-(int)today.DayOfWeek);
                        var endOfWeek = startOfWeek.AddDays(7);
                        query = query.Where(r => r.DateHeure >= startOfWeek && r.DateHeure < endOfWeek);
                        break;
                    case "mois":
                        var startOfMonth = new DateTime(today.Year, today.Month, 1);
                        var endOfMonth = startOfMonth.AddMonths(1);
                        query = query.Where(r => r.DateHeure >= startOfMonth && r.DateHeure < endOfMonth);
                        break;
                }
            }

            // Filtre par recherche textuelle
            if (!string.IsNullOrEmpty(search))
            {
                search = search.ToLower();
                query = query.Where(r =>
                    r.Patient.IdNavigation.Nom.ToLower().Contains(search) ||
                    r.Patient.IdNavigation.Prenom.ToLower().Contains(search) ||
                    r.Medecin.IdNavigation.Nom.ToLower().Contains(search) ||
                    r.Medecin.IdNavigation.Prenom.ToLower().Contains(search));
            }

            // Filtre par statut
            if (!string.IsNullOrEmpty(statut))
            {
                query = query.Where(r => r.Statut == statut);
            }

            // Filtre par date début
            if (!string.IsNullOrEmpty(dateDebut) && DateTime.TryParse(dateDebut, out var dateD))
            {
                query = query.Where(r => r.DateHeure.Date >= dateD);
            }

            // Filtre par date fin
            if (!string.IsNullOrEmpty(dateFin) && DateTime.TryParse(dateFin, out var dateF))
            {
                query = query.Where(r => r.DateHeure.Date <= dateF);
            }

            // Filtre par médecin
            if (medecinId.HasValue)
            {
                query = query.Where(r => r.MedecinId == medecinId.Value);
            }

            // Tri
            sortOrder ??= "asc";
            query = sortBy switch
            {
                "date" => sortOrder == "asc"
                    ? query.OrderBy(r => r.DateHeure)
                    : query.OrderByDescending(r => r.DateHeure),
                "patient" => sortOrder == "asc"
                    ? query.OrderBy(r => r.Patient.IdNavigation.Nom)
                    : query.OrderByDescending(r => r.Patient.IdNavigation.Nom),
                "medecin" => sortOrder == "asc"
                    ? query.OrderBy(r => r.Medecin.IdNavigation.Nom)
                    : query.OrderByDescending(r => r.Medecin.IdNavigation.Nom),
                _ => query.OrderBy(r => r.DateHeure)
            };

            // Pagination
            var totalItems = await query.CountAsync();
            var totalPages = (int)Math.Ceiling(totalItems / (double)PageSize);
            page = Math.Max(1, Math.Min(page, Math.Max(1, totalPages)));

            var items = await query
                .Skip((page - 1) * PageSize)
                .Take(PageSize)
                .ToListAsync();

            // ViewBag pour conserver les filtres
            ViewBag.CurrentSearch = search;
            ViewBag.CurrentStatut = statut;
            ViewBag.DateDebut = dateDebut;
            ViewBag.DateFin = dateFin;
            ViewBag.Periode = periode;
            ViewBag.SortBy = sortBy;
            ViewBag.SortOrder = sortOrder;
            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = totalPages;
            ViewBag.IsPatient = IsPatient();

            // Liste des médecins pour le filtre
            ViewBag.Medecins = new SelectList(
                await _context.Medecins
                    .Include(m => m.IdNavigation)
                    .Select(m => new { m.Id, Nom = "Dr. " + m.IdNavigation.Nom })
                    .ToListAsync(),
                "Id", "Nom", medecinId);

            return View(items);
        }

        // ============================================================
        // GET: RendezVous/Details/5
        // RESTRICTION: Les patients ne peuvent voir que leurs propres RDV
        // ============================================================
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

            // Vérifier l'accès pour les patients
            if (IsPatient())
            {
                var currentUserId = await GetCurrentUserIdAsync();
                if (rendezVou.PatientId != currentUserId)
                {
                    return Forbid();
                }
            }

            return View(rendezVou);
        }

        // ============================================================
        // GET: RendezVous/Create
        // ============================================================
        [Authorize(Roles = "ADMIN,MEDECIN,SECRETAIRE")]
        public IActionResult Create()
        {
            PopulateDropdowns();
            return View();
        }

        // ============================================================
        // POST: RendezVous/Create - AVEC VALIDATION CONFLIT
        // ============================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "ADMIN,MEDECIN,SECRETAIRE")]
        public async Task<IActionResult> Create([Bind("NumCom,DateHeure,Statut,MedecinId,PatientId,Motif")] RendezVou rendezVou)
        {
            ModelState.Remove("Medecin");
            ModelState.Remove("Patient");

            if (rendezVou.DateHeure <= DateTime.Now)
            {
                ModelState.AddModelError("DateHeure", "La date et l'heure du rendez-vous doivent être supérieures à maintenant.");
            }

            // Vérifier les conflits de RDV
            var (hasConflict, conflictMessage) = await CheckRdvConflictAsync(rendezVou.MedecinId, rendezVou.PatientId, rendezVou.DateHeure);
            if (hasConflict)
            {
                ModelState.AddModelError("DateHeure", conflictMessage);
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Add(rendezVou);
                    await _context.SaveChangesAsync();

                    // ====== NOTIFICATIONS ======
                    var rdvComplet = await _context.RendezVous
                        .Include(r => r.Patient).ThenInclude(p => p.IdNavigation)
                        .Include(r => r.Medecin).ThenInclude(m => m.IdNavigation)
                        .FirstOrDefaultAsync(r => r.NumCom == rendezVou.NumCom);

                    if (rdvComplet != null)
                    {
                        var patientNom = $"{rdvComplet.Patient?.IdNavigation?.Nom} {rdvComplet.Patient?.IdNavigation?.Prenom}";
                        var medecinNom = $"Dr. {rdvComplet.Medecin?.IdNavigation?.Nom}";
                        var dateRdv = rdvComplet.DateHeure.ToString("dd/MM/yyyy à HH:mm");

                        // Notification pour le patient
                        var messagePatient = $"Nouveau rendez-vous programmé le {dateRdv} avec {medecinNom}";
                        await _notificationService.CreateNotificationAsync(
                            rdvComplet.NumCom, "Confirmation", messagePatient, rdvComplet.PatientId);

                        // Notification pour le médecin
                        var messageMedecin = $"Nouveau rendez-vous programmé le {dateRdv} avec {patientNom}";
                        await _notificationService.CreateNotificationAsync(
                            rdvComplet.NumCom, "Confirmation", messageMedecin, rdvComplet.MedecinId);

                        // Notification pour l'utilisateur connecté
                        var currentUserId = await GetCurrentUserIdAsync();
                        if (currentUserId > 0)
                        {
                            await _notificationHelper.NotifyCreateSuccessAsync(
                                "Rendez-vous", $"RDV #{rdvComplet.NumCom} créé pour le {dateRdv}", currentUserId);
                        }
                    }

                    TempData["Success"] = "Rendez-vous créé avec succès !";
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    var currentUserId = await GetCurrentUserIdAsync();
                    if (currentUserId > 0)
                    {
                        await _notificationHelper.NotifyCreateErrorAsync("Rendez-vous", ex.Message, currentUserId);
                    }
                    ModelState.AddModelError("", "Erreur lors de la création du rendez-vous.");
                }
            }

            PopulateDropdowns(rendezVou.MedecinId, rendezVou.PatientId);
            return View(rendezVou);
        }

        // ============================================================
        // GET: RendezVous/Edit/5
        // ============================================================
        [Authorize(Roles = "ADMIN,MEDECIN,SECRETAIRE")]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var rendezVou = await _context.RendezVous.FindAsync(id);
            if (rendezVou == null) return NotFound();

            PopulateDropdowns(rendezVou.MedecinId, rendezVou.PatientId);
            return View(rendezVou);
        }

        // ============================================================
        // POST: RendezVous/Edit/5 - AVEC VALIDATION CONFLIT
        // ============================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "ADMIN,MEDECIN,SECRETAIRE")]
        public async Task<IActionResult> Edit(int id, [Bind("NumCom,DateHeure,Statut,MedecinId,PatientId,Motif")] RendezVou rendezVou)
        {
            if (id != rendezVou.NumCom) return NotFound();

            ModelState.Remove("Medecin");
            ModelState.Remove("Patient");

            if (rendezVou.DateHeure <= DateTime.Now && rendezVou.Statut != "Terminé" && rendezVou.Statut != "Annulé")
            {
                ModelState.AddModelError("DateHeure", "La date et l'heure du rendez-vous doivent être supérieures à maintenant.");
            }

            // Vérifier les conflits (en excluant le RDV actuel)
            var (hasConflict, conflictMessage) = await CheckRdvConflictAsync(rendezVou.MedecinId, rendezVou.PatientId, rendezVou.DateHeure, id);
            if (hasConflict)
            {
                ModelState.AddModelError("DateHeure", conflictMessage);
            }

            if (ModelState.IsValid)
            {
                try
                {
                    var ancienRdv = await _context.RendezVous
                        .AsNoTracking()
                        .FirstOrDefaultAsync(r => r.NumCom == id);

                    _context.Update(rendezVou);
                    await _context.SaveChangesAsync();

                    // ====== NOTIFICATIONS ======
                    var rdvComplet = await _context.RendezVous
                        .Include(r => r.Patient).ThenInclude(p => p.IdNavigation)
                        .Include(r => r.Medecin).ThenInclude(m => m.IdNavigation)
                        .FirstOrDefaultAsync(r => r.NumCom == rendezVou.NumCom);

                    if (rdvComplet != null)
                    {
                        var dateRdv = rdvComplet.DateHeure.ToString("dd/MM/yyyy à HH:mm");
                        var patientNom = $"{rdvComplet.Patient?.IdNavigation?.Nom} {rdvComplet.Patient?.IdNavigation?.Prenom}";
                        var medecinNom = $"Dr. {rdvComplet.Medecin?.IdNavigation?.Nom}";

                        if (ancienRdv != null && ancienRdv.Statut != rendezVou.Statut)
                        {
                            if (rendezVou.Statut == "Annulé")
                            {
                                var msgAnnulation = $"Le rendez-vous du {dateRdv} a été annulé";
                                await _notificationService.CreateNotificationAsync(
                                    rdvComplet.NumCom, "Annulation", msgAnnulation, rdvComplet.PatientId);
                                await _notificationService.CreateNotificationAsync(
                                    rdvComplet.NumCom, "Annulation", msgAnnulation, rdvComplet.MedecinId);
                            }
                            else if (rendezVou.Statut == "Confirmé")
                            {
                                var msgConfirm = $"Le rendez-vous du {dateRdv} a été confirmé";
                                await _notificationService.CreateNotificationAsync(
                                    rdvComplet.NumCom, "Confirmation", msgConfirm, rdvComplet.PatientId);
                                await _notificationService.CreateNotificationAsync(
                                    rdvComplet.NumCom, "Confirmation", msgConfirm, rdvComplet.MedecinId);
                            }
                        }
                        else
                        {
                            var msgModif = $"Le rendez-vous du {dateRdv} a été modifié";
                            await _notificationService.CreateNotificationAsync(
                                rdvComplet.NumCom, "Confirmation", msgModif, rdvComplet.PatientId);
                            await _notificationService.CreateNotificationAsync(
                                rdvComplet.NumCom, "Confirmation", msgModif, rdvComplet.MedecinId);
                        }

                        var currentUserId = await GetCurrentUserIdAsync();
                        if (currentUserId > 0)
                        {
                            await _notificationHelper.NotifyUpdateSuccessAsync(
                                "Rendez-vous", $"RDV #{rdvComplet.NumCom} modifié", currentUserId);
                        }
                    }

                    TempData["Success"] = "Rendez-vous modifié avec succès !";
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!RendezVouExists(rendezVou.NumCom)) return NotFound();
                    else throw;
                }
                catch (Exception ex)
                {
                    var currentUserId = await GetCurrentUserIdAsync();
                    if (currentUserId > 0)
                    {
                        await _notificationHelper.NotifyUpdateErrorAsync("Rendez-vous", ex.Message, currentUserId);
                    }
                }
                return RedirectToAction(nameof(Index));
            }
            PopulateDropdowns(rendezVou.MedecinId, rendezVou.PatientId);
            return View(rendezVou);
        }

        // ============================================================
        // GET: RendezVous/Delete/5
        // ============================================================
        [Authorize(Roles = "ADMIN,MEDECIN,SECRETAIRE")]
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

        // ============================================================
        // POST: RendezVous/Delete/5 - AVEC NOTIFICATIONS
        // ============================================================
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "ADMIN,MEDECIN,SECRETAIRE")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var rendezVou = await _context.RendezVous
                .Include(r => r.Patient).ThenInclude(p => p.IdNavigation)
                .Include(r => r.Medecin).ThenInclude(m => m.IdNavigation)
                .FirstOrDefaultAsync(r => r.NumCom == id);

            if (rendezVou != null)
            {
                var dateRdv = rendezVou.DateHeure.ToString("dd/MM/yyyy à HH:mm");
                var patientId = rendezVou.PatientId;
                var medecinId = rendezVou.MedecinId;

                // Supprimer les notifications liées
                var notificationsLiees = await _context.Notifications
                    .Where(n => n.RendezVousId == id)
                    .ToListAsync();
                _context.Notifications.RemoveRange(notificationsLiees);

                _context.RendezVous.Remove(rendezVou);
                await _context.SaveChangesAsync();

                // ====== NOTIFICATIONS ======
                var msgSuppression = $"Le rendez-vous du {dateRdv} a été supprimé";
                await _notificationService.CreateNotificationAsync(null, "Annulation", msgSuppression, patientId);
                await _notificationService.CreateNotificationAsync(null, "Annulation", msgSuppression, medecinId);

                var currentUserId = await GetCurrentUserIdAsync();
                if (currentUserId > 0)
                {
                    await _notificationHelper.NotifyDeleteSuccessAsync("Rendez-vous", $"RDV du {dateRdv}", currentUserId);
                }

                TempData["Success"] = "Rendez-vous supprimé avec succès !";
            }

            return RedirectToAction(nameof(Index));
        }

        // ============================================================
        // POST: RendezVous/ConfirmRdv/5 (AJAX)
        // ============================================================
        [HttpPost]
        [Authorize(Roles = "ADMIN,MEDECIN,SECRETAIRE")]
        public async Task<IActionResult> ConfirmRdv(int id)
        {
            var rdv = await _context.RendezVous
                .Include(r => r.Patient).ThenInclude(p => p.IdNavigation)
                .Include(r => r.Medecin).ThenInclude(m => m.IdNavigation)
                .FirstOrDefaultAsync(r => r.NumCom == id);

            if (rdv != null)
            {
                rdv.Statut = "Confirmé";
                await _context.SaveChangesAsync();

                var dateRdv = rdv.DateHeure.ToString("dd/MM/yyyy à HH:mm");
                var msgConfirm = $"Votre rendez-vous du {dateRdv} a été confirmé";

                await _notificationService.CreateNotificationAsync(rdv.NumCom, "Confirmation", msgConfirm, rdv.PatientId);
                await _notificationService.CreateNotificationAsync(rdv.NumCom, "Confirmation", msgConfirm, rdv.MedecinId);

                return Json(new { success = true, message = "Rendez-vous confirmé" });
            }
            return Json(new { success = false, message = "Rendez-vous non trouvé" });
        }

        // ============================================================
        // POST: RendezVous/CancelRdv/5 (AJAX)
        // ============================================================
        [HttpPost]
        [Authorize(Roles = "ADMIN,MEDECIN,SECRETAIRE")]
        public async Task<IActionResult> CancelRdv(int id, string? raison)
        {
            var rdv = await _context.RendezVous
                .Include(r => r.Patient).ThenInclude(p => p.IdNavigation)
                .Include(r => r.Medecin).ThenInclude(m => m.IdNavigation)
                .FirstOrDefaultAsync(r => r.NumCom == id);

            if (rdv != null)
            {
                rdv.Statut = "Annulé";
                await _context.SaveChangesAsync();

                var dateRdv = rdv.DateHeure.ToString("dd/MM/yyyy à HH:mm");
                var raisonText = string.IsNullOrEmpty(raison) ? "Non spécifiée" : raison;
                var msgAnnul = $"Le rendez-vous du {dateRdv} a été annulé. Raison : {raisonText}";

                await _notificationService.CreateNotificationAsync(rdv.NumCom, "Annulation", msgAnnul, rdv.PatientId);
                await _notificationService.CreateNotificationAsync(rdv.NumCom, "Annulation", msgAnnul, rdv.MedecinId);

                return Json(new { success = true, message = "Rendez-vous annulé" });
            }
            return Json(new { success = false, message = "Rendez-vous non trouvé" });
        }

        // ============================================================
        // AJAX: Vérifier conflit de RDV
        // ============================================================
        [HttpPost]
        public async Task<IActionResult> CheckConflict(int medecinId, int patientId, DateTime dateHeure, int? excludeId = null)
        {
            var (hasConflict, message) = await CheckRdvConflictAsync(medecinId, patientId, dateHeure, excludeId);
            return Json(new { hasConflict, message });
        }

        private bool RendezVouExists(int id)
        {
            return _context.RendezVous.Any(e => e.NumCom == id);
        }

        // ============================================================
        // GET: RendezVous/ExportExcel
        // ============================================================
        [Authorize(Roles = "ADMIN,MEDECIN,SECRETAIRE")]
        public async Task<IActionResult> ExportExcel(
            string? search,
            string? statut,
            string? dateDebut,
            string? dateFin,
            int? medecinId,
            string? periode)
        {
            var query = _context.RendezVous
                .Include(r => r.Patient).ThenInclude(p => p.IdNavigation)
                .Include(r => r.Medecin).ThenInclude(m => m.IdNavigation)
                .AsQueryable();

            var today = DateTime.Today;
            if (!string.IsNullOrEmpty(periode))
            {
                switch (periode)
                {
                    case "aujourd'hui":
                        query = query.Where(r => r.DateHeure.Date == today);
                        break;
                    case "semaine":
                        var startOfWeek = today.AddDays(-(int)today.DayOfWeek);
                        var endOfWeek = startOfWeek.AddDays(7);
                        query = query.Where(r => r.DateHeure >= startOfWeek && r.DateHeure < endOfWeek);
                        break;
                    case "mois":
                        var startOfMonth = new DateTime(today.Year, today.Month, 1);
                        var endOfMonth = startOfMonth.AddMonths(1);
                        query = query.Where(r => r.DateHeure >= startOfMonth && r.DateHeure < endOfMonth);
                        break;
                }
            }

            if (!string.IsNullOrEmpty(search))
            {
                search = search.ToLower();
                query = query.Where(r =>
                    r.Patient.IdNavigation.Nom.ToLower().Contains(search) ||
                    r.Patient.IdNavigation.Prenom.ToLower().Contains(search) ||
                    r.Medecin.IdNavigation.Nom.ToLower().Contains(search));
            }

            if (!string.IsNullOrEmpty(statut))
                query = query.Where(r => r.Statut == statut);

            if (!string.IsNullOrEmpty(dateDebut) && DateTime.TryParse(dateDebut, out var dateD))
                query = query.Where(r => r.DateHeure.Date >= dateD);

            if (!string.IsNullOrEmpty(dateFin) && DateTime.TryParse(dateFin, out var dateF))
                query = query.Where(r => r.DateHeure.Date <= dateF);

            if (medecinId.HasValue)
                query = query.Where(r => r.MedecinId == medecinId.Value);

            var rdvs = await query.OrderBy(r => r.DateHeure).ToListAsync();

            using var workbook = new XLWorkbook();
            var worksheet = workbook.Worksheets.Add("Rendez-vous");

            worksheet.Cell(1, 1).Value = "Liste des Rendez-vous";
            worksheet.Cell(1, 1).Style.Font.Bold = true;
            worksheet.Cell(1, 1).Style.Font.FontSize = 16;
            worksheet.Range(1, 1, 1, 6).Merge();

            worksheet.Cell(2, 1).Value = $"Exporté le {DateTime.Now:dd/MM/yyyy à HH:mm}";
            worksheet.Cell(2, 1).Style.Font.Italic = true;
            worksheet.Range(2, 1, 2, 6).Merge();

            var headerRow = 4;
            worksheet.Cell(headerRow, 1).Value = "N° RDV";
            worksheet.Cell(headerRow, 2).Value = "Date & Heure";
            worksheet.Cell(headerRow, 3).Value = "Patient";
            worksheet.Cell(headerRow, 4).Value = "Médecin";
            worksheet.Cell(headerRow, 5).Value = "Spécialité";
            worksheet.Cell(headerRow, 6).Value = "Statut";

            var headerRange = worksheet.Range(headerRow, 1, headerRow, 6);
            headerRange.Style.Font.Bold = true;
            headerRange.Style.Fill.BackgroundColor = XLColor.FromHtml("#667eea");
            headerRange.Style.Font.FontColor = XLColor.White;
            headerRange.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            headerRange.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;

            var currentRow = headerRow + 1;
            foreach (var rdv in rdvs)
            {
                worksheet.Cell(currentRow, 1).Value = rdv.NumCom;
                worksheet.Cell(currentRow, 2).Value = rdv.DateHeure.ToString("dd/MM/yyyy HH:mm");
                worksheet.Cell(currentRow, 3).Value = $"{rdv.Patient?.IdNavigation?.Nom} {rdv.Patient?.IdNavigation?.Prenom}";
                worksheet.Cell(currentRow, 4).Value = $"Dr. {rdv.Medecin?.IdNavigation?.Nom} {rdv.Medecin?.IdNavigation?.Prenom}";
                worksheet.Cell(currentRow, 5).Value = rdv.Medecin?.Specialite ?? "-";
                worksheet.Cell(currentRow, 6).Value = rdv.Statut;

                var statutCell = worksheet.Cell(currentRow, 6);
                statutCell.Style.Font.Bold = true;
                statutCell.Style.Fill.BackgroundColor = rdv.Statut switch
                {
                    "Confirmé" => XLColor.FromHtml("#d4edda"),
                    "En attente" => XLColor.FromHtml("#fff3cd"),
                    "Annulé" => XLColor.FromHtml("#f8d7da"),
                    "Terminé" => XLColor.FromHtml("#d1ecf1"),
                    _ => XLColor.White
                };

                worksheet.Range(currentRow, 1, currentRow, 6).Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                currentRow++;
            }

            worksheet.Columns().AdjustToContents();

            currentRow += 2;
            worksheet.Cell(currentRow, 1).Value = "Statistiques";
            worksheet.Cell(currentRow, 1).Style.Font.Bold = true;
            currentRow++;
            worksheet.Cell(currentRow, 1).Value = $"Total : {rdvs.Count} rendez-vous";
            currentRow++;
            worksheet.Cell(currentRow, 1).Value = $"Confirmés : {rdvs.Count(r => r.Statut == "Confirmé")}";
            currentRow++;
            worksheet.Cell(currentRow, 1).Value = $"En attente : {rdvs.Count(r => r.Statut == "En attente")}";
            currentRow++;
            worksheet.Cell(currentRow, 1).Value = $"Annulés : {rdvs.Count(r => r.Statut == "Annulé")}";

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            stream.Position = 0;

            var fileName = $"RendezVous_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";
            return File(stream.ToArray(),
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                fileName);
        }
        //public IActionResult Calendrier()
        //{
        //    return View();
        //}
        // ============================================================
        // GET: RendezVous/Calendrier
        // ============================================================
        public async Task<IActionResult> Calendrier()
        {
            ViewBag.Medecins = await _context.Medecins
                .Include(m => m.IdNavigation)
                .Select(m => new { m.Id, Nom = "Dr. " + m.IdNavigation.Nom })
                .ToListAsync();

            ViewBag.IsPatient = IsPatient();

            return View();
        }

       
        // ============================================================
        // GET: RendezVous/GetCalendarEvents (AJAX pour FullCalendar)
        // RESTRICTION: Les patients ne voient que leurs propres RDV
        // ============================================================
        [HttpGet]
        public async Task<IActionResult> GetCalendarEvents(
            string start,      // Format ISO string
            string end,        // Format ISO string
            int? medecinId = null,
            string? status = null)
        {
            // Parser les dates (FullCalendar envoie des strings ISO)
            if (!DateTime.TryParse(start, out var startDate))
                startDate = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);

            if (!DateTime.TryParse(end, out var endDate))
                endDate = startDate.AddMonths(2);

            var query = _context.RendezVous
                .Include(r => r.Patient).ThenInclude(p => p.IdNavigation)
                .Include(r => r.Medecin).ThenInclude(m => m.IdNavigation)
                .Where(r => r.DateHeure >= startDate && r.DateHeure <= endDate);

            // ========== RESTRICTION PATIENT ==========
            if (IsPatient())
            {
                var currentUserId = await GetCurrentUserIdAsync();
                query = query.Where(r => r.PatientId == currentUserId);
            }

            // Filtre par médecin
            if (medecinId.HasValue)
                query = query.Where(r => r.MedecinId == medecinId.Value);

            // Filtre par statut
            if (!string.IsNullOrEmpty(status))
                query = query.Where(r => r.Statut == status);

            try
            {
                var rendezVous = await query.ToListAsync();

                // Transformer en format FullCalendar
                var events = rendezVous.Select(r =>
                {
                    var startRdv = r.DateHeure;
                    var endRdv = startRdv.AddMinutes(30); // Durée par défaut 30 min

                    return new
                    {
                        id = r.NumCom,
                        title = $"{r.Patient?.IdNavigation?.Nom} {r.Patient?.IdNavigation?.Prenom?.Substring(0, Math.Min(1, r.Patient?.IdNavigation?.Prenom?.Length ?? 0))}.",
                        start = startRdv.ToString("yyyy-MM-ddTHH:mm:ss"),
                        end = endRdv.ToString("yyyy-MM-ddTHH:mm:ss"),
                        color = GetStatusColor(r.Statut),
                        borderColor = GetStatusBorderColor(r.Statut),
                        textColor = r.Statut == "En attente" ? "#000" : "#fff",
                        extendedProps = new
                        {
                            patient = $"{r.Patient?.IdNavigation?.Nom} {r.Patient?.IdNavigation?.Prenom}",
                            medecin = $"Dr. {r.Medecin?.IdNavigation?.Nom} {r.Medecin?.IdNavigation?.Prenom}",
                            specialite = r.Medecin?.Specialite ?? "",
                            statut = r.Statut ?? "En attente",
                            patientId = r.PatientId,
                            medecinId = r.MedecinId,
                            motif = r.Motif ?? "Non spécifié"
                        }
                    };
                }).ToList();

                Console.WriteLine($"✅ GetCalendarEvents: {events.Count} événements trouvés entre {startDate:dd/MM/yyyy} et {endDate:dd/MM/yyyy}");

                return Json(events);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Erreur GetCalendarEvents: {ex.Message}");
                return Json(new List<object>());
            }
        }


        // Helper pour la couleur selon le statut
        private string GetStatusColor(string? statut)
        {
            return statut switch
            {
                "Confirmé" => "#28a745",
                "En attente" => "#ffc107",
                "Annulé" => "#dc3545",
                "Terminé" => "#17a2b8",
                _ => "#6c757d"
            };
        }

        private string GetStatusBorderColor(string? statut)
        {
            return statut switch
            {
                "Confirmé" => "#1e7b34",
                "En attente" => "#d39e00",
                "Annulé" => "#bd2130",
                "Terminé" => "#117a8b",
                _ => "#545b62"
            };
        }
        #region ================ ESPACE PATIENT ================

        /// <summary>
        /// GET: RendezVous/MesRendezVous
        /// Liste des RDV du patient connecté uniquement
        /// </summary>
        [Authorize(Roles = "PATIENT")]
        public async Task<IActionResult> MesRendezVous(string? filtre)
        {
            var patientId = await GetCurrentUserIdAsync();
            if (patientId == 0) return RedirectToAction("Login", "Account");

            // Requête de base - UNIQUEMENT les RDV du patient connecté
            var query = _context.RendezVous
                .Include(r => r.Medecin).ThenInclude(m => m.IdNavigation)
                .Where(r => r.PatientId == patientId);

            // Appliquer le filtre
            ViewBag.Filtre = filtre ?? "tous";
            switch (filtre)
            {
                case "avenir":
                    query = query.Where(r => r.DateHeure >= DateTime.Now && r.Statut != "Annulé");
                    break;
                case "attente":
                    query = query.Where(r => r.Statut == "En attente");
                    break;
                case "confirme":
                    query = query.Where(r => r.Statut == "Confirmé");
                    break;
                case "passe":
                    query = query.Where(r => r.DateHeure < DateTime.Now || r.Statut == "Terminé");
                    break;
            }

            var mesRdv = await query.OrderByDescending(r => r.DateHeure).ToListAsync();

            // Statistiques
            var tousRdv = await _context.RendezVous.Where(r => r.PatientId == patientId).ToListAsync();
            ViewBag.MesRdv = mesRdv;
            ViewBag.TotalRdv = tousRdv.Count;
            ViewBag.EnAttente = tousRdv.Count(r => r.Statut == "En attente");
            ViewBag.Confirmes = tousRdv.Count(r => r.Statut == "Confirmé");
            ViewBag.Termines = tousRdv.Count(r => r.Statut == "Terminé");

            // Prochain RDV
            ViewBag.ProchainRdv = await _context.RendezVous
                .Include(r => r.Medecin).ThenInclude(m => m.IdNavigation)
                .Where(r => r.PatientId == patientId && r.DateHeure >= DateTime.Now && r.Statut != "Annulé")
                .OrderBy(r => r.DateHeure)
                .FirstOrDefaultAsync();

            return View();
        }

        /// <summary>
        /// GET: RendezVous/DemanderRendezVous
        /// Formulaire de demande de RDV pour le patient
        /// </summary>
        [Authorize(Roles = "PATIENT")]
        public async Task<IActionResult> DemanderRendezVous()
        {
            ViewBag.Medecins = await _context.Medecins
                .Include(m => m.IdNavigation)
                .Select(m => new {
                    m.Id,
                    Nom = m.IdNavigation.Nom,
                    Prenom = m.IdNavigation.Prenom,
                    m.Specialite
                }).ToListAsync();

            return View(new RendezVou());
        }

        /// <summary>
        /// POST: RendezVous/DemanderRendezVous
        /// Création d'une demande de RDV (statut "En attente")
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "PATIENT")]
        public async Task<IActionResult> DemanderRendezVous(RendezVou model)
        {
            var patientId = await GetCurrentUserIdAsync();
            if (patientId == 0) return RedirectToAction("Login", "Account");

            model.PatientId = patientId;
            model.Statut = "En attente"; // Demande en attente de validation

            // Validation
            if (model.DateHeure <= DateTime.Now)
            {
                ModelState.AddModelError("DateHeure", "La date doit être dans le futur.");
            }

            // Vérifier conflit
            var (hasConflict, msg) = await CheckRdvConflictAsync(model.MedecinId, patientId, model.DateHeure);
            if (hasConflict)
            {
                ModelState.AddModelError("DateHeure", msg);
            }

            ModelState.Remove("PatientId");
            ModelState.Remove("Statut");
            ModelState.Remove("Medecin");
            ModelState.Remove("Patient");

            if (ModelState.IsValid)
            {
                _context.Add(model);
                await _context.SaveChangesAsync();

                // Notification
                var rdvComplet = await _context.RendezVous
                    .Include(r => r.Medecin).ThenInclude(m => m.IdNavigation)
                    .FirstOrDefaultAsync(r => r.NumCom == model.NumCom);

                if (rdvComplet != null && _notificationService != null)
                {
                    var dateStr = rdvComplet.DateHeure.ToString("dd/MM/yyyy à HH:mm");
                    var msgNotif = $"Nouvelle demande de RDV le {dateStr}";
                    await _notificationService.CreateNotificationAsync(rdvComplet.NumCom, "Confirmation", msgNotif, rdvComplet.MedecinId);
                }

                TempData["Success"] = "Votre demande de rendez-vous a été envoyée !";
                return RedirectToAction(nameof(MesRendezVous));
            }

            // Recharger les médecins en cas d'erreur
            ViewBag.Medecins = await _context.Medecins
                .Include(m => m.IdNavigation)
                .Select(m => new {
                    m.Id,
                    Nom = m.IdNavigation.Nom,
                    Prenom = m.IdNavigation.Prenom,
                    m.Specialite
                }).ToListAsync();

            return View(model);
        }

        /// <summary>
        /// GET: RendezVous/ModifierMonRdv/5
        /// Formulaire de modification pour le patient
        /// </summary>
        [Authorize(Roles = "PATIENT")]
        public async Task<IActionResult> ModifierMonRdv(int? id)
        {
            if (id == null) return NotFound();

            var patientId = await GetCurrentUserIdAsync();
            var rdv = await _context.RendezVous
                .Include(r => r.Medecin).ThenInclude(m => m.IdNavigation)
                .FirstOrDefaultAsync(r => r.NumCom == id && r.PatientId == patientId);

            if (rdv == null) return NotFound();

            // Vérifier si modifiable
            if (rdv.DateHeure < DateTime.Now || rdv.Statut == "Annulé" || rdv.Statut == "Terminé")
            {
                TempData["Error"] = "Ce rendez-vous ne peut plus être modifié.";
                return RedirectToAction(nameof(MesRendezVous));
            }

            return View(rdv);
        }

        /// <summary>
        /// POST: RendezVous/ModifierMonRdv/5
        /// Modification du RDV par le patient
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "PATIENT")]
        public async Task<IActionResult> ModifierMonRdv(int id, RendezVou model)
        {
            if (id != model.NumCom) return NotFound();

            var patientId = await GetCurrentUserIdAsync();

            // Vérifier que c'est bien le RDV du patient
            var rdvOriginal = await _context.RendezVous.AsNoTracking()
                .FirstOrDefaultAsync(r => r.NumCom == id && r.PatientId == patientId);
            if (rdvOriginal == null) return NotFound();

            // Validation
            if (model.DateHeure <= DateTime.Now)
            {
                ModelState.AddModelError("DateHeure", "La date doit être dans le futur.");
            }

            ModelState.Remove("Medecin");
            ModelState.Remove("Patient");

            if (ModelState.IsValid)
            {
                try
                {
                    // Garder le patient et médecin originaux
                    model.PatientId = patientId;
                    model.MedecinId = rdvOriginal.MedecinId;

                    _context.Update(model);
                    await _context.SaveChangesAsync();

                    TempData["Success"] = "Votre rendez-vous a été modifié !";
                    return RedirectToAction(nameof(MesRendezVous));
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!RendezVouExists(model.NumCom)) return NotFound();
                    throw;
                }
            }

            // Recharger le médecin en cas d'erreur
            model.Medecin = await _context.Medecins
                .Include(m => m.IdNavigation)
                .FirstOrDefaultAsync(m => m.Id == model.MedecinId);

            return View(model);
        }

        /// <summary>
        /// POST: RendezVous/ConfirmerMonRdv
        /// Le patient confirme sa présence (AJAX)
        /// </summary>
        [HttpPost]
        [Authorize(Roles = "PATIENT")]
        public async Task<IActionResult> ConfirmerMonRdv(int id)
        {
            var patientId = await GetCurrentUserIdAsync();
            var rdv = await _context.RendezVous
                .FirstOrDefaultAsync(r => r.NumCom == id && r.PatientId == patientId);

            if (rdv == null)
                return Json(new { success = false, message = "Rendez-vous non trouvé" });

            if (rdv.DateHeure < DateTime.Now)
                return Json(new { success = false, message = "Ce rendez-vous est déjà passé" });

            if (rdv.Statut == "Confirmé")
                return Json(new { success = false, message = "Déjà confirmé" });

            if (rdv.Statut == "Annulé")
                return Json(new { success = false, message = "Ce rendez-vous a été annulé" });

            rdv.Statut = "Confirmé";
            await _context.SaveChangesAsync();

            // Notification
            if (_notificationService != null)
            {
                var dateStr = rdv.DateHeure.ToString("dd/MM/yyyy à HH:mm");
                await _notificationService.CreateNotificationAsync(rdv.NumCom, "Confirmation",
                    $"Le patient a confirmé sa présence pour le {dateStr}", rdv.MedecinId);
            }

            return Json(new { success = true, message = "Votre présence a été confirmée !" });
        }

        /// <summary>
        /// POST: RendezVous/AnnulerMonRdv
        /// Le patient annule son RDV (AJAX)
        /// </summary>
        [HttpPost]
        [Authorize(Roles = "PATIENT")]
        public async Task<IActionResult> AnnulerMonRdv(int id, string? motif)
        {
            var patientId = await GetCurrentUserIdAsync();
            var rdv = await _context.RendezVous
                .FirstOrDefaultAsync(r => r.NumCom == id && r.PatientId == patientId);

            if (rdv == null)
                return Json(new { success = false, message = "Rendez-vous non trouvé" });

            if (rdv.DateHeure < DateTime.Now)
                return Json(new { success = false, message = "Ce rendez-vous est déjà passé" });

            if (rdv.Statut == "Annulé")
                return Json(new { success = false, message = "Déjà annulé" });

            if (rdv.Statut == "Terminé")
                return Json(new { success = false, message = "Ce rendez-vous est terminé" });

            rdv.Statut = "Annulé";
            if (!string.IsNullOrEmpty(motif))
            {
                rdv.Motif = (rdv.Motif ?? "") + $" [Annulé par patient: {motif}]";
            }

            await _context.SaveChangesAsync();

            // Notification
            if (_notificationService != null)
            {
                var dateStr = rdv.DateHeure.ToString("dd/MM/yyyy à HH:mm");
                var raisonTxt = string.IsNullOrEmpty(motif) ? "" : $" Raison: {motif}";
                await _notificationService.CreateNotificationAsync(rdv.NumCom, "Annulation",
                    $"Le patient a annulé le RDV du {dateStr}.{raisonTxt}", rdv.MedecinId);
            }

            return Json(new { success = true, message = "Votre rendez-vous a été annulé" });
        }

        #endregion
        // HELPER METHOD pour les listes déroulantes
        private void PopulateDropdowns(object? selectedMedecin = null, object? selectedPatient = null)
        {
            var medecinsQuery = _context.Medecins
                .Include(m => m.IdNavigation)
                .Select(m => new
                {
                    Id = m.Id,
                    NomComplet = "Dr. " + m.IdNavigation.Nom + " " + m.IdNavigation.Prenom + " (" + m.Specialite + ")"
                }).ToList();

            var patientsQuery = _context.Patients
                .Include(p => p.IdNavigation)
                .Select(p => new
                {
                    Id = p.Id,
                    NomComplet = p.IdNavigation.Nom + " " + p.IdNavigation.Prenom
                }).ToList();

            ViewData["MedecinId"] = new SelectList(medecinsQuery, "Id", "NomComplet", selectedMedecin);
            ViewData["PatientId"] = new SelectList(patientsQuery, "Id", "NomComplet", selectedPatient);
        }

    }

}
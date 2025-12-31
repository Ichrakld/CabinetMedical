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
        // GET: RendezVous avec filtres et pagination
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

            return View(rendezVou);
        }

        // ============================================================
        // GET: RendezVous/Create
        // ============================================================
        public IActionResult Create()
        {
            PopulateDropdowns();
            return View();
        }

        // ============================================================
        // POST: RendezVous/Create - AVEC NOTIFICATIONS
        // ============================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("NumCom,DateHeure,Statut,MedecinId,PatientId")] RendezVou rendezVou)
        {
            ModelState.Remove("Medecin");
            ModelState.Remove("Patient");

            if (rendezVou.DateHeure <= DateTime.Now)
            {
                ModelState.AddModelError("DateHeure", "La date et l'heure du rendez-vous doivent être supérieures à maintenant.");
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Add(rendezVou);
                    await _context.SaveChangesAsync();

                    // ====== NOTIFICATIONS ======
                    // Charger les infos pour le message
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

                        // Notification pour l'utilisateur connecté (succès)
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
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var rendezVou = await _context.RendezVous.FindAsync(id);
            if (rendezVou == null) return NotFound();

            PopulateDropdowns(rendezVou.MedecinId, rendezVou.PatientId);
            return View(rendezVou);
        }

        // ============================================================
        // POST: RendezVous/Edit/5 - AVEC NOTIFICATIONS
        // ============================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("NumCom,DateHeure,Statut,MedecinId,PatientId")] RendezVou rendezVou)
        {
            if (id != rendezVou.NumCom) return NotFound();

            ModelState.Remove("Medecin");
            ModelState.Remove("Patient");

            if (rendezVou.DateHeure <= DateTime.Now)
            {
                ModelState.AddModelError("DateHeure", "La date et l'heure du rendez-vous doivent être supérieures à maintenant.");
            }

            if (ModelState.IsValid)
            {
                try
                {
                    // Récupérer l'ancien statut pour détecter les changements
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

                        // Si le statut a changé
                        if (ancienRdv != null && ancienRdv.Statut != rendezVou.Statut)
                        {
                            if (rendezVou.Statut == "Annulé")
                            {
                                // Notifications d'annulation
                                var msgAnnulation = $"Le rendez-vous du {dateRdv} a été annulé";
                                await _notificationService.CreateNotificationAsync(
                                    rdvComplet.NumCom, "Annulation", msgAnnulation, rdvComplet.PatientId);
                                await _notificationService.CreateNotificationAsync(
                                    rdvComplet.NumCom, "Annulation", msgAnnulation, rdvComplet.MedecinId);
                            }
                            else if (rendezVou.Statut == "Confirmé")
                            {
                                // Notifications de confirmation
                                var msgConfirm = $"Le rendez-vous du {dateRdv} a été confirmé";
                                await _notificationService.CreateNotificationAsync(
                                    rdvComplet.NumCom, "Confirmation", msgConfirm, rdvComplet.PatientId);
                                await _notificationService.CreateNotificationAsync(
                                    rdvComplet.NumCom, "Confirmation", msgConfirm, rdvComplet.MedecinId);
                            }
                        }
                        else
                        {
                            // Notification de modification générale
                            var msgModif = $"Le rendez-vous du {dateRdv} a été modifié";
                            await _notificationService.CreateNotificationAsync(
                                rdvComplet.NumCom, "Confirmation", msgModif, rdvComplet.PatientId);
                            await _notificationService.CreateNotificationAsync(
                                rdvComplet.NumCom, "Confirmation", msgModif, rdvComplet.MedecinId);
                        }

                        // Notification succès pour l'utilisateur connecté
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

                // Supprimer d'abord les notifications liées
                var notificationsLiees = await _context.Notifications
                    .Where(n => n.RendezVousId == id)
                    .ToListAsync();
                _context.Notifications.RemoveRange(notificationsLiees);

                // Supprimer le RDV
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
        // POST: RendezVous/ConfirmRdv/5 (AJAX) - AVEC NOTIFICATIONS
        // ============================================================
        [HttpPost]
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

                // ====== NOTIFICATIONS ======
                var dateRdv = rdv.DateHeure.ToString("dd/MM/yyyy à HH:mm");
                var msgConfirm = $"Votre rendez-vous du {dateRdv} a été confirmé";

                await _notificationService.CreateNotificationAsync(rdv.NumCom, "Confirmation", msgConfirm, rdv.PatientId);
                await _notificationService.CreateNotificationAsync(rdv.NumCom, "Confirmation", msgConfirm, rdv.MedecinId);

                return Json(new { success = true, message = "Rendez-vous confirmé" });
            }
            return Json(new { success = false, message = "Rendez-vous non trouvé" });
        }

        // ============================================================
        // POST: RendezVous/CancelRdv/5 (AJAX) - NOUVELLE ACTION
        // ============================================================
        [HttpPost]
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

                // ====== NOTIFICATIONS ======
                var dateRdv = rdv.DateHeure.ToString("dd/MM/yyyy à HH:mm");
                var raisonText = string.IsNullOrEmpty(raison) ? "Non spécifiée" : raison;
                var msgAnnul = $"Le rendez-vous du {dateRdv} a été annulé. Raison : {raisonText}";

                await _notificationService.CreateNotificationAsync(rdv.NumCom, "Annulation", msgAnnul, rdv.PatientId);
                await _notificationService.CreateNotificationAsync(rdv.NumCom, "Annulation", msgAnnul, rdv.MedecinId);

                return Json(new { success = true, message = "Rendez-vous annulé" });
            }
            return Json(new { success = false, message = "Rendez-vous non trouvé" });
        }

        private bool RendezVouExists(int id)
        {
            return _context.RendezVous.Any(e => e.NumCom == id);
        }

        // ============================================================
        // GET: RendezVous/ExportExcel - FONCTIONNEL AVEC CLOSEDXML
        // ============================================================
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

            // Appliquer les mêmes filtres que l'Index
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

            // ====== GÉNÉRATION EXCEL AVEC CLOSEDXML ======
            using var workbook = new XLWorkbook();
            var worksheet = workbook.Worksheets.Add("Rendez-vous");

            // Style du titre
            worksheet.Cell(1, 1).Value = "Liste des Rendez-vous";
            worksheet.Cell(1, 1).Style.Font.Bold = true;
            worksheet.Cell(1, 1).Style.Font.FontSize = 16;
            worksheet.Range(1, 1, 1, 6).Merge();

            // Date d'export
            worksheet.Cell(2, 1).Value = $"Exporté le {DateTime.Now:dd/MM/yyyy à HH:mm}";
            worksheet.Cell(2, 1).Style.Font.Italic = true;
            worksheet.Range(2, 1, 2, 6).Merge();

            // En-têtes
            var headerRow = 4;
            worksheet.Cell(headerRow, 1).Value = "N° RDV";
            worksheet.Cell(headerRow, 2).Value = "Date & Heure";
            worksheet.Cell(headerRow, 3).Value = "Patient";
            worksheet.Cell(headerRow, 4).Value = "Médecin";
            worksheet.Cell(headerRow, 5).Value = "Spécialité";
            worksheet.Cell(headerRow, 6).Value = "Statut";

            // Style des en-têtes
            var headerRange = worksheet.Range(headerRow, 1, headerRow, 6);
            headerRange.Style.Font.Bold = true;
            headerRange.Style.Fill.BackgroundColor = XLColor.FromHtml("#667eea");
            headerRange.Style.Font.FontColor = XLColor.White;
            headerRange.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            headerRange.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;

            // Données
            var currentRow = headerRow + 1;
            foreach (var rdv in rdvs)
            {
                worksheet.Cell(currentRow, 1).Value = rdv.NumCom;
                worksheet.Cell(currentRow, 2).Value = rdv.DateHeure.ToString("dd/MM/yyyy HH:mm");
                worksheet.Cell(currentRow, 3).Value = $"{rdv.Patient?.IdNavigation?.Nom} {rdv.Patient?.IdNavigation?.Prenom}";
                worksheet.Cell(currentRow, 4).Value = $"Dr. {rdv.Medecin?.IdNavigation?.Nom} {rdv.Medecin?.IdNavigation?.Prenom}";
                worksheet.Cell(currentRow, 5).Value = rdv.Medecin?.Specialite ?? "-";
                worksheet.Cell(currentRow, 6).Value = rdv.Statut;

                // Couleur selon le statut
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

                // Bordures
                worksheet.Range(currentRow, 1, currentRow, 6).Style.Border.OutsideBorder = XLBorderStyleValues.Thin;

                currentRow++;
            }

            // Ajuster la largeur des colonnes
            worksheet.Columns().AdjustToContents();

            // Statistiques en bas
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

            // Générer le fichier
            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            stream.Position = 0;

            var fileName = $"RendezVous_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";
            return File(stream.ToArray(),
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                fileName);
        }

        // ============================================================
        // GET: RendezVous/Calendrier
        // ============================================================
        public async Task<IActionResult> Calendrier()
        {
            // Liste des médecins pour le filtre du calendrier
            ViewBag.Medecins = await _context.Medecins
                .Include(m => m.IdNavigation)
                .Select(m => new { m.Id, Nom = "Dr. " + m.IdNavigation.Nom })
                .ToListAsync();

            return View();
        }

        // ============================================================
        // GET: RendezVous/GetCalendarEvents (AJAX pour FullCalendar)
        // ============================================================
        [HttpGet]
        public async Task<IActionResult> GetCalendarEvents(DateTime start, DateTime end, int? medecinId = null, string? status = null)
        {
            var query = _context.RendezVous
                .Include(r => r.Patient).ThenInclude(p => p.IdNavigation)
                .Include(r => r.Medecin).ThenInclude(m => m.IdNavigation)
                .Where(r => r.DateHeure >= start && r.DateHeure <= end);

            if (medecinId.HasValue)
                query = query.Where(r => r.MedecinId == medecinId.Value);

            if (!string.IsNullOrEmpty(status))
                query = query.Where(r => r.Statut == status);

            try
            {
                var data = await query
                    .Select(r => new
                    {
                        id = r.NumCom,
                        dateHeure = r.DateHeure,
                        statut = r.Statut,
                        patientNom = r.Patient.IdNavigation.Nom,
                        patientPrenom = r.Patient.IdNavigation.Prenom,
                        medecinNom = r.Medecin.IdNavigation.Nom,
                        medecinPrenom = r.Medecin.IdNavigation.Prenom,
                        specialite = r.Medecin.Specialite,
                        patientId = r.PatientId,
                        medecinId = r.MedecinId
                    })
                    .ToListAsync();

                var events = data.Select(r =>
                {
                    var startDate = r.dateHeure;
                    var endDate = startDate.AddHours(1);

                    return new
                    {
                        id = r.id,
                        title = $"{r.patientNom} {r.patientPrenom?.Substring(0, Math.Min(1, r.patientPrenom?.Length ?? 0))}.",
                        start = startDate.ToString("yyyy-MM-ddTHH:mm:ss"),
                        end = endDate.ToString("yyyy-MM-ddTHH:mm:ss"),
                        color = GetStatusColor(r.statut),
                        extendedProps = new
                        {
                            patient = $"{r.patientNom} {r.patientPrenom}",
                            medecin = $"Dr. {r.medecinNom} {r.medecinPrenom}",
                            specialite = r.specialite,
                            statut = r.statut,
                            patientId = r.patientId,
                            medecinId = r.medecinId
                        }
                    };
                }).ToList();

                return Json(events);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erreur GetCalendarEvents: {ex.Message}");
                return Json(new List<object>());
            }
        }

        // Helper pour la couleur selon le statut
        private string GetStatusColor(string statut)
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

        // HELPER METHOD pour les listes déroulantes
        private void PopulateDropdowns(object selectedMedecin = null, object selectedPatient = null)
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
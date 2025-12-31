using GestionCabinetMedical.Models;
using GestionCabinetMedical.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace GestionCabinetMedical.Controllers
{
    [Authorize]
    public class NotificationsController : Controller
    {
        private readonly BdCabinetMedicalContext _context;
        private readonly INotificationService _notificationService;

        public NotificationsController(BdCabinetMedicalContext context, INotificationService notificationService)
        {
            _context = context;
            _notificationService = notificationService;
        }

        /// <summary>
        /// Récupère l'ID de l'utilisateur connecté depuis la table Utilisateur
        /// en utilisant l'email de l'Identity
        /// </summary>
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
        // GET: Notifications - Page principale
        // ============================================================
        public async Task<IActionResult> Index()
        {
            var userId = await GetCurrentUserIdAsync();

            if (userId == 0)
            {
                TempData["Error"] = "Impossible d'identifier l'utilisateur.";
                return RedirectToAction("Index", "Home");
            }

            var notifications = await _notificationService.GetNotificationsForUserAsync(userId);
            return View(notifications);
        }

        // ============================================================
        // GET: Notifications/GetUnreadCount - AJAX pour badge
        // ============================================================
        [HttpGet]
        public async Task<IActionResult> GetUnreadCount()
        {
            var userId = await GetCurrentUserIdAsync();

            if (userId == 0)
                return Json(new { count = 0 });

            var count = await _notificationService.GetUnreadCountAsync(userId);
            return Json(new { count });
        }

        // ============================================================
        // GET: Notifications/GetRecent - AJAX pour dropdown
        // ============================================================
        [HttpGet]
        public async Task<IActionResult> GetRecent()
        {
            var userId = await GetCurrentUserIdAsync();

            if (userId == 0)
                return Json(new List<object>());

            var notifications = await _notificationService.GetUnreadNotificationsAsync(userId);

            var result = notifications.Take(5).Select(n => new
            {
                id = n.Id,
                type = n.Type,
                message = n.Message,
                dateCreation = n.DateCreation.ToString("dd/MM/yyyy HH:mm"),
                estLue = n.EstLue,
                rendezVousId = n.RendezVousId
            });

            return Json(result);
        }

        // ============================================================
        // POST: Notifications/MarkAsRead/5
        // ============================================================
        [HttpPost]
        public async Task<IActionResult> MarkAsRead(int id)
        {
            var userId = await GetCurrentUserIdAsync();

            // Vérifier que la notification appartient à l'utilisateur
            var notification = await _context.Notifications
                .FirstOrDefaultAsync(n => n.Id == id && n.UserId == userId);

            if (notification == null)
                return Json(new { success = false, message = "Notification non trouvée" });

            await _notificationService.MarkAsReadAsync(id);
            return Json(new { success = true });
        }

        // ============================================================
        // POST: Notifications/MarkAllAsRead
        // ============================================================
        [HttpPost]
        public async Task<IActionResult> MarkAllAsRead()
        {
            var userId = await GetCurrentUserIdAsync();

            if (userId == 0)
                return Json(new { success = false, message = "Utilisateur non identifié" });

            await _notificationService.MarkAllAsReadAsync(userId);
            return Json(new { success = true });
        }

        // ============================================================
        // POST: Notifications/Delete/5
        // ============================================================
        [HttpPost]
        public async Task<IActionResult> Delete(int id)
        {
            var userId = await GetCurrentUserIdAsync();

            // Vérifier que la notification appartient à l'utilisateur
            var notification = await _context.Notifications
                .FirstOrDefaultAsync(n => n.Id == id && n.UserId == userId);

            if (notification == null)
                return Json(new { success = false, message = "Notification non trouvée" });

            await _notificationService.DeleteNotificationAsync(id);
            return Json(new { success = true });
        }

        // ============================================================
        // POST: Notifications/CreateReminders - Pour job schedulé
        // ============================================================
        [HttpPost]
        [Authorize(Roles = "ADMIN")]
        public async Task<IActionResult> CreateReminders()
        {
            await _notificationService.CreateRdvReminderNotificationsAsync();
            return Json(new { success = true, message = "Rappels créés avec succès" });
        }
    }
}
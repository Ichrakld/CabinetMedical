using GestionCabinetMedical.Models;
using Microsoft.EntityFrameworkCore;

namespace GestionCabinetMedical.Services
{
    public interface INotificationService
    {
        Task<List<Notification>> GetNotificationsForUserAsync(int userId);
        Task<List<Notification>> GetUnreadNotificationsAsync(int userId);
        Task<int> GetUnreadCountAsync(int userId);
        Task CreateNotificationAsync(int? rendezVousId, string type, string message, int userId);
        Task MarkAsReadAsync(int notificationId);
        Task MarkAllAsReadAsync(int userId);
        Task DeleteNotificationAsync(int notificationId);
        Task CreateRdvReminderNotificationsAsync();
    }

    public class NotificationService : INotificationService
    {
        private readonly BdCabinetMedicalContext _context;

        public NotificationService(BdCabinetMedicalContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Récupère toutes les notifications d'un utilisateur (max 50)
        /// </summary>
        public async Task<List<Notification>> GetNotificationsForUserAsync(int userId)
        {
            return await _context.Notifications
                .Include(n => n.RendezVous)
                    .ThenInclude(r => r.Patient)
                        .ThenInclude(p => p.IdNavigation)
                .Include(n => n.RendezVous)
                    .ThenInclude(r => r.Medecin)
                        .ThenInclude(m => m.IdNavigation)
                .Where(n => n.UserId == userId)
                .OrderByDescending(n => n.DateCreation)
                .Take(50)
                .ToListAsync();
        }

        /// <summary>
        /// Récupère les notifications non lues d'un utilisateur
        /// </summary>
        public async Task<List<Notification>> GetUnreadNotificationsAsync(int userId)
        {
            return await _context.Notifications
                .Include(n => n.RendezVous)
                .Where(n => n.UserId == userId && !n.EstLue)
                .OrderByDescending(n => n.DateCreation)
                .ToListAsync();
        }

        /// <summary>
        /// Compte le nombre de notifications non lues
        /// </summary>
        public async Task<int> GetUnreadCountAsync(int userId)
        {
            return await _context.Notifications
                .CountAsync(n => n.UserId == userId && !n.EstLue);
        }

        /// <summary>
        /// Crée une nouvelle notification
        /// </summary>
        public async Task CreateNotificationAsync(int? rendezVousId, string type, string message, int userId)
        {
            var notification = new Notification
            {
                RendezVousId = rendezVousId,
                Type = type,
                Message = message,
                UserId = userId,
                DateCreation = DateTime.Now,
                EstLue = false
            };

            _context.Notifications.Add(notification);
            await _context.SaveChangesAsync();
        }

        /// <summary>
        /// Marque une notification comme lue
        /// </summary>
        public async Task MarkAsReadAsync(int notificationId)
        {
            var notification = await _context.Notifications.FindAsync(notificationId);
            if (notification != null)
            {
                notification.EstLue = true;
                await _context.SaveChangesAsync();
            }
        }

        /// <summary>
        /// Marque toutes les notifications d'un utilisateur comme lues
        /// </summary>
        public async Task MarkAllAsReadAsync(int userId)
        {
            var notifications = await _context.Notifications
                .Where(n => n.UserId == userId && !n.EstLue)
                .ToListAsync();

            foreach (var notification in notifications)
            {
                notification.EstLue = true;
            }

            await _context.SaveChangesAsync();
        }

        /// <summary>
        /// Supprime une notification
        /// </summary>
        public async Task DeleteNotificationAsync(int notificationId)
        {
            var notification = await _context.Notifications.FindAsync(notificationId);
            if (notification != null)
            {
                _context.Notifications.Remove(notification);
                await _context.SaveChangesAsync();
            }
        }

        /// <summary>
        /// Crée des rappels pour les RDV de demain (à appeler via un job schedulé)
        /// </summary>
        public async Task CreateRdvReminderNotificationsAsync()
        {
            var tomorrow = DateTime.Today.AddDays(1);
            var dayAfter = tomorrow.AddDays(1);

            var rdvDemain = await _context.RendezVous
                .Include(r => r.Patient)
                    .ThenInclude(p => p.IdNavigation)
                .Include(r => r.Medecin)
                    .ThenInclude(m => m.IdNavigation)
                .Where(r => r.DateHeure >= tomorrow &&
                           r.DateHeure < dayAfter &&
                           r.Statut == "Confirmé")
                .ToListAsync();

            foreach (var rdv in rdvDemain)
            {
                // Vérifier si un rappel n'existe pas déjà
                var existingReminder = await _context.Notifications
                    .AnyAsync(n => n.RendezVousId == rdv.NumCom &&
                                  n.Type == "Rappel RDV" &&
                                  n.DateCreation.Date == DateTime.Today);

                if (!existingReminder)
                {
                    // Notification pour le patient
                    var messagePatient = $"Rappel : Vous avez un rendez-vous demain à {rdv.DateHeure:HH:mm} avec Dr. {rdv.Medecin?.IdNavigation?.Nom}";
                    await CreateNotificationAsync(rdv.NumCom, "Rappel RDV", messagePatient, rdv.PatientId);

                    // Notification pour le médecin
                    var messageMedecin = $"Rappel : Rendez-vous demain à {rdv.DateHeure:HH:mm} avec {rdv.Patient?.IdNavigation?.Nom} {rdv.Patient?.IdNavigation?.Prenom}";
                    await CreateNotificationAsync(rdv.NumCom, "Rappel RDV", messageMedecin, rdv.MedecinId);
                }
            }
        }
    }
}
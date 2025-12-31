using GestionCabinetMedical.Models;
using GestionCabinetMedical.Services;

namespace GestionCabinetMedical.Helpers
{
    /// <summary>
    /// Helper class for creating CRUD operation notifications
    /// Implements Factory pattern for notification creation
    /// </summary>
    public interface ICrudNotificationHelper
    {
        Task NotifyCreateSuccessAsync(string entityName, string details, int userId);
        Task NotifyCreateErrorAsync(string entityName, string error, int userId);
        Task NotifyUpdateSuccessAsync(string entityName, string details, int userId);
        Task NotifyUpdateErrorAsync(string entityName, string error, int userId);
        Task NotifyDeleteSuccessAsync(string entityName, string details, int userId);
        Task NotifyDeleteErrorAsync(string entityName, string error, int userId);
        Task NotifyRdvConfirmationAsync(RendezVou rdv, int userId);
        Task NotifyRdvCancellationAsync(RendezVou rdv, int userId, string reason);
    }

    public class CrudNotificationHelper : ICrudNotificationHelper
    {
        private readonly INotificationService _notificationService;

        public CrudNotificationHelper(INotificationService notificationService)
        {
            _notificationService = notificationService;
        }

        #region Create Operations

        public async Task NotifyCreateSuccessAsync(string entityName, string details, int userId)
        {
            var message = $"✓ {entityName} créé(e) avec succès : {details}";
            await _notificationService.CreateNotificationAsync(null, "Succès", message, userId);
        }

        public async Task NotifyCreateErrorAsync(string entityName, string error, int userId)
        {
            var message = $"✗ Erreur lors de la création de {entityName} : {error}";
            await _notificationService.CreateNotificationAsync(null, "Erreur", message, userId);
        }

        #endregion

        #region Update Operations

        public async Task NotifyUpdateSuccessAsync(string entityName, string details, int userId)
        {
            var message = $"✓ {entityName} modifié(e) avec succès : {details}";
            await _notificationService.CreateNotificationAsync(null, "Succès", message, userId);
        }

        public async Task NotifyUpdateErrorAsync(string entityName, string error, int userId)
        {
            var message = $"✗ Erreur lors de la modification de {entityName} : {error}";
            await _notificationService.CreateNotificationAsync(null, "Erreur", message, userId);
        }

        #endregion

        #region Delete Operations

        public async Task NotifyDeleteSuccessAsync(string entityName, string details, int userId)
        {
            var message = $"✓ {entityName} supprimé(e) avec succès : {details}";
            await _notificationService.CreateNotificationAsync(null, "Succès", message, userId);
        }

        public async Task NotifyDeleteErrorAsync(string entityName, string error, int userId)
        {
            var message = $"✗ Erreur lors de la suppression de {entityName} : {error}";
            await _notificationService.CreateNotificationAsync(null, "Erreur", message, userId);
        }

        #endregion

        #region RDV Specific Notifications

        public async Task NotifyRdvConfirmationAsync(RendezVou rdv, int userId)
        {
            var message = $"Votre rendez-vous du {rdv.DateHeure:dd/MM/yyyy à HH:mm} a été confirmé.";
            await _notificationService.CreateNotificationAsync(rdv.NumCom, "Confirmation", message, userId);
        }

        public async Task NotifyRdvCancellationAsync(RendezVou rdv, int userId, string reason)
        {
            var message = $"Le rendez-vous du {rdv.DateHeure:dd/MM/yyyy à HH:mm} a été annulé. Raison : {reason}";
            await _notificationService.CreateNotificationAsync(rdv.NumCom, "Annulation", message, userId);
        }

        #endregion
    }

    /// <summary>
    /// Extension methods for registering notification services
    /// </summary>
    public static class NotificationServiceExtensions
    {
        public static IServiceCollection AddNotificationServices(this IServiceCollection services)
        {
            services.AddScoped<INotificationService, NotificationService>();
            services.AddScoped<ICrudNotificationHelper, CrudNotificationHelper>();
            return services;
        }
    }
}
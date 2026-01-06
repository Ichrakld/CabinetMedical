using GestionCabinetMedical.Models;
using GestionCabinetMedical.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace GestionCabinetMedical.Controllers
{
    [Authorize]
    public class ChatbotController : Controller
    {
        private readonly IChatbotService _chatbotService;
        private readonly BdCabinetMedicalContext _context;

        public ChatbotController(IChatbotService chatbotService, BdCabinetMedicalContext context)
        {
            _chatbotService = chatbotService;
            _context = context;
        }

        /// <summary>
        /// Récupère l'ID de l'utilisateur connecté
        /// </summary>
        private async Task<int?> GetCurrentUserIdAsync()
        {
            var email = User.FindFirst(ClaimTypes.Email)?.Value
                     ?? User.FindFirst(ClaimTypes.Name)?.Value;

            if (string.IsNullOrEmpty(email))
                return null;

            var utilisateur = await _context.Utilisateurs
                .FirstOrDefaultAsync(u => u.Email == email);

            return utilisateur?.Id;
        }

        /// <summary>
        /// Récupère le rôle principal de l'utilisateur
        /// </summary>
        private string GetUserRole()
        {
            if (User.IsInRole("ADMIN")) return "ADMIN";
            if (User.IsInRole("MEDECIN")) return "MEDECIN";
            if (User.IsInRole("SECRETAIRE")) return "SECRETAIRE";
            if (User.IsInRole("PATIENT")) return "PATIENT";
            return "USER";
        }

        /// <summary>
        /// POST: Chatbot/SendMessage
        /// Endpoint principal pour traiter les messages du chatbot
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> SendMessage([FromBody] ChatbotMessageRequest request)
        {
            if (string.IsNullOrWhiteSpace(request?.Message))
            {
                return BadRequest(new { error = "Le message ne peut pas être vide" });
            }

            try
            {
                var userId = await GetCurrentUserIdAsync();
                var userRole = GetUserRole();

                var response = await _chatbotService.ProcessMessageAsync(
                    request.Message,
                    userId,
                    userRole
                );

                return Json(new
                {
                    success = true,
                    response = new
                    {
                        message = response.Message,
                        type = response.Type,
                        actions = response.Actions?.Select(a => new
                        {
                            label = a.Label,
                            url = a.Url,
                            icon = a.Icon
                        }),
                        data = response.Data
                    }
                });
            }
            catch (Exception ex)
            {
                return Json(new
                {
                    success = false,
                    response = new
                    {
                        message = "Désolé, une erreur s'est produite. Veuillez réessayer.",
                        type = "error"
                    }
                });
            }
        }

        /// <summary>
        /// GET: Chatbot/GetSuggestions
        /// Récupère les suggestions de questions pour l'utilisateur
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetSuggestions()
        {
            var userRole = GetUserRole();
            var suggestions = await _chatbotService.GetSuggestionsAsync(userRole);

            return Json(new
            {
                success = true,
                suggestions = suggestions.Select(s => new
                {
                    text = s.Text,
                    icon = s.Icon
                })
            });
        }

        /// <summary>
        /// GET: Chatbot/GetWelcomeMessage
        /// Message de bienvenue personnalisé
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetWelcomeMessage()
        {
            var userId = await GetCurrentUserIdAsync();
            var userRole = GetUserRole();
            var userName = User.Identity?.Name ?? "Patient";

            // Récupérer le nom complet si possible
            if (userId.HasValue)
            {
                var utilisateur = await _context.Utilisateurs.FindAsync(userId);
                if (utilisateur != null)
                {
                    userName = $"{utilisateur.Prenom}";
                }
            }

            var message = $"Bonjour {userName} ! 👋\n\nJe suis l'assistant virtuel du Cabinet Médical. Je peux vous aider à :\n\n• Prendre un rendez-vous\n• Consulter vos RDV à venir\n• Obtenir des informations sur nos services\n\nComment puis-je vous aider ?";

            var suggestions = await _chatbotService.GetSuggestionsAsync(userRole);

            return Json(new
            {
                success = true,
                message = message,
                suggestions = suggestions.Select(s => new
                {
                    text = s.Text,
                    icon = s.Icon
                })
            });
        }
    }

    /// <summary>
    /// Modèle pour les requêtes de message
    /// </summary>
    public class ChatbotMessageRequest
    {
        public string Message { get; set; } = string.Empty;
    }
}
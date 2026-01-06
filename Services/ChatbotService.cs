using GestionCabinetMedical.Models;
using Microsoft.EntityFrameworkCore;

namespace GestionCabinetMedical.Services
{
    public interface IChatbotService
    {
        Task<ChatbotResponse> ProcessMessageAsync(string message, int? userId, string userRole);
        Task<List<ChatbotSuggestion>> GetSuggestionsAsync(string userRole);
    }

    public class ChatbotResponse
    {
        public string Message { get; set; } = string.Empty;
        public string Type { get; set; } = "text"; // text, info, warning, action
        public List<ChatbotAction>? Actions { get; set; }
        public object? Data { get; set; }
    }

    public class ChatbotAction
    {
        public string Label { get; set; } = string.Empty;
        public string Url { get; set; } = string.Empty;
        public string Icon { get; set; } = string.Empty;
    }

    public class ChatbotSuggestion
    {
        public string Text { get; set; } = string.Empty;
        public string Icon { get; set; } = string.Empty;
    }

    public class ChatbotService : IChatbotService
    {
        private readonly BdCabinetMedicalContext _context;

        public ChatbotService(BdCabinetMedicalContext context)
        {
            _context = context;
        }

        public async Task<ChatbotResponse> ProcessMessageAsync(string message, int? userId, string userRole)
        {
            var lowerMessage = message.ToLower().Trim();

            // Salutations
            if (ContainsAny(lowerMessage, "bonjour", "salut", "hello", "hi", "bonsoir", "coucou"))
            {
                return new ChatbotResponse
                {
                    Message = "Bonjour ! 👋 Je suis l'assistant virtuel du Cabinet Médical. Comment puis-je vous aider aujourd'hui ?",
                    Type = "info",
                    Actions = GetQuickActions(userRole)
                };
            }

            // Questions sur les rendez-vous
            if (ContainsAny(lowerMessage, "rendez-vous", "rdv", "appointment", "consultation"))
            {
                if (ContainsAny(lowerMessage, "prendre", "réserver", "nouveau", "créer", "planifier"))
                {
                    return new ChatbotResponse
                    {
                        Message = "Pour prendre un rendez-vous, vous pouvez utiliser notre système de réservation en ligne. Cliquez sur le bouton ci-dessous pour accéder à la page de création de rendez-vous.",
                        Type = "action",
                        Actions = new List<ChatbotAction>
                        {
                            new ChatbotAction { Label = "Prendre RDV", Url = "/RendezVous/Create", Icon = "fa-calendar-plus" },
                            new ChatbotAction { Label = "Voir le calendrier", Url = "/RendezVous/Calendrier", Icon = "fa-calendar-alt" }
                        }
                    };
                }

                if (ContainsAny(lowerMessage, "annuler", "supprimer", "cancel"))
                {
                    return new ChatbotResponse
                    {
                        Message = "Pour annuler un rendez-vous, veuillez accéder à la liste de vos rendez-vous et cliquer sur le bouton d'annulation. Notez qu'il est préférable d'annuler au moins 24h à l'avance.",
                        Type = "warning",
                        Actions = new List<ChatbotAction>
                        {
                            new ChatbotAction { Label = "Mes rendez-vous", Url = "/RendezVous", Icon = "fa-list" }
                        }
                    };
                }

                if (ContainsAny(lowerMessage, "prochain", "prochains", "à venir", "futur"))
                {
                    if (userId.HasValue && userRole == "PATIENT")
                    {
                        var prochainRdv = await _context.RendezVous
                            .Include(r => r.Medecin).ThenInclude(m => m.IdNavigation)
                            .Where(r => r.PatientId == userId && r.DateHeure > DateTime.Now && r.Statut != "Annulé")
                            .OrderBy(r => r.DateHeure)
                            .FirstOrDefaultAsync();

                        if (prochainRdv != null)
                        {
                            return new ChatbotResponse
                            {
                                Message = $"📅 Votre prochain rendez-vous est prévu le **{prochainRdv.DateHeure:dddd dd MMMM yyyy à HH:mm}** avec **Dr. {prochainRdv.Medecin?.IdNavigation?.Nom}**. Statut: {prochainRdv.Statut}",
                                Type = "info",
                                Actions = new List<ChatbotAction>
                                {
                                    new ChatbotAction { Label = "Voir détails", Url = $"/RendezVous/Details/{prochainRdv.NumCom}", Icon = "fa-eye" },
                                    new ChatbotAction { Label = "Tous mes RDV", Url = "/RendezVous", Icon = "fa-list" }
                                },
                                Data = new { rdvId = prochainRdv.NumCom, date = prochainRdv.DateHeure }
                            };
                        }
                        else
                        {
                            return new ChatbotResponse
                            {
                                Message = "Vous n'avez aucun rendez-vous à venir. Souhaitez-vous en prendre un ?",
                                Type = "info",
                                Actions = new List<ChatbotAction>
                                {
                                    new ChatbotAction { Label = "Prendre RDV", Url = "/RendezVous/Create", Icon = "fa-calendar-plus" }
                                }
                            };
                        }
                    }
                    return new ChatbotResponse
                    {
                        Message = "Pour voir vos prochains rendez-vous, accédez à la page des rendez-vous.",
                        Type = "info",
                        Actions = new List<ChatbotAction>
                        {
                            new ChatbotAction { Label = "Voir mes RDV", Url = "/RendezVous", Icon = "fa-calendar-alt" }
                        }
                    };
                }

                // Question générale sur les RDV
                return new ChatbotResponse
                {
                    Message = "Je peux vous aider avec vos rendez-vous ! Que souhaitez-vous faire ?",
                    Type = "info",
                    Actions = new List<ChatbotAction>
                    {
                        new ChatbotAction { Label = "Prendre un RDV", Url = "/RendezVous/Create", Icon = "fa-plus" },
                        new ChatbotAction { Label = "Voir mes RDV", Url = "/RendezVous", Icon = "fa-list" },
                        new ChatbotAction { Label = "Calendrier", Url = "/RendezVous/Calendrier", Icon = "fa-calendar" }
                    }
                };
            }

            // Questions sur les horaires
            if (ContainsAny(lowerMessage, "horaire", "heure", "ouvert", "ouverture", "fermeture", "disponible"))
            {
                return new ChatbotResponse
                {
                    Message = "🕐 **Nos horaires d'ouverture:**\n\n• Lundi - Vendredi: 8h00 - 18h00\n• Samedi: 9h00 - 13h00\n• Dimanche: Fermé\n\n📞 En cas d'urgence en dehors des horaires, contactez le 15 (SAMU).",
                    Type = "info"
                };
            }

            // Questions sur les médecins
            if (ContainsAny(lowerMessage, "médecin", "docteur", "doc", "spécialiste", "spécialité"))
            {
                var medecins = await _context.Medecins
                    .Include(m => m.IdNavigation)
                    .Where(m => m.IdNavigation.EstActif)
                    .ToListAsync();

                if (medecins.Any())
                {
                    var medecinsList = string.Join("\n", medecins.Select(m =>
                        $"• **Dr. {m.IdNavigation.Nom} {m.IdNavigation.Prenom}** - {m.Specialite}"));

                    return new ChatbotResponse
                    {
                        Message = $"👨‍⚕️ **Notre équipe médicale:**\n\n{medecinsList}\n\nVoulez-vous prendre rendez-vous avec l'un de nos médecins ?",
                        Type = "info",
                        Actions = new List<ChatbotAction>
                        {
                            new ChatbotAction { Label = "Prendre RDV", Url = "/RendezVous/Create", Icon = "fa-calendar-plus" },
                            new ChatbotAction { Label = "Voir équipe", Url = "/Medecins", Icon = "fa-users-medical" }
                        }
                    };
                }
            }

            // Questions sur le dossier médical
            if (ContainsAny(lowerMessage, "dossier", "médical", "historique", "antécédent"))
            {
                if (userRole == "PATIENT")
                {
                    return new ChatbotResponse
                    {
                        Message = "Pour accéder à votre dossier médical, veuillez contacter notre secrétariat ou votre médecin traitant. Vos informations médicales sont confidentielles et sécurisées.",
                        Type = "info"
                    };
                }
                return new ChatbotResponse
                {
                    Message = "Les dossiers médicaux sont accessibles via le menu Dossiers Médicaux.",
                    Type = "info",
                    Actions = new List<ChatbotAction>
                    {
                        new ChatbotAction { Label = "Dossiers médicaux", Url = "/DossiersMedicaux", Icon = "fa-folder-medical" }
                    }
                };
            }

            // Contact
            if (ContainsAny(lowerMessage, "contact", "téléphone", "email", "adresse", "joindre", "appeler"))
            {
                return new ChatbotResponse
                {
                    Message = "📞 **Nos coordonnées:**\n\n• Téléphone: 05 XX XX XX XX\n• Email: contact@cabinetmedical.ma\n• Adresse: [Votre adresse]\n\n🚗 Parking gratuit disponible.",
                    Type = "info"
                };
            }

            // Urgence
            if (ContainsAny(lowerMessage, "urgence", "urgent", "grave", "samu", "pompier"))
            {
                return new ChatbotResponse
                {
                    Message = "🚨 **En cas d'urgence médicale:**\n\n• SAMU: **15**\n• Pompiers: **18**\n• Urgences européennes: **112**\n\n⚠️ Si vous avez une urgence vitale, appelez immédiatement le 15 !",
                    Type = "warning"
                };
            }

            // Aide générale
            if (ContainsAny(lowerMessage, "aide", "help", "?", "comment", "quoi faire"))
            {
                return new ChatbotResponse
                {
                    Message = "Je peux vous aider avec:\n\n• 📅 **Rendez-vous**: prendre, modifier ou annuler\n• 👨‍⚕️ **Médecins**: voir notre équipe\n• 🕐 **Horaires**: heures d'ouverture\n• 📞 **Contact**: nos coordonnées\n• 🚨 **Urgences**: numéros utiles\n\nPosez-moi votre question !",
                    Type = "info",
                    Actions = GetQuickActions(userRole)
                };
            }

            // Merci
            if (ContainsAny(lowerMessage, "merci", "thanks", "thank you"))
            {
                return new ChatbotResponse
                {
                    Message = "Je vous en prie ! 😊 N'hésitez pas si vous avez d'autres questions. Bonne journée !",
                    Type = "text"
                };
            }

            // Au revoir
            if (ContainsAny(lowerMessage, "au revoir", "bye", "goodbye", "à bientôt", "ciao"))
            {
                return new ChatbotResponse
                {
                    Message = "Au revoir ! 👋 À bientôt au Cabinet Médical. Prenez soin de vous !",
                    Type = "text"
                };
            }

            // Réponse par défaut
            return new ChatbotResponse
            {
                Message = "Je ne suis pas sûr de comprendre votre demande. Voici ce que je peux faire pour vous :",
                Type = "info",
                Actions = GetQuickActions(userRole)
            };
        }

        public async Task<List<ChatbotSuggestion>> GetSuggestionsAsync(string userRole)
        {
            var suggestions = new List<ChatbotSuggestion>
            {
                new ChatbotSuggestion { Text = "Prendre un rendez-vous", Icon = "fa-calendar-plus" },
                new ChatbotSuggestion { Text = "Mes prochains rendez-vous", Icon = "fa-calendar-check" },
                new ChatbotSuggestion { Text = "Horaires d'ouverture", Icon = "fa-clock" },
                new ChatbotSuggestion { Text = "Contacter le cabinet", Icon = "fa-phone" }
            };

            if (userRole == "PATIENT")
            {
                suggestions.Add(new ChatbotSuggestion { Text = "Mon dossier médical", Icon = "fa-folder" });
            }

            return await Task.FromResult(suggestions);
        }

        private List<ChatbotAction> GetQuickActions(string userRole)
        {
            var actions = new List<ChatbotAction>
            {
                new ChatbotAction { Label = "Prendre RDV", Url = "/RendezVous/Create", Icon = "fa-calendar-plus" },
                new ChatbotAction { Label = "Mes RDV", Url = "/RendezVous", Icon = "fa-list" }
            };

            if (userRole != "PATIENT")
            {
                actions.Add(new ChatbotAction { Label = "Dashboard", Url = "/Dashboard", Icon = "fa-tachometer-alt" });
            }

            return actions;
        }

        private bool ContainsAny(string text, params string[] keywords)
        {
            return keywords.Any(k => text.Contains(k));
        }
    }
}
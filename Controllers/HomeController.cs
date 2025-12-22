using System; // Nécessaire pour DateTime
using System.Diagnostics;
using GestionCabinetMedical.Models;
using Microsoft.AspNetCore.Mvc;
using System.Linq; // Nécessaire pour les requêtes LINQ

namespace GestionCabinetMedical.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly BdCabinetMedicalContext _context;

        public HomeController(ILogger<HomeController> logger, BdCabinetMedicalContext context)
        {
            _logger = logger;
            _context = context;
        }

        public IActionResult Index()
        {
            if (User.Identity.IsAuthenticated)
            {
                // 1. Logique du Nom (que vous avez déjà)
                var emailConnecte = User.Identity.Name;
                var utilisateur = _context.Utilisateurs.FirstOrDefault(u => u.Email == emailConnecte);

                if (utilisateur != null)
                    ViewBag.NomComplet = utilisateur.Prenom + " " + utilisateur.Nom;
                else
                    ViewBag.NomComplet = emailConnecte;

                // 2. NOUVEAU : Calcul des statistiques pour le tableau de bord

                // Compter tous les patients
                ViewBag.NbPatients = _context.Patients.Count();

                // Compter les RDV dont la date est Aujourd'hui (DateTime.Today)
                ViewBag.NbRdvAujourdhui = _context.RendezVous
                                            .Where(r => r.DateHeure.Date == DateTime.Today)
                                            .Count();

                // Compter toutes les consultations
                ViewBag.NbConsultations = _context.Consultations.Count();

                // Compter tous les médecins
                ViewBag.NbMedecins = _context.Medecins.Count();
            }

            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
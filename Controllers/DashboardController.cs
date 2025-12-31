using GestionCabinetMedical.Models;
using GestionCabinetMedical.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GestionCabinetMedical.Controllers
{
    [Authorize]
    public class DashboardController : Controller
    {
        private readonly BdCabinetMedicalContext _context;

        public DashboardController(BdCabinetMedicalContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var today = DateTime.Today;
            var startOfMonth = new DateTime(today.Year, today.Month, 1);
            var startOfWeek = today.AddDays(-(int)today.DayOfWeek);

            // Statistiques générales
            ViewBag.TotalPatients = await _context.Patients.CountAsync();
            ViewBag.TotalMedecins = await _context.Medecins.CountAsync();
            ViewBag.TotalRdvAujourdHui = await _context.RendezVous
                .CountAsync(r => r.DateHeure.Date == today);
            ViewBag.TotalConsultationsMois = await _context.Consultations
                .CountAsync(c => c.DateConsultation >= startOfMonth);

            // Rendez-vous par statut
            ViewBag.RdvEnAttente = await _context.RendezVous
                .CountAsync(r => r.Statut == "En attente" && r.DateHeure >= today);
            ViewBag.RdvConfirmes = await _context.RendezVous
                .CountAsync(r => r.Statut == "Confirmé" && r.DateHeure >= today);
            ViewBag.RdvAnnules = await _context.RendezVous
                .CountAsync(r => r.Statut == "Annulé" && r.DateHeure >= startOfMonth);
            ViewBag.RdvTermines = await _context.RendezVous
                .CountAsync(r => r.Statut == "Terminé" && r.DateHeure >= startOfMonth);

            // Prochains rendez-vous (7 prochains)
            var prochainRdv = await _context.RendezVous
                .Include(r => r.Patient).ThenInclude(p => p.IdNavigation)
                .Include(r => r.Medecin).ThenInclude(m => m.IdNavigation)
                .Where(r => r.DateHeure >= DateTime.Now && r.Statut != "Annulé")
                .OrderBy(r => r.DateHeure)
                .Take(7)
                .ToListAsync();
            ViewBag.ProchainsRdv = prochainRdv;

            // Derniers patients inscrits
            var derniersPatients = await _context.Patients
                .Include(p => p.IdNavigation)
                .OrderByDescending(p => p.Id)
                .Take(5)
                .ToListAsync();
            ViewBag.DerniersPatients = derniersPatients;

            // Statistiques par mois (6 derniers mois)
            var stats = new List<object>();
            for (int i = 5; i >= 0; i--)
            {
                var month = today.AddMonths(-i);
                var startMonth = new DateTime(month.Year, month.Month, 1);
                var endMonth = startMonth.AddMonths(1);

                var rdvCount = await _context.RendezVous
                    .CountAsync(r => r.DateHeure >= startMonth && r.DateHeure < endMonth);
                var consultCount = await _context.Consultations
                    .CountAsync(c => c.DateConsultation >= startMonth && c.DateConsultation < endMonth);

                stats.Add(new
                {
                    Mois = startMonth.ToString("MMM yyyy"),
                    RendezVous = rdvCount,
                    Consultations = consultCount
                });
            }
            ViewBag.StatsMensuelles = stats;

            // Top 5 médecins par nombre de consultations ce mois
            var topMedecins = await _context.DossierMedicals
                .Include(d => d.Medecin).ThenInclude(m => m.IdNavigation)
                .Include(d => d.Consultations)
                .Where(d => d.Consultations.Any(c => c.DateConsultation >= startOfMonth))
                .GroupBy(d => d.Medecin)
                .Select(g => new
                {
                    Medecin = g.Key,
                    NbConsultations = g.SelectMany(d => d.Consultations)
                        .Count(c => c.DateConsultation >= startOfMonth)
                })
                .OrderByDescending(x => x.NbConsultations)
                .Take(5)
                .ToListAsync();
            ViewBag.TopMedecins = topMedecins;

            return View();
        }

        // API pour graphiques (Chart.js)
        [HttpGet]
        public async Task<IActionResult> GetStatsRdvParJour()
        {
            var today = DateTime.Today;
            var data = new List<object>();

            for (int i = 6; i >= 0; i--)
            {
                var date = today.AddDays(-i);
                var count = await _context.RendezVous
                    .CountAsync(r => r.DateHeure.Date == date);

                data.Add(new
                {
                    Date = date.ToString("dd/MM"),
                    Count = count
                });
            }

            return Json(data);
        }

        [HttpGet]
        public async Task<IActionResult> GetStatsRdvParStatut()
        {
            var stats = await _context.RendezVous
                .GroupBy(r => r.Statut)
                .Select(g => new
                {
                    Statut = g.Key,
                    Count = g.Count()
                })
                .ToListAsync();

            return Json(stats);
        }
    }
}
using GestionCabinetMedical.Models;

namespace GestionCabinetMedical.ViewModels
{
    /// <summary>
    /// ViewModel pour la liste des rendez-vous avec pagination et filtres
    /// </summary>
    public class RendezVousIndexViewModel
    {
        // ============================================================
        // Données paginées
        // ============================================================
        public PaginatedList<RendezVou> RendezVous { get; set; } = null!;

        // ============================================================
        // Filtres
        // ============================================================
        public string? SearchTerm { get; set; }
        public string? Statut { get; set; }
        public DateTime? DateDebut { get; set; }
        public DateTime? DateFin { get; set; }
        public int? MedecinId { get; set; }
        public string? Periode { get; set; }
        public string SortBy { get; set; } = "date";
        public string SortOrder { get; set; } = "asc";

        // ============================================================
        // Pagination
        // ============================================================
        public int PageSize { get; set; } = 10;
        public int CurrentPage => RendezVous?.CurrentPage ?? 1;
        public int TotalPages => RendezVous?.TotalPages ?? 1;

        // ============================================================
        // Statistiques
        // ============================================================
        public int TotalRendezVous { get; set; }
        public int RendezVousAujourdHui { get; set; }
        public int RendezVousEnAttente { get; set; }
        public int RendezVousConfirmes { get; set; }
        public int RendezVousAnnules { get; set; }
        public int RendezVousTermines { get; set; }
        public int RendezVousCetteSemaine { get; set; }

        // ============================================================
        // Contexte
        // ============================================================
        public bool IsPatient { get; set; }

        // ============================================================
        // Listes de sélection
        // ============================================================
        public List<MedecinSelectItem> Medecins { get; set; } = new();
    }

    /// <summary>
    /// Item pour la liste déroulante des médecins
    /// </summary>
    public class MedecinSelectItem
    {
        public int Id { get; set; }
        public string Nom { get; set; } = string.Empty;
        public string Specialite { get; set; } = string.Empty;
    }
}
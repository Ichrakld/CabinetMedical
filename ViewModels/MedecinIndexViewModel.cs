using GestionCabinetMedical.Models;

namespace GestionCabinetMedical.ViewModels
{
    /// <summary>
    /// ViewModel for Medecins Index with filtering, sorting, and pagination
    /// </summary>
    public class MedecinIndexViewModel
    {
        // Medecin list with pagination
        public PaginatedList<Medecin> Medecins { get; set; } = null!;

        // Statistics
        public int TotalMedecins { get; set; }
        public int MedecinsActifs { get; set; }
        public int SpecialitesDistinctes { get; set; }

        // Filters
        public string? SearchTerm { get; set; }
        public string? Specialite { get; set; }
        public string? SortBy { get; set; } = "nom";

        // Pagination
        public int PageSize { get; set; } = 10;
        public int CurrentPage => Medecins?.CurrentPage ?? 1;
        public int TotalPages => Medecins?.TotalPages ?? 1;

        // Available specialties for filter dropdown
        public List<string> Specialites { get; set; } = new();
    }
}
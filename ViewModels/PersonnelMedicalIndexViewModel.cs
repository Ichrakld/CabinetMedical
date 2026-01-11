using GestionCabinetMedical.Models;

namespace GestionCabinetMedical.ViewModels
{
    /// <summary>
    /// ViewModel for PersonnelsMedicaux Index with pagination
    /// </summary>
    public class PersonnelMedicalIndexViewModel
    {
        // Personnel list with pagination
        public PaginatedList<PersonnelMedical> PersonnelMedicals { get; set; } = null!;

        // Statistics
        public int TotalPersonnel { get; set; }
        public int FonctionsDistinctes { get; set; }

        // Filters
        public string? SearchTerm { get; set; }
        public string? Fonction { get; set; }
        public string? SortBy { get; set; } = "nom";

        // Pagination
        public int PageSize { get; set; } = 10;
        public int CurrentPage => PersonnelMedicals?.CurrentPage ?? 1;
        public int TotalPages => PersonnelMedicals?.TotalPages ?? 1;

        // Available functions for filter dropdown
        public List<string> Fonctions { get; set; } = new();
    }
}
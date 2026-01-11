using GestionCabinetMedical.Models;

namespace GestionCabinetMedical.ViewModels
{
    /// <summary>
    /// ViewModel for Traitements Index with pagination and filtering
    /// </summary>
    public class TraitementIndexViewModel
    {
        // Traitements list with pagination
        public PaginatedList<Traitement> Traitements { get; set; } = null!;

        // Statistics
        public int TotalTraitements { get; set; }
        public int TraitementsRecents { get; set; } // Last 30 days

        // Filters
        public string? SearchTerm { get; set; }
        public string? SortBy { get; set; } = "recent";

        // Pagination
        public int PageSize { get; set; } = 10;
        public int CurrentPage => Traitements?.CurrentPage ?? 1;
        public int TotalPages => Traitements?.TotalPages ?? 1;
    }
}
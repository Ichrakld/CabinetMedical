using GestionCabinetMedical.Models;

namespace GestionCabinetMedical.ViewModels
{
    /// <summary>
    /// ViewModel for RessourcesMedicales Index with pagination and stock alerts
    /// </summary>
    public class RessourceMedicaleIndexViewModel
    {
        // Resources list with pagination
        public PaginatedList<RessourceMedicale> RessourceMedicales { get; set; } = null!;

        // Statistics
        public int TotalRessources { get; set; }
        public int RessourcesStockFaible { get; set; } // Quantité < 10
        public int QuantiteTotale { get; set; }

        // Filters
        public string? SearchTerm { get; set; }
        public string? StockFilter { get; set; } // "faible", "normal", "tous"
        public string? SortBy { get; set; } = "nom";

        // Pagination
        public int PageSize { get; set; } = 10;
        public int CurrentPage => RessourceMedicales?.CurrentPage ?? 1;
        public int TotalPages => RessourceMedicales?.TotalPages ?? 1;
    }
}
using GestionCabinetMedical.Models;

namespace GestionCabinetMedical.ViewModels
{
    /// <summary>
    /// ViewModel for Patient Index with filtering, sorting, and statistics
    /// </summary>
    public class PatientIndexViewModel
    {
        // Patient list
        public IEnumerable<PatientListItem> Patients { get; set; } = new List<PatientListItem>();

        // Statistics
        public int TotalPatients { get; set; }
        public int PatientsActifs { get; set; }
        public int PatientsInactifs { get; set; }
        public int PatientsAvecRdvRecent { get; set; }

        // Filters
        public string? SearchTerm { get; set; }
        public string? Statut { get; set; }
        public string? SortBy { get; set; } = "nom";

        // Pagination
        public int CurrentPage { get; set; } = 1;
        public int PageSize { get; set; } = 10;
        public int TotalPages { get; set; }
    }

    /// <summary>
    /// Lightweight patient item for list display
    /// </summary>
    public class PatientListItem
    {
        public int Id { get; set; }
        public string? Nom { get; set; }
        public string? Prenom { get; set; }
        public string? Email { get; set; }
        public string? Telephone { get; set; }
        public string? NumSecuriteSociale { get; set; }
        public DateTime? DateNaissance { get; set; }
        public bool EstActif { get; set; }
        public DateTime? DernierRdv { get; set; }
        public int NombreRdv { get; set; }
        public int NombreDossiers { get; set; }

        // Navigation property for compatibility
        public Utilisateur? IdNavigation { get; set; }

        // Computed properties
        public string Initials => GetInitials();

        public int? Age
        {
            get
            {
                if (!DateNaissance.HasValue) return null;
                var today = DateTime.Today;
                var age = today.Year - DateNaissance.Value.Year;
                if (DateNaissance.Value.Date > today.AddYears(-age)) age--;
                return age;
            }
        }

        private string GetInitials()
        {
            var n = string.IsNullOrEmpty(Nom) ? "" : Nom.Substring(0, 1);
            var p = string.IsNullOrEmpty(Prenom) ? "" : Prenom.Substring(0, 1);
            return (n + p).ToUpper();
        }
    }
}
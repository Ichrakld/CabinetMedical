using System;
using System.Collections.Generic;
using System.Linq;

namespace GestionCabinetMedical.ViewModels
{
    /// <summary>
    /// ViewModel pour la vue partielle de pagination
    /// </summary>
    public class PaginationPartialViewModel
    {
        public int CurrentPage { get; set; }
        public int TotalPages { get; set; }
        public int PageSize { get; set; }
        public int TotalCount { get; set; }

        public bool HasPreviousPage => CurrentPage > 1;
        public bool HasNextPage => CurrentPage < TotalPages;

        public int FirstItemIndex => TotalCount == 0 ? 0 : (CurrentPage - 1) * PageSize + 1;
        public int LastItemIndex => Math.Min(CurrentPage * PageSize, TotalCount);

        /// <summary>
        /// Nom de l'action à appeler (default: "Index")
        /// </summary>
        public string ActionName { get; set; } = "Index";

        /// <summary>
        /// Nom du contrôleur (null = contrôleur actuel)
        /// </summary>
        public string? ControllerName { get; set; }

        /// <summary>
        /// Paramètres de route additionnels (filtres, tri, etc.)
        /// </summary>
        public Dictionary<string, string?>? RouteValues { get; set; }

        /// <summary>
        /// ID unique pour le formulaire (utile si plusieurs paginations sur la même page)
        /// </summary>
        public string FormId { get; set; } = Guid.NewGuid().ToString("N")[..8];

        /// <summary>
        /// Génère les numéros de page à afficher (avec ellipses représentées par null)
        /// </summary>
        public IEnumerable<int?> GetPageNumbers(int maxVisiblePages = 5)
        {
            var pages = new List<int?>();

            if (TotalPages <= maxVisiblePages)
            {
                for (int i = 1; i <= TotalPages; i++)
                    pages.Add(i);
            }
            else
            {
                // Toujours afficher la première page
                pages.Add(1);

                int start = Math.Max(2, CurrentPage - 1);
                int end = Math.Min(TotalPages - 1, CurrentPage + 1);

                // Ajuster si on est proche du début
                if (CurrentPage <= 3)
                {
                    end = Math.Min(TotalPages - 1, 4);
                }

                // Ajuster si on est proche de la fin
                if (CurrentPage >= TotalPages - 2)
                {
                    start = Math.Max(2, TotalPages - 3);
                }

                // Ajouter ellipse si nécessaire
                if (start > 2)
                    pages.Add(null); // null représente "..."

                for (int i = start; i <= end; i++)
                    pages.Add(i);

                // Ajouter ellipse si nécessaire
                if (end < TotalPages - 1)
                    pages.Add(null);

                // Toujours afficher la dernière page
                if (TotalPages > 1)
                    pages.Add(TotalPages);
            }

            return pages;
        }

        /// <summary>
        /// Crée une instance à partir d'une PaginatedList
        /// </summary>
        public static PaginationPartialViewModel FromPaginatedList<T>(
            PaginatedList<T> list,
            Dictionary<string, string?>? routeValues = null,
            string actionName = "Index",
            string? controllerName = null)
        {
            return new PaginationPartialViewModel
            {
                CurrentPage = list.CurrentPage,
                TotalPages = list.TotalPages,
                PageSize = list.PageSize,
                TotalCount = list.TotalCount,
                ActionName = actionName,
                ControllerName = controllerName,
                RouteValues = routeValues
            };
        }
    }
}
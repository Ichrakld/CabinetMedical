using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace GestionCabinetMedical.ViewModels
{
    /// <summary>
    /// Classe de base pour la pagination générique
    /// Implémente le pattern Repository avec support de pagination
    /// </summary>
    /// <typeparam name="T">Type de l'entité à paginer</typeparam>
    public class PaginatedList<T> : List<T>
    {
        public int CurrentPage { get; private set; }
        public int TotalPages { get; private set; }
        public int PageSize { get; private set; }
        public int TotalCount { get; private set; }

        public bool HasPreviousPage => CurrentPage > 1;
        public bool HasNextPage => CurrentPage < TotalPages;

        public int FirstItemIndex => (CurrentPage - 1) * PageSize + 1;
        public int LastItemIndex => Math.Min(CurrentPage * PageSize, TotalCount);

        public PaginatedList(List<T> items, int count, int pageIndex, int pageSize)
        {
            TotalCount = count;
            PageSize = pageSize;
            CurrentPage = pageIndex;
            TotalPages = (int)Math.Ceiling(count / (double)pageSize);

            this.AddRange(items);
        }

        /// <summary>
        /// Crée une liste paginée à partir d'une requête IQueryable
        /// </summary>
        public static async Task<PaginatedList<T>> CreateAsync(
            IQueryable<T> source, int pageIndex, int pageSize)
        {
            var count = await source.CountAsync();
            pageIndex = Math.Max(1, Math.Min(pageIndex, Math.Max(1, (int)Math.Ceiling(count / (double)pageSize))));
            var items = await source.Skip((pageIndex - 1) * pageSize).Take(pageSize).ToListAsync();
            return new PaginatedList<T>(items, count, pageIndex, pageSize);
        }

        /// <summary>
        /// Crée une liste paginée à partir d'une liste en mémoire
        /// </summary>
        public static PaginatedList<T> Create(
            IEnumerable<T> source, int pageIndex, int pageSize)
        {
            var list = source.ToList();
            var count = list.Count;
            pageIndex = Math.Max(1, Math.Min(pageIndex, Math.Max(1, (int)Math.Ceiling(count / (double)pageSize))));
            var items = list.Skip((pageIndex - 1) * pageSize).Take(pageSize).ToList();
            return new PaginatedList<T>(items, count, pageIndex, pageSize);
        }

        /// <summary>
        /// Génère les numéros de page à afficher (avec ellipses)
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
    }

    /// <summary>
    /// Options de taille de page disponibles
    /// </summary>
    public static class PageSizeOptions
    {
        public static readonly int[] Sizes = { 5, 10, 25, 50, 100 };
        public const int Default = 10;

        public static int Validate(int? size)
        {
            if (!size.HasValue || !Sizes.Contains(size.Value))
                return Default;
            return size.Value;
        }
    }
}
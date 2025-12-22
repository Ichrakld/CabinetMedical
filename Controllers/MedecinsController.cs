using GestionCabinetMedical.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace GestionCabinetMedical.Controllers
{
    [Authorize(Roles = "ADMIN")]
    public class MedecinsController : Controller
    {
        private readonly BdCabinetMedicalContext _context;

        public MedecinsController(BdCabinetMedicalContext context)
        {
            _context = context;
        }

        // GET: Medecins
        public async Task<IActionResult> Index()
        {
            var bdCabinetMedicalContext = _context.Medecins.Include(m => m.IdNavigation);
            return View(await bdCabinetMedicalContext.ToListAsync());
        }

        // GET: Medecins/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var medecin = await _context.Medecins
                .Include(m => m.IdNavigation)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (medecin == null)
            {
                return NotFound();
            }

            return View(medecin);
        }

        // GET: Medecins/Create
        public IActionResult Create()
        {
            // Utilisation de la méthode helper pour afficher les noms
            PopulateUsersDropDownList();
            return View();
        }

        // POST: Medecins/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,Specialite")] Medecin medecin)
        {
            // On vérifie si l'ID existe déjà dans la table Medecins pour éviter les doublons
            if (MedecinExists(medecin.Id))
            {
                ModelState.AddModelError("Id", "Cet utilisateur est déjà enregistré comme médecin.");
            }

            if (ModelState.IsValid)
            {
                _context.Add(medecin);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            PopulateUsersDropDownList(medecin.Id);
            return View(medecin);
        }

        // GET: Medecins/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var medecin = await _context.Medecins
                .Include(m => m.IdNavigation) // Important pour afficher le nom en lecture seule
                .FirstOrDefaultAsync(m => m.Id == id);

            if (medecin == null)
            {
                return NotFound();
            }
            // Pas besoin de dropdown pour l'Edit car l'ID (l'utilisateur) ne change pas, 
            // on affiche juste son nom en texte brut dans la vue.
            return View(medecin);
        }

        // POST: Medecins/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Specialite")] Medecin medecin)
        {
            if (id != medecin.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(medecin);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!MedecinExists(medecin.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }

            // En cas d'erreur, on recharge les infos de navigation pour réafficher le nom
            var medecinReloaded = await _context.Medecins
                .Include(m => m.IdNavigation)
                .FirstOrDefaultAsync(m => m.Id == id);

            // On garde la spécialité modifiée par l'utilisateur si elle est valide
            if (medecinReloaded != null)
            {
                medecinReloaded.Specialite = medecin.Specialite;
                return View(medecinReloaded);
            }

            return View(medecin);
        }

        // GET: Medecins/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var medecin = await _context.Medecins
                .Include(m => m.IdNavigation)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (medecin == null)
            {
                return NotFound();
            }

            return View(medecin);
        }

        // POST: Medecins/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var medecin = await _context.Medecins.FindAsync(id);
            if (medecin != null)
            {
                _context.Medecins.Remove(medecin);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool MedecinExists(int id)
        {
            return _context.Medecins.Any(e => e.Id == id);
        }

        // HELPER pour afficher "Nom Prénom" dans la liste déroulante du Create
        private void PopulateUsersDropDownList(object selectedUser = null)
        {
            var usersQuery = _context.Utilisateurs
                .OrderBy(u => u.Nom)
                .Select(u => new {
                    Id = u.Id,
                    NomComplet = u.Nom + " " + u.Prenom + " (" + u.Email + ")"
                });

            ViewData["Id"] = new SelectList(usersQuery, "Id", "NomComplet", selectedUser);
        }
    }
}
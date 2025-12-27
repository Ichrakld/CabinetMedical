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
            return View();
        }

        // POST: Medecins/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Medecin medecin)
        {
            // IMPORTANT : On retire [Bind] pour accepter IdNavigation (les infos utilisateur)

            if (ModelState.IsValid)
            {
                // 1. On s'assure que l'utilisateur est marqué comme Actif
                if (medecin.IdNavigation != null)
                {
                    medecin.IdNavigation.EstActif = true;
                }

                // 2. Entity Framework est intelligent.
                // Puisque medecin.IdNavigation contient des données (Nom, Email...),
                // Il va d'abord insérer l'Utilisateur, récupérer l'ID,
                // puis insérer le Médecin lié à cet ID.
                _context.Add(medecin);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }

            // Si échec, on réaffiche le formulaire
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
        public async Task<IActionResult> Edit(int id, Medecin medecin)
        {
            if (id != medecin.Id)
            {
                return NotFound();
            }

            // 1. On ignore la validation du mot de passe (car on ne le modifie pas ici)
            ModelState.Remove("IdNavigation.MotDePasse");

            // On nettoie aussi les validations des autres relations pour éviter les erreurs inutiles
            ModelState.Remove("IdNavigation.Admin");
            ModelState.Remove("IdNavigation.Medecin");
            ModelState.Remove("IdNavigation.Patient");
            ModelState.Remove("IdNavigation.Secretaire");

            if (ModelState.IsValid)
            {
                try
                {
                    // 2. On charge l'entité existante depuis la base de données
                    var medecinExist = await _context.Medecins
                        .Include(m => m.IdNavigation)
                        .FirstOrDefaultAsync(m => m.Id == id);

                    if (medecinExist == null)
                    {
                        return NotFound();
                    }

                    // 3. On met à jour les champs manuellement
                    medecinExist.Specialite = medecin.Specialite;

                    // Mise à jour des infos utilisateur
                    medecinExist.IdNavigation.Nom = medecin.IdNavigation.Nom;
                    medecinExist.IdNavigation.Prenom = medecin.IdNavigation.Prenom;
                    medecinExist.IdNavigation.Email = medecin.IdNavigation.Email;
                    medecinExist.IdNavigation.Telephone = medecin.IdNavigation.Telephone;

                    // 4. On sauvegarde le tout
                    _context.Update(medecinExist);
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

            // Si échec, on recharge les données nécessaires pour la vue
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
            // On supprime l'utilisateur global, pas juste le rôle médecin
            var utilisateur = await _context.Utilisateurs.FindAsync(id);

            if (utilisateur != null)
            {
                _context.Utilisateurs.Remove(utilisateur);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool MedecinExists(int id)
        {
            return _context.Medecins.Any(e => e.Id == id);
        }
    }
}
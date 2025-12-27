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
    [Authorize(Roles = "ADMIN,MEDECIN,SECRETAIRE")]
    public class PatientsController : Controller
    {
        private readonly BdCabinetMedicalContext _context;

        public PatientsController(BdCabinetMedicalContext context)
        {
            _context = context;
        }

        // GET: Patients
        public async Task<IActionResult> Index()
        {
            var bdCabinetMedicalContext = _context.Patients.Include(p => p.IdNavigation);
            return View(await bdCabinetMedicalContext.ToListAsync());
        }

        // GET: Patients/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var patient = await _context.Patients
                .Include(p => p.IdNavigation)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (patient == null)
            {
                return NotFound();
            }

            return View(patient);
        }

        // GET: Patients/Create
        public IActionResult Create()
        {
            // On crée un nouveau patient avec un utilisateur vide mais Actif par défaut
            var patient = new Patient
            {
                IdNavigation = new Utilisateur
                {
                    EstActif = true // <--- Ceci cochera la case automatiquement
                }
            };

            return View(patient);
        }

        // POST: Patients/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Patient patient)
        {
            // 1. Nous avons retiré [Bind(...)] pour que les données de IdNavigation (Nom, Prenom, etc.) soient prises en compte.

            if (ModelState.IsValid)
            {
                // Entity Framework est intelligent : 
                // Il va d'abord créer l'Utilisateur (IdNavigation), 
                // récupérer son nouvel ID, 
                // puis créer le Patient avec cet ID.
                _context.Add(patient);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }

            // Si le formulaire n'est pas valide, on réaffiche la page
            return View(patient);
        }

        // GET: Patients/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            // CORRECTION : On remplace FindAsync par une requête avec Include
            var patient = await _context.Patients
                .Include(p => p.IdNavigation) // <--- C'est cette ligne qui charge le Nom et Prénom
                .FirstOrDefaultAsync(m => m.Id == id);

            if (patient == null)
            {
                return NotFound();
            }

            // Plus besoin de ViewData["Id"] car on modifie directement l'objet lié
            return View(patient);
        }

        // POST: Patients/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Patient patient)
        {
            if (id != patient.Id)
            {
                return NotFound();
            }

            // 1. On enlève l'erreur "Mot de passe requis" car on ne le modifie pas ici
            ModelState.Remove("IdNavigation.MotDePasse");

            // 2. On enlève aussi les erreurs potentielles sur les listes déroulantes (Admin, Medecin, etc.)
            ModelState.Remove("IdNavigation.Admin");
            ModelState.Remove("IdNavigation.Medecin");
            ModelState.Remove("IdNavigation.Patient");
            ModelState.Remove("IdNavigation.Secretaire");

            if (ModelState.IsValid)
            {
                try
                {
                    // 3. IMPORTANT : On charge le patient existant depuis la base de données
                    // avec ses infos utilisateur (pour récupérer l'ancien mot de passe)
                    var patientExist = await _context.Patients
                        .Include(p => p.IdNavigation)
                        .FirstOrDefaultAsync(p => p.Id == id);

                    if (patientExist == null)
                    {
                        return NotFound();
                    }

                    // 4. On met à jour manuellement les champs modifiés
                    // (On ne touche PAS au MotDePasse, donc il reste tel quel en base)
                    patientExist.NumSecuriteSociale = patient.NumSecuriteSociale;

                    patientExist.IdNavigation.Nom = patient.IdNavigation.Nom;
                    patientExist.IdNavigation.Prenom = patient.IdNavigation.Prenom;
                    patientExist.IdNavigation.Email = patient.IdNavigation.Email;
                    patientExist.IdNavigation.Telephone = patient.IdNavigation.Telephone;
                    patientExist.IdNavigation.EstActif = patient.IdNavigation.EstActif;

                    // 5. On sauvegarde
                    _context.Update(patientExist);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!PatientExists(patient.Id))
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

            // Si ça échoue encore, ceci affichera pourquoi
            return View(patient);
        }

        // GET: Patients/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var patient = await _context.Patients
                .Include(p => p.IdNavigation)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (patient == null)
            {
                return NotFound();
            }

            return View(patient);
        }

        // POST: Patients/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            // Au lieu de chercher le Patient, on cherche l'Utilisateur correspondant
            // (Ils partagent le même ID)
            var utilisateur = await _context.Utilisateurs.FindAsync(id);

            if (utilisateur != null)
            {
                // En supprimant l'Utilisateur, la base de données va 
                // AUTOMATIQUEMENT supprimer la ligne dans la table Patient
                // grâce à la contrainte "Cascade Delete".
                _context.Utilisateurs.Remove(utilisateur);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool PatientExists(int id)
        {
            return _context.Patients.Any(e => e.Id == id);
        }
    }
}

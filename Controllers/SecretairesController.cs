using GestionCabinetMedical.Models;
using GestionCabinetMedical.Areas.Identity.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace GestionCabinetMedical.Controllers
{
    [Authorize(Roles = "ADMIN")]
    public class SecretairesController : Controller
    {
        private readonly BdCabinetMedicalContext _context;
        private readonly UserManager<Userper> _userManager;

        public SecretairesController(BdCabinetMedicalContext context, UserManager<Userper> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index()
        {
            var secretaires = await _context.Secretaires.Include(s => s.IdNavigation).ToListAsync();
            return View(secretaires);
        }

        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();
            var secretaire = await _context.Secretaires.Include(s => s.IdNavigation).FirstOrDefaultAsync(m => m.Id == id);
            if (secretaire == null) return NotFound();
            return View(secretaire);
        }

        public IActionResult Create() => View();

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Secretaire secretaire)
        {
            if (ModelState.IsValid)
            {
                var identityUser = new Userper
                {
                    UserName = secretaire.IdNavigation.Email,
                    Email = secretaire.IdNavigation.Email,
                    Nom = $"{secretaire.IdNavigation.Nom} {secretaire.IdNavigation.Prenom}",
                    EmailConfirmed = true,
                    PhoneNumber = secretaire.IdNavigation.Telephone
                };

                var result = await _userManager.CreateAsync(identityUser, secretaire.IdNavigation.MotDePasse);
                if (result.Succeeded)
                {
                    await _userManager.AddToRoleAsync(identityUser, "SECRETAIRE");
                    secretaire.IdNavigation.EstActif = true;
                    _context.Add(secretaire);
                    await _context.SaveChangesAsync();
                    TempData["Success"] = $"Secrétaire {secretaire.IdNavigation.Nom} créé(e) avec succès !";
                    return RedirectToAction(nameof(Index));
                }
                foreach (var error in result.Errors)
                    ModelState.AddModelError(string.Empty, error.Description);
            }
            return View(secretaire);
        }

        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();
            var secretaire = await _context.Secretaires.Include(s => s.IdNavigation).FirstOrDefaultAsync(s => s.Id == id);
            if (secretaire == null) return NotFound();
            return View(secretaire);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Secretaire secretaire)
        {
            if (id != secretaire.Id) return NotFound();
            ModelState.Remove("IdNavigation.MotDePasse");
            ModelState.Remove("IdNavigation.Admin");
            ModelState.Remove("IdNavigation.Medecin");
            ModelState.Remove("IdNavigation.Patient");
            ModelState.Remove("IdNavigation.Secretaire");

            if (ModelState.IsValid)
            {
                try
                {
                    var exist = await _context.Secretaires.Include(s => s.IdNavigation).FirstOrDefaultAsync(s => s.Id == id);
                    if (exist == null) return NotFound();
                    var oldEmail = exist.IdNavigation.Email;

                    exist.Service = secretaire.Service;
                    exist.IdNavigation.Nom = secretaire.IdNavigation.Nom;
                    exist.IdNavigation.Prenom = secretaire.IdNavigation.Prenom;
                    exist.IdNavigation.Email = secretaire.IdNavigation.Email;
                    exist.IdNavigation.Telephone = secretaire.IdNavigation.Telephone;
                    exist.IdNavigation.EstActif = secretaire.IdNavigation.EstActif;

                    var identityUser = await _userManager.FindByEmailAsync(oldEmail);
                    if (identityUser != null)
                    {
                        identityUser.Email = secretaire.IdNavigation.Email;
                        identityUser.UserName = secretaire.IdNavigation.Email;
                        identityUser.Nom = $"{secretaire.IdNavigation.Nom} {secretaire.IdNavigation.Prenom}";
                        identityUser.PhoneNumber = secretaire.IdNavigation.Telephone;
                        await _userManager.UpdateAsync(identityUser);
                    }

                    _context.Update(exist);
                    await _context.SaveChangesAsync();
                    TempData["Success"] = "Secrétaire modifié(e) avec succès !";
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!SecretaireExists(secretaire.Id)) return NotFound();
                    throw;
                }
                return RedirectToAction(nameof(Index));
            }
            return View(secretaire);
        }

        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();
            var secretaire = await _context.Secretaires.Include(s => s.IdNavigation).FirstOrDefaultAsync(m => m.Id == id);
            if (secretaire == null) return NotFound();
            return View(secretaire);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var secretaire = await _context.Secretaires.Include(s => s.IdNavigation).FirstOrDefaultAsync(s => s.Id == id);
                if (secretaire != null)
                {
                    var email = secretaire.IdNavigation.Email;
                    var notifications = await _context.Notifications.Where(n => n.UserId == id).ToListAsync();
                    if (notifications.Any()) _context.Notifications.RemoveRange(notifications);
                    await _context.SaveChangesAsync();

                    _context.Secretaires.Remove(secretaire);
                    await _context.SaveChangesAsync();

                    var utilisateur = await _context.Utilisateurs.FindAsync(id);
                    if (utilisateur != null)
                    {
                        _context.Utilisateurs.Remove(utilisateur);
                        await _context.SaveChangesAsync();
                    }

                    var identityUser = await _userManager.FindByEmailAsync(email);
                    if (identityUser != null) await _userManager.DeleteAsync(identityUser);

                    await transaction.CommitAsync();
                    TempData["Success"] = "Secrétaire supprimé(e) avec succès !";
                }
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                TempData["Error"] = $"Erreur lors de la suppression : {ex.Message}";
            }
            return RedirectToAction(nameof(Index));
        }

        private bool SecretaireExists(int id) => _context.Secretaires.Any(e => e.Id == id);
    }
}

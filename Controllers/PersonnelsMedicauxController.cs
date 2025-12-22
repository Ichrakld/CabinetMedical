using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using GestionCabinetMedical.Models;
using Microsoft.AspNetCore.Authorization;

namespace GestionCabinetMedical.Controllers
{
    [Authorize(Roles = "ADMIN")]
    public class PersonnelsMedicauxController : Controller
    {
        private readonly BdCabinetMedicalContext _context;

        public PersonnelsMedicauxController(BdCabinetMedicalContext context)
        {
            _context = context;
        }

        // GET: PersonnelsMedicaux
        public async Task<IActionResult> Index()
        {
            // Pas de .Include() nécessaire car pas de relation
            return View(await _context.PersonnelMedicals.ToListAsync());
        }

        // GET: PersonnelsMedicaux/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var personnelMedical = await _context.PersonnelMedicals
                .FirstOrDefaultAsync(m => m.Id == id);

            if (personnelMedical == null) return NotFound();

            return View(personnelMedical);
        }

        // GET: PersonnelsMedicaux/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: PersonnelsMedicaux/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,Nom,Fonction")] PersonnelMedical personnelMedical)
        {
            if (ModelState.IsValid)
            {
                _context.Add(personnelMedical);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(personnelMedical);
        }

        // GET: PersonnelsMedicaux/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var personnelMedical = await _context.PersonnelMedicals.FindAsync(id);
            if (personnelMedical == null) return NotFound();

            return View(personnelMedical);
        }

        // POST: PersonnelsMedicaux/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Nom,Fonction")] PersonnelMedical personnelMedical)
        {
            if (id != personnelMedical.Id) return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(personnelMedical);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!PersonnelMedicalExists(personnelMedical.Id)) return NotFound();
                    else throw;
                }
                return RedirectToAction(nameof(Index));
            }
            return View(personnelMedical);
        }

        // GET: PersonnelsMedicaux/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var personnelMedical = await _context.PersonnelMedicals
                .FirstOrDefaultAsync(m => m.Id == id);

            if (personnelMedical == null) return NotFound();

            return View(personnelMedical);
        }

        // POST: PersonnelsMedicaux/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var personnelMedical = await _context.PersonnelMedicals.FindAsync(id);
            if (personnelMedical != null)
            {
                _context.PersonnelMedicals.Remove(personnelMedical);
            }
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool PersonnelMedicalExists(int id)
        {
            return _context.PersonnelMedicals.Any(e => e.Id == id);
        }
    }
}
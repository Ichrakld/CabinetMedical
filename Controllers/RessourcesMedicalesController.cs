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
    [Authorize(Roles = "ADMIN,SECRETAIRE")]
    public class RessourcesMedicalesController : Controller
    {
        private readonly BdCabinetMedicalContext _context;

        public RessourcesMedicalesController(BdCabinetMedicalContext context)
        {
            _context = context;
        }

        // GET: RessourceMedicales
        public async Task<IActionResult> Index()
        {
            return View(await _context.RessourceMedicales.ToListAsync());
        }

        // GET: RessourceMedicales/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var ressourceMedicale = await _context.RessourceMedicales
                .FirstOrDefaultAsync(m => m.Id == id);
            if (ressourceMedicale == null)
            {
                return NotFound();
            }

            return View(ressourceMedicale);
        }

        // GET: RessourceMedicales/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: RessourceMedicales/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,Nom,Quantite")] RessourceMedicale ressourceMedicale)
        {
            if (ModelState.IsValid)
            {
                _context.Add(ressourceMedicale);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(ressourceMedicale);
        }

        // GET: RessourceMedicales/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var ressourceMedicale = await _context.RessourceMedicales.FindAsync(id);
            if (ressourceMedicale == null)
            {
                return NotFound();
            }
            return View(ressourceMedicale);
        }

        // POST: RessourceMedicales/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Nom,Quantite")] RessourceMedicale ressourceMedicale)
        {
            if (id != ressourceMedicale.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(ressourceMedicale);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!RessourceMedicaleExists(ressourceMedicale.Id))
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
            return View(ressourceMedicale);
        }

        // GET: RessourceMedicales/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var ressourceMedicale = await _context.RessourceMedicales
                .FirstOrDefaultAsync(m => m.Id == id);
            if (ressourceMedicale == null)
            {
                return NotFound();
            }

            return View(ressourceMedicale);
        }

        // POST: RessourceMedicales/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var ressourceMedicale = await _context.RessourceMedicales.FindAsync(id);
            if (ressourceMedicale != null)
            {
                _context.RessourceMedicales.Remove(ressourceMedicale);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool RessourceMedicaleExists(int id)
        {
            return _context.RessourceMedicales.Any(e => e.Id == id);
        }
    }
}

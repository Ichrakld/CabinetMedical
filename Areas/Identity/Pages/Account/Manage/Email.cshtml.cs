using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel.DataAnnotations;
using GestionCabinetMedical.Areas.Identity.Data;

namespace GestionCabinetMedical.Areas.Identity.Pages.Account.Manage
{
    public class EmailModel : PageModel
    {
        private readonly UserManager<Userper> _userManager;
        private readonly SignInManager<Userper> _signInManager;

        public EmailModel(UserManager<Userper> userManager, SignInManager<Userper> signInManager)
        {
            _userManager = userManager;
            _signInManager = signInManager;
        }

        public string Email { get; set; }

        [TempData]
        public string StatusMessage { get; set; }

        [BindProperty]
        public InputModel Input { get; set; }

        public class InputModel
        {
            [Required]
            [EmailAddress]
            [Display(Name = "Nouvel email")]
            public string NewEmail { get; set; }
        }

        public async Task<IActionResult> OnGetAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return NotFound();

            Email = await _userManager.GetEmailAsync(user);
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return NotFound();

            if (!ModelState.IsValid)
            {
                Email = await _userManager.GetEmailAsync(user);
                return Page();
            }

            var email = await _userManager.GetEmailAsync(user);
            if (Input.NewEmail != email)
            {
                var setEmailResult = await _userManager.SetEmailAsync(user, Input.NewEmail);
                if (!setEmailResult.Succeeded)
                {
                    StatusMessage = "Erreur lors de la modification de l'email.";
                    return RedirectToPage();
                }

                await _userManager.SetUserNameAsync(user, Input.NewEmail);
                await _signInManager.RefreshSignInAsync(user);
                StatusMessage = "Email modifié avec succès.";
            }

            return RedirectToPage();
        }
    }
}
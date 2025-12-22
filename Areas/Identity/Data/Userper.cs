using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace GestionCabinetMedical.Areas.Identity.Data
{
    public class Userper : IdentityUser
    {
        [Required]
        [StringLength(100)]
        public string Nom { get; set; }
    }
}
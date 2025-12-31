using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace GestionCabinetMedical.Models;

public partial class Patient
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Le numéro de sécurité sociale est requis")]
    [Display(Name = "N° Sécurité Sociale")]
    public string NumSecuriteSociale { get; set; } = null!;

    [Display(Name = "Date de Naissance")]
    [DataType(DataType.Date)]
    public DateTime? DateNaissance { get; set; }

    // Propriété calculée pour l'âge
    public int? Age
    {
        get
        {
            if (!DateNaissance.HasValue) return null;
            var today = DateTime.Today;
            var age = today.Year - DateNaissance.Value.Year;
            if (DateNaissance.Value.Date > today.AddYears(-age)) age--;
            return age;
        }
    }

    public virtual ICollection<DossierMedical> DossierMedicals { get; set; } = new List<DossierMedical>();

    public virtual Utilisateur IdNavigation { get; set; } = null!;

    public virtual ICollection<RendezVou> RendezVous { get; set; } = new List<RendezVou>();
}
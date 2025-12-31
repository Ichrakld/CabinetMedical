using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace GestionCabinetMedical.Models;

/// <summary>
/// Attribut de validation personnalisé pour vérifier que la date est dans le futur
/// </summary>
public class FutureDateAttribute : ValidationAttribute
{
    public FutureDateAttribute() : base("La date et l'heure doivent être supérieures à la date/heure actuelle.")
    {
    }

    public override bool IsValid(object? value)
    {
        if (value is DateTime dateTime)
        {
            return dateTime > DateTime.Now;
        }
        return false;
    }
}

public partial class RendezVou
{
    public int NumCom { get; set; }

    [Required(ErrorMessage = "La date et l'heure sont requises")]
    [FutureDate(ErrorMessage = "La date et l'heure du rendez-vous doivent être supérieures à maintenant")]
    [Display(Name = "Date et Heure")]
    public DateTime DateHeure { get; set; }

    [Display(Name = "Statut")]
    public string? Statut { get; set; }

    [Required(ErrorMessage = "Veuillez sélectionner un médecin")]
    [Display(Name = "Médecin")]
    public int MedecinId { get; set; }

    [Required(ErrorMessage = "Veuillez sélectionner un patient")]
    [Display(Name = "Patient")]
    public int PatientId { get; set; }

    public virtual Medecin Medecin { get; set; } = null!;
    public virtual ICollection<Notification> Notifications { get; set; } = new List<Notification>();
   

    public virtual Patient Patient { get; set; } = null!;
}
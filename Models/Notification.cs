using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GestionCabinetMedical.Models;

public partial class Notification
{
    public int Id { get; set; }

    [Required]
    [StringLength(100)]
    public string Type { get; set; } = null!;
    // Types: "Rappel RDV", "Confirmation", "Annulation", "Succès", "Erreur"

    [Required]
    public string Message { get; set; } = null!;

    public DateTime DateCreation { get; set; } = DateTime.Now;

  
    /// Indique si la notification a été lue par l'utilisateur
  
    public bool EstLue { get; set; } = false;

    /// ID de l'utilisateur destinataire de la notification
   
    public int UserId { get; set; }

    public int? RendezVousId { get; set; }
    public virtual RendezVou? RendezVous { get; set; }

  
    /// Utilisateur destinataire de la notification
  
    public virtual Utilisateur? User { get; set; }
}
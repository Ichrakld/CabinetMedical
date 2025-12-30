using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace GestionCabinetMedical.Models;

public partial class Traitement
{
    public int NumPro { get; set; }

    [Required(ErrorMessage = "Le type de traitement est requis")]
    [StringLength(1000, ErrorMessage = "Le traitement ne peut pas dépasser 1000 caractères")]
    public string TypeTraitement { get; set; } = null!;

    public int ConsultationId { get; set; }

    public virtual Consultation Consultation { get; set; } = null!;
}

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace GestionCabinetMedical.Models;

public partial class DossierMedical
{
    public int NumDossier { get; set; }

    [Display(Name = "Groupe Sanguin")]
    [StringLength(10)]
    public string? GroupeSanguin { get; set; }

    [Display(Name = "Allergies")]
    [StringLength(500)]
    public string? Allergies { get; set; }

    [Display(Name = "Antécédents Médicaux")]
    public string? AntecedentsMedicaux { get; set; }

    public int PatientId { get; set; }

    public int MedecinId { get; set; }

    public virtual Medecin Medecin { get; set; } = null!;

    public virtual Patient Patient { get; set; } = null!;

    public virtual ICollection<Consultation> Consultations { get; set; } = new List<Consultation>();
}
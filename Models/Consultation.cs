using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace GestionCabinetMedical.Models;

public partial class Consultation
{
    public int NumDetail { get; set; }

    [Display(Name = "Date de Consultation")]
    public DateTime DateConsultation { get; set; }

    [Display(Name = "Diagnostic")]
    [StringLength(500)]
    public string? Diagnostic { get; set; }

    [Display(Name = "Notes")]
    public string? Notes { get; set; }

    public int DossierMedicalId { get; set; }

    public virtual DossierMedical DossierMedical { get; set; } = null!;

    public virtual ICollection<Traitement> Traitements { get; set; } = new List<Traitement>();
}
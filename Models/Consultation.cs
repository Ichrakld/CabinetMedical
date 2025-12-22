using System;
using System.Collections.Generic;

namespace GestionCabinetMedical.Models;

public partial class Consultation
{
    public int NumDetail { get; set; }

    public string? Diagnostic { get; set; }

    public DateTime? DateConsultation { get; set; }

    public int DossierMedicalId { get; set; }

    public virtual DossierMedical DossierMedical { get; set; } = null!;

    public virtual ICollection<Traitement> Traitements { get; set; } = new List<Traitement>();
}

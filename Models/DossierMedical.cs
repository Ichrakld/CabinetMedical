using System;
using System.Collections.Generic;

namespace GestionCabinetMedical.Models;

public partial class DossierMedical
{
    public int NumDossier { get; set; }

    public string? GroupeSanguin { get; set; }

    public int PatientId { get; set; }

    public int MedecinId { get; set; }

    public virtual ICollection<Consultation> Consultations { get; set; } = new List<Consultation>();

    public virtual Medecin Medecin { get; set; } = null!;

    public virtual Patient Patient { get; set; } = null!;
}

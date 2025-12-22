using System;
using System.Collections.Generic;

namespace GestionCabinetMedical.Models;

public partial class Traitement
{
    public int NumPro { get; set; }

    public string TypeTraitement { get; set; } = null!;

    public int ConsultationId { get; set; }

    public virtual Consultation Consultation { get; set; } = null!;
}

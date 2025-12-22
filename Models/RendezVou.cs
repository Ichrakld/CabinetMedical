using System;
using System.Collections.Generic;

namespace GestionCabinetMedical.Models;

public partial class RendezVou
{
    public int NumCom { get; set; }

    public DateTime DateHeure { get; set; }

    public string? Statut { get; set; }

    public int MedecinId { get; set; }

    public int PatientId { get; set; }

    public virtual Medecin Medecin { get; set; } = null!;

    public virtual ICollection<Notification> Notifications { get; set; } = new List<Notification>();

    public virtual Patient Patient { get; set; } = null!;
}

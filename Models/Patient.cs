using System;
using System.Collections.Generic;

namespace GestionCabinetMedical.Models;

public partial class Patient
{
    public int Id { get; set; }

    public string NumSecuriteSociale { get; set; } = null!;

    public virtual ICollection<DossierMedical> DossierMedicals { get; set; } = new List<DossierMedical>();

    public virtual Utilisateur IdNavigation { get; set; } = null!;

    public virtual ICollection<RendezVou> RendezVous { get; set; } = new List<RendezVou>();
}

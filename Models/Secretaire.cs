using System;
using System.Collections.Generic;

namespace GestionCabinetMedical.Models;

public partial class Secretaire
{
    public int Id { get; set; }

    public string? Service { get; set; }

    public virtual Utilisateur IdNavigation { get; set; } = null!;
}

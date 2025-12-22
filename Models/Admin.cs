using System;
using System.Collections.Generic;

namespace GestionCabinetMedical.Models;

public partial class Admin
{
    public int Id { get; set; }

    public int NiveauAcces { get; set; }

    public virtual Utilisateur IdNavigation { get; set; } = null!;
}

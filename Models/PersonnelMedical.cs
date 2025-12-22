using System;
using System.Collections.Generic;

namespace GestionCabinetMedical.Models;

public partial class PersonnelMedical
{
    public int Id { get; set; }

    public string Nom { get; set; } = null!;

    public string Fonction { get; set; } = null!;
}

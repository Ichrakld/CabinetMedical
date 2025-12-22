using System;
using System.Collections.Generic;

namespace GestionCabinetMedical.Models;

public partial class RessourceMedicale
{
    public int Id { get; set; }

    public string Nom { get; set; } = null!;

    public int Quantite { get; set; }
}

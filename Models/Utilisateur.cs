using System;
using System.Collections.Generic;

namespace GestionCabinetMedical.Models;

public partial class Utilisateur
{
    public int Id { get; set; }

    public string Nom { get; set; } = null!;

    public string Prenom { get; set; } = null!;

    public string Email { get; set; } = null!;

    public string MotDePasse { get; set; } = null!;

    public string? Telephone { get; set; }

    public bool EstActif { get; set; } = true;

    public virtual Admin? Admin { get; set; }

    public virtual Medecin? Medecin { get; set; }

    public virtual Patient? Patient { get; set; }

    public virtual Secretaire? Secretaire { get; set; }
}

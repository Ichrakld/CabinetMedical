using System;
using System.Collections.Generic;

namespace GestionCabinetMedical.Models;

public partial class Notification
{
    public int Id { get; set; }

    public string Type { get; set; } = null!;

    public string Message { get; set; } = null!;

    public DateTime? DateCreation { get; set; }

    public int? RendezVousId { get; set; }

    public virtual RendezVou? RendezVous { get; set; }
}

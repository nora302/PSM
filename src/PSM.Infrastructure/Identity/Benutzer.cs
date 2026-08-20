using Microsoft.AspNetCore.Identity;
using PSM.Domain.Entities;

namespace PSM.Infrastructure.Identity;

public class Benutzer : IdentityUser
{
    public string Vorname { get; set; } = string.Empty;

    public string Nachname { get; set; } = string.Empty;

    public int? StandortId { get; set; }

    public Standort? Standort { get; set; }

    public bool IstAktiv { get; set; } = true;
}
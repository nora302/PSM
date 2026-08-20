using PSM.Domain.Enums;

namespace PSM.Domain.Entities;

public class Pflegedokumentation
{
    public Guid Id { get; set; }

    public Guid BewohnerId { get; set; }

    public Bewohner Bewohner { get; set; } = null!;

    // Jour auquel le Pflegebericht appartient
    public DateOnly Datum { get; set; }

    // Frühschicht, Spätdienst ou Nachtschicht
    public PflegeSchicht Schicht { get; set; }

    public string Inhalt { get; set; } = string.Empty;

    // Moment exact de création
    public DateTime ErstelltAm { get; set; }

    public DateTime? GeaendertAm { get; set; }

    // Pflegefachkraft qui a créé le Bericht
    public string ErstelltVonBenutzerId { get; set; } = string.Empty;

    // Pflegefachkraft qui l'a modifié en dernier
    public string? GeaendertVonBenutzerId { get; set; }

    public bool MitSpracheErstellt { get; set; }
}
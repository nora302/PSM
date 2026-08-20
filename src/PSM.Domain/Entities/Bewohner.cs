using PSM.Domain.Enums;

namespace PSM.Domain.Entities;

public class Bewohner
{
    public Guid Id { get; set; }

    public int StandortId { get; set; }
    
    public Standort Standort { get; set; } = null!;

    public int StandortBewohnerNummer { get; set; }

    public string Vorname { get; set; } = string.Empty;

    public string Nachname { get; set; } = string.Empty;

    public DateOnly Geburtsdatum { get; set; }

    public string Zimmernummer { get; set; } = string.Empty;

    public BewohnerStatus Status { get; set; }

    public DateTime ErstelltAm { get; set; }

    public DateTime? GeaendertAm { get; set; }
}
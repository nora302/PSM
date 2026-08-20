namespace PSM.Domain.Entities;

public class Lebensmittelbestellung
{
    public Guid Id { get; set; }

    public int StandortId { get; set; }

    public Standort Standort { get; set; } = null!;

    public DateTime Bestelldatum { get; set; }

    public DateOnly Lieferdatum { get; set; }

    public string Status { get; set; } = string.Empty;

    public string ErstelltVonBenutzerId { get; set; } = string.Empty;

    public string? BearbeitetVonBenutzerId { get; set; }

    public DateTime? ErledigtAm { get; set; }

    public string Bemerkung { get; set; } = string.Empty;
}
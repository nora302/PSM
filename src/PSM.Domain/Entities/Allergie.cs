namespace PSM.Domain.Entities;

public class Allergie
{
    public Guid Id { get; set; }

    public Guid BewohnerId { get; set; }

    public Bewohner Bewohner { get; set; } = null!;

    public string Name { get; set; } = string.Empty;

    public string Bemerkung { get; set; } = string.Empty;

    public bool IstAktiv { get; set; } = true;

    public DateTime ErstelltAm { get; set; }

    public DateTime? GeaendertAm { get; set; }
}
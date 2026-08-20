namespace PSM.Domain.Entities;

public class Ernaehrung
{
    public Guid Id { get; set; }

    public Guid BewohnerId { get; set; }

    public Bewohner Bewohner { get; set; } = null!;

    public string Fruehstueck { get; set; } = string.Empty;

    public string Mittagessen { get; set; } = string.Empty;

    public string Abendessen { get; set; } = string.Empty;

    public string Besonderheiten { get; set; } = string.Empty;

    public string Kostform { get; set; } = string.Empty;

    public DateTime ErstelltAm { get; set; }

    public DateTime? GeaendertAm { get; set; }
}
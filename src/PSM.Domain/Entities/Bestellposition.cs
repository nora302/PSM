namespace PSM.Domain.Entities;

public class Bestellposition
{
    public Guid Id { get; set; }

    public Guid LebensmittelbestellungId { get; set; }

    public Lebensmittelbestellung Lebensmittelbestellung { get; set; } = null!;

    public string Lebensmittelname { get; set; } = string.Empty;

    public decimal Menge { get; set; }

    public string Einheit { get; set; } = string.Empty;

    public string Bemerkung { get; set; } = string.Empty;
}
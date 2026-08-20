namespace PSM.Domain.Entities;

public class BewohnerStandortHistorie
{
    public Guid Id { get; set; }

    public Guid BewohnerId { get; set; }

    public Bewohner Bewohner { get; set; } = null!;

    public int AlterStandortId { get; set; }

    public int NeuerStandortId { get; set; }

    public int AlteStandortBewohnerNummer { get; set; }

    public int NeueStandortBewohnerNummer { get; set; }

    public string AlteZimmernummer { get; set; } = string.Empty;

    public string NeueZimmernummer { get; set; } = string.Empty;

    public DateTime GeaendertAm { get; set; }

    public string GeaendertVonBenutzerId { get; set; } = string.Empty;
}   
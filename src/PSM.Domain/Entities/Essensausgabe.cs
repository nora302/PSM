namespace PSM.Domain.Entities;

public class Essensausgabe
{
    public Guid Id { get; set; }

    public Guid BewohnerId { get; set; }

    public Bewohner Bewohner { get; set; } = null!;

    public DateOnly Datum { get; set; }

    public string Mahlzeit { get; set; } = string.Empty;

    public bool IstErledigt { get; set; }

    public DateTime? ErledigtAm { get; set; }

    public string? ErledigtVonBenutzerId { get; set; }
}
namespace PSM.Domain.Entities;

public class Standort
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string Code { get; set; } = string.Empty;

    public string Strasse { get; set; } = string.Empty;

    public string Hausnummer { get; set; } = string.Empty;

    public string Postleitzahl { get; set; } = string.Empty;

    public string Ort { get; set; } = string.Empty;

    public string Telefonnummer { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public bool IstAktiv { get; set; } = true;
}
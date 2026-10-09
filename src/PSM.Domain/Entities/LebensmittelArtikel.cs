namespace PSM.Domain.Entities;

public class LebensmittelArtikel
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public bool IstAktiv { get; set; } = true;
    public DateTime ErstelltAm { get; set; }
    public string ErstelltVonBenutzerId { get; set; } = string.Empty;
}
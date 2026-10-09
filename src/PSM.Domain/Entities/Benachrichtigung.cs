namespace PSM.Domain.Entities;

public class Benachrichtigung
{
    public Guid Id { get; set; }
    public int StandortId { get; set; }
    public Standort Standort { get; set; } = null!;
    public Guid? BewohnerId { get; set; }
    public string Typ { get; set; } = string.Empty;
    public string Nachricht { get; set; } = string.Empty;
    public string EmpfaengerRolle { get; set; } = string.Empty;
    public bool Gelesen { get; set; }
    public DateTime ErstelltAm { get; set; }
}
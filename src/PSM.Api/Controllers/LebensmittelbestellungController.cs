using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MigraDoc.DocumentObjectModel;
using MigraDoc.Rendering;
using PSM.Domain.Entities;
using PSM.Infrastructure.Data;
using System.Security.Claims;

namespace PSM.Api.Controllers;

[ApiController]
[Route("api/lebensmittelbestellungen")]
[Authorize]
public class LebensmittelbestellungController : ControllerBase
{
    private readonly AppDbContext _context;

    public LebensmittelbestellungController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<IActionResult> Alle()
    {
        var rolle = User.FindFirstValue(ClaimTypes.Role);
        var benutzerId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (rolle == "Administrator" || rolle == "Kuechenmitarbeiter")
        {
            var alle = await _context.Lebensmittelbestellungen
                .Include(b => b.Standort)
                .OrderByDescending(b => b.Bestelldatum)
                .ToListAsync();

            return Ok(alle);
        }

        var benutzer = await _context.Users
            .FirstOrDefaultAsync(u => u.Id == benutzerId);

        if (benutzer == null)
        {
            return Unauthorized();
        }

        if (benutzer.StandortId == null)
        {
            return Forbid();
        }

        var eigene = await _context.Lebensmittelbestellungen
            .Include(b => b.Standort)
            .Where(b => b.StandortId == benutzer.StandortId)
            .OrderByDescending(b => b.Bestelldatum)
            .ToListAsync();

        return Ok(eigene);
    }

    private async Task<string> BenutzerNameLaden(string? benutzerId)
    {
        if (string.IsNullOrEmpty(benutzerId))
        {
            return "-";
        }

        var benutzer = await _context.Users
            .FirstOrDefaultAsync(u => u.Id == benutzerId);

        if (benutzer == null)
        {
            return "-";
        }

        var rolleName = await (
            from ur in _context.UserRoles
            join r in _context.Roles on ur.RoleId equals r.Id
            where ur.UserId == benutzerId
            select r.Name
        ).FirstOrDefaultAsync();

        var rolleAnzeige = rolleName switch
        {
            "Administrator" => "Admin",
            "Hauswirtschaftskraft" => "Hauswirtschaft",
            "Pflegekraft" => "Pflegekraft",
            "Kuechenmitarbeiter" => "Küche",
            _ => ""
        };

        return string.IsNullOrEmpty(rolleAnzeige)
            ? $"{benutzer.Vorname} {benutzer.Nachname}"
            : $"{benutzer.Vorname} {benutzer.Nachname} ({rolleAnzeige})";
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> NachId(Guid id)
    {
        var bestellung = await _context.Lebensmittelbestellungen
            .Include(b => b.Standort)
            .FirstOrDefaultAsync(b => b.Id == id);

        if (bestellung == null)
        {
            return NotFound(new { message = "Lebensmittelbestellung wurde nicht gefunden." });
        }

        var rolle = User.FindFirstValue(ClaimTypes.Role);

        if (rolle != "Administrator" && rolle != "Kuechenmitarbeiter")
        {
            var benutzerId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var benutzer = await _context.Users
                .FirstOrDefaultAsync(u => u.Id == benutzerId);

            if (benutzer == null || benutzer.StandortId != bestellung.StandortId)
            {
                return Forbid();
            }
        }

        var erstelltVonName = await BenutzerNameLaden(bestellung.ErstelltVonBenutzerId);

        return Ok(new
        {
            bestellung.Id,
            bestellung.StandortId,
            bestellung.Standort,
            bestellung.Bestellnummer,
            bestellung.Bestelldatum,
            bestellung.Lieferdatum,
            bestellung.Status,
            bestellung.ErstelltVonBenutzerId,
            ErstelltVonName = erstelltVonName,
            bestellung.GesendetVonBenutzerId,
            bestellung.GesendetAm,
            bestellung.BearbeitungGestartetVonBenutzerId,
            bestellung.BearbeitungGestartetAm,
            bestellung.ErledigtVonBenutzerId,
            bestellung.ErledigtAm,
            bestellung.Bemerkung
        });
    }

    [HttpGet("standorte-uebersicht")]
    [Authorize(Roles = "Administrator,Kuechenmitarbeiter")]
    public async Task<IActionResult> StandorteUebersicht()
    {
        var heute = DateOnly.FromDateTime(DateTime.UtcNow);

        var standorte = await _context.Standorte
            .Where(s => s.IstAktiv)
            .OrderBy(s => s.Name)
            .ToListAsync();

        var ergebnis = new List<object>();

        foreach (var standort in standorte)
        {
            var bestellung = await _context.Lebensmittelbestellungen
                .Where(b => b.StandortId == standort.Id && b.Lieferdatum == heute)
                .OrderByDescending(b => b.Bestelldatum)
                .FirstOrDefaultAsync();

            string status;

            if (bestellung == null)
            {
                status = "KeineBestellung";
            }
            else if (bestellung.Status == "Erledigt")
            {
                status = "Erledigt";
            }
            else
            {
                status = "NichtErledigt";
            }

            ergebnis.Add(new
            {
                standort.Id,
                standort.Name,
                standort.Code,
                Status = status,
                BestellungId = bestellung?.Id,
                Bestellnummer = bestellung?.Bestellnummer
            });
        }

        return Ok(ergebnis);
    }

    private async Task<string> BestellnummerGenerieren(int standortId, DateTime bestelldatum)
    {
        var standort = await _context.Standorte
            .FirstOrDefaultAsync(s => s.Id == standortId);

        var code = standort?.Code ?? "STD";
        var datumTeil = bestelldatum.ToString("ddMMyy");

        var praefix = $"{code}-{datumTeil}";

        var anzahlHeute = await _context.Lebensmittelbestellungen
            .CountAsync(b => b.Bestellnummer.StartsWith(praefix));

        var laufnummer = anzahlHeute + 1;

        return $"{praefix}-{laufnummer}";
    }

    [HttpPost]
    [Authorize(Roles = "Administrator,Hauswirtschaftskraft")]
    public async Task<IActionResult> Erstellen(LebensmittelbestellungErstellenRequest request)
    {
        var rolle = User.FindFirstValue(ClaimTypes.Role);
        var benutzerId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        var benutzer = await _context.Users
            .FirstOrDefaultAsync(u => u.Id == benutzerId);

        if (benutzer == null)
        {
            return Unauthorized();
        }

        int standortId;

        if (rolle == "Administrator")
        {
            if (request.StandortId == null)
            {
                return BadRequest(new { message = "Bitte einen Standort auswählen." });
            }

            var standortExistiert = await _context.Standorte
                .AnyAsync(s => s.Id == request.StandortId.Value);

            if (!standortExistiert)
            {
                return BadRequest(new { message = $"Standort mit Id {request.StandortId} existiert nicht." });
            }

            standortId = request.StandortId.Value;
        }
        else
        {
            if (benutzer.StandortId == null)
            {
                return BadRequest(new { message = "Der Benutzer ist keinem Standort zugeordnet." });
            }

            standortId = benutzer.StandortId.Value;
        }

        if (request.Lieferdatum < DateOnly.FromDateTime(DateTime.UtcNow))
        {
            return BadRequest(new { message = "Das Lieferdatum darf nicht in der Vergangenheit liegen." });
        }

        var bestellungExistiertBereits = await _context.Lebensmittelbestellungen
            .AnyAsync(b => b.StandortId == standortId && b.Lieferdatum == request.Lieferdatum);

        if (bestellungExistiertBereits)
        {
            return BadRequest(new { message = $"Für diesen Standort existiert bereits eine Bestellung mit Lieferdatum {request.Lieferdatum}." });
        }

        var bestelldatum = DateTime.UtcNow;
        var bestellnummer = await BestellnummerGenerieren(standortId, bestelldatum);

        var bestellung = new Lebensmittelbestellung
        {
            Id = Guid.NewGuid(),
            StandortId = standortId,
            Bestellnummer = bestellnummer,
            Bestelldatum = bestelldatum,
            Lieferdatum = request.Lieferdatum,
            Status = "Entwurf",
            ErstelltVonBenutzerId = benutzer.Id,
            Bemerkung = request.Bemerkung
        };

        _context.Lebensmittelbestellungen.Add(bestellung);

        await _context.SaveChangesAsync();

        return Ok(bestellung);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Roles = "Administrator,Hauswirtschaftskraft")]
    public async Task<IActionResult> Bearbeiten(Guid id, LebensmittelbestellungBearbeitenRequest request)
    {
        var rolle = User.FindFirstValue(ClaimTypes.Role);

        var bestellung = await _context.Lebensmittelbestellungen
            .FirstOrDefaultAsync(b => b.Id == id);

        if (bestellung == null)
        {
            return NotFound(new { message = "Lebensmittelbestellung wurde nicht gefunden." });
        }

        if (rolle != "Administrator")
        {
            var benutzerId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var benutzer = await _context.Users
                .FirstOrDefaultAsync(u => u.Id == benutzerId);

            if (benutzer == null || benutzer.StandortId != bestellung.StandortId)
            {
                return Forbid();
            }
        }

        if (bestellung.Status != "Entwurf")
        {
            return BadRequest(new { message = "Nur Bestellungen im Status Entwurf können bearbeitet werden." });
        }

        if (request.Lieferdatum < DateOnly.FromDateTime(DateTime.UtcNow))
        {
            return BadRequest(new { message = "Das Lieferdatum darf nicht in der Vergangenheit liegen." });
        }

        var lieferdatumGeaendert = bestellung.Lieferdatum != request.Lieferdatum;

        if (lieferdatumGeaendert)
        {
            var bestellungExistiertBereits = await _context.Lebensmittelbestellungen
                .AnyAsync(b => b.Id != id && b.StandortId == bestellung.StandortId && b.Lieferdatum == request.Lieferdatum);

            if (bestellungExistiertBereits)
            {
                return BadRequest(new { message = $"Für diesen Standort existiert bereits eine Bestellung mit Lieferdatum {request.Lieferdatum}." });
            }
        }

        bestellung.Lieferdatum = request.Lieferdatum;
        bestellung.Bemerkung = request.Bemerkung;

        await _context.SaveChangesAsync();

        return Ok(bestellung);
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "Administrator")]
    public async Task<IActionResult> Loeschen(Guid id)
    {
        var bestellung = await _context.Lebensmittelbestellungen
            .FirstOrDefaultAsync(b => b.Id == id);

        if (bestellung == null)
        {
            return NotFound(new { message = "Lebensmittelbestellung wurde nicht gefunden." });
        }

        if (bestellung.Status != "Entwurf")
        {
            return BadRequest(new { message = "Nur Bestellungen im Status Entwurf können gelöscht werden." });
        }

        var positionen = _context.Bestellpositionen.Where(p => p.LebensmittelbestellungId == id);

        _context.Bestellpositionen.RemoveRange(positionen);
        _context.Lebensmittelbestellungen.Remove(bestellung);

        await _context.SaveChangesAsync();

        return Ok(new { message = "Lebensmittelbestellung wurde gelöscht." });
    }

    [HttpPost("{id:guid}/senden")]
    [Authorize(Roles = "Administrator,Hauswirtschaftskraft")]
    public async Task<IActionResult> Senden(Guid id)
    {
        var rolle = User.FindFirstValue(ClaimTypes.Role);
        var benutzerId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        var benutzer = await _context.Users
            .FirstOrDefaultAsync(u => u.Id == benutzerId);

        if (benutzer == null)
        {
            return Unauthorized();
        }

        var bestellung = await _context.Lebensmittelbestellungen
            .FirstOrDefaultAsync(b => b.Id == id);

        if (bestellung == null)
        {
            return NotFound(new { message = "Lebensmittelbestellung wurde nicht gefunden." });
        }

        if (rolle != "Administrator" && benutzer.StandortId != bestellung.StandortId)
        {
            return Forbid();
        }

        if (bestellung.Status != "Entwurf")
        {
            return BadRequest(new { message = "Nur Bestellungen im Status Entwurf können gesendet werden." });
        }

        var hatPositionen = await _context.Bestellpositionen.AnyAsync(p => p.LebensmittelbestellungId == id);

        if (!hatPositionen)
        {
            return BadRequest(new { message = "Die Bestellung muss mindestens eine Bestellposition enthalten." });
        }

        bestellung.Status = "Gesendet";
        bestellung.GesendetVonBenutzerId = benutzer.Id;
        bestellung.GesendetAm = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return Ok(bestellung);
    }

    [HttpPost("{id:guid}/bearbeiten")]
    [Authorize(Roles = "Kuechenmitarbeiter")]
    public async Task<IActionResult> BearbeitungStarten(Guid id)
    {
        var benutzerId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        var bestellung = await _context.Lebensmittelbestellungen
            .FirstOrDefaultAsync(b => b.Id == id);

        if (bestellung == null)
        {
            return NotFound(new { message = "Lebensmittelbestellung wurde nicht gefunden." });
        }

        if (bestellung.Status != "Gesendet")
        {
            return BadRequest(new { message = "Nur gesendete Bestellungen können bearbeitet werden." });
        }

        bestellung.Status = "InBearbeitung";
        bestellung.BearbeitungGestartetVonBenutzerId = benutzerId;
        bestellung.BearbeitungGestartetAm = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return Ok(bestellung);
    }

    [HttpPost("{id:guid}/erledigen")]
    [Authorize(Roles = "Kuechenmitarbeiter")]
    public async Task<IActionResult> Erledigen(Guid id)
    {
        var benutzerId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        var bestellung = await _context.Lebensmittelbestellungen
            .FirstOrDefaultAsync(b => b.Id == id);

        if (bestellung == null)
        {
            return NotFound(new { message = "Lebensmittelbestellung wurde nicht gefunden." });
        }

        if (bestellung.Status != "InBearbeitung")
        {
            return BadRequest(new { message = "Die Bestellung muss zuerst in Bearbeitung sein." });
        }

        bestellung.Status = "Erledigt";
        bestellung.ErledigtVonBenutzerId = benutzerId;
        bestellung.ErledigtAm = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return Ok(bestellung);
    }

    [HttpGet("{id:guid}/pdf")]
    public async Task<IActionResult> PdfErstellen(Guid id)
    {
        var bestellung = await _context.Lebensmittelbestellungen
            .Include(b => b.Standort)
            .FirstOrDefaultAsync(b => b.Id == id);

        if (bestellung == null)
        {
            return NotFound(new { message = "Lebensmittelbestellung wurde nicht gefunden." });
        }

        if (bestellung.Status == "Entwurf")
        {
            return BadRequest(new { message = "Für Bestellungen im Status Entwurf steht noch kein Lieferschein zur Verfügung." });
        }

        var rolle = User.FindFirstValue(ClaimTypes.Role);

        if (rolle != "Administrator" && rolle != "Kuechenmitarbeiter")
        {
            var benutzerId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var benutzer = await _context.Users
                .FirstOrDefaultAsync(u => u.Id == benutzerId);

            if (benutzer == null || benutzer.StandortId != bestellung.StandortId)
            {
                return Forbid();
            }
        }

        var positionen = await _context.Bestellpositionen
            .Where(p => p.LebensmittelbestellungId == id)
            .OrderBy(p => p.Lebensmittelname)
            .ToListAsync();

        var erledigtName = "-";

        if (!string.IsNullOrEmpty(bestellung.ErledigtVonBenutzerId))
        {
            var erledigtBenutzer = await _context.Users
                .FirstOrDefaultAsync(u => u.Id == bestellung.ErledigtVonBenutzerId);

            if (erledigtBenutzer != null)
            {
                erledigtName = $"{erledigtBenutzer.Vorname} {erledigtBenutzer.Nachname}";
            }
        }

        var pdfBytes = ErstellePdf(bestellung, positionen, erledigtName);

        var dateiname = $"Lieferschein_{bestellung.Bestellnummer}.pdf";

        return File(pdfBytes, "application/pdf", dateiname);
    }

    private static byte[] ErstellePdf(
        Lebensmittelbestellung bestellung,
        List<Bestellposition> positionen,
        string erledigtName)
    {
        var document = new Document();

        document.Info.Title = $"Lieferschein {bestellung.Bestellnummer}";

        var section = document.AddSection();

        section.PageSetup.TopMargin = "2cm";
        section.PageSetup.BottomMargin = "2cm";
        section.PageSetup.LeftMargin = "2cm";
        section.PageSetup.RightMargin = "2cm";

        var kopfzeile = section.AddParagraph("PSM");
        kopfzeile.Format.Font.Size = 22;
        kopfzeile.Format.Font.Bold = true;
        kopfzeile.Format.Font.Color = new Color(0, 121, 107);
        kopfzeile.Format.SpaceAfter = "0.1cm";

        var unterzeile = section.AddParagraph("Pflege Management System");
        unterzeile.Format.Font.Size = 10;
        unterzeile.Format.Font.Color = Colors.Gray;
        unterzeile.Format.SpaceAfter = "0.6cm";

        var titel = section.AddParagraph("Lieferschein");
        titel.Format.Font.Size = 18;
        titel.Format.Font.Bold = true;
        titel.Format.SpaceAfter = "0.5cm";

        var infoTabelle = section.AddTable();
        infoTabelle.Borders.Width = 0;

        infoTabelle.AddColumn("4cm");
        infoTabelle.AddColumn("10cm");

        void ZeileHinzufuegen(string label, string wert)
        {
            var zeile = infoTabelle.AddRow();
            zeile.Cells[0].AddParagraph(label);
            zeile.Cells[0].Format.Font.Bold = true;
            zeile.Cells[1].AddParagraph(wert);
        }

        ZeileHinzufuegen("Bestellnummer:", bestellung.Bestellnummer);
        ZeileHinzufuegen(
            "Adresse:",
            $"{bestellung.Standort.Strasse} {bestellung.Standort.Hausnummer}, " +
            $"{bestellung.Standort.Postleitzahl} {bestellung.Standort.Ort}");
        ZeileHinzufuegen("Lieferdatum:", bestellung.Lieferdatum.ToString("dd.MM.yyyy"));
        ZeileHinzufuegen("Status:", bestellung.Status);
        ZeileHinzufuegen("Erledigt von:", erledigtName);

        if (!string.IsNullOrEmpty(bestellung.Bemerkung))
        {
            ZeileHinzufuegen("Bemerkung:", bestellung.Bemerkung);
        }

        section.AddParagraph().Format.SpaceAfter = "0.4cm";

        var artikelTitel = section.AddParagraph("Bestellte Artikel");
        artikelTitel.Format.Font.Size = 13;
        artikelTitel.Format.Font.Bold = true;
        artikelTitel.Format.SpaceAfter = "0.2cm";

        var artikelTabelle = section.AddTable();
        artikelTabelle.Borders.Width = 0.5;
        artikelTabelle.Borders.Color = Colors.LightGray;

        artikelTabelle.AddColumn("6cm");
        artikelTabelle.AddColumn("2.5cm");
        artikelTabelle.AddColumn("2.5cm");
        artikelTabelle.AddColumn("5cm");

        var kopfZeile = artikelTabelle.AddRow();
        kopfZeile.Shading.Color = new Color(230, 240, 238);
        kopfZeile.Cells[0].AddParagraph("Lebensmittel");
        kopfZeile.Cells[0].Format.Font.Bold = true;
        kopfZeile.Cells[1].AddParagraph("Bestellt");
        kopfZeile.Cells[1].Format.Font.Bold = true;
        kopfZeile.Cells[2].AddParagraph("Geliefert");
        kopfZeile.Cells[2].Format.Font.Bold = true;
        kopfZeile.Cells[3].AddParagraph("Bemerkung");
        kopfZeile.Cells[3].Format.Font.Bold = true;

        if (positionen.Count == 0)
        {
            var leerZeile = artikelTabelle.AddRow();
            leerZeile.Cells[0].AddParagraph("Keine Positionen vorhanden.");
        }
        else
        {
            foreach (var position in positionen)
            {
                var zeile = artikelTabelle.AddRow();
                zeile.Cells[0].AddParagraph(position.Lebensmittelname);
                zeile.Cells[1].AddParagraph($"{position.Menge}");
                zeile.Cells[2].AddParagraph(
                    position.GelieferteMenge != null
                        ? $"{position.GelieferteMenge}"
                        : "-");
                zeile.Cells[3].AddParagraph(
                    string.IsNullOrEmpty(position.Bemerkung) ? "-" : position.Bemerkung);
            }
        }

        using var stream = new MemoryStream();

        var renderer = new PdfDocumentRenderer
        {
            Document = document
        };

        renderer.RenderDocument();
        renderer.PdfDocument.Save(stream, false);

        return stream.ToArray();
    }
}

public class LebensmittelbestellungErstellenRequest
{
    public int? StandortId { get; set; }
    public DateOnly Lieferdatum { get; set; }
    public string Bemerkung { get; set; } = string.Empty;
}

public class LebensmittelbestellungBearbeitenRequest
{
    public DateOnly Lieferdatum { get; set; }
    public string Bemerkung { get; set; } = string.Empty;
}
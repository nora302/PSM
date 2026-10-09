using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PSM.Domain.Entities;
using PSM.Domain.Enums;
using PSM.Infrastructure.Data;
using System.Security.Claims;

namespace PSM.Api.Controllers;

[ApiController]
[Route("api/bewohner")]
[Authorize]
public class BewohnerController : ControllerBase
{
    private readonly AppDbContext _context;

    private static readonly string[] EmpfaengerRollen =
    {
        "Administrator",
        "Pflegekraft",
        "Hauswirtschaftskraft",
        "Kuechenmitarbeiter"
    };

    public BewohnerController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<IActionResult> Alle()
    {
        var rolle = User.FindFirstValue(ClaimTypes.Role);
        var benutzerId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (rolle == "Administrator")
        {
            var alleBewohner = await _context.Bewohner
                .Include(b => b.Standort)
                .Where(b => !b.IstArchiviert)
                .OrderBy(b => b.Nachname)
                .ThenBy(b => b.Vorname)
                .ToListAsync();

            return Ok(alleBewohner);
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

        var bewohner = await _context.Bewohner
            .Include(b => b.Standort)
            .Where(b => b.StandortId == benutzer.StandortId && !b.IstArchiviert)
            .OrderBy(b => b.Nachname)
            .ThenBy(b => b.Vorname)
            .ToListAsync();

        return Ok(bewohner);
    }

    [HttpGet("archiv")]
    [Authorize(Roles = "Administrator")]
    public async Task<IActionResult> Archiv()
    {
        var archivierte = await _context.Bewohner
            .Include(b => b.Standort)
            .Where(b => b.IstArchiviert)
            .OrderBy(b => b.Nachname)
            .ThenBy(b => b.Vorname)
            .ToListAsync();

        return Ok(archivierte);
    }

    [HttpGet("status-uebersicht")]
    public async Task<IActionResult> StatusUebersicht()
    {
        var rolle = User.FindFirstValue(ClaimTypes.Role);

        IQueryable<Standort> standortAbfrage = _context.Standorte;

        if (rolle == "Pflegekraft" || rolle == "Hauswirtschaftskraft")
        {
            var benutzerId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var benutzer = await _context.Users
                .FirstOrDefaultAsync(u => u.Id == benutzerId);

            if (benutzer?.StandortId == null)
            {
                return Ok(Array.Empty<object>());
            }

            standortAbfrage = standortAbfrage.Where(s => s.Id == benutzer.StandortId);
        }

        var standorte = await standortAbfrage
            .OrderBy(s => s.Name)
            .ToListAsync();

        var ergebnis = new List<object>();

        foreach (var standort in standorte)
        {
            var bewohnerListe = await _context.Bewohner
                .Where(b => b.StandortId == standort.Id && !b.IstArchiviert)
                .ToListAsync();

            ergebnis.Add(new
            {
                StandortId = standort.Id,
                StandortName = standort.Name,
                ImHaus = bewohnerListe.Count(b => b.Status == BewohnerStatus.ImHaus),
                Umgezogen = bewohnerListe.Count(b => b.Status == BewohnerStatus.Umgezogen),
                Verstorben = bewohnerListe.Count(b => b.Status == BewohnerStatus.Verstorben),
                Krankenhaus = bewohnerListe.Count(b => b.Status == BewohnerStatus.Krankenhaus)
            });
        }

        return Ok(ergebnis);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> NachId(Guid id)
    {
        var bewohner = await _context.Bewohner
            .Include(b => b.Standort)
            .FirstOrDefaultAsync(b => b.Id == id);

        if (bewohner == null)
        {
            return NotFound(new { message = "Bewohner wurde nicht gefunden." });
        }

        var rolle = User.FindFirstValue(ClaimTypes.Role);

        if (rolle == "Administrator")
        {
            return Ok(bewohner);
        }

        var benutzerId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        var benutzer = await _context.Users
            .FirstOrDefaultAsync(u => u.Id == benutzerId);

        if (benutzer == null || benutzer.StandortId != bewohner.StandortId)
        {
            return Forbid();
        }

        return Ok(bewohner);
    }

    [HttpGet("naechste-nummer/{standortId:int}")]
    public async Task<IActionResult> NaechsteBewohnerNummer(int standortId)
    {
        var rolle = User.FindFirstValue(ClaimTypes.Role);

        if (rolle != "Administrator")
        {
            var benutzerId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var benutzer = await _context.Users
                .FirstOrDefaultAsync(u => u.Id == benutzerId);

            if (benutzer?.StandortId != standortId)
            {
                return Forbid();
            }
        }

        var standortExistiert = await _context.Standorte
            .AnyAsync(s => s.Id == standortId);

        if (!standortExistiert)
        {
            return BadRequest(new { message = $"Standort mit Id {standortId} existiert nicht." });
        }

        var hoechsteNummer = await _context.Bewohner
            .Where(b => b.StandortId == standortId)
            .Select(b => (int?)b.StandortBewohnerNummer)
            .MaxAsync();

        var naechsteNummer = (hoechsteNummer ?? 0) + 1;

        return Ok(new { naechsteNummer });
    }

    private async Task BenachrichtigungErstellen(
        int standortId,
        Guid bewohnerId,
        string typ,
        string nachricht)
    {
        foreach (var rolle in EmpfaengerRollen)
        {
            _context.Benachrichtigungen.Add(new Benachrichtigung
            {
                Id = Guid.NewGuid(),
                StandortId = standortId,
                BewohnerId = bewohnerId,
                Typ = typ,
                Nachricht = nachricht,
                EmpfaengerRolle = rolle,
                Gelesen = false,
                ErstelltAm = DateTime.UtcNow
            });
        }

        await Task.CompletedTask;
    }

    private static bool ZimmernummerUngueltig(string zimmernummer)
    {
        return zimmernummer.Trim() == "13";
    }

    private static bool EtageUngueltig(int etage)
    {
        return etage < 1 || etage > 50;
    }

    [HttpPost]
    [Authorize(Roles = "Administrator,Pflegekraft")]
    public async Task<IActionResult> Erstellen(
        BewohnerErstellenRequest request)
    {
        var rolle = User.FindFirstValue(ClaimTypes.Role);

        if (rolle != "Administrator")
        {
            var benutzerId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var benutzer = await _context.Users
                .FirstOrDefaultAsync(u => u.Id == benutzerId);

            if (benutzer?.StandortId == null ||
                benutzer.StandortId != request.StandortId)
            {
                return Forbid();
            }
        }

        if (ZimmernummerUngueltig(request.Zimmernummer))
        {
            return BadRequest(new
            {
                message = "Die Zimmernummer 13 ist nicht zulässig."
            });
        }

        if (EtageUngueltig(request.Etage))
        {
            return BadRequest(new
            {
                message = "Die Etage muss zwischen 1 und 50 liegen."
            });
        }

        var standortExistiert = await _context.Standorte
            .AnyAsync(s => s.Id == request.StandortId);

        if (!standortExistiert)
        {
            return BadRequest(new
            {
                message = $"Standort mit Id {request.StandortId} existiert nicht."
            });
        }

        var nummerExistiert = await _context.Bewohner
            .AnyAsync(b =>
                b.StandortId == request.StandortId &&
                b.StandortBewohnerNummer == request.StandortBewohnerNummer);

        if (nummerExistiert)
        {
            return BadRequest(new
            {
                message = "Die StandortBewohnerNummer ist an diesem Standort bereits vergeben."
            });
        }

        var bewohner = new Bewohner
        {
            Id = Guid.NewGuid(),
            StandortId = request.StandortId,
            StandortBewohnerNummer = request.StandortBewohnerNummer,
            Vorname = request.Vorname,
            Nachname = request.Nachname,
            Geburtsdatum = request.Geburtsdatum,
            Etage = request.Etage,
            Zimmernummer = request.Zimmernummer,
            Status = BewohnerStatus.ImHaus,
            IstArchiviert = false,
            ErstelltAm = DateTime.UtcNow
        };

        _context.Bewohner.Add(bewohner);

        await BenachrichtigungErstellen(
            request.StandortId,
            bewohner.Id,
            "Neu",
            $"Neuer Bewohner: {bewohner.Vorname} {bewohner.Nachname}");

        await _context.SaveChangesAsync();

        return Ok(bewohner);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Roles = "Administrator,Pflegekraft")]
    public async Task<IActionResult> Bearbeiten(
        Guid id,
        BewohnerBearbeitenRequest request)
    {
        var bewohner = await _context.Bewohner
            .FirstOrDefaultAsync(b => b.Id == id);

        if (bewohner == null)
        {
            return NotFound(new { message = "Bewohner wurde nicht gefunden." });
        }

        var rolle = User.FindFirstValue(ClaimTypes.Role);

        if (rolle != "Administrator")
        {
            var benutzerId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var benutzer = await _context.Users
                .FirstOrDefaultAsync(u => u.Id == benutzerId);

            if (benutzer?.StandortId == null ||
                benutzer.StandortId != bewohner.StandortId ||
                benutzer.StandortId != request.StandortId)
            {
                return Forbid();
            }
        }

        if (ZimmernummerUngueltig(request.Zimmernummer))
        {
            return BadRequest(new
            {
                message = "Die Zimmernummer 13 ist nicht zulässig."
            });
        }

        if (EtageUngueltig(request.Etage))
        {
            return BadRequest(new
            {
                message = "Die Etage muss zwischen 1 und 50 liegen."
            });
        }

        var standortExistiert = await _context.Standorte
            .AnyAsync(s => s.Id == request.StandortId);

        if (!standortExistiert)
        {
            return BadRequest(new
            {
                message = $"Standort mit Id {request.StandortId} existiert nicht."
            });
        }

        var nummerExistiert = await _context.Bewohner
            .AnyAsync(b =>
                b.Id != id &&
                b.StandortId == request.StandortId &&
                b.StandortBewohnerNummer == request.StandortBewohnerNummer);

        if (nummerExistiert)
        {
            return BadRequest(new
            {
                message = "Die StandortBewohnerNummer ist an diesem Standort bereits vergeben."
            });
        }

        var standortGeaendert = bewohner.StandortId != request.StandortId;

        if (standortGeaendert)
        {
            var benutzerId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;

            var historie = new BewohnerStandortHistorie
            {
                Id = Guid.NewGuid(),
                BewohnerId = bewohner.Id,
                AlterStandortId = bewohner.StandortId,
                NeuerStandortId = request.StandortId,
                AlteStandortBewohnerNummer = bewohner.StandortBewohnerNummer,
                NeueStandortBewohnerNummer = request.StandortBewohnerNummer,
                AlteZimmernummer = bewohner.Zimmernummer,
                NeueZimmernummer = request.Zimmernummer,
                GeaendertAm = DateTime.UtcNow,
                GeaendertVonBenutzerId = benutzerId
            };

            _context.BewohnerStandortHistorien.Add(historie);
        }

        var statusGeaendert = bewohner.Status != request.Status;
        var alterStandortIdFuerBenachrichtigung = bewohner.StandortId;

        bewohner.StandortId = request.StandortId;
        bewohner.StandortBewohnerNummer = request.StandortBewohnerNummer;
        bewohner.Vorname = request.Vorname;
        bewohner.Nachname = request.Nachname;
        bewohner.Geburtsdatum = request.Geburtsdatum;
        bewohner.Etage = request.Etage;
        bewohner.Zimmernummer = request.Zimmernummer;
        bewohner.Status = request.Status;
        bewohner.GeaendertAm = DateTime.UtcNow;

        if (statusGeaendert)
        {
            var typ = request.Status switch
            {
                BewohnerStatus.Umgezogen => "Umgezogen",
                BewohnerStatus.Verstorben => "Verstorben",
                BewohnerStatus.Krankenhaus => "Krankenhaus",
                _ => null
            };

            if (typ != null)
            {
                await BenachrichtigungErstellen(
                    alterStandortIdFuerBenachrichtigung,
                    bewohner.Id,
                    typ,
                    $"{bewohner.Vorname} {bewohner.Nachname}: Status geändert zu {typ}");
            }
        }

        await _context.SaveChangesAsync();

        return Ok(bewohner);
    }

    [HttpPost("{id:guid}/archivieren")]
    [Authorize(Roles = "Administrator")]
    public async Task<IActionResult> Archivieren(Guid id)
    {
        var bewohner = await _context.Bewohner
            .FirstOrDefaultAsync(b => b.Id == id);

        if (bewohner == null)
        {
            return NotFound(new { message = "Bewohner wurde nicht gefunden." });
        }

        bewohner.IstArchiviert = true;
        bewohner.GeaendertAm = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return Ok(new { message = "Bewohner wurde archiviert." });
    }

    [HttpPost("{id:guid}/reaktivieren")]
    [Authorize(Roles = "Administrator")]
    public async Task<IActionResult> Reaktivieren(Guid id)
    {
        var bewohner = await _context.Bewohner
            .FirstOrDefaultAsync(b => b.Id == id);

        if (bewohner == null)
        {
            return NotFound(new { message = "Bewohner wurde nicht gefunden." });
        }

        bewohner.IstArchiviert = false;
        bewohner.GeaendertAm = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return Ok(new { message = "Bewohner wurde reaktiviert." });
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "Administrator")]
    public async Task<IActionResult> Loeschen(Guid id)
    {
        var bewohner = await _context.Bewohner
            .FirstOrDefaultAsync(b => b.Id == id);

        if (bewohner == null)
        {
            return NotFound(new { message = "Bewohner wurde nicht gefunden." });
        }

        if (!bewohner.IstArchiviert)
        {
            return BadRequest(new
            {
                message = "Nur archivierte Bewohner können endgültig gelöscht werden."
            });
        }

        var hatDaten = await HatVerknuepfteDaten(id);

        if (hatDaten)
        {
            return BadRequest(new
            {
                message =
                    "Dieser Bewohner hat verknüpfte Daten und kann nicht gelöscht werden."
            });
        }

        _context.Bewohner.Remove(bewohner);

        await _context.SaveChangesAsync();

        return Ok(new { message = "Bewohner wurde endgültig gelöscht." });
    }

    private async Task<bool> HatVerknuepfteDaten(Guid bewohnerId)
    {
        var hatPflegedokumentation = await _context.Pflegedokumentationen
            .AnyAsync(p => p.BewohnerId == bewohnerId);

        if (hatPflegedokumentation) return true;

        var hatErnaehrung = await _context.Ernaehrungen
            .AnyAsync(e => e.BewohnerId == bewohnerId);

        if (hatErnaehrung) return true;

        var hatAllergie = await _context.Allergien
            .AnyAsync(a => a.BewohnerId == bewohnerId);

        if (hatAllergie) return true;

        var hatEssensausgabe = await _context.Essensausgaben
            .AnyAsync(e => e.BewohnerId == bewohnerId);

        if (hatEssensausgabe) return true;

        var hatHistorie = await _context.BewohnerStandortHistorien
            .AnyAsync(h => h.BewohnerId == bewohnerId);

        if (hatHistorie) return true;

        var hatBenachrichtigung = await _context.Benachrichtigungen
            .AnyAsync(n => n.BewohnerId == bewohnerId);

        if (hatBenachrichtigung) return true;

        return false;
    }

    [HttpGet("{id:guid}/standort-historie")]
    [Authorize(Roles = "Administrator")]
    public async Task<IActionResult> StandortHistorie(Guid id)
    {
        var bewohnerExistiert = await _context.Bewohner.AnyAsync(b => b.Id == id);

        if (!bewohnerExistiert)
        {
            return NotFound(new { message = "Bewohner wurde nicht gefunden." });
        }

        var historie = await _context.BewohnerStandortHistorien
            .Where(h => h.BewohnerId == id)
            .OrderByDescending(h => h.GeaendertAm)
            .ToListAsync();

        return Ok(historie);
    }
}

public class BewohnerErstellenRequest
{
    public int StandortId { get; set; }
    public int StandortBewohnerNummer { get; set; }
    public string Vorname { get; set; } = string.Empty;
    public string Nachname { get; set; } = string.Empty;
    public DateOnly Geburtsdatum { get; set; }
    public int Etage { get; set; }
    public string Zimmernummer { get; set; } = string.Empty;
}

public class BewohnerBearbeitenRequest
{
    public int StandortId { get; set; }
    public int StandortBewohnerNummer { get; set; }
    public string Vorname { get; set; } = string.Empty;
    public string Nachname { get; set; } = string.Empty;
    public DateOnly Geburtsdatum { get; set; }
    public int Etage { get; set; }
    public string Zimmernummer { get; set; } = string.Empty;
    public BewohnerStatus Status { get; set; }
}
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
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

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> NachId(Guid id)
    {
        var bestellung = await _context.Lebensmittelbestellungen
            .Include(b => b.Standort)
            .FirstOrDefaultAsync(b => b.Id == id);

        if (bestellung == null)
        {
            return NotFound(new
            {
                message = "Lebensmittelbestellung wurde nicht gefunden."
            });
        }

        var rolle = User.FindFirstValue(ClaimTypes.Role);

        if (rolle == "Administrator" || rolle == "Kuechenmitarbeiter")
        {
            return Ok(bestellung);
        }

        var benutzerId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        var benutzer = await _context.Users
            .FirstOrDefaultAsync(u => u.Id == benutzerId);

        if (benutzer == null ||
            benutzer.StandortId != bestellung.StandortId)
        {
            return Forbid();
        }

        return Ok(bestellung);
    }

    [HttpPost]
    [Authorize(Roles = "Hauswirtschaftskraft")]
    public async Task<IActionResult> Erstellen(
        LebensmittelbestellungErstellenRequest request)
    {
        var benutzerId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        var benutzer = await _context.Users
            .FirstOrDefaultAsync(u => u.Id == benutzerId);

        if (benutzer == null)
        {
            return Unauthorized();
        }

        if (benutzer.StandortId == null)
        {
            return BadRequest(new
            {
                message = "Der Benutzer ist keinem Standort zugeordnet."
            });
        }

        if (request.Lieferdatum < DateOnly.FromDateTime(DateTime.UtcNow))
        {
            return BadRequest(new
            {
                message = "Das Lieferdatum darf nicht in der Vergangenheit liegen."
            });
        }

        var bestellung = new Lebensmittelbestellung
        {
            Id = Guid.NewGuid(),
            StandortId = benutzer.StandortId.Value,
            Bestelldatum = DateTime.UtcNow,
            Lieferdatum = request.Lieferdatum,
            Status = "Entwurf",
            ErstelltVonBenutzerId = benutzer.Id,
            Bemerkung = request.Bemerkung
        };

        _context.Lebensmittelbestellungen.Add(bestellung);

        await _context.SaveChangesAsync();

        return Ok(bestellung);
    }

    [HttpPost("{id:guid}/senden")]
    [Authorize(Roles = "Hauswirtschaftskraft")]
    public async Task<IActionResult> Senden(Guid id)
    {
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
            return NotFound(new
            {
                message = "Lebensmittelbestellung wurde nicht gefunden."
            });
        }

        if (benutzer.StandortId != bestellung.StandortId)
        {
            return Forbid();
        }

        if (bestellung.Status != "Entwurf")
        {
            return BadRequest(new
            {
                message = "Nur Bestellungen im Status Entwurf können gesendet werden."
            });
        }

        var hatPositionen = await _context.Bestellpositionen
            .AnyAsync(p => p.LebensmittelbestellungId == id);

        if (!hatPositionen)
        {
            return BadRequest(new
            {
                message = "Die Bestellung muss mindestens eine Bestellposition enthalten."
            });
        }

        bestellung.Status = "Gesendet";

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
            return NotFound(new
            {
                message = "Lebensmittelbestellung wurde nicht gefunden."
            });
        }

        if (bestellung.Status != "Gesendet")
        {
            return BadRequest(new
            {
                message = "Nur gesendete Bestellungen können bearbeitet werden."
            });
        }

        bestellung.Status = "InBearbeitung";
        bestellung.BearbeitetVonBenutzerId = benutzerId;

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
            return NotFound(new
            {
                message = "Lebensmittelbestellung wurde nicht gefunden."
            });
        }

        if (bestellung.Status != "InBearbeitung")
        {
            return BadRequest(new
            {
                message = "Die Bestellung muss zuerst in Bearbeitung sein."
            });
        }

        bestellung.Status = "Erledigt";
        bestellung.BearbeitetVonBenutzerId = benutzerId;
        bestellung.ErledigtAm = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return Ok(bestellung);
    }
}

public class LebensmittelbestellungErstellenRequest
{
    public DateOnly Lieferdatum { get; set; }

    public string Bemerkung { get; set; } = string.Empty;
}
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PSM.Domain.Entities;
using PSM.Infrastructure.Data;
using System.Security.Claims;

namespace PSM.Api.Controllers;

[ApiController]
[Route("api/bestellpositionen")]
[Authorize]
public class BestellpositionController : ControllerBase
{
    private readonly AppDbContext _context;

    public BestellpositionController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet("bestellung/{bestellungId:guid}")]
    public async Task<IActionResult> AlleVonBestellung(Guid bestellungId)
    {
        var bestellung = await _context.Lebensmittelbestellungen
            .FirstOrDefaultAsync(b => b.Id == bestellungId);

        if (bestellung == null)
        {
            return NotFound(new
            {
                message = "Lebensmittelbestellung wurde nicht gefunden."
            });
        }

        var rolle = User.FindFirstValue(ClaimTypes.Role);

        if (rolle != "Administrator" &&
            rolle != "Kuechenmitarbeiter")
        {
            var benutzerId =
                User.FindFirstValue(ClaimTypes.NameIdentifier);

            var benutzer = await _context.Users
                .FirstOrDefaultAsync(u => u.Id == benutzerId);

            if (benutzer == null ||
                benutzer.StandortId != bestellung.StandortId)
            {
                return Forbid();
            }
        }

        var positionen = await _context.Bestellpositionen
            .Where(p =>
                p.LebensmittelbestellungId == bestellungId)
            .OrderBy(p => p.Lebensmittelname)
            .ToListAsync();

        return Ok(positionen);
    }

    [HttpPost]
    [Authorize(Roles = "Hauswirtschaftskraft")]
    public async Task<IActionResult> Erstellen(
        BestellpositionErstellenRequest request)
    {
        var benutzerId =
            User.FindFirstValue(ClaimTypes.NameIdentifier);

        var benutzer = await _context.Users
            .FirstOrDefaultAsync(u => u.Id == benutzerId);

        if (benutzer == null)
        {
            return Unauthorized();
        }

        var bestellung =
            await _context.Lebensmittelbestellungen
                .FirstOrDefaultAsync(
                    b => b.Id == request.LebensmittelbestellungId);

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
                message =
                    "Bestellpositionen können nur bei Bestellungen im Status Entwurf hinzugefügt werden."
            });
        }

        if (request.Menge <= 0)
        {
            return BadRequest(new
            {
                message = "Die Menge muss größer als 0 sein."
            });
        }

        var position = new Bestellposition
        {
            Id = Guid.NewGuid(),
            LebensmittelbestellungId =
                request.LebensmittelbestellungId,
            Lebensmittelname =
                request.Lebensmittelname,
            Menge = request.Menge,
            Einheit = request.Einheit,
            Bemerkung = request.Bemerkung
        };

        _context.Bestellpositionen.Add(position);

        await _context.SaveChangesAsync();

        return Ok(position);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Roles = "Hauswirtschaftskraft")]
    public async Task<IActionResult> Bearbeiten(
        Guid id,
        BestellpositionBearbeitenRequest request)
    {
        var benutzerId =
            User.FindFirstValue(ClaimTypes.NameIdentifier);

        var benutzer = await _context.Users
            .FirstOrDefaultAsync(u => u.Id == benutzerId);

        if (benutzer == null)
        {
            return Unauthorized();
        }

        var position = await _context.Bestellpositionen
            .Include(p => p.Lebensmittelbestellung)
            .FirstOrDefaultAsync(p => p.Id == id);

        if (position == null)
        {
            return NotFound(new
            {
                message = "Bestellposition wurde nicht gefunden."
            });
        }

        if (benutzer.StandortId !=
            position.Lebensmittelbestellung.StandortId)
        {
            return Forbid();
        }

        if (position.Lebensmittelbestellung.Status != "Entwurf")
        {
            return BadRequest(new
            {
                message =
                    "Bestellpositionen können nur bei Bestellungen im Status Entwurf geändert werden."
            });
        }

        if (request.Menge <= 0)
        {
            return BadRequest(new
            {
                message = "Die Menge muss größer als 0 sein."
            });
        }

        position.Lebensmittelname =
            request.Lebensmittelname;

        position.Menge =
            request.Menge;

        position.Einheit =
            request.Einheit;

        position.Bemerkung =
            request.Bemerkung;

        await _context.SaveChangesAsync();

        return Ok(position);
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "Hauswirtschaftskraft")]
    public async Task<IActionResult> Loeschen(Guid id)
    {
        var benutzerId =
            User.FindFirstValue(ClaimTypes.NameIdentifier);

        var benutzer = await _context.Users
            .FirstOrDefaultAsync(u => u.Id == benutzerId);

        if (benutzer == null)
        {
            return Unauthorized();
        }

        var position = await _context.Bestellpositionen
            .Include(p => p.Lebensmittelbestellung)
            .FirstOrDefaultAsync(p => p.Id == id);

        if (position == null)
        {
            return NotFound(new
            {
                message = "Bestellposition wurde nicht gefunden."
            });
        }

        if (benutzer.StandortId !=
            position.Lebensmittelbestellung.StandortId)
        {
            return Forbid();
        }

        if (position.Lebensmittelbestellung.Status != "Entwurf")
        {
            return BadRequest(new
            {
                message =
                    "Bestellpositionen können nur bei Bestellungen im Status Entwurf gelöscht werden."
            });
        }

        _context.Bestellpositionen.Remove(position);

        await _context.SaveChangesAsync();

        return Ok(new
        {
            message = "Bestellposition wurde gelöscht."
        });
    }
}

public class BestellpositionErstellenRequest
{
    public Guid LebensmittelbestellungId { get; set; }

    public string Lebensmittelname { get; set; } =
        string.Empty;

    public decimal Menge { get; set; }

    public string Einheit { get; set; } =
        string.Empty;

    public string Bemerkung { get; set; } =
        string.Empty;
}

public class BestellpositionBearbeitenRequest
{
    public string Lebensmittelname { get; set; } =
        string.Empty;

    public decimal Menge { get; set; }

    public string Einheit { get; set; } =
        string.Empty;

    public string Bemerkung { get; set; } =
        string.Empty;
}
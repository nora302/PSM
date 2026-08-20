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
            .Where(b => b.StandortId == benutzer.StandortId)
            .OrderBy(b => b.Nachname)
            .ThenBy(b => b.Vorname)
            .ToListAsync();

        return Ok(bewohner);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> NachId(Guid id)
    {
        var bewohner = await _context.Bewohner
            .Include(b => b.Standort)
            .FirstOrDefaultAsync(b => b.Id == id);

        if (bewohner == null)
        {
            return NotFound(new
            {
                message = "Bewohner wurde nicht gefunden."
            });
        }

        var rolle = User.FindFirstValue(ClaimTypes.Role);

        if (rolle == "Administrator")
        {
            return Ok(bewohner);
        }

        var benutzerId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        var benutzer = await _context.Users
            .FirstOrDefaultAsync(u => u.Id == benutzerId);

        if (benutzer == null ||
            benutzer.StandortId != bewohner.StandortId)
        {
            return Forbid();
        }

        return Ok(bewohner);
    }

    [HttpPost]
    [Authorize(Roles = "Administrator")]
    public async Task<IActionResult> Erstellen(
        BewohnerErstellenRequest request)
    {
        var standortExistiert = await _context.Standorte
            .AnyAsync(s => s.Id == request.StandortId);

        if (!standortExistiert)
        {
            return BadRequest(new
            {
                message =
                    $"Standort mit Id {request.StandortId} existiert nicht."
            });
        }

        var nummerExistiert = await _context.Bewohner
            .AnyAsync(b =>
                b.StandortId == request.StandortId &&
                b.StandortBewohnerNummer ==
                    request.StandortBewohnerNummer);

        if (nummerExistiert)
        {
            return BadRequest(new
            {
                message =
                    "Die StandortBewohnerNummer ist an diesem Standort bereits vergeben."
            });
        }

        var bewohner = new Bewohner
        {
            Id = Guid.NewGuid(),
            StandortId = request.StandortId,
            StandortBewohnerNummer =
                request.StandortBewohnerNummer,
            Vorname = request.Vorname,
            Nachname = request.Nachname,
            Geburtsdatum = request.Geburtsdatum,
            Zimmernummer = request.Zimmernummer,
            Status = BewohnerStatus.ImHaus,
            ErstelltAm = DateTime.UtcNow
        };

        _context.Bewohner.Add(bewohner);
        await _context.SaveChangesAsync();

        return Ok(bewohner);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Roles = "Administrator")]
    public async Task<IActionResult> Bearbeiten(
        Guid id,
        BewohnerBearbeitenRequest request)
    {
        var bewohner = await _context.Bewohner
            .FirstOrDefaultAsync(b => b.Id == id);

        if (bewohner == null)
        {
            return NotFound(new
            {
                message = "Bewohner wurde nicht gefunden."
            });
        }

        var standortExistiert = await _context.Standorte
            .AnyAsync(s => s.Id == request.StandortId);

        if (!standortExistiert)
        {
            return BadRequest(new
            {
                message =
                    $"Standort mit Id {request.StandortId} existiert nicht."
            });
        }

        var nummerExistiert = await _context.Bewohner
            .AnyAsync(b =>
                b.Id != id &&
                b.StandortId == request.StandortId &&
                b.StandortBewohnerNummer ==
                    request.StandortBewohnerNummer);

        if (nummerExistiert)
        {
            return BadRequest(new
            {
                message =
                    "Die StandortBewohnerNummer ist an diesem Standort bereits vergeben."
            });
        }

        var standortGeaendert =
            bewohner.StandortId != request.StandortId;

        if (standortGeaendert)
        {
            var benutzerId =
                User.FindFirstValue(ClaimTypes.NameIdentifier)
                ?? string.Empty;

            var historie = new BewohnerStandortHistorie
            {
                Id = Guid.NewGuid(),
                BewohnerId = bewohner.Id,
                AlterStandortId = bewohner.StandortId,
                NeuerStandortId = request.StandortId,
                AlteStandortBewohnerNummer =
                    bewohner.StandortBewohnerNummer,
                NeueStandortBewohnerNummer =
                    request.StandortBewohnerNummer,
                AlteZimmernummer =
                    bewohner.Zimmernummer,
                NeueZimmernummer =
                    request.Zimmernummer,
                GeaendertAm =
                    DateTime.UtcNow,
                GeaendertVonBenutzerId =
                    benutzerId
            };

            _context.BewohnerStandortHistorien.Add(historie);
        }

        bewohner.StandortId = request.StandortId;

        bewohner.StandortBewohnerNummer =
            request.StandortBewohnerNummer;

        bewohner.Vorname = request.Vorname;

        bewohner.Nachname = request.Nachname;

        bewohner.Geburtsdatum = request.Geburtsdatum;

        bewohner.Zimmernummer = request.Zimmernummer;

        bewohner.Status = request.Status;

        bewohner.GeaendertAm = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return Ok(bewohner);
    }

    [HttpGet("{id:guid}/standort-historie")]
    [Authorize(Roles = "Administrator")]
    public async Task<IActionResult> StandortHistorie(Guid id)
    {
        var bewohnerExistiert =
            await _context.Bewohner.AnyAsync(
                b => b.Id == id);

        if (!bewohnerExistiert)
        {
            return NotFound(new
            {
                message = "Bewohner wurde nicht gefunden."
            });
        }

        var historie =
            await _context.BewohnerStandortHistorien
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

    public string Zimmernummer { get; set; } = string.Empty;
}

public class BewohnerBearbeitenRequest
{
    public int StandortId { get; set; }

    public int StandortBewohnerNummer { get; set; }

    public string Vorname { get; set; } = string.Empty;

    public string Nachname { get; set; } = string.Empty;

    public DateOnly Geburtsdatum { get; set; }

    public string Zimmernummer { get; set; } = string.Empty;

    public BewohnerStatus Status { get; set; }
}
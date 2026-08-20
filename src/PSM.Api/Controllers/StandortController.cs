using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PSM.Domain.Entities;
using PSM.Infrastructure.Data;

namespace PSM.Api.Controllers;

[ApiController]
[Route("api/standorte")]
public class StandortController : ControllerBase
{
    private readonly AppDbContext _context;

    public StandortController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    [Authorize]
    public async Task<IActionResult> Alle()
    {
        var standorte = await _context.Standorte
            .OrderBy(s => s.Id)
            .ToListAsync();

        return Ok(standorte);
    }

    [HttpGet("{id:int}")]
    [Authorize]
    public async Task<IActionResult> NachId(int id)
    {
        var standort = await _context.Standorte
            .FirstOrDefaultAsync(s => s.Id == id);

        if (standort == null)
        {
            return NotFound(new
            {
                message = $"Standort mit Id {id} wurde nicht gefunden."
            });
        }

        return Ok(standort);
    }

    [HttpPost]
    [Authorize(Roles = "Administrator")]
    public async Task<IActionResult> Erstellen(
        StandortErstellenRequest request)
    {
        var codeExistiert = await _context.Standorte
            .AnyAsync(s => s.Code == request.Code);

        if (codeExistiert)
        {
            return BadRequest(new
            {
                message = "Der Standort-Code existiert bereits."
            });
        }

        var standort = new Standort
        {
            Name = request.Name,
            Code = request.Code,
            Strasse = request.Strasse,
            Hausnummer = request.Hausnummer,
            Postleitzahl = request.Postleitzahl,
            Ort = request.Ort,
            Telefonnummer = request.Telefonnummer,
            Email = request.Email,
            IstAktiv = true
        };

        _context.Standorte.Add(standort);
        await _context.SaveChangesAsync();

        return Ok(standort);
    }

    [HttpPut("{id:int}")]
    [Authorize(Roles = "Administrator")]
    public async Task<IActionResult> Bearbeiten(
        int id,
        StandortBearbeitenRequest request)
    {
        var standort = await _context.Standorte
            .FirstOrDefaultAsync(s => s.Id == id);

        if (standort == null)
        {
            return NotFound(new
            {
                message = $"Standort mit Id {id} wurde nicht gefunden."
            });
        }

        var codeExistiert = await _context.Standorte
            .AnyAsync(s =>
                s.Id != id &&
                s.Code == request.Code);

        if (codeExistiert)
        {
            return BadRequest(new
            {
                message = "Der Standort-Code existiert bereits."
            });
        }

        standort.Name = request.Name;
        standort.Code = request.Code;
        standort.Strasse = request.Strasse;
        standort.Hausnummer = request.Hausnummer;
        standort.Postleitzahl = request.Postleitzahl;
        standort.Ort = request.Ort;
        standort.Telefonnummer = request.Telefonnummer;
        standort.Email = request.Email;

        await _context.SaveChangesAsync();

        return Ok(standort);
    }

    [HttpPost("{id:int}/deaktivieren")]
    [Authorize(Roles = "Administrator")]
    public async Task<IActionResult> Deaktivieren(int id)
    {
        var standort = await _context.Standorte
            .FirstOrDefaultAsync(s => s.Id == id);

        if (standort == null)
        {
            return NotFound(new
            {
                message = $"Standort mit Id {id} wurde nicht gefunden."
            });
        }

        if (!standort.IstAktiv)
        {
            return BadRequest(new
            {
                message = "Der Standort ist bereits deaktiviert."
            });
        }

        var aktiveBenutzerExistieren =
            await _context.Users.AnyAsync(u =>
                u.StandortId == id &&
                u.IstAktiv);

        if (aktiveBenutzerExistieren)
        {
            return BadRequest(new
            {
                message =
                    "Der Standort kann nicht deaktiviert werden, solange aktive Benutzer zugeordnet sind."
            });
        }

        standort.IstAktiv = false;

        await _context.SaveChangesAsync();

        return Ok(new
        {
            message = "Standort wurde deaktiviert."
        });
    }

    [HttpPost("{id:int}/aktivieren")]
    [Authorize(Roles = "Administrator")]
    public async Task<IActionResult> Aktivieren(int id)
    {
        var standort = await _context.Standorte
            .FirstOrDefaultAsync(s => s.Id == id);

        if (standort == null)
        {
            return NotFound(new
            {
                message = $"Standort mit Id {id} wurde nicht gefunden."
            });
        }

        if (standort.IstAktiv)
        {
            return BadRequest(new
            {
                message = "Der Standort ist bereits aktiv."
            });
        }

        standort.IstAktiv = true;

        await _context.SaveChangesAsync();

        return Ok(new
        {
            message = "Standort wurde aktiviert."
        });
    }
}

public class StandortErstellenRequest
{
    public string Name { get; set; } = string.Empty;

    public string Code { get; set; } = string.Empty;

    public string Strasse { get; set; } = string.Empty;

    public string Hausnummer { get; set; } = string.Empty;

    public string Postleitzahl { get; set; } = string.Empty;

    public string Ort { get; set; } = string.Empty;

    public string Telefonnummer { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;
}

public class StandortBearbeitenRequest
{
    public string Name { get; set; } = string.Empty;

    public string Code { get; set; } = string.Empty;

    public string Strasse { get; set; } = string.Empty;

    public string Hausnummer { get; set; } = string.Empty;

    public string Postleitzahl { get; set; } = string.Empty;

    public string Ort { get; set; } = string.Empty;

    public string Telefonnummer { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;
}
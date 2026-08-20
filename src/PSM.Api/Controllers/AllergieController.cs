using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PSM.Domain.Entities;
using PSM.Infrastructure.Data;
using System.Security.Claims;

namespace PSM.Api.Controllers;

[ApiController]
[Route("api/allergien")]
[Authorize]
public class AllergieController : ControllerBase
{
    private readonly AppDbContext _context;

    public AllergieController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet("bewohner/{bewohnerId:guid}")]
    public async Task<IActionResult> AlleVonBewohner(Guid bewohnerId)
    {
        var bewohner = await _context.Bewohner
            .FirstOrDefaultAsync(b => b.Id == bewohnerId);

        if (bewohner == null)
        {
            return NotFound(new
            {
                message = "Bewohner wurde nicht gefunden."
            });
        }

        var rolle = User.FindFirstValue(ClaimTypes.Role);

        if (rolle != "Administrator")
        {
            var benutzerId =
                User.FindFirstValue(ClaimTypes.NameIdentifier);

            var benutzer = await _context.Users
                .FirstOrDefaultAsync(u => u.Id == benutzerId);

            if (benutzer == null ||
                benutzer.StandortId != bewohner.StandortId)
            {
                return Forbid();
            }
        }

        var allergien = await _context.Allergien
            .Where(a => a.BewohnerId == bewohnerId)
            .OrderBy(a => a.Name)
            .ToListAsync();

        return Ok(allergien);
    }

    [HttpPost]
    [Authorize(Roles = "Pflegekraft")]
    public async Task<IActionResult> Erstellen(
        AllergieRequest request)
    {
        var benutzerId =
            User.FindFirstValue(ClaimTypes.NameIdentifier);

        var benutzer = await _context.Users
            .FirstOrDefaultAsync(u => u.Id == benutzerId);

        if (benutzer == null)
        {
            return Unauthorized();
        }

        var bewohner = await _context.Bewohner
            .FirstOrDefaultAsync(b => b.Id == request.BewohnerId);

        if (bewohner == null)
        {
            return NotFound(new
            {
                message = "Bewohner wurde nicht gefunden."
            });
        }

        if (benutzer.StandortId != bewohner.StandortId)
        {
            return Forbid();
        }

        var allergie = new Allergie
        {
            Id = Guid.NewGuid(),
            BewohnerId = request.BewohnerId,
            Name = request.Name,
            Bemerkung = request.Bemerkung,
            IstAktiv = true,
            ErstelltAm = DateTime.UtcNow
        };

        _context.Allergien.Add(allergie);

        await _context.SaveChangesAsync();

        return Ok(allergie);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Roles = "Pflegekraft")]
    public async Task<IActionResult> Bearbeiten(
        Guid id,
        AllergieBearbeitenRequest request)
    {
        var benutzerId =
            User.FindFirstValue(ClaimTypes.NameIdentifier);

        var benutzer = await _context.Users
            .FirstOrDefaultAsync(u => u.Id == benutzerId);

        if (benutzer == null)
        {
            return Unauthorized();
        }

        var allergie = await _context.Allergien
            .Include(a => a.Bewohner)
            .FirstOrDefaultAsync(a => a.Id == id);

        if (allergie == null)
        {
            return NotFound(new
            {
                message = "Allergie wurde nicht gefunden."
            });
        }

        if (benutzer.StandortId != allergie.Bewohner.StandortId)
        {
            return Forbid();
        }

        allergie.Name = request.Name;
        allergie.Bemerkung = request.Bemerkung;
        allergie.IstAktiv = request.IstAktiv;
        allergie.GeaendertAm = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return Ok(allergie);
    }
}

public class AllergieRequest
{
    public Guid BewohnerId { get; set; }

    public string Name { get; set; } = string.Empty;

    public string Bemerkung { get; set; } = string.Empty;
}

public class AllergieBearbeitenRequest
{
    public string Name { get; set; } = string.Empty;

    public string Bemerkung { get; set; } = string.Empty;

    public bool IstAktiv { get; set; }
}
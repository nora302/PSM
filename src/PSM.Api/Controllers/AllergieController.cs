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
    public async Task<IActionResult> NachBewohner(Guid bewohnerId)
    {
        var bewohner = await _context.Bewohner
            .FirstOrDefaultAsync(b => b.Id == bewohnerId);

        if (bewohner == null)
        {
            return NotFound(new { message = "Bewohner wurde nicht gefunden." });
        }

        var zugriffsFehler = await ZugriffPruefen(bewohner.StandortId);

        if (zugriffsFehler != null)
        {
            return zugriffsFehler;
        }

        var allergien = await _context.Allergien
            .Where(a => a.BewohnerId == bewohnerId && a.IstAktiv)
            .OrderBy(a => a.Name)
            .ToListAsync();

        return Ok(allergien);
    }

    [HttpPost]
    [Authorize(Roles = "Administrator,Pflegekraft")]
    public async Task<IActionResult> Erstellen(AllergieErstellenRequest request)
    {
        var bewohner = await _context.Bewohner
            .FirstOrDefaultAsync(b => b.Id == request.BewohnerId);

        if (bewohner == null)
        {
            return NotFound(new { message = "Bewohner wurde nicht gefunden." });
        }

        var zugriffsFehler = await ZugriffPruefen(bewohner.StandortId);

        if (zugriffsFehler != null)
        {
            return zugriffsFehler;
        }

        if (string.IsNullOrWhiteSpace(request.Name))
        {
            return BadRequest(new { message = "Bitte einen Namen angeben." });
        }

        var allergie = new Allergie
        {
            Id = Guid.NewGuid(),
            BewohnerId = request.BewohnerId,
            Name = request.Name.Trim(),
            Bemerkung = request.Bemerkung,
            IstAktiv = true,
            ErstelltAm = DateTime.UtcNow
        };

        _context.Allergien.Add(allergie);

        await _context.SaveChangesAsync();

        return Ok(allergie);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Roles = "Administrator,Pflegekraft")]
    public async Task<IActionResult> Bearbeiten(Guid id, AllergieBearbeitenRequest request)
    {
        var allergie = await _context.Allergien
            .Include(a => a.Bewohner)
            .FirstOrDefaultAsync(a => a.Id == id);

        if (allergie == null)
        {
            return NotFound(new { message = "Allergie wurde nicht gefunden." });
        }

        var zugriffsFehler = await ZugriffPruefen(allergie.Bewohner.StandortId);

        if (zugriffsFehler != null)
        {
            return zugriffsFehler;
        }

        if (string.IsNullOrWhiteSpace(request.Name))
        {
            return BadRequest(new { message = "Bitte einen Namen angeben." });
        }

        allergie.Name = request.Name.Trim();
        allergie.Bemerkung = request.Bemerkung;
        allergie.GeaendertAm = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return Ok(allergie);
    }

    [HttpPost("{id:guid}/deaktivieren")]
    [Authorize(Roles = "Administrator,Pflegekraft")]
    public async Task<IActionResult> Deaktivieren(Guid id)
    {
        var allergie = await _context.Allergien
            .Include(a => a.Bewohner)
            .FirstOrDefaultAsync(a => a.Id == id);

        if (allergie == null)
        {
            return NotFound(new { message = "Allergie wurde nicht gefunden." });
        }

        var zugriffsFehler = await ZugriffPruefen(allergie.Bewohner.StandortId);

        if (zugriffsFehler != null)
        {
            return zugriffsFehler;
        }

        allergie.IstAktiv = false;
        allergie.GeaendertAm = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return Ok(new { message = "Allergie wurde deaktiviert." });
    }

    private async Task<IActionResult?> ZugriffPruefen(int bewohnerStandortId)
    {
        var rolle = User.FindFirstValue(ClaimTypes.Role);

        if (rolle == "Administrator")
        {
            return null;
        }

        var benutzerId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        var benutzer = await _context.Users
            .FirstOrDefaultAsync(u => u.Id == benutzerId);

        if (benutzer == null || benutzer.StandortId != bewohnerStandortId)
        {
            return Forbid();
        }

        return null;
    }
}

public class AllergieErstellenRequest
{
    public Guid BewohnerId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Bemerkung { get; set; } = string.Empty;
}

public class AllergieBearbeitenRequest
{
    public string Name { get; set; } = string.Empty;
    public string Bemerkung { get; set; } = string.Empty;
}
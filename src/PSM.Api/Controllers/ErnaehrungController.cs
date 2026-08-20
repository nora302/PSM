using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PSM.Domain.Entities;
using PSM.Infrastructure.Data;
using System.Security.Claims;

namespace PSM.Api.Controllers;

[ApiController]
[Route("api/ernaehrung")]
[Authorize]
public class ErnaehrungController : ControllerBase
{
    private readonly AppDbContext _context;

    public ErnaehrungController(AppDbContext context)
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

        var ernaehrung = await _context.Ernaehrungen
            .FirstOrDefaultAsync(e =>
                e.BewohnerId == bewohnerId);

        if (ernaehrung == null)
        {
            return NotFound(new
            {
                message =
                    "Für diesen Bewohner wurde noch keine Ernährung erfasst."
            });
        }

        return Ok(ernaehrung);
    }

    [HttpPost]
    [Authorize(Roles = "Pflegekraft")]
    public async Task<IActionResult> Erstellen(
        ErnaehrungRequest request)
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
            .FirstOrDefaultAsync(b =>
                b.Id == request.BewohnerId);

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

        var existiert = await _context.Ernaehrungen
            .AnyAsync(e =>
                e.BewohnerId == request.BewohnerId);

        if (existiert)
        {
            return BadRequest(new
            {
                message =
                    "Für diesen Bewohner existiert bereits eine Ernährung."
            });
        }

        var ernaehrung = new Ernaehrung
        {
            Id = Guid.NewGuid(),
            BewohnerId = request.BewohnerId,
            Fruehstueck = request.Fruehstueck,
            Mittagessen = request.Mittagessen,
            Abendessen = request.Abendessen,
            Besonderheiten = request.Besonderheiten,
            Kostform = request.Kostform,
            ErstelltAm = DateTime.UtcNow
        };

        _context.Ernaehrungen.Add(ernaehrung);

        await _context.SaveChangesAsync();

        return Ok(ernaehrung);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Roles = "Pflegekraft")]
    public async Task<IActionResult> Bearbeiten(
        Guid id,
        ErnaehrungRequest request)
    {
        var benutzerId =
            User.FindFirstValue(ClaimTypes.NameIdentifier);

        var benutzer = await _context.Users
            .FirstOrDefaultAsync(u => u.Id == benutzerId);

        if (benutzer == null)
        {
            return Unauthorized();
        }

        var ernaehrung = await _context.Ernaehrungen
            .Include(e => e.Bewohner)
            .FirstOrDefaultAsync(e => e.Id == id);

        if (ernaehrung == null)
        {
            return NotFound(new
            {
                message = "Ernährung wurde nicht gefunden."
            });
        }

        if (benutzer.StandortId !=
            ernaehrung.Bewohner.StandortId)
        {
            return Forbid();
        }

        ernaehrung.Fruehstueck =
            request.Fruehstueck;

        ernaehrung.Mittagessen =
            request.Mittagessen;

        ernaehrung.Abendessen =
            request.Abendessen;

        ernaehrung.Besonderheiten =
            request.Besonderheiten;

        ernaehrung.Kostform =
            request.Kostform;

        ernaehrung.GeaendertAm =
            DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return Ok(ernaehrung);
    }
}

public class ErnaehrungRequest
{
    public Guid BewohnerId { get; set; }

    public string Fruehstueck { get; set; } =
        string.Empty;

    public string Mittagessen { get; set; } =
        string.Empty;

    public string Abendessen { get; set; } =
        string.Empty;

    public string Besonderheiten { get; set; } =
        string.Empty;

    public string Kostform { get; set; } =
        string.Empty;
}
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
            return NotFound(new { message = "Bewohner wurde nicht gefunden." });
        }

        var zugriffsFehler = await ZugriffPruefen(bewohner.StandortId);

        if (zugriffsFehler != null)
        {
            return zugriffsFehler;
        }

        var ernaehrung = await _context.Ernaehrungen
            .FirstOrDefaultAsync(e => e.BewohnerId == bewohnerId);

        if (ernaehrung == null)
        {
            return Ok(null);
        }

        return Ok(ernaehrung);
    }

    [HttpPost]
    [Authorize(Roles = "Administrator,Pflegekraft")]
    public async Task<IActionResult> ErstellenOderAktualisieren(
        ErnaehrungRequest request)
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

        var ernaehrung = await _context.Ernaehrungen
            .FirstOrDefaultAsync(e => e.BewohnerId == request.BewohnerId);

        if (ernaehrung == null)
        {
            ernaehrung = new Ernaehrung
            {
                Id = Guid.NewGuid(),
                BewohnerId = request.BewohnerId,
                Fruehstueck = request.Fruehstueck,
                Mittagessen = request.Mittagessen,
                Abendessen = request.Abendessen,
                Kostform = request.Kostform,
                Besonderheiten = request.Besonderheiten,
                Krankheit = request.Krankheit,
                ErstelltAm = DateTime.UtcNow
            };

            _context.Ernaehrungen.Add(ernaehrung);
        }
        else
        {
            ernaehrung.Fruehstueck = request.Fruehstueck;
            ernaehrung.Mittagessen = request.Mittagessen;
            ernaehrung.Abendessen = request.Abendessen;
            ernaehrung.Kostform = request.Kostform;
            ernaehrung.Besonderheiten = request.Besonderheiten;
            ernaehrung.Krankheit = request.Krankheit;
            ernaehrung.GeaendertAm = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync();

        return Ok(ernaehrung);
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

public class ErnaehrungRequest
{
    public Guid BewohnerId { get; set; }
    public string Fruehstueck { get; set; } = string.Empty;
    public string Mittagessen { get; set; } = string.Empty;
    public string Abendessen { get; set; } = string.Empty;
    public string Kostform { get; set; } = string.Empty;
    public string Besonderheiten { get; set; } = string.Empty;
    public string Krankheit { get; set; } = string.Empty;
}
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PSM.Domain.Entities;
using PSM.Infrastructure.Data;
using System.Security.Claims;

namespace PSM.Api.Controllers;

[ApiController]
[Route("api/essensausgaben")]
[Authorize]
public class EssensausgabeController : ControllerBase
{
    private readonly AppDbContext _context;

    private static readonly string[] Mahlzeiten =
    {
        "Fruehstueck",
        "Mittagessen",
        "Abendessen"
    };

    public EssensausgabeController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet("bewohner/{bewohnerId:guid}/{datum}")]
    public async Task<IActionResult> ProTag(Guid bewohnerId, DateOnly datum)
    {
        var bewohner = await _context.Bewohner
            .FirstOrDefaultAsync(b => b.Id == bewohnerId);

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

            if (benutzer == null || benutzer.StandortId != bewohner.StandortId)
            {
                return Forbid();
            }
        }

        var vorhandene = await _context.Essensausgaben
            .Where(e => e.BewohnerId == bewohnerId && e.Datum == datum)
            .ToListAsync();

        foreach (var mahlzeit in Mahlzeiten)
        {
            if (!vorhandene.Any(e => e.Mahlzeit == mahlzeit))
            {
                var neueAusgabe = new Essensausgabe
                {
                    Id = Guid.NewGuid(),
                    BewohnerId = bewohnerId,
                    Datum = datum,
                    Mahlzeit = mahlzeit,
                    IstErledigt = false
                };
                _context.Essensausgaben.Add(neueAusgabe);
                vorhandene.Add(neueAusgabe);
            }
        }

        await _context.SaveChangesAsync();

        var sortiert = vorhandene
            .OrderBy(e => Array.IndexOf(Mahlzeiten, e.Mahlzeit))
            .ToList();

        return Ok(sortiert);
    }

    [HttpPost("{id:guid}/erledigen")]
    [Authorize(Roles = "Administrator,Pflegekraft,Hauswirtschaftskraft")]
    public async Task<IActionResult> Erledigen(Guid id)
    {
        var ausgabe = await _context.Essensausgaben
            .Include(e => e.Bewohner)
            .FirstOrDefaultAsync(e => e.Id == id);

        if (ausgabe == null)
        {
            return NotFound(new { message = "Essensausgabe wurde nicht gefunden." });
        }

        var rolle = User.FindFirstValue(ClaimTypes.Role);
        var benutzerId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;

        if (rolle != "Administrator")
        {
            var benutzer = await _context.Users
                .FirstOrDefaultAsync(u => u.Id == benutzerId);

            if (benutzer == null || benutzer.StandortId != ausgabe.Bewohner.StandortId)
            {
                return Forbid();
            }
        }

        ausgabe.IstErledigt = true;
        ausgabe.ErledigtAm = DateTime.UtcNow;
        ausgabe.ErledigtVonBenutzerId = benutzerId;

        await _context.SaveChangesAsync();

        return Ok(ausgabe);
    }

    [HttpPost("{id:guid}/nicht-erledigt")]
    [Authorize(Roles = "Administrator,Pflegekraft,Hauswirtschaftskraft")]
    public async Task<IActionResult> NichtErledigt(Guid id)
    {
        var ausgabe = await _context.Essensausgaben
            .Include(e => e.Bewohner)
            .FirstOrDefaultAsync(e => e.Id == id);

        if (ausgabe == null)
        {
            return NotFound(new { message = "Essensausgabe wurde nicht gefunden." });
        }

        var rolle = User.FindFirstValue(ClaimTypes.Role);

        if (rolle != "Administrator")
        {
            var benutzerId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var benutzer = await _context.Users
                .FirstOrDefaultAsync(u => u.Id == benutzerId);

            if (benutzer == null || benutzer.StandortId != ausgabe.Bewohner.StandortId)
            {
                return Forbid();
            }
        }

        ausgabe.IstErledigt = false;
        ausgabe.ErledigtAm = null;
        ausgabe.ErledigtVonBenutzerId = null;

        await _context.SaveChangesAsync();

        return Ok(ausgabe);
    }
}
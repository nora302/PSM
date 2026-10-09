using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PSM.Infrastructure.Data;
using System.Security.Claims;

namespace PSM.Api.Controllers;

[ApiController]
[Route("api/benachrichtigungen")]
[Authorize]
public class BenachrichtigungController : ControllerBase
{
    private readonly AppDbContext _context;

    public BenachrichtigungController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<IActionResult> Alle()
    {
        var rolle = User.FindFirstValue(ClaimTypes.Role);

        if (string.IsNullOrEmpty(rolle))
        {
            return Unauthorized();
        }

        var abfrage = _context.Benachrichtigungen
            .Include(n => n.Standort)
            .Where(n => n.EmpfaengerRolle == rolle && !n.Gelesen);

        if (rolle == "Pflegekraft" || rolle == "Hauswirtschaftskraft")
        {
            var benutzerId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var benutzer = await _context.Users
                .FirstOrDefaultAsync(u => u.Id == benutzerId);

            if (benutzer?.StandortId == null)
            {
                return Ok(new
                {
                    gesamtAnzahl = 0,
                    nachTyp = Array.Empty<object>(),
                    details = Array.Empty<object>()
                });
            }

            abfrage = abfrage.Where(n => n.StandortId == benutzer.StandortId);
        }

        var benachrichtigungen = await abfrage
            .OrderByDescending(n => n.ErstelltAm)
            .ToListAsync();

        var zusammenfassung = benachrichtigungen
            .GroupBy(n => n.Typ)
            .Select(g => new
            {
                Typ = g.Key,
                Anzahl = g.Count()
            })
            .ToList();

        return Ok(new
        {
            gesamtAnzahl = benachrichtigungen.Count,
            nachTyp = zusammenfassung,
            details = benachrichtigungen.Select(n => new
            {
                n.Id,
                n.Typ,
                n.Nachricht,
                Standort = n.Standort.Name,
                n.ErstelltAm
            })
        });
    }

    [HttpPost("tout-lu")]
    public async Task<IActionResult> AlleAlsGelesenMarkieren()
    {
        var rolle = User.FindFirstValue(ClaimTypes.Role);

        if (string.IsNullOrEmpty(rolle))
        {
            return Unauthorized();
        }

        var abfrage = _context.Benachrichtigungen
            .Where(n => n.EmpfaengerRolle == rolle && !n.Gelesen);

        if (rolle == "Pflegekraft" || rolle == "Hauswirtschaftskraft")
        {
            var benutzerId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var benutzer = await _context.Users
                .FirstOrDefaultAsync(u => u.Id == benutzerId);

            if (benutzer?.StandortId == null)
            {
                return Ok(new { message = "Keine Benachrichtigungen." });
            }

            abfrage = abfrage.Where(n => n.StandortId == benutzer.StandortId);
        }

        var betroffene = await abfrage.ToListAsync();

        foreach (var benachrichtigung in betroffene)
        {
            benachrichtigung.Gelesen = true;
        }

        await _context.SaveChangesAsync();

        return Ok(new { message = "Alle Benachrichtigungen wurden als gelesen markiert." });
    }

    [HttpGet("anzahl-oeffentlich")]
    [AllowAnonymous]
    public async Task<IActionResult> AnzahlOeffentlich()
    {
        var anzahl = await _context.Benachrichtigungen
            .CountAsync(n => !n.Gelesen);

        return Ok(new { anzahl });
    }
}
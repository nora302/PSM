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

    public EssensausgabeController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<IActionResult> Alle(
        DateOnly datum,
        string mahlzeit)
    {
        var rolle =
            User.FindFirstValue(ClaimTypes.Role);

        var benutzerId =
            User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (rolle == "Administrator")
        {
            var alle = await _context.Essensausgaben
                .Include(e => e.Bewohner)
                .ThenInclude(b => b.Standort)
                .Where(e =>
                    e.Datum == datum &&
                    e.Mahlzeit == mahlzeit)
                .OrderBy(e =>
                    e.Bewohner.StandortBewohnerNummer)
                .ToListAsync();

            return Ok(alle);
        }

        var benutzer = await _context.Users
            .FirstOrDefaultAsync(
                u => u.Id == benutzerId);

        if (benutzer == null)
        {
            return Unauthorized();
        }

        if (benutzer.StandortId == null)
        {
            return Forbid();
        }

        var ausgaben = await _context.Essensausgaben
            .Include(e => e.Bewohner)
            .Where(e =>
                e.Bewohner.StandortId ==
                    benutzer.StandortId &&
                e.Datum == datum &&
                e.Mahlzeit == mahlzeit)
            .OrderBy(e =>
                e.Bewohner.StandortBewohnerNummer)
            .ToListAsync();

        return Ok(ausgaben);
    }

    [HttpPost]
    [Authorize(Roles = "Hauswirtschaftskraft")]
    public async Task<IActionResult> Erstellen(
        EssensausgabeErstellenRequest request)
    {
        var benutzerId =
            User.FindFirstValue(ClaimTypes.NameIdentifier);

        var benutzer = await _context.Users
            .FirstOrDefaultAsync(
                u => u.Id == benutzerId);

        if (benutzer == null)
        {
            return Unauthorized();
        }

        var bewohner = await _context.Bewohner
            .FirstOrDefaultAsync(
                b => b.Id == request.BewohnerId);

        if (bewohner == null)
        {
            return NotFound(new
            {
                message =
                    "Bewohner wurde nicht gefunden."
            });
        }

        if (benutzer.StandortId !=
            bewohner.StandortId)
        {
            return Forbid();
        }

        var existiert =
            await _context.Essensausgaben.AnyAsync(
                e =>
                    e.BewohnerId ==
                        request.BewohnerId &&
                    e.Datum ==
                        request.Datum &&
                    e.Mahlzeit ==
                        request.Mahlzeit);

        if (existiert)
        {
            return BadRequest(new
            {
                message =
                    "Für diesen Bewohner existiert bereits eine Essensausgabe für diese Mahlzeit."
            });
        }

        var essensausgabe =
            new Essensausgabe
            {
                Id = Guid.NewGuid(),
                BewohnerId =
                    request.BewohnerId,
                Datum =
                    request.Datum,
                Mahlzeit =
                    request.Mahlzeit,
                IstErledigt =
                    false
            };

        _context.Essensausgaben.Add(
            essensausgabe);

        await _context.SaveChangesAsync();

        return Ok(essensausgabe);
    }

    [HttpPost("{id:guid}/erledigen")]
    [Authorize(Roles = "Hauswirtschaftskraft")]
    public async Task<IActionResult> Erledigen(
        Guid id)
    {
        var benutzerId =
            User.FindFirstValue(ClaimTypes.NameIdentifier);

        var benutzer = await _context.Users
            .FirstOrDefaultAsync(
                u => u.Id == benutzerId);

        if (benutzer == null)
        {
            return Unauthorized();
        }

        var essensausgabe =
            await _context.Essensausgaben
                .Include(e => e.Bewohner)
                .FirstOrDefaultAsync(
                    e => e.Id == id);

        if (essensausgabe == null)
        {
            return NotFound(new
            {
                message =
                    "Essensausgabe wurde nicht gefunden."
            });
        }

        if (benutzer.StandortId !=
            essensausgabe.Bewohner.StandortId)
        {
            return Forbid();
        }

        if (essensausgabe.IstErledigt)
        {
            return BadRequest(new
            {
                message =
                    "Die Essensausgabe ist bereits erledigt."
            });
        }

        essensausgabe.IstErledigt = true;
        essensausgabe.ErledigtAm =
            DateTime.UtcNow;
        essensausgabe.ErledigtVonBenutzerId =
            benutzer.Id;

        await _context.SaveChangesAsync();

        var naechsteEssensausgabe =
            await _context.Essensausgaben
                .Include(e => e.Bewohner)
                .Where(e =>
                    e.Bewohner.StandortId ==
                        benutzer.StandortId &&
                    e.Datum ==
                        essensausgabe.Datum &&
                    e.Mahlzeit ==
                        essensausgabe.Mahlzeit &&
                    !e.IstErledigt)
                .OrderBy(e =>
                    e.Bewohner.StandortBewohnerNummer)
                .FirstOrDefaultAsync();

        return Ok(new
        {
            message =
                "Essensausgabe wurde als erledigt markiert.",

            essensausgabe.Id,

            NaechsterBewohnerId =
                naechsteEssensausgabe?
                    .BewohnerId,

            HatNaechstenBewohner =
                naechsteEssensausgabe != null
        });
    }
}

public class EssensausgabeErstellenRequest
{
    public Guid BewohnerId { get; set; }

    public DateOnly Datum { get; set; }

    public string Mahlzeit { get; set; } =
        string.Empty;
}
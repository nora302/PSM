using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PSM.Domain.Entities;
using PSM.Infrastructure.Data;
using System.Security.Claims;

namespace PSM.Api.Controllers;

[ApiController]
[Route("api/lebensmittelartikel")]
[Authorize]
public class LebensmittelArtikelController : ControllerBase
{
    private readonly AppDbContext _context;

    public LebensmittelArtikelController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<IActionResult> Alle()
    {
        var artikel = await _context.LebensmittelArtikel
            .Where(a => a.IstAktiv)
            .OrderBy(a => a.Name)
            .ToListAsync();

        return Ok(artikel);
    }

    [HttpPost]
    [Authorize(Roles = "Administrator,Hauswirtschaftskraft")]
    public async Task<IActionResult> Erstellen(LebensmittelArtikelErstellenRequest request)
    {
        var benutzerId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;

        if (string.IsNullOrWhiteSpace(request.Name))
        {
            return BadRequest(new { message = "Bitte einen Namen angeben." });
        }

        var existiertBereits = await _context.LebensmittelArtikel
            .AnyAsync(a => a.Name.ToLower() == request.Name.Trim().ToLower() && a.IstAktiv);

        if (existiertBereits)
        {
            return BadRequest(new { message = "Dieser Artikel existiert bereits im Katalog." });
        }

        var artikel = new LebensmittelArtikel
        {
            Id = Guid.NewGuid(),
            Name = request.Name.Trim(),
            IstAktiv = true,
            ErstelltAm = DateTime.UtcNow,
            ErstelltVonBenutzerId = benutzerId
        };

        _context.LebensmittelArtikel.Add(artikel);
        await _context.SaveChangesAsync();

        return Ok(artikel);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Roles = "Administrator,Hauswirtschaftskraft")]
    public async Task<IActionResult> Bearbeiten(Guid id, LebensmittelArtikelErstellenRequest request)
    {
        var artikel = await _context.LebensmittelArtikel
            .FirstOrDefaultAsync(a => a.Id == id);

        if (artikel == null)
        {
            return NotFound(new { message = "Artikel wurde nicht gefunden." });
        }

        if (string.IsNullOrWhiteSpace(request.Name))
        {
            return BadRequest(new { message = "Bitte einen Namen angeben." });
        }

        var existiertBereits = await _context.LebensmittelArtikel
            .AnyAsync(a =>
                a.Id != id &&
                a.Name.ToLower() == request.Name.Trim().ToLower() &&
                a.IstAktiv);

        if (existiertBereits)
        {
            return BadRequest(new { message = "Dieser Artikel existiert bereits im Katalog." });
        }

        artikel.Name = request.Name.Trim();

        await _context.SaveChangesAsync();

        return Ok(artikel);
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "Administrator,Hauswirtschaftskraft")]
    public async Task<IActionResult> Loeschen(Guid id)
    {
        var artikel = await _context.LebensmittelArtikel
            .FirstOrDefaultAsync(a => a.Id == id);

        if (artikel == null)
        {
            return NotFound(new { message = "Artikel wurde nicht gefunden." });
        }

        artikel.IstAktiv = false;

        await _context.SaveChangesAsync();

        return Ok(new { message = "Artikel wurde entfernt." });
    }
}

public class LebensmittelArtikelErstellenRequest
{
    public string Name { get; set; } = string.Empty;
}
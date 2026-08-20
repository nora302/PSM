using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PSM.Infrastructure.Data;
using PSM.Infrastructure.Identity;

namespace PSM.Api.Controllers;

[ApiController]
[Route("api/benutzer")]
[Authorize(Roles = "Administrator")]
public class BenutzerController : ControllerBase
{
    private readonly UserManager<Benutzer> _userManager;
    private readonly RoleManager<IdentityRole> _roleManager;
    private readonly AppDbContext _context;

    public BenutzerController(
        UserManager<Benutzer> userManager,
        RoleManager<IdentityRole> roleManager,
        AppDbContext context)
    {
        _userManager = userManager;
        _roleManager = roleManager;
        _context = context;
    }

    [HttpGet]
    public async Task<IActionResult> Alle()
    {
        var benutzerListe = await _userManager.Users
            .OrderBy(u => u.Nachname)
            .ThenBy(u => u.Vorname)
            .ToListAsync();

        var result = new List<object>();

        foreach (var benutzer in benutzerListe)
        {
            var rollen = await _userManager.GetRolesAsync(benutzer);

            result.Add(new
            {
                benutzer.Id,
                benutzer.UserName,
                benutzer.Vorname,
                benutzer.Nachname,
                benutzer.StandortId,
                benutzer.IstAktiv,
                Rollen = rollen
            });
        }

        return Ok(result);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> NachId(string id)
    {
        var benutzer = await _userManager.FindByIdAsync(id);

        if (benutzer == null)
        {
            return NotFound(new
            {
                message = "Benutzer wurde nicht gefunden."
            });
        }

        var rollen = await _userManager.GetRolesAsync(benutzer);

        return Ok(new
        {
            benutzer.Id,
            benutzer.UserName,
            benutzer.Vorname,
            benutzer.Nachname,
            benutzer.StandortId,
            benutzer.IstAktiv,
            Rollen = rollen
        });
    }

    [HttpPost]
    public async Task<IActionResult> Erstellen(
        BenutzerErstellenRequest request)
    {
        if (!await _roleManager.RoleExistsAsync(request.Rolle))
        {
            return BadRequest(new
            {
                message = "Die angegebene Rolle existiert nicht."
            });
        }

        if (request.Rolle == "Administrator")
        {
            return BadRequest(new
            {
                message =
                    "Weitere Administratoren können hier nicht erstellt werden."
            });
        }

        var vorhanden =
            await _userManager.FindByNameAsync(request.Benutzername);

        if (vorhanden != null)
        {
            return BadRequest(new
            {
                message = "Benutzername existiert bereits."
            });
        }

        var validierungsFehler =
            await StandortUndRollePruefen(
                request.Rolle,
                request.StandortId);

        if (validierungsFehler != null)
        {
            return validierungsFehler;
        }

        if (request.Rolle == "Kuechenmitarbeiter")
        {
            request.StandortId = null;
        }

        var benutzer = new Benutzer
        {
            UserName = request.Benutzername,
            Vorname = request.Vorname,
            Nachname = request.Nachname,
            StandortId = request.StandortId,
            IstAktiv = true
        };

        var result = await _userManager.CreateAsync(
            benutzer,
            request.Passwort);

        if (!result.Succeeded)
        {
            return BadRequest(new
            {
                errors = result.Errors.Select(e => e.Description)
            });
        }

        var rollenResult =
            await _userManager.AddToRoleAsync(
                benutzer,
                request.Rolle);

        if (!rollenResult.Succeeded)
        {
            await _userManager.DeleteAsync(benutzer);

            return BadRequest(new
            {
                errors = rollenResult.Errors.Select(
                    e => e.Description)
            });
        }

        return Ok(new
        {
            message = "Benutzer wurde erfolgreich erstellt.",
            benutzer.Id,
            benutzer.UserName,
            benutzer.Vorname,
            benutzer.Nachname,
            benutzer.StandortId,
            Rolle = request.Rolle
        });
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Bearbeiten(
        string id,
        BenutzerBearbeitenRequest request)
    {
        var benutzer = await _userManager.FindByIdAsync(id);

        if (benutzer == null)
        {
            return NotFound(new
            {
                message = "Benutzer wurde nicht gefunden."
            });
        }

        if (!await _roleManager.RoleExistsAsync(request.Rolle))
        {
            return BadRequest(new
            {
                message = "Die angegebene Rolle existiert nicht."
            });
        }

        if (request.Rolle == "Administrator")
        {
            return BadRequest(new
            {
                message =
                    "Die Rolle Administrator kann hier nicht zugewiesen werden."
            });
        }

        var validierungsFehler =
            await StandortUndRollePruefen(
                request.Rolle,
                request.StandortId);

        if (validierungsFehler != null)
        {
            return validierungsFehler;
        }

        if (request.Rolle == "Kuechenmitarbeiter")
        {
            request.StandortId = null;
        }

        benutzer.Vorname = request.Vorname;
        benutzer.Nachname = request.Nachname;
        benutzer.StandortId = request.StandortId;

        var updateResult =
            await _userManager.UpdateAsync(benutzer);

        if (!updateResult.Succeeded)
        {
            return BadRequest(new
            {
                errors = updateResult.Errors.Select(
                    e => e.Description)
            });
        }

        var aktuelleRollen =
            await _userManager.GetRolesAsync(benutzer);

        if (!aktuelleRollen.Contains(request.Rolle))
        {
            if (aktuelleRollen.Any())
            {
                var removeResult =
                    await _userManager.RemoveFromRolesAsync(
                        benutzer,
                        aktuelleRollen);

                if (!removeResult.Succeeded)
                {
                    return BadRequest(new
                    {
                        errors = removeResult.Errors.Select(
                            e => e.Description)
                    });
                }
            }

            var addResult =
                await _userManager.AddToRoleAsync(
                    benutzer,
                    request.Rolle);

            if (!addResult.Succeeded)
            {
                return BadRequest(new
                {
                    errors = addResult.Errors.Select(
                        e => e.Description)
                });
            }
        }

        return Ok(new
        {
            message = "Benutzer wurde erfolgreich aktualisiert.",
            benutzer.Id,
            benutzer.UserName,
            benutzer.Vorname,
            benutzer.Nachname,
            benutzer.StandortId,
            Rolle = request.Rolle,
            benutzer.IstAktiv
        });
    }

    [HttpPost("{id}/deaktivieren")]
    public async Task<IActionResult> Deaktivieren(string id)
    {
        var benutzer = await _userManager.FindByIdAsync(id);

        if (benutzer == null)
        {
            return NotFound(new
            {
                message = "Benutzer wurde nicht gefunden."
            });
        }

        if (benutzer.UserName == "H.Aidouni")
        {
            return BadRequest(new
            {
                message =
                    "Der initiale Administrator kann nicht deaktiviert werden."
            });
        }

        benutzer.IstAktiv = false;

        var result =
            await _userManager.UpdateAsync(benutzer);

        if (!result.Succeeded)
        {
            return BadRequest(new
            {
                errors = result.Errors.Select(
                    e => e.Description)
            });
        }

        return Ok(new
        {
            message = "Benutzer wurde deaktiviert."
        });
    }

    [HttpPost("{id}/aktivieren")]
    public async Task<IActionResult> Aktivieren(string id)
    {
        var benutzer = await _userManager.FindByIdAsync(id);

        if (benutzer == null)
        {
            return NotFound(new
            {
                message = "Benutzer wurde nicht gefunden."
            });
        }

        benutzer.IstAktiv = true;

        var result =
            await _userManager.UpdateAsync(benutzer);

        if (!result.Succeeded)
        {
            return BadRequest(new
            {
                errors = result.Errors.Select(
                    e => e.Description)
            });
        }

        return Ok(new
        {
            message = "Benutzer wurde aktiviert."
        });
    }

    private async Task<IActionResult?> StandortUndRollePruefen(
        string rolle,
        int? standortId)
    {
        if (rolle == "Pflegekraft" ||
            rolle == "Hauswirtschaftskraft")
        {
            if (standortId == null)
            {
                return BadRequest(new
                {
                    message =
                        "Für diese Rolle muss ein Standort angegeben werden."
                });
            }
        }

        if (standortId != null)
        {
            var standortExistiert =
                await _context.Standorte.AnyAsync(
                    s => s.Id == standortId.Value);

            if (!standortExistiert)
            {
                return BadRequest(new
                {
                    message =
                        $"Standort mit Id {standortId} existiert nicht."
                });
            }
        }

        return null;
    }
}

public class BenutzerErstellenRequest
{
    public string Benutzername { get; set; } = string.Empty;

    public string Passwort { get; set; } = string.Empty;

    public string Vorname { get; set; } = string.Empty;

    public string Nachname { get; set; } = string.Empty;

    public int? StandortId { get; set; }

    public string Rolle { get; set; } = string.Empty;
}

public class BenutzerBearbeitenRequest
{
    public string Vorname { get; set; } = string.Empty;

    public string Nachname { get; set; } = string.Empty;

    public int? StandortId { get; set; }

    public string Rolle { get; set; } = string.Empty;
}
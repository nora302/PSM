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

        if (benutzer.UserName == "admin")
        {
            return BadRequest(new
            {
                message = "Der Benutzer 'admin' kann hier nicht bearbeitet werden."
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

        if (!string.Equals(benutzer.UserName, request.Benutzername, StringComparison.OrdinalIgnoreCase))
        {
            var vorhanden = await _userManager.FindByNameAsync(request.Benutzername);

            if (vorhanden != null)
            {
                return BadRequest(new
                {
                    message = "Dieser Benutzername ist bereits vergeben."
                });
            }

            var nameResult = await _userManager.SetUserNameAsync(benutzer, request.Benutzername);

            if (!nameResult.Succeeded)
            {
                return BadRequest(new
                {
                    errors = nameResult.Errors.Select(e => e.Description)
                });
            }
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

        if (benutzer.UserName == "admin")
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

    [HttpDelete("{id}")]
    public async Task<IActionResult> Loeschen(string id)
    {
        var benutzer = await _userManager.FindByIdAsync(id);

        if (benutzer == null)
        {
            return NotFound(new
            {
                message = "Benutzer wurde nicht gefunden."
            });
        }

        if (benutzer.UserName == "admin")
        {
            return BadRequest(new
            {
                message = "Der initiale Administrator kann nicht gelöscht werden."
            });
        }

        var hatDaten = await HatVerknuepfteDaten(id);

        if (hatDaten)
        {
            return BadRequest(new
            {
                message =
                    "Dieser Benutzer hat bereits Daten erstellt und kann nicht " +
                    "gelöscht werden. Bitte deaktivieren Sie den Benutzer stattdessen."
            });
        }

        var result = await _userManager.DeleteAsync(benutzer);

        if (!result.Succeeded)
        {
            return BadRequest(new
            {
                errors = result.Errors.Select(e => e.Description)
            });
        }

        return Ok(new
        {
            message = "Benutzer wurde endgültig gelöscht."
        });
    }

    private async Task<bool> HatVerknuepfteDaten(string benutzerId)
    {
        var hatPflegedokumentation = await _context.Pflegedokumentationen
            .AnyAsync(p =>
                p.ErstelltVonBenutzerId == benutzerId ||
                p.GeaendertVonBenutzerId == benutzerId);

        if (hatPflegedokumentation) return true;

        var hatEssensausgabe = await _context.Essensausgaben
            .AnyAsync(e => e.ErledigtVonBenutzerId == benutzerId);

        if (hatEssensausgabe) return true;

        var hatBestellung = await _context.Lebensmittelbestellungen
            .AnyAsync(b =>
                b.ErstelltVonBenutzerId == benutzerId ||
                b.GesendetVonBenutzerId == benutzerId ||
                b.BearbeitungGestartetVonBenutzerId == benutzerId ||
                b.ErledigtVonBenutzerId == benutzerId);

        if (hatBestellung) return true;

        var hatHistorie = await _context.BewohnerStandortHistorien
            .AnyAsync(h => h.GeaendertVonBenutzerId == benutzerId);

        if (hatHistorie) return true;

        return false;
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
    public string Benutzername { get; set; } = string.Empty;

    public string Vorname { get; set; } = string.Empty;

    public string Nachname { get; set; } = string.Empty;

    public int? StandortId { get; set; }

    public string Rolle { get; set; } = string.Empty;
}
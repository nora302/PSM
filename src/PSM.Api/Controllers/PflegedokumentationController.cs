using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PSM.Domain.Entities;
using PSM.Domain.Enums;
using PSM.Infrastructure.Data;
using System.Security.Claims;

namespace PSM.Api.Controllers;

[ApiController]
[Route("api/pflegedokumentation")]
[Authorize]
public class PflegedokumentationController : ControllerBase
{
    private readonly AppDbContext _context;

    public PflegedokumentationController(AppDbContext context)
    {
        _context = context;
    }

    // --------------------------------------------------
    // ARCHIV / SUCHE
    //
    // Beispiele:
    //
    // GET /api/pflegedokumentation/archiv
    //
    // GET /api/pflegedokumentation/archiv
    //     ?bewohnerName=bab
    //
    // GET /api/pflegedokumentation/archiv
    //     ?datum=2026-09-09
    //
    // GET /api/pflegedokumentation/archiv
    //     ?bewohnerName=bab&datum=2026-09-09
    // --------------------------------------------------

    [HttpGet("archiv")]
    [Authorize(Roles = "Pflegekraft,Administrator")]
    public async Task<IActionResult> Archiv(
        [FromQuery] string? bewohnerName,
        [FromQuery] DateOnly? datum)
    {
        var rolle = User.FindFirstValue(ClaimTypes.Role);

        var benutzerId =
            User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (string.IsNullOrWhiteSpace(benutzerId))
        {
            return Unauthorized();
        }

        var aktuellerBenutzer = await _context.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == benutzerId);

        if (aktuellerBenutzer == null)
        {
            return Unauthorized();
        }

        // Pflegekraft benötigt einen Standort.
        if (rolle != "Administrator" &&
            aktuellerBenutzer.StandortId == null)
        {
            return Forbid();
        }

        var query = _context.Pflegedokumentationen
            .AsNoTracking()
            .Include(p => p.Bewohner)
            .AsQueryable();

        // --------------------------------------------------
        // Standort-Schutz
        // Administrator = alle Standorte
        // Pflegekraft   = nur eigener Standort
        // --------------------------------------------------

        if (rolle != "Administrator")
        {
            query = query.Where(
                p => p.Bewohner.StandortId ==
                     aktuellerBenutzer.StandortId);
        }

        // --------------------------------------------------
        // Bewohner-Name filtern
        // Suche funktioniert mit Vorname oder Nachname.
        //
        // Beispiele:
        // bab
        // Meier
        // bab bab
        // --------------------------------------------------

        if (!string.IsNullOrWhiteSpace(bewohnerName))
        {
            var suchText =
                bewohnerName.Trim().ToLower();

            query = query.Where(p =>
                p.Bewohner.Vorname.ToLower()
                    .Contains(suchText) ||

                p.Bewohner.Nachname.ToLower()
                    .Contains(suchText) ||

                (p.Bewohner.Vorname + " " +
                 p.Bewohner.Nachname)
                    .ToLower()
                    .Contains(suchText));
        }

        // --------------------------------------------------
        // Datum filtern
        // --------------------------------------------------

        if (datum.HasValue)
        {
            query = query.Where(
                p => p.Datum == datum.Value);
        }

        // --------------------------------------------------
        // Dokumentationen laden
        // --------------------------------------------------

        var dokumentationen = await query
            .OrderByDescending(p => p.Datum)
            .ThenBy(p => p.Bewohner.Nachname)
            .ThenBy(p => p.Bewohner.Vorname)
            .ThenBy(p => p.Schicht)
            .ToListAsync();

        // --------------------------------------------------
        // Benutzer laden, damit wir Autoren anzeigen können
        // --------------------------------------------------

        var benutzerIds = dokumentationen
            .SelectMany(p => new[]
            {
                p.ErstelltVonBenutzerId,
                p.GeaendertVonBenutzerId
            })
            .Where(id => !string.IsNullOrWhiteSpace(id))
            .Distinct()
            .ToList();

        var benutzerListe = await _context.Users
            .AsNoTracking()
            .Where(u => benutzerIds.Contains(u.Id))
            .Select(u => new
            {
                u.Id,
                u.Vorname,
                u.Nachname
            })
            .ToListAsync();

        var benutzerNamen =
            benutzerListe.ToDictionary(
                u => u.Id,
                u => $"{u.Vorname} {u.Nachname}".Trim());

        string BenutzerNameOderUnbekannt(string? id)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                return "-";
            }

            return benutzerNamen.TryGetValue(
                id,
                out var name)
                ? name
                : "-";
        }

        // --------------------------------------------------
        // Ein Tagesbericht =
        // 1 Bewohner + 1 Datum
        //
        // Darunter:
        // Frühschicht
        // Spätdienst
        // Nachtschicht
        // --------------------------------------------------

        var ergebnisse = dokumentationen
            .GroupBy(p => new
            {
                p.BewohnerId,
                p.Datum
            })
            .Select(gruppe =>
            {
                var ersteDokumentation =
                    gruppe.First();

                var bewohner =
                    ersteDokumentation.Bewohner;

                return new
                {
                    bewohnerId = bewohner.Id,

                    bewohnerName =
                        $"{bewohner.Vorname} " +
                        $"{bewohner.Nachname}",

                    vorname = bewohner.Vorname,
                    nachname = bewohner.Nachname,

                    geburtsdatum =
                        bewohner.Geburtsdatum,

                    zimmernummer =
                        bewohner.Zimmernummer,

                    etage =
                        bewohner.Etage,

                    standortId =
                        bewohner.StandortId,

                    standortBewohnerNummer =
                        bewohner.StandortBewohnerNummer,

                    bewohnerIstArchiviert =
                        bewohner.IstArchiviert,

                    datum =
                        gruppe.Key.Datum,

                    dokumentationen = gruppe
                        .OrderBy(p => p.Schicht)
                        .Select(p => new
                        {
                            id = p.Id,

                            schicht = p.Schicht,

                            schichtName =
                                SchichtName(p.Schicht),

                            inhalt = p.Inhalt,

                            mitSpracheErstellt =
                                p.MitSpracheErstellt,

                            erstelltAm =
                                p.ErstelltAm,

                            erstelltVonBenutzerId =
                                p.ErstelltVonBenutzerId,

                            erstelltVon =
                                BenutzerNameOderUnbekannt(
                                    p.ErstelltVonBenutzerId),

                            geaendertAm =
                                p.GeaendertAm,

                            geaendertVonBenutzerId =
                                p.GeaendertVonBenutzerId,

                            geaendertVon =
                                BenutzerNameOderUnbekannt(
                                    p.GeaendertVonBenutzerId)
                        })
                        .ToList()
                };
            })
            .OrderByDescending(x => x.datum)
            .ThenBy(x => x.nachname)
            .ThenBy(x => x.vorname)
            .ToList();

        return Ok(ergebnisse);
    }

    // --------------------------------------------------
    // Alle Dokumentationen eines Bewohners an einem Datum
    // (Grundlage für den Tagesbericht)
    // --------------------------------------------------

    [HttpGet("bewohner/{bewohnerId:guid}/datum/{datum}")]
    public async Task<IActionResult> NachBewohnerUndDatum(
        Guid bewohnerId,
        DateOnly datum)
    {
        var bewohner = await _context.Bewohner
            .FirstOrDefaultAsync(
                b => b.Id == bewohnerId);

        if (bewohner == null)
        {
            return NotFound(new
            {
                message =
                    "Bewohner wurde nicht gefunden."
            });
        }

        var zugriffsFehler =
            await ZugriffPruefen(
                bewohner.StandortId);

        if (zugriffsFehler != null)
        {
            return zugriffsFehler;
        }

        var dokumentationen =
            await _context
                .Pflegedokumentationen
                .Where(p =>
                    p.BewohnerId == bewohnerId &&
                    p.Datum == datum)
                .OrderBy(p => p.Schicht)
                .ToListAsync();

        return Ok(dokumentationen);
    }

    // --------------------------------------------------
    // Einzelne Dokumentation
    // --------------------------------------------------

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> NachId(
        Guid id)
    {
        var dokumentation =
            await _context
                .Pflegedokumentationen
                .Include(p => p.Bewohner)
                .FirstOrDefaultAsync(
                    p => p.Id == id);

        if (dokumentation == null)
        {
            return NotFound(new
            {
                message =
                    "Pflegedokumentation wurde nicht gefunden."
            });
        }

        var zugriffsFehler =
            await ZugriffPruefen(
                dokumentation
                    .Bewohner
                    .StandortId);

        if (zugriffsFehler != null)
        {
            return zugriffsFehler;
        }

        return Ok(dokumentation);
    }

    // --------------------------------------------------
    // Erstellen
    // --------------------------------------------------

    [HttpPost]
    [Authorize(Roles = "Pflegekraft")]
    public async Task<IActionResult> Erstellen(
        PflegedokumentationErstellenRequest request)
    {
        var benutzerId =
            User.FindFirstValue(
                ClaimTypes.NameIdentifier);

        var benutzer =
            await _context.Users
                .FirstOrDefaultAsync(
                    u => u.Id == benutzerId);

        if (benutzer == null)
        {
            return Unauthorized();
        }

        var bewohner =
            await _context.Bewohner
                .FirstOrDefaultAsync(
                    b => b.Id ==
                         request.BewohnerId);

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

        if (!Enum.IsDefined(
                typeof(PflegeSchicht),
                request.Schicht))
        {
            return BadRequest(new
            {
                message =
                    "Die angegebene Schicht ist ungültig."
            });
        }

        if (string.IsNullOrWhiteSpace(
                request.Inhalt))
        {
            return BadRequest(new
            {
                message =
                    "Der Pflegebericht darf nicht leer sein."
            });
        }

        // --------------------------------------------------
        // Geschäftsregel:
        //
        // 1 Bewohner
        // + 1 Datum
        // + 1 Schicht
        // = maximal 1 Dokumentation
        // --------------------------------------------------

        var existiertBereits =
            await _context
                .Pflegedokumentationen
                .AnyAsync(p =>
                    p.BewohnerId ==
                        request.BewohnerId &&
                    p.Datum ==
                        request.Datum &&
                    p.Schicht ==
                        request.Schicht);

        if (existiertBereits)
        {
            return BadRequest(new
            {
                message =
                    "Für diesen Bewohner, dieses Datum " +
                    "und diese Schicht existiert bereits " +
                    "eine Pflegedokumentation."
            });
        }

        var dokumentation =
            new Pflegedokumentation
            {
                Id = Guid.NewGuid(),

                BewohnerId =
                    request.BewohnerId,

                Datum =
                    request.Datum,

                Schicht =
                    request.Schicht,

                Inhalt =
                    request.Inhalt,

                MitSpracheErstellt =
                    request.MitSpracheErstellt,

                ErstelltAm =
                    DateTime.UtcNow,

                ErstelltVonBenutzerId =
                    benutzer.Id
            };

        _context
            .Pflegedokumentationen
            .Add(dokumentation);

        try
        {
            await _context
                .SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            return BadRequest(new
            {
                message =
                    "Für diesen Bewohner, dieses Datum " +
                    "und diese Schicht existiert bereits " +
                    "eine Pflegedokumentation."
            });
        }

        return Ok(dokumentation);
    }

    // --------------------------------------------------
    // Korrigieren
    //
    // Nur Inhalt.
    // Bewohner, Datum und Schicht bleiben unverändert.
    // --------------------------------------------------

    [HttpPut("{id:guid}")]
    [Authorize(Roles = "Pflegekraft")]
    public async Task<IActionResult> Bearbeiten(
        Guid id,
        PflegedokumentationBearbeitenRequest request)
    {
        var benutzerId =
            User.FindFirstValue(
                ClaimTypes.NameIdentifier);

        var benutzer =
            await _context.Users
                .FirstOrDefaultAsync(
                    u => u.Id == benutzerId);

        if (benutzer == null)
        {
            return Unauthorized();
        }

        var dokumentation =
            await _context
                .Pflegedokumentationen
                .Include(p => p.Bewohner)
                .FirstOrDefaultAsync(
                    p => p.Id == id);

        if (dokumentation == null)
        {
            return NotFound(new
            {
                message =
                    "Pflegedokumentation wurde nicht gefunden."
            });
        }

        if (benutzer.StandortId !=
            dokumentation
                .Bewohner
                .StandortId)
        {
            return Forbid();
        }

        if (string.IsNullOrWhiteSpace(
                request.Inhalt))
        {
            return BadRequest(new
            {
                message =
                    "Der Pflegebericht darf nicht leer sein."
            });
        }

        // Keine automatische KI-Umschreibung.
        dokumentation.Inhalt =
            request.Inhalt;

        dokumentation.MitSpracheErstellt =
            request.MitSpracheErstellt;

        dokumentation.GeaendertAm =
            DateTime.UtcNow;

        dokumentation.GeaendertVonBenutzerId =
            benutzer.Id;

        await _context.SaveChangesAsync();

        return Ok(dokumentation);
    }

    // --------------------------------------------------
    // Schicht als deutschen Text zurückgeben
    // --------------------------------------------------

    private static string SchichtName(
        PflegeSchicht schicht)
    {
        return schicht switch
        {
            PflegeSchicht.Fruehschicht =>
                "Frühschicht",

            PflegeSchicht.Spaetdienst =>
                "Spätdienst",

            PflegeSchicht.Nachtschicht =>
                "Nachtschicht",

            _ => schicht.ToString()
        };
    }

    // --------------------------------------------------
    // Standort-Zugriff prüfen
    // --------------------------------------------------

    private async Task<IActionResult?>
        ZugriffPruefen(
            int bewohnerStandortId)
    {
        var rolle =
            User.FindFirstValue(
                ClaimTypes.Role);

        if (rolle == "Administrator")
        {
            return null;
        }

        var benutzerId =
            User.FindFirstValue(
                ClaimTypes.NameIdentifier);

        var benutzer =
            await _context.Users
                .FirstOrDefaultAsync(
                    u => u.Id == benutzerId);

        if (benutzer == null ||
            benutzer.StandortId !=
            bewohnerStandortId)
        {
            return Forbid();
        }

        return null;
    }
}

public class PflegedokumentationErstellenRequest
{
    public Guid BewohnerId { get; set; }

    public DateOnly Datum { get; set; }

    public PflegeSchicht Schicht { get; set; }

    public string Inhalt { get; set; } =
        string.Empty;

    public bool MitSpracheErstellt { get; set; }
}

public class PflegedokumentationBearbeitenRequest
{
    public string Inhalt { get; set; } =
        string.Empty;

    public bool MitSpracheErstellt { get; set; }
}
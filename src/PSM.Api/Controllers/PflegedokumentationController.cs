using MigraDoc.DocumentObjectModel;
using MigraDoc.Rendering;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PSM.Domain.Entities;
using PSM.Domain.Enums;
using PSM.Infrastructure.Data;
using System.Security.Claims;

namespace PSM.Api.Controllers;

[ApiController]
[Route("api/pflegedokumentationen")]
[Authorize]
public class PflegedokumentationController : ControllerBase
{
    private readonly AppDbContext _context;

    public PflegedokumentationController(AppDbContext context)
    {
        _context = context;
    }

    // --------------------------------------------------
    // Alle Dokumentationen eines Bewohners
    // --------------------------------------------------

    [HttpGet("bewohner/{bewohnerId:guid}")]
    public async Task<IActionResult> AlleVonBewohner(Guid bewohnerId)
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

        var zugriff = await DarfBewohnerSehen(bewohner);

        if (!zugriff)
        {
            return Forbid();
        }

        var dokumentationen =
            await _context.Pflegedokumentationen
                .Where(p => p.BewohnerId == bewohnerId)
                .OrderByDescending(p => p.Datum)
                .ThenBy(p => p.Schicht)
                .ToListAsync();

        return Ok(dokumentationen);
    }

    // --------------------------------------------------
    // Tagesbericht eines Bewohners lesen
    // Beispiel:
    // GET /api/pflegedokumentationen/
    // bewohner/{id}/tagesbericht?datum=2026-08-16
    // --------------------------------------------------

    [HttpGet("bewohner/{bewohnerId:guid}/tagesbericht")]
    public async Task<IActionResult> Tagesbericht(
        Guid bewohnerId,
        [FromQuery] DateOnly datum)
    {
        var bewohner = await _context.Bewohner
            .Include(b => b.Standort)
            .FirstOrDefaultAsync(b => b.Id == bewohnerId);

        if (bewohner == null)
        {
            return NotFound(new
            {
                message = "Bewohner wurde nicht gefunden."
            });
        }

        var zugriff = await DarfBewohnerSehen(bewohner);

        if (!zugriff)
        {
            return Forbid();
        }

        var dokumentationen =
            await _context.Pflegedokumentationen
                .Where(p =>
                    p.BewohnerId == bewohnerId &&
                    p.Datum == datum)
                .OrderBy(p => p.Schicht)
                .ToListAsync();

        var result = new List<object>();

        foreach (var dokumentation in dokumentationen)
        {
            var pflegekraft = await _context.Users
                .FirstOrDefaultAsync(
                    u => u.Id ==
                        dokumentation.ErstelltVonBenutzerId);

            result.Add(new
            {
                dokumentation.Id,
                dokumentation.BewohnerId,
                dokumentation.Datum,
                dokumentation.Schicht,
                SchichtName = SchichtName(dokumentation.Schicht),
                dokumentation.Inhalt,
                dokumentation.ErstelltAm,
                dokumentation.GeaendertAm,

                Pflegefachkraft = pflegekraft == null
                    ? dokumentation.ErstelltVonBenutzerId
                    : $"{pflegekraft.Vorname} {pflegekraft.Nachname}"
            });
        }

        return Ok(new
        {
            bewohner = new
            {
                bewohner.Id,
                bewohner.Vorname,
                bewohner.Nachname,
                bewohner.Geburtsdatum,
                bewohner.Zimmernummer,
                bewohner.StandortBewohnerNummer,
                Standort = bewohner.Standort.Name
            },

            datum,

            dokumentationen = result
        });
    }

    // --------------------------------------------------
    // Dokumentation erstellen
    // Nur Pflegekraft
    // --------------------------------------------------

    [HttpPost]
    [Authorize(Roles = "Pflegekraft")]
    public async Task<IActionResult> Erstellen(
        PflegedokumentationErstellenRequest request)
    {
        var benutzerId =
            User.FindFirstValue(ClaimTypes.NameIdentifier);

        var benutzer = await _context.Users
            .FirstOrDefaultAsync(u => u.Id == benutzerId);

        if (benutzer == null)
        {
            return Unauthorized();
        }

        if (!benutzer.IstAktiv)
        {
            return Unauthorized(new
            {
                message = "Benutzer ist nicht aktiv."
            });
        }

        var bewohner = await _context.Bewohner
            .FirstOrDefaultAsync(
                b => b.Id == request.BewohnerId);

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

        if (request.Datum == default)
        {
            return BadRequest(new
            {
                message = "Ein gültiges Datum muss angegeben werden."
            });
        }

        if (!Enum.IsDefined(typeof(PflegeSchicht), request.Schicht))
        {
            return BadRequest(new
            {
                message = "Ungültige Schicht."
            });
        }

        if (string.IsNullOrWhiteSpace(request.Inhalt))
        {
            return BadRequest(new
            {
                message = "Der Bericht darf nicht leer sein."
            });
        }

        // --------------------------------------------------
        // Nur eine Dokumentation:
        // Bewohner + Datum + Schicht
        // --------------------------------------------------

        var existiert =
            await _context.Pflegedokumentationen
                .AnyAsync(p =>
                    p.BewohnerId == request.BewohnerId &&
                    p.Datum == request.Datum &&
                    p.Schicht == request.Schicht);

        if (existiert)
        {
            return BadRequest(new
            {
                message =
                    $"Für {SchichtName(request.Schicht)} " +
                    $"am {request.Datum:dd.MM.yyyy} " +
                    "existiert bereits eine Dokumentation."
            });
        }

        var dokumentation = new Pflegedokumentation
        {
            Id = Guid.NewGuid(),

            BewohnerId = request.BewohnerId,

            Datum = request.Datum,

            Schicht = request.Schicht,

            Inhalt = request.Inhalt.Trim(),

            ErstelltAm = DateTime.UtcNow,

            ErstelltVonBenutzerId = benutzer.Id,

            MitSpracheErstellt =
                request.MitSpracheErstellt
        };

        _context.Pflegedokumentationen.Add(
            dokumentation);

        await _context.SaveChangesAsync();

        return Ok(new
        {
            message =
                "Pflegedokumentation wurde gespeichert.",

            dokumentation.Id,
            dokumentation.BewohnerId,
            dokumentation.Datum,
            dokumentation.Schicht,

            SchichtName =
                SchichtName(dokumentation.Schicht),

            dokumentation.Inhalt,
            dokumentation.ErstelltAm,

            Pflegefachkraft =
                $"{benutzer.Vorname} {benutzer.Nachname}"
        });
    }

    // --------------------------------------------------
    // Bericht korrigieren
    // Datum und Schicht werden NICHT verändert.
    // --------------------------------------------------

    [HttpPut("{id:guid}")]
    [Authorize(Roles = "Pflegekraft")]
    public async Task<IActionResult> Bearbeiten(
        Guid id,
        PflegedokumentationBearbeitenRequest request)
    {
        var benutzerId =
            User.FindFirstValue(ClaimTypes.NameIdentifier);

        var benutzer = await _context.Users
            .FirstOrDefaultAsync(u => u.Id == benutzerId);

        if (benutzer == null)
        {
            return Unauthorized();
        }

        var dokumentation =
            await _context.Pflegedokumentationen
                .Include(p => p.Bewohner)
                .FirstOrDefaultAsync(p => p.Id == id);

        if (dokumentation == null)
        {
            return NotFound(new
            {
                message =
                    "Pflegedokumentation wurde nicht gefunden."
            });
        }

        if (benutzer.StandortId !=
            dokumentation.Bewohner.StandortId)
        {
            return Forbid();
        }

        if (string.IsNullOrWhiteSpace(request.Inhalt))
        {
            return BadRequest(new
            {
                message = "Der Bericht darf nicht leer sein."
            });
        }

        dokumentation.Inhalt =
            request.Inhalt.Trim();

        dokumentation.MitSpracheErstellt =
            request.MitSpracheErstellt;

        dokumentation.GeaendertAm =
            DateTime.UtcNow;

        dokumentation.GeaendertVonBenutzerId =
            benutzer.Id;

        await _context.SaveChangesAsync();

        return Ok(new
        {
            message =
                "Pflegedokumentation wurde aktualisiert.",

            dokumentation.Id,
            dokumentation.Datum,
            dokumentation.Schicht,

            SchichtName =
                SchichtName(dokumentation.Schicht),

            dokumentation.Inhalt,
            dokumentation.GeaendertAm
        });
    }

    // --------------------------------------------------
    // EIN PDF für Bewohner + Datum
    //
    // GET:
    // /api/pflegedokumentationen/
    // bewohner/{bewohnerId}/tagesbericht/pdf
    // ?datum=2026-08-16
    // --------------------------------------------------

    [HttpGet("bewohner/{bewohnerId:guid}/tagesbericht/pdf")]
    public async Task<IActionResult> TagesberichtPdf(
        Guid bewohnerId,
        [FromQuery] DateOnly datum)
    {
        var bewohner = await _context.Bewohner
            .Include(b => b.Standort)
            .FirstOrDefaultAsync(
                b => b.Id == bewohnerId);

        if (bewohner == null)
        {
            return NotFound(new
            {
                message = "Bewohner wurde nicht gefunden."
            });
        }

        var zugriff = await DarfBewohnerSehen(bewohner);

        if (!zugriff)
        {
            return Forbid();
        }

        var dokumentationen =
            await _context.Pflegedokumentationen
                .Where(p =>
                    p.BewohnerId == bewohnerId &&
                    p.Datum == datum)
                .OrderBy(p => p.Schicht)
                .ToListAsync();

        if (!dokumentationen.Any())
        {
            return NotFound(new
            {
                message =
                    $"Für den {datum:dd.MM.yyyy} " +
                    "existiert kein Pflegebericht."
            });
        }

        var frueh =
            dokumentationen.FirstOrDefault(
                p => p.Schicht ==
                    PflegeSchicht.Fruehschicht);

        var spaet =
            dokumentationen.FirstOrDefault(
                p => p.Schicht ==
                    PflegeSchicht.Spaetdienst);

        var nacht =
            dokumentationen.FirstOrDefault(
                p => p.Schicht ==
                    PflegeSchicht.Nachtschicht);

        // --------------------------------------------------
        // PDF erstellen
        // --------------------------------------------------

        var document = new Document();

        document.Info.Title =
            $"PSM Pflegebericht {datum:dd.MM.yyyy}";

        var section =
            document.AddSection();

        section.PageSetup.TopMargin =
            Unit.FromCentimeter(1.8);

        section.PageSetup.BottomMargin =
            Unit.FromCentimeter(1.8);

        section.PageSetup.LeftMargin =
            Unit.FromCentimeter(2);

        section.PageSetup.RightMargin =
            Unit.FromCentimeter(2);

        // --------------------------------------------------
        // Titel
        // --------------------------------------------------

        var titel =
            section.AddParagraph();

        titel.AddFormattedText(
            "PSM - Pflegebericht",
            TextFormat.Bold);

        titel.Format.Font.Size = 18;

        titel.Format.SpaceAfter =
            Unit.FromCentimeter(0.6);

        // --------------------------------------------------
        // Bewohnerdaten
        // --------------------------------------------------

        AddZeile(
            section,
            "Bewohner:",
            $"{bewohner.Vorname} {bewohner.Nachname}");

        AddZeile(
            section,
            "Geburtsdatum:",
            bewohner.Geburtsdatum
                .ToString("dd.MM.yyyy"));

        AddZeile(
            section,
            "Standort:",
            bewohner.Standort.Name);

        AddZeile(
            section,
            "Zimmer:",
            bewohner.Zimmernummer);

        AddZeile(
            section,
            "Bewohnernummer:",
            bewohner.StandortBewohnerNummer
                .ToString());

        AddZeile(
            section,
            "Berichtsdatum:",
            datum.ToString("dd.MM.yyyy"));

        section.AddParagraph();

        // --------------------------------------------------
        // Frühschicht
        // --------------------------------------------------

        await AddSchichtZumPdf(
            section,
            "Frühschicht",
            frueh);

        // --------------------------------------------------
        // Spätdienst
        // --------------------------------------------------

        await AddSchichtZumPdf(
            section,
            "Spätdienst",
            spaet);

        // --------------------------------------------------
        // Nachtschicht
        // --------------------------------------------------

        await AddSchichtZumPdf(
            section,
            "Nachtschicht",
            nacht);

        // --------------------------------------------------
        // Footer
        // --------------------------------------------------

        var footer =
            section.Footers.Primary
                .AddParagraph();

        footer.AddText(
            $"PSM Pflegebericht - " +
            $"{datum:dd.MM.yyyy}");

        footer.Format.Font.Size = 8;

        footer.Format.Alignment =
            ParagraphAlignment.Center;

        // --------------------------------------------------
        // PDF rendern
        // --------------------------------------------------

        var renderer =
            new PdfDocumentRenderer
            {
                Document = document
            };

        renderer.RenderDocument();

        using var stream =
            new MemoryStream();

        renderer.PdfDocument.Save(
            stream,
            false);

        var fileName =
            $"Pflegebericht_" +
            $"{bewohner.Vorname}_" +
            $"{bewohner.Nachname}_" +
            $"{datum:yyyyMMdd}.pdf";

        return File(
            stream.ToArray(),
            "application/pdf",
            fileName);
    }

    // --------------------------------------------------
    // Zugriffsprüfung
    // --------------------------------------------------

    private async Task<bool> DarfBewohnerSehen(
        Bewohner bewohner)
    {
        var rolle =
            User.FindFirstValue(ClaimTypes.Role);

        if (rolle == "Administrator")
        {
            return true;
        }

        var benutzerId =
            User.FindFirstValue(ClaimTypes.NameIdentifier);

        var benutzer = await _context.Users
            .FirstOrDefaultAsync(
                u => u.Id == benutzerId);

        if (benutzer == null)
        {
            return false;
        }

        return benutzer.StandortId ==
               bewohner.StandortId;
    }

    // --------------------------------------------------
    // Schicht in PDF schreiben
    // --------------------------------------------------

    private async Task AddSchichtZumPdf(
        Section section,
        string titel,
        Pflegedokumentation? dokumentation)
    {
        var ueberschrift =
            section.AddParagraph();

        ueberschrift.AddFormattedText(
            titel,
            TextFormat.Bold);

        ueberschrift.Format.Font.Size = 14;

        ueberschrift.Format.SpaceBefore =
            Unit.FromCentimeter(0.4);

        ueberschrift.Format.SpaceAfter =
            Unit.FromCentimeter(0.2);

        if (dokumentation == null)
        {
            var leer =
                section.AddParagraph(
                    "Keine Dokumentation vorhanden.");

            leer.Format.Font.Size = 10;

            leer.Format.SpaceAfter =
                Unit.FromCentimeter(0.6);

            return;
        }

        var pflegekraft =
            await _context.Users
                .FirstOrDefaultAsync(
                    u => u.Id ==
                        dokumentation
                            .ErstelltVonBenutzerId);

        var pflegekraftName =
            pflegekraft == null
                ? dokumentation
                    .ErstelltVonBenutzerId
                : $"{pflegekraft.Vorname} " +
                  $"{pflegekraft.Nachname}";

        AddZeile(
            section,
            "Pflegefachkraft:",
            pflegekraftName);

        AddZeile(
            section,
            "Erstellt:",
            dokumentation.ErstelltAm
                .ToLocalTime()
                .ToString("dd.MM.yyyy HH:mm"));

        if (dokumentation.GeaendertAm != null)
        {
            AddZeile(
                section,
                "Zuletzt geändert:",
                dokumentation.GeaendertAm.Value
                    .ToLocalTime()
                    .ToString("dd.MM.yyyy HH:mm"));
        }

        var bericht =
            section.AddParagraph(
                dokumentation.Inhalt);

        bericht.Format.Font.Size = 11;

        bericht.Format.SpaceBefore =
            Unit.FromCentimeter(0.2);

        bericht.Format.SpaceAfter =
            Unit.FromCentimeter(0.7);
    }

    // --------------------------------------------------
    // PDF Hilfsmethode
    // --------------------------------------------------

    private static void AddZeile(
        Section section,
        string titel,
        string wert)
    {
        var paragraph =
            section.AddParagraph();

        paragraph.AddFormattedText(
            titel + " ",
            TextFormat.Bold);

        paragraph.AddText(wert);

        paragraph.Format.SpaceAfter =
            Unit.FromCentimeter(0.12);
    }

    // --------------------------------------------------
    // Schichtname
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

            _ => "Unbekannt"
        };
    }
}

// --------------------------------------------------
// Request: Erstellen
// --------------------------------------------------

public class PflegedokumentationErstellenRequest
{
    public Guid BewohnerId { get; set; }

    public DateOnly Datum { get; set; }

    public PflegeSchicht Schicht { get; set; }

    public string Inhalt { get; set; } =
        string.Empty;

    public bool MitSpracheErstellt { get; set; }
}

// --------------------------------------------------
// Request: Bearbeiten
// --------------------------------------------------

public class PflegedokumentationBearbeitenRequest
{
    public string Inhalt { get; set; } =
        string.Empty;

    public bool MitSpracheErstellt { get; set; }
}
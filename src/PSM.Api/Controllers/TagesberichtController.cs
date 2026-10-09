using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MigraDoc.DocumentObjectModel;
using MigraDoc.DocumentObjectModel.Tables;
using MigraDoc.Rendering;
using PSM.Domain.Enums;
using PSM.Infrastructure.Data;
using System.Security.Claims;

namespace PSM.Api.Controllers;

[ApiController]
[Route("api/tagesbericht")]
[Authorize]
public class TagesberichtController : ControllerBase
{
    private readonly AppDbContext _context;

    public TagesberichtController(AppDbContext context)
    {
        _context = context;
    }

    // =========================================================
    // EIN TAGESBERICHT
    // = 1 BEWOHNER + 1 DATUM + ALLE 3 SCHICHTEN
    // =========================================================

    [HttpGet("{bewohnerId:guid}/{datum}/pdf")]
    public async Task<IActionResult> PdfErstellen(
        Guid bewohnerId,
        DateOnly datum)
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

        // -------------------------------------------------
        // Standort-Zugriff prüfen
        // -------------------------------------------------

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

        // -------------------------------------------------
        // Alle Dokumentationen des Bewohners
        // für genau diesen Tag laden
        // -------------------------------------------------

        var dokumentationen = await _context.Pflegedokumentationen
            .Where(p =>
                p.BewohnerId == bewohnerId &&
                p.Datum == datum)
            .OrderBy(p => p.Schicht)
            .ToListAsync();

        // -------------------------------------------------
        // Pflegekräfte laden
        // -------------------------------------------------

        var benutzerIds = dokumentationen
            .Select(d => d.ErstelltVonBenutzerId)
            .Where(id => !string.IsNullOrWhiteSpace(id))
            .Distinct()
            .ToList();

        var benutzerListe = await _context.Users
            .Where(u => benutzerIds.Contains(u.Id))
            .ToListAsync();

        // -------------------------------------------------
        // PDF erzeugen
        // -------------------------------------------------

        var pdfBytes = ErstellePdf(
            bewohner,
            datum,
            dokumentationen,
            benutzerListe);

        var sichererNachname =
            DateinameBereinigen(bewohner.Nachname);

        var sichererVorname =
            DateinameBereinigen(bewohner.Vorname);

        var dateiname =
            $"Tagesbericht_{sichererNachname}_{sichererVorname}_{datum:yyyy-MM-dd}.pdf";

        return File(
            pdfBytes,
            "application/pdf",
            dateiname);
    }

    // =========================================================
    // PDF
    // =========================================================

    private static byte[] ErstellePdf(
        PSM.Domain.Entities.Bewohner bewohner,
        DateOnly datum,
        List<PSM.Domain.Entities.Pflegedokumentation> dokumentationen,
        List<PSM.Infrastructure.Identity.Benutzer> benutzerListe)
    {
        var document = new Document();

        document.Info.Title =
            $"Tagesbericht - {bewohner.Vorname} {bewohner.Nachname} - {datum:dd.MM.yyyy}";

        document.Info.Subject =
            "Pflege- und Betreuungsdokumentation";

        // =====================================================
        // STANDARD-SCHRIFT
        // =====================================================

        var normalStyle =
            document.Styles["Normal"];

        normalStyle.Font.Name = "Arial";
        normalStyle.Font.Size = 10;

        // =====================================================
        // SEITE
        // =====================================================

        var section = document.AddSection();

        section.PageSetup.PageFormat =
            PageFormat.A4;

        section.PageSetup.TopMargin = "1.5cm";
        section.PageSetup.BottomMargin = "1.5cm";
        section.PageSetup.LeftMargin = "1.6cm";
        section.PageSetup.RightMargin = "1.6cm";

        // =====================================================
        // TITEL
        // =====================================================

        var titel = section.AddParagraph();

        titel.AddText("Tagesbericht");

        titel.Format.Font.Size = 24;
        titel.Format.Font.Bold = true;
        titel.Format.Font.Color = Colors.DarkBlue;
        titel.Format.Alignment =
            ParagraphAlignment.Center;

        var untertitel = section.AddParagraph();

        untertitel.AddText(
            "Pflege- und Betreuungsdokumentation");

        untertitel.Format.Font.Size = 13;
        untertitel.Format.Font.Bold = true;
        untertitel.Format.Font.Color = Colors.DarkBlue;
        untertitel.Format.Alignment =
            ParagraphAlignment.Center;
        untertitel.Format.SpaceAfter = "0.7cm";

        // =====================================================
        // BEWOHNER-INFORMATIONEN
        // =====================================================

        var infoTabelle = section.AddTable();

        infoTabelle.Borders.Width = 0.5;
        infoTabelle.Borders.Color = Colors.LightGray;

        infoTabelle.AddColumn("3.2cm");
        infoTabelle.AddColumn("5.3cm");
        infoTabelle.AddColumn("3.2cm");
        infoTabelle.AddColumn("5.3cm");

        var zeile1 = infoTabelle.AddRow();
        InfoZelle(
            zeile1.Cells[0],
            "Name:",
            true);

        InfoZelle(
            zeile1.Cells[1],
            $"{bewohner.Vorname} {bewohner.Nachname}");

        InfoZelle(
            zeile1.Cells[2],
            "Zimmernummer:",
            true);

        InfoZelle(
            zeile1.Cells[3],
            bewohner.Zimmernummer ?? "-");

        var zeile2 = infoTabelle.AddRow();

        InfoZelle(
            zeile2.Cells[0],
            "Geburtsdatum:",
            true);

        InfoZelle(
            zeile2.Cells[1],
            bewohner.Geburtsdatum.ToString(
                "dd.MM.yyyy"));

        InfoZelle(
            zeile2.Cells[2],
            "Bewohnernummer:",
            true);

        InfoZelle(
            zeile2.Cells[3],
            bewohner.StandortBewohnerNummer.ToString());

        var zeile3 = infoTabelle.AddRow();

        InfoZelle(
            zeile3.Cells[0],
            "Standort:",
            true);

        InfoZelle(
            zeile3.Cells[1],
            bewohner.Standort?.Name ?? "-");

        InfoZelle(
            zeile3.Cells[2],
            "Datum:",
            true);

        InfoZelle(
            zeile3.Cells[3],
            datum.ToString("dd.MM.yyyy"));

        section.AddParagraph()
            .Format.SpaceAfter = "0.4cm";

        // =====================================================
        // ALLE 3 SCHICHTEN
        // =====================================================

        var schichten = new[]
        {
            PflegeSchicht.Fruehschicht,
            PflegeSchicht.Spaetdienst,
            PflegeSchicht.Nachtschicht
        };

        foreach (var schicht in schichten)
        {
            var dokumentation =
                dokumentationen.FirstOrDefault(
                    d => d.Schicht == schicht);

            SchichtHinzufuegen(
                section,
                schicht,
                dokumentation,
                benutzerListe);
        }

        // =====================================================
        // FOOTER
        // =====================================================

        var footer =
            section.Footers.Primary;

        var footerTabelle =
            footer.AddTable();

        footerTabelle.Borders.Top.Width = 0.5;
        footerTabelle.Borders.Top.Color =
            Colors.Gray;

        footerTabelle.AddColumn("9cm");
        footerTabelle.AddColumn("8cm");

        var footerZeile =
            footerTabelle.AddRow();

        var links =
            footerZeile.Cells[0].AddParagraph();

        links.AddText(
            "PSM – Pflege System Management");

        links.Format.Font.Size = 8;
        links.Format.Font.Color = Colors.Gray;

        var rechts =
            footerZeile.Cells[1].AddParagraph();

        rechts.AddText(
            $"Tagesbericht · {datum:dd.MM.yyyy}");

        rechts.Format.Alignment =
            ParagraphAlignment.Right;

        rechts.Format.Font.Size = 8;
        rechts.Format.Font.Color = Colors.Gray;

        // =====================================================
        // PDF RENDERING
        // =====================================================

        using var stream =
            new MemoryStream();

        var renderer =
            new PdfDocumentRenderer
            {
                Document = document
            };

        renderer.RenderDocument();

        renderer.PdfDocument.Save(
            stream,
            false);

        return stream.ToArray();
    }

    // =========================================================
    // EINE SCHICHT IM PDF
    // =========================================================

    private static void SchichtHinzufuegen(
        Section section,
        PflegeSchicht schicht,
        PSM.Domain.Entities.Pflegedokumentation?
            dokumentation,
        List<PSM.Infrastructure.Identity.Benutzer>
            benutzerListe)
    {
        var tabelle =
            section.AddTable();

        tabelle.Borders.Width = 0.6;
        tabelle.Borders.Color =
            Colors.LightGray;

        tabelle.AddColumn("9cm");
        tabelle.AddColumn("8cm");

        // -------------------------------------------------
        // Kopfzeile der Schicht
        // -------------------------------------------------

        var kopf =
            tabelle.AddRow();

        kopf.Shading.Color =
            FarbenFuerSchicht(schicht);

        kopf.Height = "1cm";
        kopf.VerticalAlignment =
            VerticalAlignment.Center;

        var titel =
            kopf.Cells[0].AddParagraph();

        titel.AddText(
            SchichtName(schicht));

        titel.Format.Font.Size = 14;
        titel.Format.Font.Bold = true;
        titel.Format.Font.Color =
            Colors.DarkBlue;

        // -------------------------------------------------
        // Pflegekraft + Datum + Uhrzeit
        // -------------------------------------------------

        if (dokumentation != null)
        {
            var pflegekraft =
                benutzerListe.FirstOrDefault(
                    u =>
                        u.Id ==
                        dokumentation
                            .ErstelltVonBenutzerId);

            var pflegekraftName =
                pflegekraft != null
                    ? $"{pflegekraft.Vorname} {pflegekraft.Nachname}"
                    : "Unbekannt";

            var zeit =
                DeutscheZeit(
                    dokumentation.ErstelltAm);

            var info =
                kopf.Cells[1].AddParagraph();

            info.AddFormattedText(
                $"Pflegekraft: {pflegekraftName}",
                TextFormat.Bold);

            info.AddLineBreak();

            info.AddText(
                $"{zeit:dd.MM.yyyy}, {zeit:HH:mm} Uhr");

            info.Format.Font.Size = 9;
            info.Format.Alignment =
                ParagraphAlignment.Right;
        }

        // -------------------------------------------------
        // Bericht
        // -------------------------------------------------

        var inhaltZeile =
            tabelle.AddRow();

        inhaltZeile.Cells[0].MergeRight = 1;

        var inhalt =
            inhaltZeile.Cells[0].AddParagraph();

        if (dokumentation == null)
        {
            inhalt.AddText(
                "Keine Dokumentation vorhanden.");

            inhalt.Format.Font.Italic = true;
            inhalt.Format.Font.Color =
                Colors.Gray;
        }
        else
        {
            inhalt.AddText(
                dokumentation.Inhalt);
        }

        inhalt.Format.Font.Size = 10;
        inhalt.Format.SpaceBefore = "0.15cm";
        inhalt.Format.SpaceAfter = "0.15cm";

        section.AddParagraph()
            .Format.SpaceAfter = "0.25cm";
    }

    // =========================================================
    // INFO-ZELLE
    // =========================================================

    private static void InfoZelle(
        Cell cell,
        string text,
        bool fett = false)
    {
        cell.VerticalAlignment =
            VerticalAlignment.Center;

        cell.Format.LeftIndent = "0.1cm";
        cell.Format.RightIndent = "0.1cm";

        var paragraph =
            cell.AddParagraph();

        paragraph.AddText(text);

        paragraph.Format.Font.Size = 9.5;

        if (fett)
        {
            paragraph.Format.Font.Bold = true;
            paragraph.Format.Font.Color =
                Colors.DarkBlue;
        }
    }

    // =========================================================
    // SCHICHT-NAME
    // =========================================================

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

    // =========================================================
    // SCHICHT-FARBE
    // =========================================================

    private static Color FarbenFuerSchicht(
        PflegeSchicht schicht)
    {
        return schicht switch
        {
            PflegeSchicht.Fruehschicht =>
                Colors.LightBlue,

            PflegeSchicht.Spaetdienst =>
                Colors.LightGreen,

            PflegeSchicht.Nachtschicht =>
                Colors.Lavender,

            _ =>
                Colors.LightGray
        };
    }

    // =========================================================
    // UTC -> DEUTSCHE LOKALZEIT
    // =========================================================

    private static DateTime DeutscheZeit(
        DateTime utcZeit)
    {
        try
        {
            TimeZoneInfo zeitzone;

            try
            {
                // Linux / Hetzner
                zeitzone =
                    TimeZoneInfo.FindSystemTimeZoneById(
                        "Europe/Berlin");
            }
            catch
            {
                // Windows
                zeitzone =
                    TimeZoneInfo.FindSystemTimeZoneById(
                        "W. Europe Standard Time");
            }

            var utc =
                utcZeit.Kind == DateTimeKind.Utc
                    ? utcZeit
                    : DateTime.SpecifyKind(
                        utcZeit,
                        DateTimeKind.Utc);

            return TimeZoneInfo.ConvertTimeFromUtc(
                utc,
                zeitzone);
        }
        catch
        {
            return utcZeit;
        }
    }

    // =========================================================
    // DATEINAME BEREINIGEN
    // =========================================================

    private static string DateinameBereinigen(
        string? wert)
    {
        if (string.IsNullOrWhiteSpace(wert))
        {
            return "Unbekannt";
        }

        var ungueltigeZeichen =
            Path.GetInvalidFileNameChars();

        var ergebnis =
            new string(
                wert
                    .Where(
                        c =>
                            !ungueltigeZeichen
                                .Contains(c))
                    .ToArray());

        return string.IsNullOrWhiteSpace(ergebnis)
            ? "Unbekannt"
            : ergebnis;
    }
}
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using PSM.Domain.Entities;
using PSM.Infrastructure.Identity;

namespace PSM.Infrastructure.Data;

public class AppDbContext : IdentityDbContext<Benutzer>
{
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    public DbSet<Bewohner> Bewohner => Set<Bewohner>();
    public DbSet<Standort> Standorte => Set<Standort>();
    public DbSet<Pflegedokumentation> Pflegedokumentationen => Set<Pflegedokumentation>();
    public DbSet<Ernaehrung> Ernaehrungen => Set<Ernaehrung>();
    public DbSet<Allergie> Allergien => Set<Allergie>();
    public DbSet<Lebensmittelbestellung> Lebensmittelbestellungen => Set<Lebensmittelbestellung>();
    public DbSet<Bestellposition> Bestellpositionen => Set<Bestellposition>();
    public DbSet<LebensmittelArtikel> LebensmittelArtikel => Set<LebensmittelArtikel>();
    public DbSet<Essensausgabe> Essensausgaben => Set<Essensausgabe>();
    public DbSet<BewohnerStandortHistorie> BewohnerStandortHistorien => Set<BewohnerStandortHistorie>();
    public DbSet<Benachrichtigung> Benachrichtigungen => Set<Benachrichtigung>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Standort>().HasIndex(s => s.Code).IsUnique();
        modelBuilder.Entity<Standort>().Property(s => s.Name).HasMaxLength(100);
        modelBuilder.Entity<Standort>().Property(s => s.Code).HasMaxLength(20);
        modelBuilder.Entity<Standort>().Property(s => s.Postleitzahl).HasMaxLength(10);
        modelBuilder.Entity<Standort>().Property(s => s.Email).HasMaxLength(200);
        modelBuilder.Entity<Standort>().Property(s => s.Telefonnummer).HasMaxLength(50);

        modelBuilder.Entity<Bewohner>()
            .HasOne(b => b.Standort)
            .WithMany()
            .HasForeignKey(b => b.StandortId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Bewohner>()
            .HasIndex(b => new { b.StandortId, b.StandortBewohnerNummer })
            .IsUnique();

        modelBuilder.Entity<Bewohner>()
            .HasIndex(b => new { b.Nachname, b.Vorname, b.Geburtsdatum });

        modelBuilder.Entity<Bewohner>().Property(b => b.Vorname).HasMaxLength(100);
        modelBuilder.Entity<Bewohner>().Property(b => b.Nachname).HasMaxLength(100);
        modelBuilder.Entity<Bewohner>().Property(b => b.Zimmernummer).HasMaxLength(30);

        modelBuilder.Entity<BewohnerStandortHistorie>()
            .HasOne(h => h.Bewohner)
            .WithMany()
            .HasForeignKey(h => h.BewohnerId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<BewohnerStandortHistorie>()
            .HasIndex(h => new { h.BewohnerId, h.GeaendertAm });

        modelBuilder.Entity<BewohnerStandortHistorie>().Property(h => h.AlteZimmernummer).HasMaxLength(30);
        modelBuilder.Entity<BewohnerStandortHistorie>().Property(h => h.NeueZimmernummer).HasMaxLength(30);

        modelBuilder.Entity<Pflegedokumentation>()
            .HasOne(p => p.Bewohner)
            .WithMany()
            .HasForeignKey(p => p.BewohnerId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Pflegedokumentation>()
            .HasIndex(p => new { p.BewohnerId, p.Datum, p.Schicht })
            .IsUnique();

        modelBuilder.Entity<Pflegedokumentation>()
            .HasIndex(p => new { p.BewohnerId, p.Datum });

        modelBuilder.Entity<Ernaehrung>()
            .HasOne(e => e.Bewohner)
            .WithOne()
            .HasForeignKey<Ernaehrung>(e => e.BewohnerId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Ernaehrung>()
            .Property(e => e.Krankheit)
            .HasMaxLength(500);

        modelBuilder.Entity<Allergie>()
            .HasOne(a => a.Bewohner)
            .WithMany()
            .HasForeignKey(a => a.BewohnerId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Allergie>().Property(a => a.Name).HasMaxLength(150);

        modelBuilder.Entity<Lebensmittelbestellung>()
            .HasOne(b => b.Standort)
            .WithMany()
            .HasForeignKey(b => b.StandortId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Lebensmittelbestellung>()
            .HasIndex(b => new { b.StandortId, b.Lieferdatum });

        modelBuilder.Entity<Lebensmittelbestellung>()
            .HasIndex(b => b.Bestellnummer)
            .IsUnique();

        modelBuilder.Entity<Lebensmittelbestellung>().Property(b => b.Status).HasMaxLength(50);
        modelBuilder.Entity<Lebensmittelbestellung>().Property(b => b.Bestellnummer).HasMaxLength(50);

        modelBuilder.Entity<Bestellposition>()
            .HasOne(p => p.Lebensmittelbestellung)
            .WithMany()
            .HasForeignKey(p => p.LebensmittelbestellungId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Bestellposition>().Property(p => p.Lebensmittelname).HasMaxLength(200);
        modelBuilder.Entity<Bestellposition>().Property(p => p.Einheit).HasMaxLength(50);

        modelBuilder.Entity<LebensmittelArtikel>()
            .Property(a => a.Name)
            .HasMaxLength(200);

        modelBuilder.Entity<LebensmittelArtikel>()
            .HasIndex(a => a.Name);

        modelBuilder.Entity<Essensausgabe>()
            .HasOne(e => e.Bewohner)
            .WithMany()
            .HasForeignKey(e => e.BewohnerId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Essensausgabe>()
            .HasIndex(e => new { e.BewohnerId, e.Datum, e.Mahlzeit })
            .IsUnique();

        modelBuilder.Entity<Essensausgabe>().Property(e => e.Mahlzeit).HasMaxLength(50);

        modelBuilder.Entity<Benachrichtigung>()
            .HasOne(n => n.Standort)
            .WithMany()
            .HasForeignKey(n => n.StandortId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Benachrichtigung>()
            .HasIndex(n => new { n.EmpfaengerRolle, n.Gelesen });

        modelBuilder.Entity<Benachrichtigung>().Property(n => n.Typ).HasMaxLength(50);
        modelBuilder.Entity<Benachrichtigung>().Property(n => n.EmpfaengerRolle).HasMaxLength(50);
        modelBuilder.Entity<Benachrichtigung>().Property(n => n.Nachricht).HasMaxLength(500);

        modelBuilder.Entity<Benutzer>()
            .HasOne(b => b.Standort)
            .WithMany()
            .HasForeignKey(b => b.StandortId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Benutzer>().Property(b => b.Vorname).HasMaxLength(100);
        modelBuilder.Entity<Benutzer>().Property(b => b.Nachname).HasMaxLength(100);
    }
}
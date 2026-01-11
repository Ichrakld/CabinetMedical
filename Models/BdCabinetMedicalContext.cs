using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace GestionCabinetMedical.Models;

public partial class BdCabinetMedicalContext : DbContext
{
    public BdCabinetMedicalContext()
    {
    }

    public BdCabinetMedicalContext(DbContextOptions<BdCabinetMedicalContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Admin> Admins { get; set; }

    public virtual DbSet<Consultation> Consultations { get; set; }

    public virtual DbSet<DossierMedical> DossierMedicals { get; set; }

    public virtual DbSet<Medecin> Medecins { get; set; }

    public virtual DbSet<Notification> Notifications { get; set; }

    public virtual DbSet<Patient> Patients { get; set; }

    public virtual DbSet<PersonnelMedical> PersonnelMedicals { get; set; }

    public virtual DbSet<RendezVou> RendezVous { get; set; }

    public virtual DbSet<RessourceMedicale> RessourceMedicales { get; set; }

    public virtual DbSet<Secretaire> Secretaires { get; set; }

    public virtual DbSet<Traitement> Traitements { get; set; }

    public virtual DbSet<Utilisateur> Utilisateurs { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
#warning To protect potentially sensitive information in your connection string, you should move it out of source code. You can avoid scaffolding the connection string by using the Name= syntax to read it from configuration - see https://go.microsoft.com/fwlink/?linkid=2131148. For more guidance on storing connection strings, see https://go.microsoft.com/fwlink/?LinkId=723263.
        => optionsBuilder.UseSqlServer("Server=DESKTOP-LG6MRRR;Database=BD_CabinetMedical;Trusted_Connection=True;TrustServerCertificate=True;");

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Admin>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Admin__3214EC0704EF404F");

            entity.ToTable("Admin");

            entity.Property(e => e.Id).ValueGeneratedNever();

            entity.HasOne(d => d.IdNavigation).WithOne(p => p.Admin)
                .HasForeignKey<Admin>(d => d.Id)
                .HasConstraintName("FK__Admin__Id__3B75D760");
        });

        modelBuilder.Entity<DossierMedical>(entity =>
        {
            entity.HasKey(e => e.NumDossier).HasName("PK__DossierM__4ED7359D35DBAEAC");

            entity.ToTable("DossierMedical");

            entity.Property(e => e.GroupeSanguin).HasMaxLength(10);
            entity.Property(e => e.Allergies).HasMaxLength(500);
            entity.Property(e => e.AntecedentsMedicaux);

            entity.HasOne(d => d.Medecin).WithMany(p => p.DossierMedicals)
                .HasForeignKey(d => d.MedecinId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__DossierMe__Medec__4D94879B");

            entity.HasOne(d => d.Patient).WithMany(p => p.DossierMedicals)
                .HasForeignKey(d => d.PatientId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__DossierMe__Patie__4CA06362");
        });

        modelBuilder.Entity<Consultation>(entity =>
        {
            entity.HasKey(e => e.NumDetail).HasName("PK__Consulta__D84B7AFE32A8846D");

            entity.ToTable("Consultation");

            entity.Property(e => e.DateConsultation)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.Diagnostic).HasMaxLength(500);
            entity.Property(e => e.Notes);

            entity.HasOne(d => d.DossierMedical).WithMany(p => p.Consultations)
                .HasForeignKey(d => d.DossierMedicalId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Consultat__Dossi__5165187F");
        });




        modelBuilder.Entity<Medecin>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Medecin__3214EC07F94A74B8");

            entity.ToTable("Medecin");

            entity.Property(e => e.Id).ValueGeneratedNever();
            entity.Property(e => e.Specialite).HasMaxLength(100);

            entity.HasOne(d => d.IdNavigation).WithOne(p => p.Medecin)
                .HasForeignKey<Medecin>(d => d.Id)
                .HasConstraintName("FK__Medecin__Id__3E52440B");
        });

        modelBuilder.Entity<Notification>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Notifica__3214EC079CE1CB28");

            entity.ToTable("Notification");

            entity.Property(e => e.DateCreation)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");

            entity.Property(e => e.Type).HasMaxLength(100);

            // ====== NOUVEAUX CHAMPS ======

            entity.Property(e => e.EstLue)
                .HasDefaultValue(false);

            entity.Property(e => e.UserId)
                .IsRequired();

            // ====== RELATIONS ======

            // Relation existante avec RendezVous
            entity.HasOne(d => d.RendezVous)
                .WithMany(p => p.Notifications)
                .HasForeignKey(d => d.RendezVousId)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("FK__Notificat__Rende__5CD6CB2B");

            // Nouvelle relation avec Utilisateur
            entity.HasOne(d => d.User)
                .WithMany(p => p.Notifications)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("FK_Notification_Utilisateur");
        });
        modelBuilder.Entity<Patient>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Patient__3214EC075743A3DC");

            entity.ToTable("Patient");

            entity.HasIndex(e => e.NumSecuriteSociale, "UQ__Patient__C8EA55E7FFB228AB").IsUnique();

            entity.Property(e => e.Id).ValueGeneratedNever();
            entity.Property(e => e.NumSecuriteSociale).HasMaxLength(50);


            entity.Property(e => e.DateNaissance)
                .HasColumnType("date");

            entity.HasOne(d => d.IdNavigation).WithOne(p => p.Patient)
                .HasForeignKey<Patient>(d => d.Id)
                .HasConstraintName("FK__Patient__Id__4222D4EF");
        });

        modelBuilder.Entity<PersonnelMedical>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Personne__3214EC079BFE4B2E");

            entity.ToTable("PersonnelMedical");

            entity.Property(e => e.Fonction).HasMaxLength(100);
            entity.Property(e => e.Nom).HasMaxLength(100);
        });

        modelBuilder.Entity<RendezVou>(entity =>
        {
            entity.HasKey(e => e.NumCom).HasName("PK__RendezVo__7DF505B37D375ABF");

            entity.Property(e => e.DateHeure).HasColumnType("datetime");
            entity.Property(e => e.Statut)
                .HasMaxLength(50)
                .HasDefaultValue("En attente");
            entity.Property(e => e.Motif)
                .HasMaxLength(500);

            entity.HasOne(d => d.Medecin).WithMany(p => p.RendezVous)
                .HasForeignKey(d => d.MedecinId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__RendezVou__Medec__48CFD27E");

            entity.HasOne(d => d.Patient).WithMany(p => p.RendezVous)
                .HasForeignKey(d => d.PatientId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__RendezVou__Patie__49C3F6B7");
        });

        modelBuilder.Entity<RessourceMedicale>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Ressourc__3214EC07AC3A39AC");

            entity.ToTable("RessourceMedicale");

            entity.Property(e => e.Nom).HasMaxLength(255);
        });

        modelBuilder.Entity<Secretaire>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Secretai__3214EC07401A3E4D");

            entity.ToTable("Secretaire");

            entity.Property(e => e.Id).ValueGeneratedNever();
            entity.Property(e => e.Service).HasMaxLength(100);

            entity.HasOne(d => d.IdNavigation).WithOne(p => p.Secretaire)
                .HasForeignKey<Secretaire>(d => d.Id)
                .HasConstraintName("FK__Secretaire__Id__44FF419A");
        });

        modelBuilder.Entity<Traitement>(entity =>
        {
            entity.HasKey(e => e.NumPro).HasName("PK__Traiteme__75833A615C2DFE7A");

            entity.ToTable("Traitement");

            entity.Property(e => e.TypeTraitement).HasMaxLength(1000);

            entity.HasOne(d => d.Consultation).WithMany(p => p.Traitements)
                .HasForeignKey(d => d.ConsultationId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Traitemen__Consu__5441852A");
        });

        modelBuilder.Entity<Utilisateur>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Utilisat__3214EC0726F305A9");

            entity.ToTable("Utilisateur");

            entity.HasIndex(e => e.Email, "UQ__Utilisat__A9D105349A9BE6AA").IsUnique();

            entity.Property(e => e.Email).HasMaxLength(255);
            entity.Property(e => e.EstActif).HasDefaultValue(true);
            entity.Property(e => e.MotDePasse).HasMaxLength(255);
            entity.Property(e => e.Nom).HasMaxLength(100);
            entity.Property(e => e.Prenom).HasMaxLength(100);
            entity.Property(e => e.Telephone).HasMaxLength(20);
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}

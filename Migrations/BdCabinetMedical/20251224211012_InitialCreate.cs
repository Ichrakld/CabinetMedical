using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GestionCabinetMedical.Migrations.BdCabinetMedical
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PersonnelMedical",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Nom = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Fonction = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__Personne__3214EC079BFE4B2E", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "RessourceMedicale",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Nom = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    Quantite = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__Ressourc__3214EC07AC3A39AC", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Utilisateur",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Nom = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Prenom = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Email = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    MotDePasse = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    Telephone = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    EstActif = table.Column<bool>(type: "bit", nullable: true, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__Utilisat__3214EC0726F305A9", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Admin",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false),
                    NiveauAcces = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__Admin__3214EC0704EF404F", x => x.Id);
                    table.ForeignKey(
                        name: "FK__Admin__Id__3B75D760",
                        column: x => x.Id,
                        principalTable: "Utilisateur",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Medecin",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false),
                    Specialite = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__Medecin__3214EC07F94A74B8", x => x.Id);
                    table.ForeignKey(
                        name: "FK__Medecin__Id__3E52440B",
                        column: x => x.Id,
                        principalTable: "Utilisateur",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Patient",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false),
                    NumSecuriteSociale = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__Patient__3214EC075743A3DC", x => x.Id);
                    table.ForeignKey(
                        name: "FK__Patient__Id__4222D4EF",
                        column: x => x.Id,
                        principalTable: "Utilisateur",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Secretaire",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false),
                    Service = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__Secretai__3214EC07401A3E4D", x => x.Id);
                    table.ForeignKey(
                        name: "FK__Secretaire__Id__44FF419A",
                        column: x => x.Id,
                        principalTable: "Utilisateur",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "DossierMedical",
                columns: table => new
                {
                    NumDossier = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    GroupeSanguin = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true),
                    PatientId = table.Column<int>(type: "int", nullable: false),
                    MedecinId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__DossierM__4ED7359D35DBAEAC", x => x.NumDossier);
                    table.ForeignKey(
                        name: "FK__DossierMe__Medec__4D94879B",
                        column: x => x.MedecinId,
                        principalTable: "Medecin",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK__DossierMe__Patie__4CA06362",
                        column: x => x.PatientId,
                        principalTable: "Patient",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "RendezVous",
                columns: table => new
                {
                    NumCom = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DateHeure = table.Column<DateTime>(type: "datetime", nullable: false),
                    Statut = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true, defaultValue: "En attente"),
                    MedecinId = table.Column<int>(type: "int", nullable: false),
                    PatientId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__RendezVo__7DF505B37D375ABF", x => x.NumCom);
                    table.ForeignKey(
                        name: "FK__RendezVou__Medec__48CFD27E",
                        column: x => x.MedecinId,
                        principalTable: "Medecin",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK__RendezVou__Patie__49C3F6B7",
                        column: x => x.PatientId,
                        principalTable: "Patient",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "Consultation",
                columns: table => new
                {
                    NumDetail = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Diagnostic = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DateConsultation = table.Column<DateTime>(type: "datetime", nullable: true, defaultValueSql: "(getdate())"),
                    DossierMedicalId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__Consulta__D84B7AFE32A8846D", x => x.NumDetail);
                    table.ForeignKey(
                        name: "FK__Consultat__Dossi__5165187F",
                        column: x => x.DossierMedicalId,
                        principalTable: "DossierMedical",
                        principalColumn: "NumDossier");
                });

            migrationBuilder.CreateTable(
                name: "Notification",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Type = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Message = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DateCreation = table.Column<DateTime>(type: "datetime", nullable: true, defaultValueSql: "(getdate())"),
                    RendezVousId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__Notifica__3214EC079CE1CB28", x => x.Id);
                    table.ForeignKey(
                        name: "FK__Notificat__Rende__5CD6CB2B",
                        column: x => x.RendezVousId,
                        principalTable: "RendezVous",
                        principalColumn: "NumCom");
                });

            migrationBuilder.CreateTable(
                name: "Traitement",
                columns: table => new
                {
                    NumPro = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TypeTraitement = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    ConsultationId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__Traiteme__75833A615C2DFE7A", x => x.NumPro);
                    table.ForeignKey(
                        name: "FK__Traitemen__Consu__5441852A",
                        column: x => x.ConsultationId,
                        principalTable: "Consultation",
                        principalColumn: "NumDetail");
                });

            migrationBuilder.CreateIndex(
                name: "IX_Consultation_DossierMedicalId",
                table: "Consultation",
                column: "DossierMedicalId");

            migrationBuilder.CreateIndex(
                name: "IX_DossierMedical_MedecinId",
                table: "DossierMedical",
                column: "MedecinId");

            migrationBuilder.CreateIndex(
                name: "IX_DossierMedical_PatientId",
                table: "DossierMedical",
                column: "PatientId");

            migrationBuilder.CreateIndex(
                name: "IX_Notification_RendezVousId",
                table: "Notification",
                column: "RendezVousId");

            migrationBuilder.CreateIndex(
                name: "UQ__Patient__C8EA55E7FFB228AB",
                table: "Patient",
                column: "NumSecuriteSociale",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RendezVous_MedecinId",
                table: "RendezVous",
                column: "MedecinId");

            migrationBuilder.CreateIndex(
                name: "IX_RendezVous_PatientId",
                table: "RendezVous",
                column: "PatientId");

            migrationBuilder.CreateIndex(
                name: "IX_Traitement_ConsultationId",
                table: "Traitement",
                column: "ConsultationId");

            migrationBuilder.CreateIndex(
                name: "UQ__Utilisat__A9D105349A9BE6AA",
                table: "Utilisateur",
                column: "Email",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Admin");

            migrationBuilder.DropTable(
                name: "Notification");

            migrationBuilder.DropTable(
                name: "PersonnelMedical");

            migrationBuilder.DropTable(
                name: "RessourceMedicale");

            migrationBuilder.DropTable(
                name: "Secretaire");

            migrationBuilder.DropTable(
                name: "Traitement");

            migrationBuilder.DropTable(
                name: "RendezVous");

            migrationBuilder.DropTable(
                name: "Consultation");

            migrationBuilder.DropTable(
                name: "DossierMedical");

            migrationBuilder.DropTable(
                name: "Medecin");

            migrationBuilder.DropTable(
                name: "Patient");

            migrationBuilder.DropTable(
                name: "Utilisateur");
        }
    }
}

using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GestionCabinetMedical.Migrations.BdCabinetMedical
{
    /// <inheritdoc />
    public partial class FixEstActifBool : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // On modifie SEULEMENT la colonne EstActif pour la rendre non-nullable (bool)
            // et on définit la valeur par défaut à 'true' (1)
            migrationBuilder.AlterColumn<bool>(
                name: "EstActif",
                table: "Utilisateur",
                type: "bit",
                nullable: false,
                defaultValue: true,
                oldClrType: typeof(bool),
                oldType: "bit",
                oldNullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Cette méthode permet d'annuler la migration si besoin (retour à nullable)
            migrationBuilder.AlterColumn<bool>(
                name: "EstActif",
                table: "Utilisateur",
                type: "bit",
                nullable: true,
                oldClrType: typeof(bool),
                oldType: "bit",
                oldNullable: false,
                oldDefaultValue: true);
        }
    }
}
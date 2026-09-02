using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SubastaYa.Infrastructure.Migrations
{
    public partial class AjusteBilleteraVersion : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Agregar columna temporal que almacenará GUIDs como CHAR(36)
            migrationBuilder.AddColumn<string>(
                name: "Version_New",
                table: "Billeteras",
                type: "char(36)",
                nullable: false,
                defaultValueSql: "UUID()");

            // Poblar la columna temporal con valores UUID para todas las filas
            migrationBuilder.Sql("UPDATE Billeteras SET Version_New = UUID();");

            // Eliminar la columna antigua y renombrar la nueva
            migrationBuilder.DropColumn(
                name: "Version",
                table: "Billeteras");

            migrationBuilder.RenameColumn(
                name: "Version_New",
                table: "Billeteras",
                newName: "Version");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Restaurar columna Version como timestamp rowversion
            migrationBuilder.AddColumn<DateTime>(
                name: "Version_Old",
                table: "Billeteras",
                type: "timestamp(6)",
                rowVersion: true,
                nullable: false,
                defaultValueSql: "CURRENT_TIMESTAMP");

            // (Opcional) no se pueden mapear los GUID anteriores a timestamp de forma fiable;
            // aquí simplemente dejamos la columna con CURRENT_TIMESTAMP para el downgrade.

            migrationBuilder.DropColumn(
                name: "Version",
                table: "Billeteras");

            migrationBuilder.RenameColumn(
                name: "Version_Old",
                table: "Billeteras",
                newName: "Version");
        }
    }
}

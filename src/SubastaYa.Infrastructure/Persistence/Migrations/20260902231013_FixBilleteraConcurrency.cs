using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SubastaYa.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class FixBilleteraConcurrency : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "Tipo",
                table: "TransaccionesLedger",
                type: "varchar(20)",
                maxLength: 20,
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int")
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AlterColumn<Guid>(
                name: "Version",
                table: "Billeteras",
                type: "char(36)",
                rowVersion: true,
                nullable: false,
                collation: "ascii_general_ci",
                oldClrType: typeof(DateTime),
                oldType: "timestamp(6)",
                oldRowVersion: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<int>(
                name: "Tipo",
                table: "TransaccionesLedger",
                type: "int",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(20)",
                oldMaxLength: 20)
                .OldAnnotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AlterColumn<DateTime>(
                name: "Version",
                table: "Billeteras",
                type: "timestamp(6)",
                rowVersion: true,
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "char(36)",
                oldRowVersion: true)
                .OldAnnotation("Relational:Collation", "ascii_general_ci");
        }
    }
}

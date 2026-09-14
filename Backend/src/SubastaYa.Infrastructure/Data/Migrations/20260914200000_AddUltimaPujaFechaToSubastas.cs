using System;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using SubastaYa.Infrastructure.Data;

#nullable disable

namespace SubastaYa.Infrastructure.Data.Migrations;

[DbContext(typeof(SubastaYaDbContext))]
[Migration("20260914200000_AddUltimaPujaFechaToSubastas")]
public partial class AddUltimaPujaFechaToSubastas : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<DateTime>(
            name: "UltimaPujaFecha",
            table: "Subastas",
            type: "datetime(6)",
            nullable: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "UltimaPujaFecha",
            table: "Subastas");
    }
}
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SubastaYa.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AgregarArreglosAuditoriaEIndices : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_Subastas_Estado_FechaFin",
                table: "Subastas",
                columns: new[] { "Estado", "FechaFin" });

            migrationBuilder.CreateIndex(
                name: "IX_AuditoriasLog_UsuarioId",
                table: "AuditoriasLog",
                column: "UsuarioId");

            migrationBuilder.AddForeignKey(
                name: "FK_AuditoriasLog_Usuarios_UsuarioId",
                table: "AuditoriasLog",
                column: "UsuarioId",
                principalTable: "Usuarios",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AuditoriasLog_Usuarios_UsuarioId",
                table: "AuditoriasLog");

            migrationBuilder.DropIndex(
                name: "IX_Subastas_Estado_FechaFin",
                table: "Subastas");

            migrationBuilder.DropIndex(
                name: "IX_AuditoriasLog_UsuarioId",
                table: "AuditoriasLog");
        }
    }
}

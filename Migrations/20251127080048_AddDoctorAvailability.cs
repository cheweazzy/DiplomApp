using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DiplomApp.Migrations
{
    /// <inheritdoc />
    public partial class AddDoctorAvailability : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DoctorAvailabilities",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    DoctorId = table.Column<string>(type: "TEXT", nullable: false),
                    Date = table.Column<DateTime>(type: "TEXT", nullable: false),
                    AvailableSlots = table.Column<string>(type: "TEXT", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DoctorAvailabilities", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DoctorAvailabilities_Date",
                table: "DoctorAvailabilities",
                column: "Date");

            migrationBuilder.CreateIndex(
                name: "IX_DoctorAvailabilities_DoctorId_Date",
                table: "DoctorAvailabilities",
                columns: new[] { "DoctorId", "Date" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DoctorAvailabilities");
        }
    }
}

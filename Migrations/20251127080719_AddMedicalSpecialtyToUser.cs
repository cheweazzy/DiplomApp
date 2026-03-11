using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DiplomApp.Migrations
{
    /// <inheritdoc />
    public partial class AddMedicalSpecialtyToUser : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "MedicalSpecialty",
                table: "AspNetUsers",
                type: "INTEGER",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "MedicalSpecialty",
                table: "AspNetUsers");
        }
    }
}

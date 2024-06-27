using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DottoressaIorio.Migrations
{
    public partial class PatientNotes : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Notes",
                table: "Patients",
                type: "TEXT",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Notes",
                table: "Patients");
        }
    }
}

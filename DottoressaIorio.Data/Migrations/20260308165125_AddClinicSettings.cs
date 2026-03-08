using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DottoressaIorio.Migrations
{
    /// <inheritdoc />
    public partial class AddClinicSettings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ClinicSettings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    DoctorName = table.Column<string>(type: "TEXT", nullable: false),
                    DoctorTitle = table.Column<string>(type: "TEXT", nullable: false),
                    Specialization = table.Column<string>(type: "TEXT", nullable: false),
                    ClinicName = table.Column<string>(type: "TEXT", nullable: false),
                    Address = table.Column<string>(type: "TEXT", nullable: false),
                    Phone = table.Column<string>(type: "TEXT", nullable: false),
                    Email = table.Column<string>(type: "TEXT", nullable: false),
                    FooterText = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ClinicSettings", x => x.Id);
                });

            migrationBuilder.InsertData(
                table: "ClinicSettings",
                columns: new[] { "Id", "Address", "ClinicName", "DoctorName", "DoctorTitle", "Email", "FooterText", "Phone", "Specialization" },
                values: new object[] { 1, "", "Studio Medico", "Dott.ssa Alessandra Iorio", "Medico Chirurgo", "alessandraiorio8@gmail.com", "", "320.1871136", "Specialista in Dermatologia e Venereologia" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ClinicSettings");
        }
    }
}

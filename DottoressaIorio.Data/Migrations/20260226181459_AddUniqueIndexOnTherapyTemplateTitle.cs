using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DottoressaIorio.Migrations
{
    /// <inheritdoc />
    public partial class AddUniqueIndexOnTherapyTemplateTitle : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_TherapyTemplates_Title",
                table: "TherapyTemplates",
                column: "Title",
                unique: true,
                filter: "\"Deleted\" = 0");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_TherapyTemplates_Title",
                table: "TherapyTemplates");
        }
    }
}

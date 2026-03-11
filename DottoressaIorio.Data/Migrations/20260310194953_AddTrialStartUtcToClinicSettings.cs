using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DottoressaIorio.Migrations
{
    /// <inheritdoc />
    public partial class AddTrialStartUtcToClinicSettings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "TrialStartUtc",
                table: "ClinicSettings",
                type: "TEXT",
                nullable: true);

            migrationBuilder.UpdateData(
                table: "ClinicSettings",
                keyColumn: "Id",
                keyValue: 1,
                column: "TrialStartUtc",
                value: null);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "TrialStartUtc",
                table: "ClinicSettings");
        }
    }
}

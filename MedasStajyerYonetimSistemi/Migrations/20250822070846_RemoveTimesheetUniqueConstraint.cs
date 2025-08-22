using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MedasStajyerYonetimSistemi.Migrations
{
    /// <inheritdoc />
    public partial class RemoveTimesheetUniqueConstraint : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Timesheets_InternId_PeriodDate",
                table: "Timesheets");

            migrationBuilder.CreateIndex(
                name: "IX_Timesheets_InternId",
                table: "Timesheets",
                column: "InternId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Timesheets_InternId",
                table: "Timesheets");

            migrationBuilder.CreateIndex(
                name: "IX_Timesheets_InternId_PeriodDate",
                table: "Timesheets",
                columns: new[] { "InternId", "PeriodDate" },
                unique: true);
        }
    }
}

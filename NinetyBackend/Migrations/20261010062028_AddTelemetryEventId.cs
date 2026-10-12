using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NinetyBackend.Migrations
{
    /// <inheritdoc />
    public partial class AddTelemetryEventId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "EventId",
                table: "Telemetries",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateIndex(
                name: "IX_Telemetries_EventId",
                table: "Telemetries",
                column: "EventId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Telemetries_EventId",
                table: "Telemetries");

            migrationBuilder.DropColumn(
                name: "EventId",
                table: "Telemetries");
        }
    }
}

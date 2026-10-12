using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NinetyBackend.Migrations
{
    /// <inheritdoc />
    public partial class UpdateAgentApis2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "ApprovedAt",
                table: "Agents",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "BootstrapSecretHash",
                table: "Agents",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "RejectedAt",
                table: "Agents",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "RevokedAt",
                table: "Agents",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Agents_StationId_Status",
                table: "Agents",
                columns: new[] { "StationId", "Status" });

            migrationBuilder.AddForeignKey(
                name: "FK_Agents_GamingStations_StationId",
                table: "Agents",
                column: "StationId",
                principalTable: "GamingStations",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Agents_GamingStations_StationId",
                table: "Agents");

            migrationBuilder.DropIndex(
                name: "IX_Agents_StationId_Status",
                table: "Agents");

            migrationBuilder.DropColumn(
                name: "ApprovedAt",
                table: "Agents");

            migrationBuilder.DropColumn(
                name: "BootstrapSecretHash",
                table: "Agents");

            migrationBuilder.DropColumn(
                name: "RejectedAt",
                table: "Agents");

            migrationBuilder.DropColumn(
                name: "RevokedAt",
                table: "Agents");
        }
    }
}

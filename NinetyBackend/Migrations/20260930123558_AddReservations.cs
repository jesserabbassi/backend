using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NinetyBackend.Migrations
{
    /// <inheritdoc />
    public partial class AddReservations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "BranchId",
                table: "GamingStations",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Reservations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CustomerId = table.Column<Guid>(type: "uuid", nullable: false),
                    BranchId = table.Column<Guid>(type: "uuid", nullable: false),
                    StationId = table.Column<Guid>(type: "uuid", nullable: false),
                    StartTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    EndTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Status = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Reservations", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_GamingStations_BranchId",
                table: "GamingStations",
                column: "BranchId");

            migrationBuilder.CreateIndex(
                name: "IX_Reservations_BranchId_StationId_StartTime_EndTime",
                table: "Reservations",
                columns: new[] { "BranchId", "StationId", "StartTime", "EndTime" });

            migrationBuilder.CreateIndex(
                name: "IX_Reservations_CustomerId_StartTime",
                table: "Reservations",
                columns: new[] { "CustomerId", "StartTime" });

            migrationBuilder.Sql(
                """
                CREATE EXTENSION IF NOT EXISTS btree_gist;
                ALTER TABLE "Reservations"
                ADD CONSTRAINT "EX_Reservations_Station_Time"
                EXCLUDE USING gist (
                    "BranchId" WITH =,
                    "StationId" WITH =,
                    tstzrange("StartTime", "EndTime", '[)') WITH &&
                )
                WHERE ("Status" IN ('PENDING', 'CONFIRMED', 'ACTIVE'));
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                "ALTER TABLE \"Reservations\" DROP CONSTRAINT \"EX_Reservations_Station_Time\";");

            migrationBuilder.DropTable(
                name: "Reservations");

            migrationBuilder.DropIndex(
                name: "IX_GamingStations_BranchId",
                table: "GamingStations");

            migrationBuilder.DropColumn(
                name: "BranchId",
                table: "GamingStations");
        }
    }
}

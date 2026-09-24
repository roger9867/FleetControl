using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FleetControlServer.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddTrip : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Trips",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TelemetryUnitId = table.Column<Guid>(type: "uuid", nullable: false),
                    StartTimestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    EndTimestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Trips", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Trips_TelemetryUnits_TelemetryUnitId",
                        column: x => x.TelemetryUnitId,
                        principalTable: "TelemetryUnits",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Trips_TelemetryUnitId",
                table: "Trips",
                column: "TelemetryUnitId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Trips");
        }
    }
}

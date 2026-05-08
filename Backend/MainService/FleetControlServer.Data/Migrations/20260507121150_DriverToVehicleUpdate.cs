using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FleetControlServer.Data.Migrations
{
    /// <inheritdoc />
    public partial class DriverToVehicleUpdate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_TelemetryUnits_VehicleDrivers_VehicleDriverId",
                table: "TelemetryUnits");

            migrationBuilder.DropIndex(
                name: "IX_TelemetryUnits_VehicleDriverId",
                table: "TelemetryUnits");

            migrationBuilder.DropColumn(
                name: "VehicleDriverId",
                table: "TelemetryUnits");

            migrationBuilder.AddColumn<Guid>(
                name: "VehicleDriverId",
                table: "Vehicles",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Vehicles_VehicleDriverId",
                table: "Vehicles",
                column: "VehicleDriverId");

            migrationBuilder.AddForeignKey(
                name: "FK_Vehicles_VehicleDrivers_VehicleDriverId",
                table: "Vehicles",
                column: "VehicleDriverId",
                principalTable: "VehicleDrivers",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Vehicles_VehicleDrivers_VehicleDriverId",
                table: "Vehicles");

            migrationBuilder.DropIndex(
                name: "IX_Vehicles_VehicleDriverId",
                table: "Vehicles");

            migrationBuilder.DropColumn(
                name: "VehicleDriverId",
                table: "Vehicles");

            migrationBuilder.AddColumn<Guid>(
                name: "VehicleDriverId",
                table: "TelemetryUnits",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_TelemetryUnits_VehicleDriverId",
                table: "TelemetryUnits",
                column: "VehicleDriverId");

            migrationBuilder.AddForeignKey(
                name: "FK_TelemetryUnits_VehicleDrivers_VehicleDriverId",
                table: "TelemetryUnits",
                column: "VehicleDriverId",
                principalTable: "VehicleDrivers",
                principalColumn: "Id");
        }
    }
}

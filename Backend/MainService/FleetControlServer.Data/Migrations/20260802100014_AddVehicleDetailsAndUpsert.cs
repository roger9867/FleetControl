using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FleetControlServer.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddVehicleDetailsAndUpsert : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_TelemetryUnits_Vehicles_VehicleId",
                table: "TelemetryUnits");

            migrationBuilder.DropForeignKey(
                name: "FK_Vehicles_VehicleDrivers_VehicleDriverId",
                table: "Vehicles");

            migrationBuilder.DropIndex(
                name: "IX_TelemetryUnits_VehicleId",
                table: "TelemetryUnits");

            migrationBuilder.AddColumn<string>(
                name: "Brand",
                table: "Vehicles",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Color",
                table: "Vehicles",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateOnly>(
                name: "FirstRegistration",
                table: "Vehicles",
                type: "date",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "PowerPs",
                table: "Vehicles",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "RequiredLicense",
                table: "Vehicles",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Year",
                table: "Vehicles",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_TelemetryUnits_VehicleId",
                table: "TelemetryUnits",
                column: "VehicleId",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_TelemetryUnits_Vehicles_VehicleId",
                table: "TelemetryUnits",
                column: "VehicleId",
                principalTable: "Vehicles",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_Vehicles_VehicleDrivers_VehicleDriverId",
                table: "Vehicles",
                column: "VehicleDriverId",
                principalTable: "VehicleDrivers",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_TelemetryUnits_Vehicles_VehicleId",
                table: "TelemetryUnits");

            migrationBuilder.DropForeignKey(
                name: "FK_Vehicles_VehicleDrivers_VehicleDriverId",
                table: "Vehicles");

            migrationBuilder.DropIndex(
                name: "IX_TelemetryUnits_VehicleId",
                table: "TelemetryUnits");

            migrationBuilder.DropColumn(
                name: "Brand",
                table: "Vehicles");

            migrationBuilder.DropColumn(
                name: "Color",
                table: "Vehicles");

            migrationBuilder.DropColumn(
                name: "FirstRegistration",
                table: "Vehicles");

            migrationBuilder.DropColumn(
                name: "PowerPs",
                table: "Vehicles");

            migrationBuilder.DropColumn(
                name: "RequiredLicense",
                table: "Vehicles");

            migrationBuilder.DropColumn(
                name: "Year",
                table: "Vehicles");

            migrationBuilder.CreateIndex(
                name: "IX_TelemetryUnits_VehicleId",
                table: "TelemetryUnits",
                column: "VehicleId");

            migrationBuilder.AddForeignKey(
                name: "FK_TelemetryUnits_Vehicles_VehicleId",
                table: "TelemetryUnits",
                column: "VehicleId",
                principalTable: "Vehicles",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Vehicles_VehicleDrivers_VehicleDriverId",
                table: "Vehicles",
                column: "VehicleDriverId",
                principalTable: "VehicleDrivers",
                principalColumn: "Id");
        }
    }
}

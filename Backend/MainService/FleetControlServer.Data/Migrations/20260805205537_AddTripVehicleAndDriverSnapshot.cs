using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FleetControlServer.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddTripVehicleAndDriverSnapshot : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "DriverId",
                table: "Trips",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "VehicleId",
                table: "Trips",
                type: "character varying(40)",
                maxLength: 40,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DriverId",
                table: "Trips");

            migrationBuilder.DropColumn(
                name: "VehicleId",
                table: "Trips");
        }
    }
}

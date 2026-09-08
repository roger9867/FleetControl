using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FleetControlServer.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddPersonAndDriversLicenses : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Email",
                table: "VehicleDrivers");

            migrationBuilder.DropColumn(
                name: "PasswordHash",
                table: "VehicleDrivers");

            migrationBuilder.CreateTable(
                name: "DriversLicenses",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    LicenseType = table.Column<int>(type: "integer", nullable: false),
                    ObtainedDate = table.Column<DateOnly>(type: "date", nullable: false),
                    VehicleDriverId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DriversLicenses", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DriversLicenses_VehicleDrivers_VehicleDriverId",
                        column: x => x.VehicleDriverId,
                        principalTable: "VehicleDrivers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DriversLicenses_VehicleDriverId",
                table: "DriversLicenses",
                column: "VehicleDriverId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DriversLicenses");

            migrationBuilder.AddColumn<string>(
                name: "Email",
                table: "VehicleDrivers",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "PasswordHash",
                table: "VehicleDrivers",
                type: "text",
                nullable: false,
                defaultValue: "");
        }
    }
}

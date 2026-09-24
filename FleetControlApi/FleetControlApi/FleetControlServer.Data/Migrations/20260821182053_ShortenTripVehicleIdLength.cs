using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FleetControlServer.Data.Migrations
{
    /// <inheritdoc />
    public partial class ShortenTripVehicleIdLength : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "VehicleId",
                table: "Trips",
                type: "character(17)",
                maxLength: 17,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(40)",
                oldMaxLength: 40,
                oldNullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "VehicleId",
                table: "Trips",
                type: "character varying(40)",
                maxLength: 40,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character(17)",
                oldMaxLength: 17,
                oldNullable: true);
        }
    }
}

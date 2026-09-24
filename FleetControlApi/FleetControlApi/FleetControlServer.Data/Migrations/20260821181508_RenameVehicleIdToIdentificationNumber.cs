using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FleetControlServer.Data.Migrations
{
    /// <inheritdoc />
    public partial class RenameVehicleIdToIdentificationNumber : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_TelemetryUnits_Vehicles_VehicleId",
                table: "TelemetryUnits");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Vehicles",
                table: "Vehicles");

            migrationBuilder.DropIndex(
                name: "IX_Vehicles_IdentificationNumber",
                table: "Vehicles");

            migrationBuilder.DropColumn(
                name: "Id",
                table: "Vehicles");

            migrationBuilder.RenameColumn(
                name: "IdentificationNumber",
                table: "Vehicles",
                newName: "VehicleIdentificationNumber");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Vehicles",
                table: "Vehicles",
                column: "VehicleIdentificationNumber");

            migrationBuilder.AddForeignKey(
                name: "FK_TelemetryUnits_Vehicles_VehicleId",
                table: "TelemetryUnits",
                column: "VehicleId",
                principalTable: "Vehicles",
                principalColumn: "VehicleIdentificationNumber",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_TelemetryUnits_Vehicles_VehicleId",
                table: "TelemetryUnits");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Vehicles",
                table: "Vehicles");

            migrationBuilder.RenameColumn(
                name: "VehicleIdentificationNumber",
                table: "Vehicles",
                newName: "IdentificationNumber");

            migrationBuilder.AddColumn<string>(
                name: "Id",
                table: "Vehicles",
                type: "character(17)",
                maxLength: 17,
                nullable: true);

            // Id and IdentificationNumber always held the same value before this migration —
            // backfill from the renamed column instead of leaving Id empty.
            migrationBuilder.Sql("""UPDATE "Vehicles" SET "Id" = "IdentificationNumber";""");

            migrationBuilder.AlterColumn<string>(
                name: "Id",
                table: "Vehicles",
                type: "character(17)",
                maxLength: 17,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character(17)",
                oldMaxLength: 17,
                oldNullable: true);

            migrationBuilder.AddPrimaryKey(
                name: "PK_Vehicles",
                table: "Vehicles",
                column: "Id");

            migrationBuilder.CreateIndex(
                name: "IX_Vehicles_IdentificationNumber",
                table: "Vehicles",
                column: "IdentificationNumber",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_TelemetryUnits_Vehicles_VehicleId",
                table: "TelemetryUnits",
                column: "VehicleId",
                principalTable: "Vehicles",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }
    }
}

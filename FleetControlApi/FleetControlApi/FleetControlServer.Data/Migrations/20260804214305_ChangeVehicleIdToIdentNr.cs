using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FleetControlServer.Data.Migrations
{
    /// <inheritdoc />
    // Hand-written: the EF-scaffolded version only re-typed Vehicles.Id/TelemetryUnits.VehicleId
    // from uuid to varchar, which would have kept the old random Guid as a string instead of
    // backfilling the new Id from IdentificationNumber. This version preserves the live FK
    // (TelemetryUnits.VehicleId) across the value swap.
    public partial class ChangeVehicleIdToIdentNr : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "IdentificationNumber",
                table: "Vehicles",
                type: "character varying(40)",
                maxLength: 40,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.Sql(
                "ALTER TABLE \"TelemetryUnits\" DROP CONSTRAINT \"FK_TelemetryUnits_Vehicles_VehicleId\";");

            // Repoint the FK column at the new key value (IdentificationNumber) while the old
            // uuid Vehicles.Id can still be joined against.
            migrationBuilder.AddColumn<string>(
                name: "VehicleIdentNr",
                table: "TelemetryUnits",
                type: "character varying(40)",
                maxLength: 40,
                nullable: true);

            migrationBuilder.Sql(
                "UPDATE \"TelemetryUnits\" tu " +
                "SET \"VehicleIdentNr\" = v.\"IdentificationNumber\" " +
                "FROM \"Vehicles\" v " +
                "WHERE tu.\"VehicleId\" = v.\"Id\";");

            migrationBuilder.DropColumn(name: "VehicleId", table: "TelemetryUnits");

            migrationBuilder.RenameColumn(
                name: "VehicleIdentNr",
                table: "TelemetryUnits",
                newName: "VehicleId");

            migrationBuilder.Sql("ALTER TABLE \"Vehicles\" DROP CONSTRAINT \"PK_Vehicles\";");

            migrationBuilder.Sql(
                "ALTER TABLE \"Vehicles\" ALTER COLUMN \"Id\" TYPE character varying(40) " +
                "USING \"IdentificationNumber\";");

            migrationBuilder.Sql(
                "ALTER TABLE \"Vehicles\" ADD CONSTRAINT \"PK_Vehicles\" PRIMARY KEY (\"Id\");");

            migrationBuilder.Sql(
                "ALTER TABLE \"TelemetryUnits\" ADD CONSTRAINT \"FK_TelemetryUnits_Vehicles_VehicleId\" " +
                "FOREIGN KEY (\"VehicleId\") REFERENCES \"Vehicles\" (\"Id\") ON DELETE SET NULL;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Not data-preserving: the original random Guid ids are gone, so this regenerates
            // fresh ones rather than attempting to recover the pre-migration values.
            migrationBuilder.Sql(
                "ALTER TABLE \"TelemetryUnits\" DROP CONSTRAINT \"FK_TelemetryUnits_Vehicles_VehicleId\";");

            migrationBuilder.Sql("ALTER TABLE \"Vehicles\" DROP CONSTRAINT \"PK_Vehicles\";");

            migrationBuilder.AddColumn<Guid>(
                name: "NewId",
                table: "Vehicles",
                type: "uuid",
                nullable: false,
                defaultValueSql: "gen_random_uuid()");

            migrationBuilder.AddColumn<Guid>(
                name: "NewVehicleId",
                table: "TelemetryUnits",
                type: "uuid",
                nullable: true);

            migrationBuilder.Sql(
                "UPDATE \"TelemetryUnits\" tu " +
                "SET \"NewVehicleId\" = v.\"NewId\" " +
                "FROM \"Vehicles\" v " +
                "WHERE tu.\"VehicleId\" = v.\"Id\";");

            migrationBuilder.DropColumn(name: "Id", table: "Vehicles");
            migrationBuilder.RenameColumn(name: "NewId", table: "Vehicles", newName: "Id");
            migrationBuilder.Sql("ALTER TABLE \"Vehicles\" ADD CONSTRAINT \"PK_Vehicles\" PRIMARY KEY (\"Id\");");

            migrationBuilder.DropColumn(name: "VehicleId", table: "TelemetryUnits");
            migrationBuilder.RenameColumn(name: "NewVehicleId", table: "TelemetryUnits", newName: "VehicleId");

            migrationBuilder.Sql(
                "ALTER TABLE \"TelemetryUnits\" ADD CONSTRAINT \"FK_TelemetryUnits_Vehicles_VehicleId\" " +
                "FOREIGN KEY (\"VehicleId\") REFERENCES \"Vehicles\" (\"Id\") ON DELETE SET NULL;");

            migrationBuilder.AlterColumn<string>(
                name: "IdentificationNumber",
                table: "Vehicles",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(40)",
                oldMaxLength: 40);
        }
    }
}

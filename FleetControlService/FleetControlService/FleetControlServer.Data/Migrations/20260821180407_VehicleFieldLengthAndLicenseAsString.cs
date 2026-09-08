using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FleetControlServer.Data.Migrations
{
    /// <inheritdoc />
    public partial class VehicleFieldLengthAndLicenseAsString : Migration
    {
        // Maps DriversLicenseType's numeric enum values to their member names —
        // must stay in sync with FleetControlServer.Domain.DriversLicenseType.
        // Mirrors the mapping used by the ConvertLicenseTypeToString migration.
        private const string CaseToString = """
            CASE "RequiredLicense"
                WHEN 0 THEN 'AM' WHEN 1 THEN 'A1' WHEN 2 THEN 'A2' WHEN 3 THEN 'A'
                WHEN 4 THEN 'B' WHEN 5 THEN 'B96' WHEN 6 THEN 'BE'
                WHEN 7 THEN 'C1' WHEN 8 THEN 'C1E' WHEN 9 THEN 'C' WHEN 10 THEN 'CE'
                WHEN 11 THEN 'D1' WHEN 12 THEN 'D1E' WHEN 13 THEN 'D' WHEN 14 THEN 'DE'
                WHEN 15 THEN 'L' WHEN 16 THEN 'T'
                ELSE NULL
            END
            """;

        private const string CaseToInt = """
            CASE "RequiredLicense"
                WHEN 'AM' THEN 0 WHEN 'A1' THEN 1 WHEN 'A2' THEN 2 WHEN 'A' THEN 3
                WHEN 'B' THEN 4 WHEN 'B96' THEN 5 WHEN 'BE' THEN 6
                WHEN 'C1' THEN 7 WHEN 'C1E' THEN 8 WHEN 'C' THEN 9 WHEN 'CE' THEN 10
                WHEN 'D1' THEN 11 WHEN 'D1E' THEN 12 WHEN 'D' THEN 13 WHEN 'DE' THEN 14
                WHEN 'L' THEN 15 WHEN 'T' THEN 16
                ELSE NULL
            END
            """;

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql($"""
                ALTER TABLE "Vehicles"
                ALTER COLUMN "RequiredLicense" TYPE text
                USING ({CaseToString});
                """);

            migrationBuilder.AlterColumn<string>(
                name: "ModelName",
                table: "Vehicles",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "LicensePlateNumber",
                table: "Vehicles",
                type: "character varying(9)",
                maxLength: 9,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "IdentificationNumber",
                table: "Vehicles",
                type: "character(17)",
                maxLength: 17,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(40)",
                oldMaxLength: 40);

            migrationBuilder.AlterColumn<string>(
                name: "Color",
                table: "Vehicles",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Brand",
                table: "Vehicles",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Id",
                table: "Vehicles",
                type: "character(17)",
                maxLength: 17,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(40)",
                oldMaxLength: 40);

            migrationBuilder.AlterColumn<string>(
                name: "LastName",
                table: "VehicleDrivers",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "FirstName",
                table: "VehicleDrivers",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "VehicleId",
                table: "TelemetryUnits",
                type: "character(17)",
                maxLength: 40,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(40)",
                oldMaxLength: 40,
                oldNullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql($"""
                ALTER TABLE "Vehicles"
                ALTER COLUMN "RequiredLicense" TYPE integer
                USING ({CaseToInt});
                """);

            migrationBuilder.AlterColumn<string>(
                name: "ModelName",
                table: "Vehicles",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(50)",
                oldMaxLength: 50);

            migrationBuilder.AlterColumn<string>(
                name: "LicensePlateNumber",
                table: "Vehicles",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(9)",
                oldMaxLength: 9,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "IdentificationNumber",
                table: "Vehicles",
                type: "character varying(40)",
                maxLength: 40,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character(17)",
                oldMaxLength: 17);

            migrationBuilder.AlterColumn<string>(
                name: "Color",
                table: "Vehicles",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(50)",
                oldMaxLength: 50,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Brand",
                table: "Vehicles",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(50)",
                oldMaxLength: 50,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Id",
                table: "Vehicles",
                type: "character varying(40)",
                maxLength: 40,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character(17)",
                oldMaxLength: 17);

            migrationBuilder.AlterColumn<string>(
                name: "LastName",
                table: "VehicleDrivers",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(50)",
                oldMaxLength: 50);

            migrationBuilder.AlterColumn<string>(
                name: "FirstName",
                table: "VehicleDrivers",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(50)",
                oldMaxLength: 50);

            migrationBuilder.AlterColumn<string>(
                name: "VehicleId",
                table: "TelemetryUnits",
                type: "character varying(40)",
                maxLength: 40,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character(17)",
                oldMaxLength: 40,
                oldNullable: true);
        }
    }
}

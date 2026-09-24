using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FleetControlServer.Data.Migrations
{
    /// <inheritdoc />
    public partial class ConvertLicenseTypeToString : Migration
    {
        // Maps DriversLicenseType's numeric enum values to their member names —
        // must stay in sync with FleetControlServer.Domain.DriversLicenseType.
        private const string CaseToString = """
            CASE "LicenseType"
                WHEN 0 THEN 'AM' WHEN 1 THEN 'A1' WHEN 2 THEN 'A2' WHEN 3 THEN 'A'
                WHEN 4 THEN 'B' WHEN 5 THEN 'B96' WHEN 6 THEN 'BE'
                WHEN 7 THEN 'C1' WHEN 8 THEN 'C1E' WHEN 9 THEN 'C' WHEN 10 THEN 'CE'
                WHEN 11 THEN 'D1' WHEN 12 THEN 'D1E' WHEN 13 THEN 'D' WHEN 14 THEN 'DE'
                WHEN 15 THEN 'L' WHEN 16 THEN 'T'
            END
            """;

        private const string CaseToInt = """
            CASE "LicenseType"
                WHEN 'AM' THEN 0 WHEN 'A1' THEN 1 WHEN 'A2' THEN 2 WHEN 'A' THEN 3
                WHEN 'B' THEN 4 WHEN 'B96' THEN 5 WHEN 'BE' THEN 6
                WHEN 'C1' THEN 7 WHEN 'C1E' THEN 8 WHEN 'C' THEN 9 WHEN 'CE' THEN 10
                WHEN 'D1' THEN 11 WHEN 'D1E' THEN 12 WHEN 'D' THEN 13 WHEN 'DE' THEN 14
                WHEN 'L' THEN 15 WHEN 'T' THEN 16
            END
            """;

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql($"""
                ALTER TABLE "DriversLicenses"
                ALTER COLUMN "LicenseType" TYPE text
                USING ({CaseToString});
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql($"""
                ALTER TABLE "DriversLicenses"
                ALTER COLUMN "LicenseType" TYPE integer
                USING ({CaseToInt});
                """);
        }
    }
}

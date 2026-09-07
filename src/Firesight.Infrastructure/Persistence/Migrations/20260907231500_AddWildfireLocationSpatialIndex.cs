using Firesight.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Firesight.Infrastructure.Persistence.Migrations;

[DbContext(typeof(FiresightDbContext))]
[Migration("20260907231500_AddWildfireLocationSpatialIndex")]
public partial class AddWildfireLocationSpatialIndex : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateIndex(
            name: "IX_Wildfires_Location",
            table: "Wildfires",
            column: "Location")
            .Annotation("Npgsql:IndexMethod", "gist");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(
            name: "IX_Wildfires_Location",
            table: "Wildfires");
    }
}

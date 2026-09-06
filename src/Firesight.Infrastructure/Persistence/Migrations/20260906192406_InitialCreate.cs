using System;
using Microsoft.EntityFrameworkCore.Migrations;
using NetTopologySuite.Geometries;

#nullable disable

namespace Firesight.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterDatabase()
                .Annotation("Npgsql:PostgresExtension:postgis", ",,");

            migrationBuilder.CreateTable(
                name: "CwfisSyncState",
                columns: table => new
                {
                    Source = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    LastAttemptUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    LastSuccessfulFetchUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastAttemptSucceeded = table.Column<bool>(type: "boolean", nullable: false),
                    Received = table.Column<int>(type: "integer", nullable: false),
                    Accepted = table.Column<int>(type: "integer", nullable: false),
                    Rejected = table.Column<int>(type: "integer", nullable: false),
                    LastError = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CwfisSyncState", x => x.Source);
                });

            migrationBuilder.CreateTable(
                name: "Wildfires",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ExternalId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Agency = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    Location = table.Column<Point>(type: "geography (point)", nullable: false),
                    StartDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    AreaHectares = table.Column<double>(type: "double precision", nullable: true),
                    Status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    StatusDateUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastSeenInFeedUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    FirstObservedExtinguishedUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Wildfires", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Wildfires_ExternalId",
                table: "Wildfires",
                column: "ExternalId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CwfisSyncState");

            migrationBuilder.DropTable(
                name: "Wildfires");
        }
    }
}

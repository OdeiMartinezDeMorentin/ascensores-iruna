using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AscensoresIruna.Api.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Elevators",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    Location = table.Column<string>(type: "TEXT", nullable: false),
                    Latitude = table.Column<double>(type: "REAL", nullable: false),
                    Longitude = table.Column<double>(type: "REAL", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Elevators", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ReporterIps",
                columns: table => new
                {
                    IpAddressHash = table.Column<string>(type: "TEXT", nullable: false),
                    TrustScore = table.Column<double>(type: "REAL", nullable: false),
                    Confirmations = table.Column<int>(type: "INTEGER", nullable: false),
                    Contradictions = table.Column<int>(type: "INTEGER", nullable: false),
                    LastSeenAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReporterIps", x => x.IpAddressHash);
                });

            migrationBuilder.CreateTable(
                name: "StatusReports",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ElevatorId = table.Column<int>(type: "INTEGER", nullable: false),
                    Status = table.Column<int>(type: "INTEGER", nullable: false),
                    ReportedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    IpAddressHash = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StatusReports", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StatusReports_Elevators_ElevatorId",
                        column: x => x.ElevatorId,
                        principalTable: "Elevators",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_StatusReports_ElevatorId_ReportedAt",
                table: "StatusReports",
                columns: new[] { "ElevatorId", "ReportedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_StatusReports_IpAddressHash_ElevatorId_ReportedAt",
                table: "StatusReports",
                columns: new[] { "IpAddressHash", "ElevatorId", "ReportedAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ReporterIps");

            migrationBuilder.DropTable(
                name: "StatusReports");

            migrationBuilder.DropTable(
                name: "Elevators");
        }
    }
}

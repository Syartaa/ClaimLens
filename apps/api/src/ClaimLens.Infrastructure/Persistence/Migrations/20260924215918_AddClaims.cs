using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ClaimLens.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddClaims : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ai_runs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    InvestigationId = table.Column<Guid>(type: "uuid", nullable: false),
                    ClaimId = table.Column<Guid>(type: "uuid", nullable: true),
                    Purpose = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    PromptName = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    PromptVersion = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    ModelUsed = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    ErrorCategory = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    LatencyMs = table.Column<int>(type: "integer", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ai_runs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "claims",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    InvestigationId = table.Column<Guid>(type: "uuid", nullable: false),
                    Position = table.Column<int>(type: "integer", nullable: false),
                    ClaimText = table.Column<string>(type: "text", nullable: false),
                    NormalizedClaim = table.Column<string>(type: "text", nullable: false),
                    ClaimType = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    ImportanceScore = table.Column<decimal>(type: "numeric(5,4)", precision: 5, scale: 4, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_claims", x => x.Id);
                    table.ForeignKey(
                        name: "FK_claims_investigations_InvestigationId",
                        column: x => x.InvestigationId,
                        principalTable: "investigations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ai_runs_InvestigationId_CreatedAt",
                table: "ai_runs",
                columns: new[] { "InvestigationId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_claims_InvestigationId_Position",
                table: "claims",
                columns: new[] { "InvestigationId", "Position" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ai_runs");

            migrationBuilder.DropTable(
                name: "claims");
        }
    }
}

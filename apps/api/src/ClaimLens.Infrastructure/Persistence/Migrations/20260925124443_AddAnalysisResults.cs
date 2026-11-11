using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ClaimLens.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddAnalysisResults : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "analysis_results",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ClaimId = table.Column<Guid>(type: "uuid", nullable: false),
                    Verdict = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    Confidence = table.Column<decimal>(type: "numeric(5,4)", precision: 5, scale: 4, nullable: false),
                    Explanation = table.Column<string>(type: "text", nullable: false),
                    AlgorithmVersion = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    GeneratedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_analysis_results", x => x.Id);
                    table.ForeignKey(
                        name: "FK_analysis_results_claims_ClaimId",
                        column: x => x.ClaimId,
                        principalTable: "claims",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_analysis_results_ClaimId_GeneratedAt",
                table: "analysis_results",
                columns: new[] { "ClaimId", "GeneratedAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "analysis_results");
        }
    }
}

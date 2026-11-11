using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ClaimLens.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddEvidence : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "sources",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Url = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    Domain = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    Title = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Author = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    Publisher = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    PublishedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    RetrievedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_sources", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "evidence",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ClaimId = table.Column<Guid>(type: "uuid", nullable: false),
                    SourceId = table.Column<Guid>(type: "uuid", nullable: false),
                    EvidenceText = table.Column<string>(type: "text", nullable: false),
                    AiStance = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    Stance = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    RelevanceScore = table.Column<decimal>(type: "numeric(5,4)", precision: 5, scale: 4, nullable: true),
                    ConfidenceScore = table.Column<decimal>(type: "numeric(5,4)", precision: 5, scale: 4, nullable: true),
                    Explanation = table.Column<string>(type: "text", nullable: true),
                    PromptVersion = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    ModelUsed = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_evidence", x => x.Id);
                    table.ForeignKey(
                        name: "FK_evidence_claims_ClaimId",
                        column: x => x.ClaimId,
                        principalTable: "claims",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_evidence_sources_SourceId",
                        column: x => x.SourceId,
                        principalTable: "sources",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "source_qualities",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SourceId = table.Column<Guid>(type: "uuid", nullable: false),
                    Band = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    FactorsJson = table.Column<string>(type: "jsonb", nullable: false),
                    Explanation = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_source_qualities", x => x.Id);
                    table.ForeignKey(
                        name: "FK_source_qualities_sources_SourceId",
                        column: x => x.SourceId,
                        principalTable: "sources",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_evidence_ClaimId_Stance",
                table: "evidence",
                columns: new[] { "ClaimId", "Stance" });

            migrationBuilder.CreateIndex(
                name: "IX_evidence_SourceId",
                table: "evidence",
                column: "SourceId");

            migrationBuilder.CreateIndex(
                name: "IX_source_qualities_SourceId",
                table: "source_qualities",
                column: "SourceId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_sources_Url",
                table: "sources",
                column: "Url",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "evidence");

            migrationBuilder.DropTable(
                name: "source_qualities");

            migrationBuilder.DropTable(
                name: "sources");
        }
    }
}

using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ClaimLens.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddInvestigations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "investigations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    OriginalInput = table.Column<string>(type: "text", nullable: false),
                    InputType = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    ProcessingStatus = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    Verdict = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    ConfidenceScore = table.Column<decimal>(type: "numeric(5,4)", precision: 5, scale: 4, nullable: true),
                    Summary = table.Column<string>(type: "text", nullable: true),
                    FailureCode = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    EvidenceProvider = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    AttemptCount = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CompletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_investigations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_investigations_users_UserId",
                        column: x => x.UserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_investigations_ProcessingStatus_CreatedAt",
                table: "investigations",
                columns: new[] { "ProcessingStatus", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_investigations_UserId_CreatedAt",
                table: "investigations",
                columns: new[] { "UserId", "CreatedAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "investigations");
        }
    }
}

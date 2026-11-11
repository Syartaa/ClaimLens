using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ClaimLens.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddReviews : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "FlagNote",
                table: "sources",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "FlaggedAt",
                table: "sources",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "FlaggedByUserId",
                table: "sources",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "ApprovedAt",
                table: "investigations",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ApprovedByUserId",
                table: "investigations",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "audit_events",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    InvestigationId = table.Column<Guid>(type: "uuid", nullable: false),
                    ActorUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    EventType = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Message = table.Column<string>(type: "text", nullable: false),
                    Metadata = table.Column<string>(type: "jsonb", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_audit_events", x => x.Id);
                    table.ForeignKey(
                        name: "FK_audit_events_investigations_InvestigationId",
                        column: x => x.InvestigationId,
                        principalTable: "investigations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_audit_events_users_ActorUserId",
                        column: x => x.ActorUserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "evidence_reviews",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EvidenceId = table.Column<Guid>(type: "uuid", nullable: false),
                    ReviewerId = table.Column<Guid>(type: "uuid", nullable: false),
                    PreviousStance = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    NewStance = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    Note = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_evidence_reviews", x => x.Id);
                    table.ForeignKey(
                        name: "FK_evidence_reviews_evidence_EvidenceId",
                        column: x => x.EvidenceId,
                        principalTable: "evidence",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_evidence_reviews_users_ReviewerId",
                        column: x => x.ReviewerId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_sources_FlaggedByUserId",
                table: "sources",
                column: "FlaggedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_investigations_ApprovedByUserId",
                table: "investigations",
                column: "ApprovedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_audit_events_ActorUserId",
                table: "audit_events",
                column: "ActorUserId");

            migrationBuilder.CreateIndex(
                name: "IX_audit_events_InvestigationId_CreatedAt",
                table: "audit_events",
                columns: new[] { "InvestigationId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_evidence_reviews_EvidenceId",
                table: "evidence_reviews",
                column: "EvidenceId");

            migrationBuilder.CreateIndex(
                name: "IX_evidence_reviews_ReviewerId",
                table: "evidence_reviews",
                column: "ReviewerId");

            migrationBuilder.AddForeignKey(
                name: "FK_investigations_users_ApprovedByUserId",
                table: "investigations",
                column: "ApprovedByUserId",
                principalTable: "users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_sources_users_FlaggedByUserId",
                table: "sources",
                column: "FlaggedByUserId",
                principalTable: "users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_investigations_users_ApprovedByUserId",
                table: "investigations");

            migrationBuilder.DropForeignKey(
                name: "FK_sources_users_FlaggedByUserId",
                table: "sources");

            migrationBuilder.DropTable(
                name: "audit_events");

            migrationBuilder.DropTable(
                name: "evidence_reviews");

            migrationBuilder.DropIndex(
                name: "IX_sources_FlaggedByUserId",
                table: "sources");

            migrationBuilder.DropIndex(
                name: "IX_investigations_ApprovedByUserId",
                table: "investigations");

            migrationBuilder.DropColumn(
                name: "FlagNote",
                table: "sources");

            migrationBuilder.DropColumn(
                name: "FlaggedAt",
                table: "sources");

            migrationBuilder.DropColumn(
                name: "FlaggedByUserId",
                table: "sources");

            migrationBuilder.DropColumn(
                name: "ApprovedAt",
                table: "investigations");

            migrationBuilder.DropColumn(
                name: "ApprovedByUserId",
                table: "investigations");
        }
    }
}

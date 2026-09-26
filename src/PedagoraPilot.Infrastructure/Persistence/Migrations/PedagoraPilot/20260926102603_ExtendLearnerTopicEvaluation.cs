using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PedagoraPilot.Infrastructure.Persistence.Migrations.PedagoraPilot
{
    /// <inheritdoc />
    public partial class ExtendLearnerTopicEvaluation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Improvements",
                schema: "learning",
                table: "learner_topic_progress",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "NextObjective",
                schema: "learning",
                table: "learner_topic_progress",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PositivePoints",
                schema: "learning",
                table: "learner_topic_progress",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "learner_topic_evaluation_criteria",
                schema: "learning",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    LearnerTopicProgressId = table.Column<Guid>(type: "uuid", nullable: false),
                    Code = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    Level = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_learner_topic_evaluation_criteria", x => x.Id);
                    table.ForeignKey(
                        name: "FK_learner_topic_evaluation_criteria_learner_topic_progress_Le~",
                        column: x => x.LearnerTopicProgressId,
                        principalSchema: "learning",
                        principalTable: "learner_topic_progress",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_learner_topic_evaluation_criteria_LearnerTopicProgressId_Co~",
                schema: "learning",
                table: "learner_topic_evaluation_criteria",
                columns: new[] { "LearnerTopicProgressId", "Code" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "learner_topic_evaluation_criteria",
                schema: "learning");

            migrationBuilder.DropColumn(
                name: "Improvements",
                schema: "learning",
                table: "learner_topic_progress");

            migrationBuilder.DropColumn(
                name: "NextObjective",
                schema: "learning",
                table: "learner_topic_progress");

            migrationBuilder.DropColumn(
                name: "PositivePoints",
                schema: "learning",
                table: "learner_topic_progress");
        }
    }
}

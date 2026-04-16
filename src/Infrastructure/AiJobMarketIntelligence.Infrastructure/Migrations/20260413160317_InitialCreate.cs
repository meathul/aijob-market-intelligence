using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AiJobMarketIntelligence.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "JobsRaw",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Title = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    Company = table.Column<string>(type: "TEXT", maxLength: 300, nullable: false),
                    Location = table.Column<string>(type: "TEXT", maxLength: 300, nullable: false),
                    Description = table.Column<string>(type: "TEXT", nullable: false),
                    SalaryRaw = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    Source = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Url = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: false),
                    PostedDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    IsProcessed = table.Column<bool>(type: "INTEGER", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_JobsRaw", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Skills",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Skills", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "JobsProcessed",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    JobRawId = table.Column<int>(type: "INTEGER", nullable: false),
                    SalaryMin = table.Column<int>(type: "INTEGER", nullable: true),
                    SalaryMax = table.Column<int>(type: "INTEGER", nullable: true),
                    Currency = table.Column<string>(type: "TEXT", maxLength: 3, nullable: true),
                    ExperienceLevel = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_JobsProcessed", x => x.Id);
                    table.ForeignKey(
                        name: "FK_JobsProcessed_JobsRaw_JobRawId",
                        column: x => x.JobRawId,
                        principalTable: "JobsRaw",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "JobSkills",
                columns: table => new
                {
                    JobRawId = table.Column<int>(type: "INTEGER", nullable: false),
                    SkillId = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_JobSkills", x => new { x.JobRawId, x.SkillId });
                    table.CheckConstraint("CK_JobSkill_Ids", "[JobRawId] > 0 AND [SkillId] > 0");
                    table.ForeignKey(
                        name: "FK_JobSkills_JobsRaw_JobRawId",
                        column: x => x.JobRawId,
                        principalTable: "JobsRaw",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_JobSkills_Skills_SkillId",
                        column: x => x.SkillId,
                        principalTable: "Skills",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_JobSkills_SkillId",
                table: "JobSkills",
                column: "SkillId");

            migrationBuilder.CreateIndex(
                name: "IX_JobsProcessed_JobRawId",
                table: "JobsProcessed",
                column: "JobRawId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_JobsRaw_Url_Unique",
                table: "JobsRaw",
                column: "Url",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Skills_Name_Unique",
                table: "Skills",
                column: "Name",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "JobSkills");

            migrationBuilder.DropTable(
                name: "JobsProcessed");

            migrationBuilder.DropTable(
                name: "Skills");

            migrationBuilder.DropTable(
                name: "JobsRaw");
        }
    }
}

using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AiJobMarketIntelligence.Infrastructure.Migrations.JobsProcessed
{
    /// <inheritdoc />
    public partial class AddOnboardingCompleted : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "OnboardingCompleted",
                table: "UserJobPreferences",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "OnboardingCompleted",
                table: "UserJobPreferences");
        }
    }
}

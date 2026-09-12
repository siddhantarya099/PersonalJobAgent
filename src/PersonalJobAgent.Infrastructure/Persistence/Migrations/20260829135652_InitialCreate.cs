using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PersonalJobAgent.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "applications",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CandidateId = table.Column<Guid>(type: "uuid", nullable: false),
                    JobId = table.Column<Guid>(type: "uuid", nullable: false),
                    Status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    AppliedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ResumeVersionId = table.Column<Guid>(type: "uuid", nullable: true),
                    CoverLetterVersionId = table.Column<Guid>(type: "uuid", nullable: true),
                    ApplicationUrl = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    LastStatusChangeUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    NextAction = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    NextActionDateUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_applications", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "candidates",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    CurrentRole = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    CurrentCompany = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    TotalExperienceYears = table.Column<decimal>(type: "numeric(5,2)", nullable: false),
                    CurrentLocation = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    MinimumSalaryLpa = table.Column<decimal>(type: "numeric(10,2)", nullable: false),
                    TargetSalaryLpa = table.Column<decimal>(type: "numeric(10,2)", nullable: false),
                    NoticePeriodMonths = table.Column<int>(type: "integer", nullable: false),
                    IsActivelyLooking = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_candidates", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "job_matches",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    JobId = table.Column<Guid>(type: "uuid", nullable: false),
                    CandidateId = table.Column<Guid>(type: "uuid", nullable: false),
                    OverallScore = table.Column<decimal>(type: "numeric(5,2)", nullable: false),
                    TechnicalScore = table.Column<decimal>(type: "numeric(5,2)", nullable: false),
                    ExperienceScore = table.Column<decimal>(type: "numeric(5,2)", nullable: false),
                    SalaryScore = table.Column<decimal>(type: "numeric(5,2)", nullable: false),
                    CompanyScore = table.Column<decimal>(type: "numeric(5,2)", nullable: false),
                    LocationScore = table.Column<decimal>(type: "numeric(5,2)", nullable: false),
                    Recommendation = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Reasoning = table.Column<string>(type: "text", nullable: false),
                    MatchedSkillsJson = table.Column<string>(type: "jsonb", nullable: false),
                    MissingSkillsJson = table.Column<string>(type: "jsonb", nullable: false),
                    MandatoryGapsJson = table.Column<string>(type: "jsonb", nullable: false),
                    ResumeStrategyJson = table.Column<string>(type: "jsonb", nullable: false),
                    ModelVersion = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_job_matches", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "jobs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ExternalId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Source = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Company = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "text", nullable: false),
                    Location = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    SalaryMinLpa = table.Column<decimal>(type: "numeric(10,2)", nullable: true),
                    SalaryMaxLpa = table.Column<decimal>(type: "numeric(10,2)", nullable: true),
                    JobUrl = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    PostedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ContentHash = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_jobs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "application_events",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ApplicationId = table.Column<Guid>(type: "uuid", nullable: false),
                    OldStatus = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    NewStatus = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Source = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    OccurredAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_application_events", x => x.Id);
                    table.ForeignKey(
                        name: "FK_application_events_applications_ApplicationId",
                        column: x => x.ApplicationId,
                        principalTable: "applications",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "candidate_skills",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CandidateId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Category = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    YearsOfExperience = table.Column<decimal>(type: "numeric(5,2)", nullable: false),
                    Proficiency = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Evidence = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_candidate_skills", x => x.Id);
                    table.ForeignKey(
                        name: "FK_candidate_skills_candidates_CandidateId",
                        column: x => x.CandidateId,
                        principalTable: "candidates",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_application_events_ApplicationId",
                table: "application_events",
                column: "ApplicationId");

            migrationBuilder.CreateIndex(
                name: "IX_applications_CandidateId",
                table: "applications",
                column: "CandidateId");

            migrationBuilder.CreateIndex(
                name: "IX_applications_JobId",
                table: "applications",
                column: "JobId");

            migrationBuilder.CreateIndex(
                name: "IX_candidate_skills_CandidateId",
                table: "candidate_skills",
                column: "CandidateId");

            migrationBuilder.CreateIndex(
                name: "IX_job_matches_CandidateId",
                table: "job_matches",
                column: "CandidateId");

            migrationBuilder.CreateIndex(
                name: "IX_job_matches_JobId",
                table: "job_matches",
                column: "JobId");

            migrationBuilder.CreateIndex(
                name: "IX_job_matches_JobId_CandidateId",
                table: "job_matches",
                columns: new[] { "JobId", "CandidateId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_jobs_ContentHash",
                table: "jobs",
                column: "ContentHash");

            migrationBuilder.CreateIndex(
                name: "IX_jobs_ExternalId",
                table: "jobs",
                column: "ExternalId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "application_events");

            migrationBuilder.DropTable(
                name: "candidate_skills");

            migrationBuilder.DropTable(
                name: "job_matches");

            migrationBuilder.DropTable(
                name: "jobs");

            migrationBuilder.DropTable(
                name: "applications");

            migrationBuilder.DropTable(
                name: "candidates");
        }
    }
}

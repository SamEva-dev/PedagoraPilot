using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PedagoraPilot.Infrastructure.Persistence.Migrations.PedagoraPilot
{
    /// <inheritdoc />
    public partial class initialPedagoraPilot : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "workplace");

            migrationBuilder.EnsureSchema(
                name: "certification");

            migrationBuilder.EnsureSchema(
                name: "distance_learning");

            migrationBuilder.EnsureSchema(
                name: "training");

            migrationBuilder.EnsureSchema(
                name: "audit");

            migrationBuilder.EnsureSchema(
                name: "learning");

            migrationBuilder.EnsureSchema(
                name: "document");

            migrationBuilder.EnsureSchema(
                name: "integration");

            migrationBuilder.EnsureSchema(
                name: "organization");

            migrationBuilder.EnsureSchema(
                name: "catalog");

            migrationBuilder.EnsureSchema(
                name: "workforce");

            migrationBuilder.CreateTable(
                name: "activity_definitions",
                schema: "workplace",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ReferentialVersionId = table.Column<Guid>(type: "uuid", nullable: false),
                    PeriodTypeCode = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Code = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Title = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    LabelKey = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    Mandatory = table.Column<bool>(type: "boolean", nullable: false),
                    SortOrder = table.Column<int>(type: "integer", nullable: false),
                    Active = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Version = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_activity_definitions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "async_modules",
                schema: "distance_learning",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: false),
                    SiteId = table.Column<Guid>(type: "uuid", nullable: false),
                    ProgramId = table.Column<Guid>(type: "uuid", nullable: false),
                    CohortId = table.Column<Guid>(type: "uuid", nullable: false),
                    Title = table.Column<string>(type: "character varying(240)", maxLength: 240, nullable: false),
                    Description = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    EstimatedMinutes = table.Column<int>(type: "integer", nullable: false),
                    DueDate = table.Column<DateOnly>(type: "date", nullable: false),
                    TrainerDisplayName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Status = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    ProgressPercent = table.Column<int>(type: "integer", nullable: false),
                    CompletedStudents = table.Column<int>(type: "integer", nullable: false),
                    ExpectedStudents = table.Column<int>(type: "integer", nullable: false),
                    AverageScore = table.Column<decimal>(type: "numeric", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Version = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_async_modules", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "attendance_sheets",
                schema: "training",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: false),
                    SessionId = table.Column<Guid>(type: "uuid", nullable: false),
                    CohortId = table.Column<Guid>(type: "uuid", nullable: false),
                    ExpectedMinutes = table.Column<int>(type: "integer", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Version = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_attendance_sheets", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "audit_entries",
                schema: "audit",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: true),
                    UserId = table.Column<Guid>(type: "uuid", nullable: true),
                    UserDisplayName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    Action = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    EntityType = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    EntityId = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    Route = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CorrelationId = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    TraceId = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    IpAddress = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: true),
                    BeforeJson = table.Column<string>(type: "jsonb", nullable: true),
                    AfterJson = table.Column<string>(type: "jsonb", nullable: true),
                    MetadataJson = table.Column<string>(type: "jsonb", nullable: true),
                    OccurredAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_audit_entries", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "candidates",
                schema: "certification",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: false),
                    ExamSessionId = table.Column<Guid>(type: "uuid", nullable: false),
                    EnrollmentId = table.Column<Guid>(type: "uuid", nullable: false),
                    Status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    Eligible = table.Column<bool>(type: "boolean", nullable: true),
                    EligibilitySnapshotJson = table.Column<string>(type: "jsonb", nullable: true),
                    Decision = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    DecisionComment = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    DecisionAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Version = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_candidates", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "cohorts",
                schema: "training",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: false),
                    SiteId = table.Column<Guid>(type: "uuid", nullable: false),
                    ProgramOfferingId = table.Column<Guid>(type: "uuid", nullable: false),
                    ReferentialVersionId = table.Column<Guid>(type: "uuid", nullable: false),
                    Code = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    StartDate = table.Column<DateOnly>(type: "date", nullable: false),
                    EndDate = table.Column<DateOnly>(type: "date", nullable: false),
                    Capacity = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    ExternalKey = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Version = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_cohorts", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "competency_definitions",
                schema: "learning",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ReferentialVersionId = table.Column<Guid>(type: "uuid", nullable: false),
                    ParentId = table.Column<Guid>(type: "uuid", nullable: true),
                    Code = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    Title = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    Kind = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    SortOrder = table.Column<int>(type: "integer", nullable: false),
                    Active = table.Column<bool>(type: "boolean", nullable: false),
                    ExternalKey = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Version = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_competency_definitions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "document_requirements",
                schema: "workplace",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ReferentialVersionId = table.Column<Guid>(type: "uuid", nullable: false),
                    PeriodTypeCode = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Code = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Title = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    LabelKey = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    Mandatory = table.Column<bool>(type: "boolean", nullable: false),
                    SortOrder = table.Column<int>(type: "integer", nullable: false),
                    Active = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Version = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_document_requirements", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "documents",
                schema: "document",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: false),
                    SiteId = table.Column<Guid>(type: "uuid", nullable: true),
                    ProgramId = table.Column<Guid>(type: "uuid", nullable: true),
                    CohortId = table.Column<Guid>(type: "uuid", nullable: true),
                    OwnerType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    OwnerId = table.Column<Guid>(type: "uuid", nullable: true),
                    Title = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    Description = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    Category = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Visibility = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    CreatedByUserId = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: false),
                    CreatedByDisplayName = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Version = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_documents", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "driving_evaluations",
                schema: "learning",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: false),
                    EnrollmentId = table.Column<Guid>(type: "uuid", nullable: false),
                    CompetencyDefinitionId = table.Column<Guid>(type: "uuid", nullable: false),
                    TrainingSessionId = table.Column<Guid>(type: "uuid", nullable: true),
                    EvaluatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    TrainerAuthGateUserId = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    TrainerDisplayName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Subject = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Positive = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    Difficulty = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    NextGoal = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    FreeObservation = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Version = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_driving_evaluations", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "enrollments",
                schema: "training",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: false),
                    LearnerProfileId = table.Column<Guid>(type: "uuid", nullable: false),
                    CohortId = table.Column<Guid>(type: "uuid", nullable: false),
                    EnrolledOn = table.Column<DateOnly>(type: "date", nullable: false),
                    EndedOn = table.Column<DateOnly>(type: "date", nullable: true),
                    Status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    ExternalKey = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Version = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_enrollments", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "exam_sessions",
                schema: "certification",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: false),
                    SiteId = table.Column<Guid>(type: "uuid", nullable: false),
                    CohortId = table.Column<Guid>(type: "uuid", nullable: false),
                    SchemeId = table.Column<Guid>(type: "uuid", nullable: false),
                    Title = table.Column<string>(type: "character varying(240)", maxLength: 240, nullable: false),
                    StartsAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    EndsAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Venue = table.Column<string>(type: "character varying(240)", maxLength: 240, nullable: true),
                    Status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Version = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_exam_sessions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "idempotency_requests",
                schema: "integration",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Key = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Scope = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    RequestHash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    ResponseStatusCode = table.Column<int>(type: "integer", nullable: true),
                    ResponseContentType = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    ResponseBody = table.Column<string>(type: "text", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ExpiresAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CompletedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_idempotency_requests", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "jury_assignments",
                schema: "certification",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: false),
                    ExamSessionId = table.Column<Guid>(type: "uuid", nullable: false),
                    AuthGateUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    DisplayName = table.Column<string>(type: "character varying(240)", maxLength: 240, nullable: false),
                    Role = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    AssignedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_jury_assignments", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "learner_competency_records",
                schema: "learning",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: false),
                    EnrollmentId = table.Column<Guid>(type: "uuid", nullable: false),
                    CompetencyDefinitionId = table.Column<Guid>(type: "uuid", nullable: false),
                    Level = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    Score = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: true),
                    Comment = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    EvaluatorAuthGateUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    EvaluatorDisplayName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    EvaluatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Version = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_learner_competency_records", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "learner_profiles",
                schema: "learning",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PersonId = table.Column<Guid>(type: "uuid", nullable: false),
                    AuthGateUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    Status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    ExternalKey = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Version = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_learner_profiles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "learner_topic_progress",
                schema: "learning",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: false),
                    EnrollmentId = table.Column<Guid>(type: "uuid", nullable: false),
                    TopicId = table.Column<Guid>(type: "uuid", nullable: false),
                    Status = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    PreparationDate = table.Column<DateOnly>(type: "date", nullable: true),
                    PresentationDate = table.Column<DateOnly>(type: "date", nullable: true),
                    PresentationDurationMinutes = table.Column<int>(type: "integer", nullable: true),
                    EvaluatorDisplayName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    Comment = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Version = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_learner_topic_progress", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "live_sessions",
                schema: "distance_learning",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: false),
                    SiteId = table.Column<Guid>(type: "uuid", nullable: false),
                    ProgramId = table.Column<Guid>(type: "uuid", nullable: false),
                    CohortId = table.Column<Guid>(type: "uuid", nullable: false),
                    Title = table.Column<string>(type: "character varying(240)", maxLength: 240, nullable: false),
                    TrainerDisplayName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    TrainerEmail = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: true),
                    StartsAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    EndsAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Platform = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    JoinUrl = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    Objectives = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    Status = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Version = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_live_sessions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "organizations",
                schema: "organization",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OwnerUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    code = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    LegalName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    CountryCode = table.Column<string>(type: "character varying(2)", maxLength: 2, nullable: false),
                    OwnerEmail = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    OwnerPhone = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: true),
                    Status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Version = table.Column<long>(type: "bigint", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    LastModifiedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    LastModifiedByUserId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_organizations", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "outbox_messages",
                schema: "integration",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EventId = table.Column<Guid>(type: "uuid", nullable: false),
                    TypeKey = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    Version = table.Column<int>(type: "integer", nullable: false),
                    OccurredOnUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    EnqueuedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ProcessedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    AttemptCount = table.Column<int>(type: "integer", nullable: false),
                    NextAttemptUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    LockedUntilUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LockedBy = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    LastError = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    HeadersJson = table.Column<string>(type: "text", nullable: true),
                    PayloadJson = table.Column<string>(type: "text", nullable: false),
                    ContentType = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    CorrelationId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    RowVersion = table.Column<byte[]>(type: "bytea", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_outbox_messages", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "pedagogical_topics",
                schema: "learning",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ReferentialVersionId = table.Column<Guid>(type: "uuid", nullable: false),
                    Code = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    Number = table.Column<int>(type: "integer", nullable: true),
                    Title = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    Category = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    DurationMinutes = table.Column<int>(type: "integer", nullable: false),
                    Reference = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Active = table.Column<bool>(type: "boolean", nullable: false),
                    ExternalKey = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Version = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_pedagogical_topics", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "people",
                schema: "learning",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    FirstName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    LastName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Email = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    Phone = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: true),
                    BirthDate = table.Column<DateOnly>(type: "date", nullable: true),
                    ExternalKey = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Version = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_people", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "periods",
                schema: "workplace",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: false),
                    EnrollmentId = table.Column<Guid>(type: "uuid", nullable: false),
                    CohortId = table.Column<Guid>(type: "uuid", nullable: false),
                    ReferentialVersionId = table.Column<Guid>(type: "uuid", nullable: false),
                    PeriodTypeCode = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Company = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    City = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    TutorName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    TutorEmail = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: true),
                    TutorPhone = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: true),
                    StartDate = table.Column<DateOnly>(type: "date", nullable: false),
                    EndDate = table.Column<DateOnly>(type: "date", nullable: false),
                    PlannedMinutes = table.Column<int>(type: "integer", nullable: false),
                    CompletedMinutes = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    AgreementReceived = table.Column<bool>(type: "boolean", nullable: false),
                    TrainerVisible = table.Column<bool>(type: "boolean", nullable: false),
                    Notes = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    TutorObservation = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Version = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_periods", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "program_families",
                schema: "catalog",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    code = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    name = table.Column<string>(type: "character varying(180)", maxLength: 180, nullable: false),
                    icon = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    version = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_program_families", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "program_offerings",
                schema: "catalog",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    site_id = table.Column<Guid>(type: "uuid", nullable: false),
                    program_id = table.Column<Guid>(type: "uuid", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    external_key = table.Column<string>(type: "character varying(140)", maxLength: 140, nullable: true),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    version = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_program_offerings", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "referential_version_capabilities",
                schema: "catalog",
                columns: table => new
                {
                    referential_version_id = table.Column<Guid>(type: "uuid", nullable: false),
                    capability_code = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_referential_version_capabilities", x => new { x.referential_version_id, x.capability_code });
                });

            migrationBuilder.CreateTable(
                name: "referential_versions",
                schema: "catalog",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    referential_id = table.Column<Guid>(type: "uuid", nullable: false),
                    version_label = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    certification_code = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: true),
                    status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    effective_from = table.Column<DateOnly>(type: "date", nullable: false),
                    effective_to = table.Column<DateOnly>(type: "date", nullable: true),
                    total_hours = table.Column<int>(type: "integer", nullable: false),
                    sheet_count = table.Column<int>(type: "integer", nullable: false),
                    required_document_count = table.Column<int>(type: "integer", nullable: false),
                    notes_key = table.Column<string>(type: "character varying(220)", maxLength: 220, nullable: true),
                    external_key = table.Column<string>(type: "character varying(140)", maxLength: 140, nullable: true),
                    published_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    version = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_referential_versions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "referentials",
                schema: "catalog",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    program_id = table.Column<Guid>(type: "uuid", nullable: false),
                    code = table.Column<string>(type: "character varying(96)", maxLength: 96, nullable: false),
                    name = table.Column<string>(type: "character varying(220)", maxLength: 220, nullable: false),
                    external_key = table.Column<string>(type: "character varying(140)", maxLength: 140, nullable: true),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    version = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_referentials", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "remote_work_requests",
                schema: "workforce",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: false),
                    SiteId = table.Column<Guid>(type: "uuid", nullable: false),
                    AuthGateUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserDisplayName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    UserEmail = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: true),
                    Date = table.Column<DateOnly>(type: "date", nullable: false),
                    Period = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    StartTime = table.Column<TimeOnly>(type: "time without time zone", nullable: true),
                    EndTime = table.Column<TimeOnly>(type: "time without time zone", nullable: true),
                    Status = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    Comment = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    ApproverUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    ApproverDisplayName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    DecidedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Version = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_remote_work_requests", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "schemes",
                schema: "certification",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ReferentialVersionId = table.Column<Guid>(type: "uuid", nullable: false),
                    Code = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    Name = table.Column<string>(type: "character varying(240)", maxLength: 240, nullable: false),
                    Status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    EffectiveFrom = table.Column<DateOnly>(type: "date", nullable: true),
                    EffectiveTo = table.Column<DateOnly>(type: "date", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Version = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_schemes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "training_program_capabilities",
                schema: "catalog",
                columns: table => new
                {
                    program_id = table.Column<Guid>(type: "uuid", nullable: false),
                    capability_code = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_training_program_capabilities", x => new { x.program_id, x.capability_code });
                });

            migrationBuilder.CreateTable(
                name: "training_programs",
                schema: "catalog",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    family_id = table.Column<Guid>(type: "uuid", nullable: false),
                    code = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    name = table.Column<string>(type: "character varying(220)", maxLength: 220, nullable: false),
                    description_key = table.Column<string>(type: "character varying(220)", maxLength: 220, nullable: false),
                    icon = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    duration_hours = table.Column<int>(type: "integer", nullable: false),
                    status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    external_key = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    version = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_training_programs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "training_sessions",
                schema: "training",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: false),
                    SiteId = table.Column<Guid>(type: "uuid", nullable: false),
                    CohortId = table.Column<Guid>(type: "uuid", nullable: false),
                    Type = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    Modality = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    Title = table.Column<string>(type: "character varying(240)", maxLength: 240, nullable: false),
                    StartsAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    EndsAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    TimeZoneId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    TrainerAuthGateUserId = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    TrainerDisplayName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    Location = table.Column<string>(type: "character varying(240)", maxLength: 240, nullable: true),
                    Objective = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    Supports = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    Comments = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    Status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    AudienceMode = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    ExternalKey = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Version = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_training_sessions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "training_sites",
                schema: "organization",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: false),
                    code = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    Name = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    City = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    ExternalKey = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: true),
                    Status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Version = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_training_sites", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "async_module_steps",
                schema: "distance_learning",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ModuleId = table.Column<Guid>(type: "uuid", nullable: false),
                    Code = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Label = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    SortOrder = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_async_module_steps", x => x.Id);
                    table.ForeignKey(
                        name: "FK_async_module_steps_async_modules_ModuleId",
                        column: x => x.ModuleId,
                        principalSchema: "distance_learning",
                        principalTable: "async_modules",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "attendance_entries",
                schema: "training",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EnrollmentId = table.Column<Guid>(type: "uuid", nullable: false),
                    Status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    ArrivalAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    DepartureAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ExpectedMinutes = table.Column<int>(type: "integer", nullable: false),
                    PresentMinutes = table.Column<int>(type: "integer", nullable: false),
                    MissedMinutes = table.Column<int>(type: "integer", nullable: false),
                    CatchupMinutes = table.Column<int>(type: "integer", nullable: false),
                    AddToCatchup = table.Column<bool>(type: "boolean", nullable: false),
                    Comment = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    attendance_sheet_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_attendance_entries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_attendance_entries_attendance_sheets_attendance_sheet_id",
                        column: x => x.attendance_sheet_id,
                        principalSchema: "training",
                        principalTable: "attendance_sheets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "assessments",
                schema: "certification",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CandidateId = table.Column<Guid>(type: "uuid", nullable: false),
                    StepDefinitionId = table.Column<Guid>(type: "uuid", nullable: false),
                    JuryDisplayName = table.Column<string>(type: "character varying(240)", maxLength: 240, nullable: false),
                    Outcome = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    Score = table.Column<decimal>(type: "numeric", nullable: true),
                    Comment = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    RecordedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_assessments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_assessments_candidates_CandidateId",
                        column: x => x.CandidateId,
                        principalSchema: "certification",
                        principalTable: "candidates",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "document_versions",
                schema: "document",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    DocumentId = table.Column<Guid>(type: "uuid", nullable: false),
                    VersionNumber = table.Column<int>(type: "integer", nullable: false),
                    FileName = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    ContentType = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    SizeBytes = table.Column<long>(type: "bigint", nullable: false),
                    Sha256 = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    StorageKey = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: false),
                    BlobAvailable = table.Column<bool>(type: "boolean", nullable: false),
                    SecurityStatus = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    UploadedByUserId = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: false),
                    UploadedByDisplayName = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: false),
                    UploadedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_document_versions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_document_versions_documents_DocumentId",
                        column: x => x.DocumentId,
                        principalSchema: "document",
                        principalTable: "documents",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "driving_evaluation_criteria",
                schema: "learning",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    DrivingEvaluationId = table.Column<Guid>(type: "uuid", nullable: false),
                    Code = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    Label = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    Level = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_driving_evaluation_criteria", x => x.Id);
                    table.ForeignKey(
                        name: "FK_driving_evaluation_criteria_driving_evaluations_DrivingEval~",
                        column: x => x.DrivingEvaluationId,
                        principalSchema: "learning",
                        principalTable: "driving_evaluations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "live_participants",
                schema: "distance_learning",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SessionId = table.Column<Guid>(type: "uuid", nullable: false),
                    EnrollmentId = table.Column<Guid>(type: "uuid", nullable: false),
                    DisplayName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Attendance = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    ConnectedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    DisconnectedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ConnectedMinutes = table.Column<int>(type: "integer", nullable: false),
                    ParticipationPercent = table.Column<int>(type: "integer", nullable: false),
                    CompletedActivities = table.Column<int>(type: "integer", nullable: false),
                    ActivityCount = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_live_participants", x => x.Id);
                    table.ForeignKey(
                        name: "FK_live_participants_live_sessions_SessionId",
                        column: x => x.SessionId,
                        principalSchema: "distance_learning",
                        principalTable: "live_sessions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "activities",
                schema: "workplace",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PeriodId = table.Column<Guid>(type: "uuid", nullable: false),
                    DefinitionId = table.Column<Guid>(type: "uuid", nullable: false),
                    Code = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Title = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    LabelKey = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    Mandatory = table.Column<bool>(type: "boolean", nullable: false),
                    Status = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    CompletedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    Comment = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_activities", x => x.Id);
                    table.ForeignKey(
                        name: "FK_activities_periods_PeriodId",
                        column: x => x.PeriodId,
                        principalSchema: "workplace",
                        principalTable: "periods",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "document_checklist",
                schema: "workplace",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PeriodId = table.Column<Guid>(type: "uuid", nullable: false),
                    RequirementId = table.Column<Guid>(type: "uuid", nullable: false),
                    Code = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Title = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    LabelKey = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    Mandatory = table.Column<bool>(type: "boolean", nullable: false),
                    Status = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    DocumentId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_document_checklist", x => x.Id);
                    table.ForeignKey(
                        name: "FK_document_checklist_periods_PeriodId",
                        column: x => x.PeriodId,
                        principalSchema: "workplace",
                        principalTable: "periods",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "evaluations",
                schema: "workplace",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PeriodId = table.Column<Guid>(type: "uuid", nullable: false),
                    Kind = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    EvaluatorDisplayName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    EvaluatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Summary = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: false),
                    Strengths = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    ImprovementAreas = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    Validated = table.Column<bool>(type: "boolean", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_evaluations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_evaluations_periods_PeriodId",
                        column: x => x.PeriodId,
                        principalSchema: "workplace",
                        principalTable: "periods",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "remote_work_activities",
                schema: "workforce",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    RequestId = table.Column<Guid>(type: "uuid", nullable: false),
                    Code = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Label = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    Status = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_remote_work_activities", x => x.Id);
                    table.ForeignKey(
                        name: "FK_remote_work_activities_remote_work_requests_RequestId",
                        column: x => x.RequestId,
                        principalSchema: "workforce",
                        principalTable: "remote_work_requests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "scheme_units",
                schema: "certification",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SchemeId = table.Column<Guid>(type: "uuid", nullable: false),
                    Code = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    Title = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    SortOrder = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_scheme_units", x => x.Id);
                    table.ForeignKey(
                        name: "FK_scheme_units_schemes_SchemeId",
                        column: x => x.SchemeId,
                        principalSchema: "certification",
                        principalTable: "schemes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "step_definitions",
                schema: "certification",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SchemeId = table.Column<Guid>(type: "uuid", nullable: false),
                    UnitId = table.Column<Guid>(type: "uuid", nullable: true),
                    Code = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Title = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    Kind = table.Column<string>(type: "character varying(48)", maxLength: 48, nullable: false),
                    DurationMinutes = table.Column<int>(type: "integer", nullable: false),
                    SortOrder = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_step_definitions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_step_definitions_schemes_SchemeId",
                        column: x => x.SchemeId,
                        principalSchema: "certification",
                        principalTable: "schemes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "training_session_participants",
                schema: "training",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EnrollmentId = table.Column<Guid>(type: "uuid", nullable: false),
                    training_session_id = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_training_session_participants", x => x.Id);
                    table.ForeignKey(
                        name: "FK_training_session_participants_training_sessions_training_se~",
                        column: x => x.training_session_id,
                        principalSchema: "training",
                        principalTable: "training_sessions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_activities_PeriodId_DefinitionId",
                schema: "workplace",
                table: "activities",
                columns: new[] { "PeriodId", "DefinitionId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_activity_definitions_ReferentialVersionId_PeriodTypeCode_Co~",
                schema: "workplace",
                table: "activity_definitions",
                columns: new[] { "ReferentialVersionId", "PeriodTypeCode", "Code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_assessments_CandidateId_StepDefinitionId",
                schema: "certification",
                table: "assessments",
                columns: new[] { "CandidateId", "StepDefinitionId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_async_module_steps_ModuleId_Code",
                schema: "distance_learning",
                table: "async_module_steps",
                columns: new[] { "ModuleId", "Code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_async_modules_OrganizationId_CohortId_DueDate",
                schema: "distance_learning",
                table: "async_modules",
                columns: new[] { "OrganizationId", "CohortId", "DueDate" });

            migrationBuilder.CreateIndex(
                name: "IX_attendance_entries_attendance_sheet_id_EnrollmentId",
                schema: "training",
                table: "attendance_entries",
                columns: new[] { "attendance_sheet_id", "EnrollmentId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_attendance_entries_EnrollmentId",
                schema: "training",
                table: "attendance_entries",
                column: "EnrollmentId");

            migrationBuilder.CreateIndex(
                name: "IX_attendance_sheets_OrganizationId_CohortId",
                schema: "training",
                table: "attendance_sheets",
                columns: new[] { "OrganizationId", "CohortId" });

            migrationBuilder.CreateIndex(
                name: "IX_attendance_sheets_SessionId",
                schema: "training",
                table: "attendance_sheets",
                column: "SessionId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_audit_entries_CorrelationId",
                schema: "audit",
                table: "audit_entries",
                column: "CorrelationId");

            migrationBuilder.CreateIndex(
                name: "IX_audit_entries_EntityType_EntityId",
                schema: "audit",
                table: "audit_entries",
                columns: new[] { "EntityType", "EntityId" });

            migrationBuilder.CreateIndex(
                name: "IX_audit_entries_OrganizationId_OccurredAtUtc",
                schema: "audit",
                table: "audit_entries",
                columns: new[] { "OrganizationId", "OccurredAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_audit_entries_UserId",
                schema: "audit",
                table: "audit_entries",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_candidates_ExamSessionId_EnrollmentId",
                schema: "certification",
                table: "candidates",
                columns: new[] { "ExamSessionId", "EnrollmentId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_cohorts_ExternalKey",
                schema: "training",
                table: "cohorts",
                column: "ExternalKey",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_cohorts_OrganizationId_Code",
                schema: "training",
                table: "cohorts",
                columns: new[] { "OrganizationId", "Code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_cohorts_SiteId_ProgramOfferingId",
                schema: "training",
                table: "cohorts",
                columns: new[] { "SiteId", "ProgramOfferingId" });

            migrationBuilder.CreateIndex(
                name: "IX_competency_definitions_ReferentialVersionId_Code",
                schema: "learning",
                table: "competency_definitions",
                columns: new[] { "ReferentialVersionId", "Code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_document_checklist_PeriodId_RequirementId",
                schema: "workplace",
                table: "document_checklist",
                columns: new[] { "PeriodId", "RequirementId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_document_requirements_ReferentialVersionId_PeriodTypeCode_C~",
                schema: "workplace",
                table: "document_requirements",
                columns: new[] { "ReferentialVersionId", "PeriodTypeCode", "Code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_document_versions_DocumentId_VersionNumber",
                schema: "document",
                table: "document_versions",
                columns: new[] { "DocumentId", "VersionNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_document_versions_Sha256",
                schema: "document",
                table: "document_versions",
                column: "Sha256");

            migrationBuilder.CreateIndex(
                name: "IX_documents_OrganizationId_CohortId_Category",
                schema: "document",
                table: "documents",
                columns: new[] { "OrganizationId", "CohortId", "Category" });

            migrationBuilder.CreateIndex(
                name: "IX_documents_OrganizationId_OwnerType_OwnerId",
                schema: "document",
                table: "documents",
                columns: new[] { "OrganizationId", "OwnerType", "OwnerId" });

            migrationBuilder.CreateIndex(
                name: "IX_driving_evaluation_criteria_DrivingEvaluationId_Code",
                schema: "learning",
                table: "driving_evaluation_criteria",
                columns: new[] { "DrivingEvaluationId", "Code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_enrollments_ExternalKey",
                schema: "training",
                table: "enrollments",
                column: "ExternalKey",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_enrollments_LearnerProfileId_CohortId",
                schema: "training",
                table: "enrollments",
                columns: new[] { "LearnerProfileId", "CohortId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_enrollments_OrganizationId_CohortId_Status",
                schema: "training",
                table: "enrollments",
                columns: new[] { "OrganizationId", "CohortId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_evaluations_PeriodId",
                schema: "workplace",
                table: "evaluations",
                column: "PeriodId");

            migrationBuilder.CreateIndex(
                name: "IX_exam_sessions_OrganizationId_CohortId_StartsAtUtc",
                schema: "certification",
                table: "exam_sessions",
                columns: new[] { "OrganizationId", "CohortId", "StartsAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_idempotency_requests_ExpiresAtUtc",
                schema: "integration",
                table: "idempotency_requests",
                column: "ExpiresAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_idempotency_requests_Key_Scope",
                schema: "integration",
                table: "idempotency_requests",
                columns: new[] { "Key", "Scope" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_jury_assignments_ExamSessionId_AuthGateUserId",
                schema: "certification",
                table: "jury_assignments",
                columns: new[] { "ExamSessionId", "AuthGateUserId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_learner_competency_records_EnrollmentId_CompetencyDefinitio~",
                schema: "learning",
                table: "learner_competency_records",
                columns: new[] { "EnrollmentId", "CompetencyDefinitionId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_learner_profiles_AuthGateUserId",
                schema: "learning",
                table: "learner_profiles",
                column: "AuthGateUserId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_learner_profiles_ExternalKey",
                schema: "learning",
                table: "learner_profiles",
                column: "ExternalKey",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_learner_profiles_PersonId",
                schema: "learning",
                table: "learner_profiles",
                column: "PersonId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_learner_topic_progress_EnrollmentId_TopicId",
                schema: "learning",
                table: "learner_topic_progress",
                columns: new[] { "EnrollmentId", "TopicId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_live_participants_SessionId_EnrollmentId",
                schema: "distance_learning",
                table: "live_participants",
                columns: new[] { "SessionId", "EnrollmentId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_live_sessions_OrganizationId_CohortId_StartsAtUtc",
                schema: "distance_learning",
                table: "live_sessions",
                columns: new[] { "OrganizationId", "CohortId", "StartsAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_organizations_code",
                schema: "organization",
                table: "organizations",
                column: "code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_organizations_OwnerUserId",
                schema: "organization",
                table: "organizations",
                column: "OwnerUserId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_outbox_messages_EventId",
                schema: "integration",
                table: "outbox_messages",
                column: "EventId");

            migrationBuilder.CreateIndex(
                name: "IX_outbox_messages_LockedUntilUtc_LockedBy",
                schema: "integration",
                table: "outbox_messages",
                columns: new[] { "LockedUntilUtc", "LockedBy" });

            migrationBuilder.CreateIndex(
                name: "IX_outbox_messages_Status_NextAttemptUtc",
                schema: "integration",
                table: "outbox_messages",
                columns: new[] { "Status", "NextAttemptUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_pedagogical_topics_ReferentialVersionId_Code",
                schema: "learning",
                table: "pedagogical_topics",
                columns: new[] { "ReferentialVersionId", "Code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_pedagogical_topics_ReferentialVersionId_Number",
                schema: "learning",
                table: "pedagogical_topics",
                columns: new[] { "ReferentialVersionId", "Number" });

            migrationBuilder.CreateIndex(
                name: "IX_people_Email",
                schema: "learning",
                table: "people",
                column: "Email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_people_ExternalKey",
                schema: "learning",
                table: "people",
                column: "ExternalKey",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_periods_EnrollmentId_StartDate_EndDate",
                schema: "workplace",
                table: "periods",
                columns: new[] { "EnrollmentId", "StartDate", "EndDate" });

            migrationBuilder.CreateIndex(
                name: "IX_periods_OrganizationId_CohortId",
                schema: "workplace",
                table: "periods",
                columns: new[] { "OrganizationId", "CohortId" });

            migrationBuilder.CreateIndex(
                name: "IX_program_families_code",
                schema: "catalog",
                table: "program_families",
                column: "code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_program_offerings_site_id_program_id",
                schema: "catalog",
                table: "program_offerings",
                columns: new[] { "site_id", "program_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_referential_versions_referential_id_version_label",
                schema: "catalog",
                table: "referential_versions",
                columns: new[] { "referential_id", "version_label" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_referentials_program_id_code",
                schema: "catalog",
                table: "referentials",
                columns: new[] { "program_id", "code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_remote_work_activities_RequestId_Code",
                schema: "workforce",
                table: "remote_work_activities",
                columns: new[] { "RequestId", "Code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_remote_work_requests_AuthGateUserId_Date",
                schema: "workforce",
                table: "remote_work_requests",
                columns: new[] { "AuthGateUserId", "Date" });

            migrationBuilder.CreateIndex(
                name: "IX_remote_work_requests_OrganizationId_SiteId_Date",
                schema: "workforce",
                table: "remote_work_requests",
                columns: new[] { "OrganizationId", "SiteId", "Date" });

            migrationBuilder.CreateIndex(
                name: "IX_scheme_units_SchemeId_Code",
                schema: "certification",
                table: "scheme_units",
                columns: new[] { "SchemeId", "Code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_schemes_ReferentialVersionId_Code",
                schema: "certification",
                table: "schemes",
                columns: new[] { "ReferentialVersionId", "Code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_step_definitions_SchemeId_Code",
                schema: "certification",
                table: "step_definitions",
                columns: new[] { "SchemeId", "Code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_training_programs_code",
                schema: "catalog",
                table: "training_programs",
                column: "code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_training_programs_external_key",
                schema: "catalog",
                table: "training_programs",
                column: "external_key",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_training_session_participants_EnrollmentId",
                schema: "training",
                table: "training_session_participants",
                column: "EnrollmentId");

            migrationBuilder.CreateIndex(
                name: "IX_training_session_participants_training_session_id_Enrollmen~",
                schema: "training",
                table: "training_session_participants",
                columns: new[] { "training_session_id", "EnrollmentId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_training_sessions_ExternalKey",
                schema: "training",
                table: "training_sessions",
                column: "ExternalKey",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_training_sessions_OrganizationId_CohortId_StartsAtUtc",
                schema: "training",
                table: "training_sessions",
                columns: new[] { "OrganizationId", "CohortId", "StartsAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_training_sessions_SiteId_StartsAtUtc",
                schema: "training",
                table: "training_sessions",
                columns: new[] { "SiteId", "StartsAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_training_sites_OrganizationId_ExternalKey",
                schema: "organization",
                table: "training_sites",
                columns: new[] { "OrganizationId", "ExternalKey" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "activities",
                schema: "workplace");

            migrationBuilder.DropTable(
                name: "activity_definitions",
                schema: "workplace");

            migrationBuilder.DropTable(
                name: "assessments",
                schema: "certification");

            migrationBuilder.DropTable(
                name: "async_module_steps",
                schema: "distance_learning");

            migrationBuilder.DropTable(
                name: "attendance_entries",
                schema: "training");

            migrationBuilder.DropTable(
                name: "audit_entries",
                schema: "audit");

            migrationBuilder.DropTable(
                name: "cohorts",
                schema: "training");

            migrationBuilder.DropTable(
                name: "competency_definitions",
                schema: "learning");

            migrationBuilder.DropTable(
                name: "document_checklist",
                schema: "workplace");

            migrationBuilder.DropTable(
                name: "document_requirements",
                schema: "workplace");

            migrationBuilder.DropTable(
                name: "document_versions",
                schema: "document");

            migrationBuilder.DropTable(
                name: "driving_evaluation_criteria",
                schema: "learning");

            migrationBuilder.DropTable(
                name: "enrollments",
                schema: "training");

            migrationBuilder.DropTable(
                name: "evaluations",
                schema: "workplace");

            migrationBuilder.DropTable(
                name: "exam_sessions",
                schema: "certification");

            migrationBuilder.DropTable(
                name: "idempotency_requests",
                schema: "integration");

            migrationBuilder.DropTable(
                name: "jury_assignments",
                schema: "certification");

            migrationBuilder.DropTable(
                name: "learner_competency_records",
                schema: "learning");

            migrationBuilder.DropTable(
                name: "learner_profiles",
                schema: "learning");

            migrationBuilder.DropTable(
                name: "learner_topic_progress",
                schema: "learning");

            migrationBuilder.DropTable(
                name: "live_participants",
                schema: "distance_learning");

            migrationBuilder.DropTable(
                name: "organizations",
                schema: "organization");

            migrationBuilder.DropTable(
                name: "outbox_messages",
                schema: "integration");

            migrationBuilder.DropTable(
                name: "pedagogical_topics",
                schema: "learning");

            migrationBuilder.DropTable(
                name: "people",
                schema: "learning");

            migrationBuilder.DropTable(
                name: "program_families",
                schema: "catalog");

            migrationBuilder.DropTable(
                name: "program_offerings",
                schema: "catalog");

            migrationBuilder.DropTable(
                name: "referential_version_capabilities",
                schema: "catalog");

            migrationBuilder.DropTable(
                name: "referential_versions",
                schema: "catalog");

            migrationBuilder.DropTable(
                name: "referentials",
                schema: "catalog");

            migrationBuilder.DropTable(
                name: "remote_work_activities",
                schema: "workforce");

            migrationBuilder.DropTable(
                name: "scheme_units",
                schema: "certification");

            migrationBuilder.DropTable(
                name: "step_definitions",
                schema: "certification");

            migrationBuilder.DropTable(
                name: "training_program_capabilities",
                schema: "catalog");

            migrationBuilder.DropTable(
                name: "training_programs",
                schema: "catalog");

            migrationBuilder.DropTable(
                name: "training_session_participants",
                schema: "training");

            migrationBuilder.DropTable(
                name: "training_sites",
                schema: "organization");

            migrationBuilder.DropTable(
                name: "candidates",
                schema: "certification");

            migrationBuilder.DropTable(
                name: "async_modules",
                schema: "distance_learning");

            migrationBuilder.DropTable(
                name: "attendance_sheets",
                schema: "training");

            migrationBuilder.DropTable(
                name: "documents",
                schema: "document");

            migrationBuilder.DropTable(
                name: "driving_evaluations",
                schema: "learning");

            migrationBuilder.DropTable(
                name: "periods",
                schema: "workplace");

            migrationBuilder.DropTable(
                name: "live_sessions",
                schema: "distance_learning");

            migrationBuilder.DropTable(
                name: "remote_work_requests",
                schema: "workforce");

            migrationBuilder.DropTable(
                name: "schemes",
                schema: "certification");

            migrationBuilder.DropTable(
                name: "training_sessions",
                schema: "training");
        }
    }
}

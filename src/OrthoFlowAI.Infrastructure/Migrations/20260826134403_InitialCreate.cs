using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace OrthoFlowAI.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AspNetRoles",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    Name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    NormalizedName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    ConcurrencyStamp = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetRoles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUsers",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    FirstName = table.Column<string>(type: "text", nullable: false),
                    LastName = table.Column<string>(type: "text", nullable: false),
                    ProfileImage = table.Column<string>(type: "text", nullable: true),
                    Role = table.Column<string>(type: "text", nullable: false),
                    ClinicId = table.Column<string>(type: "text", nullable: true),
                    BranchId = table.Column<string>(type: "text", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    LastLoginAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Language = table.Column<string>(type: "text", nullable: true),
                    UserName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    NormalizedUserName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    Email = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    NormalizedEmail = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    EmailConfirmed = table.Column<bool>(type: "boolean", nullable: false),
                    PasswordHash = table.Column<string>(type: "text", nullable: true),
                    SecurityStamp = table.Column<string>(type: "text", nullable: true),
                    ConcurrencyStamp = table.Column<string>(type: "text", nullable: true),
                    PhoneNumber = table.Column<string>(type: "text", nullable: true),
                    PhoneNumberConfirmed = table.Column<bool>(type: "boolean", nullable: false),
                    TwoFactorEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    LockoutEnd = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    LockoutEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    AccessFailedCount = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUsers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "audit_logs",
                columns: table => new
                {
                    id = table.Column<string>(type: "text", nullable: false),
                    user_id = table.Column<string>(type: "text", nullable: true),
                    clinic_id = table.Column<string>(type: "text", nullable: true),
                    action = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    entity_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    entity_id = table.Column<string>(type: "text", nullable: true),
                    old_values_json = table.Column<string>(type: "text", nullable: true),
                    new_values_json = table.Column<string>(type: "text", nullable: true),
                    ip_address = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    user_agent = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false),
                    created_by = table.Column<string>(type: "text", nullable: true),
                    updated_by = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_audit_logs", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "clinics",
                columns: table => new
                {
                    id = table.Column<string>(type: "text", nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    logo = table.Column<string>(type: "text", nullable: true),
                    phone = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    email = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    address = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    city = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    country = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    website = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    business_hours = table.Column<string>(type: "text", nullable: true),
                    working_days = table.Column<string>(type: "text", nullable: true),
                    primary_color = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    secondary_color = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    subscription_plan = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    subscription_status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    language = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false),
                    created_by = table.Column<string>(type: "text", nullable: true),
                    updated_by = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_clinics", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "AspNetRoleClaims",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    RoleId = table.Column<string>(type: "text", nullable: false),
                    ClaimType = table.Column<string>(type: "text", nullable: true),
                    ClaimValue = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetRoleClaims", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AspNetRoleClaims_AspNetRoles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "AspNetRoles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserClaims",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserId = table.Column<string>(type: "text", nullable: false),
                    ClaimType = table.Column<string>(type: "text", nullable: true),
                    ClaimValue = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserClaims", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AspNetUserClaims_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserLogins",
                columns: table => new
                {
                    LoginProvider = table.Column<string>(type: "text", nullable: false),
                    ProviderKey = table.Column<string>(type: "text", nullable: false),
                    ProviderDisplayName = table.Column<string>(type: "text", nullable: true),
                    UserId = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserLogins", x => new { x.LoginProvider, x.ProviderKey });
                    table.ForeignKey(
                        name: "FK_AspNetUserLogins_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserRoles",
                columns: table => new
                {
                    UserId = table.Column<string>(type: "text", nullable: false),
                    RoleId = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserRoles", x => new { x.UserId, x.RoleId });
                    table.ForeignKey(
                        name: "FK_AspNetUserRoles_AspNetRoles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "AspNetRoles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AspNetUserRoles_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserTokens",
                columns: table => new
                {
                    UserId = table.Column<string>(type: "text", nullable: false),
                    LoginProvider = table.Column<string>(type: "text", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Value = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserTokens", x => new { x.UserId, x.LoginProvider, x.Name });
                    table.ForeignKey(
                        name: "FK_AspNetUserTokens_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "branches",
                columns: table => new
                {
                    id = table.Column<string>(type: "text", nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    address = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    phone = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    email = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    is_main_branch = table.Column<bool>(type: "boolean", nullable: false),
                    operating_hours = table.Column<string>(type: "text", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false),
                    created_by = table.Column<string>(type: "text", nullable: true),
                    updated_by = table.Column<string>(type: "text", nullable: true),
                    clinic_id = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_branches", x => x.id);
                    table.ForeignKey(
                        name: "FK_branches_clinics_clinic_id",
                        column: x => x.clinic_id,
                        principalTable: "clinics",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "notifications",
                columns: table => new
                {
                    id = table.Column<string>(type: "text", nullable: false),
                    user_id = table.Column<string>(type: "text", nullable: false),
                    title = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    message = table.Column<string>(type: "text", nullable: false),
                    type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    is_read = table.Column<bool>(type: "boolean", nullable: false),
                    read_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    channel = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    metadata_json = table.Column<string>(type: "text", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false),
                    created_by = table.Column<string>(type: "text", nullable: true),
                    updated_by = table.Column<string>(type: "text", nullable: true),
                    clinic_id = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_notifications", x => x.id);
                    table.ForeignKey(
                        name: "FK_notifications_clinics_clinic_id",
                        column: x => x.clinic_id,
                        principalTable: "clinics",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "subscriptions",
                columns: table => new
                {
                    id = table.Column<string>(type: "text", nullable: false),
                    clinic_id = table.Column<string>(type: "text", nullable: false),
                    plan = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    billing_period = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    amount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    start_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    end_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    next_billing_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    trial_ends_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    coupons_json = table.Column<string>(type: "text", nullable: true),
                    max_patients = table.Column<int>(type: "integer", nullable: false),
                    max_staff = table.Column<int>(type: "integer", nullable: false),
                    max_branches = table.Column<int>(type: "integer", nullable: false),
                    features_json = table.Column<string>(type: "text", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false),
                    created_by = table.Column<string>(type: "text", nullable: true),
                    updated_by = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_subscriptions", x => x.id);
                    table.ForeignKey(
                        name: "FK_subscriptions_clinics_clinic_id",
                        column: x => x.clinic_id,
                        principalTable: "clinics",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "inventory_items",
                columns: table => new
                {
                    id = table.Column<string>(type: "text", nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    category = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    brand = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    model = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    quantity = table.Column<int>(type: "integer", nullable: false),
                    min_quantity = table.Column<int>(type: "integer", nullable: false),
                    unit_price = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    supplier_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    supplier_phone = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    last_restocked_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    branch_id = table.Column<string>(type: "text", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false),
                    created_by = table.Column<string>(type: "text", nullable: true),
                    updated_by = table.Column<string>(type: "text", nullable: true),
                    clinic_id = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_inventory_items", x => x.id);
                    table.ForeignKey(
                        name: "FK_inventory_items_branches_branch_id",
                        column: x => x.branch_id,
                        principalTable: "branches",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_inventory_items_clinics_clinic_id",
                        column: x => x.clinic_id,
                        principalTable: "clinics",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "application_users",
                columns: table => new
                {
                    id = table.Column<string>(type: "text", nullable: false),
                    first_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    last_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    phone = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    profile_image = table.Column<string>(type: "text", nullable: true),
                    role = table.Column<string>(type: "text", nullable: false),
                    clinic_id = table.Column<string>(type: "text", nullable: true),
                    branch_id = table.Column<string>(type: "text", nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    last_login_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    language = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    date_of_birth = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    gender = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    DoctorProfileId = table.Column<string>(type: "text", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false),
                    created_by = table.Column<string>(type: "text", nullable: true),
                    updated_by = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_application_users", x => x.id);
                    table.ForeignKey(
                        name: "FK_application_users_branches_branch_id",
                        column: x => x.branch_id,
                        principalTable: "branches",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_application_users_clinics_clinic_id",
                        column: x => x.clinic_id,
                        principalTable: "clinics",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "doctors",
                columns: table => new
                {
                    id = table.Column<string>(type: "text", nullable: false),
                    user_id = table.Column<string>(type: "text", nullable: false),
                    specialty = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    license_number = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    bio = table.Column<string>(type: "text", nullable: true),
                    years_of_experience = table.Column<int>(type: "integer", nullable: false),
                    consultation_fee = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    is_accepting_patients = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false),
                    created_by = table.Column<string>(type: "text", nullable: true),
                    updated_by = table.Column<string>(type: "text", nullable: true),
                    clinic_id = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_doctors", x => x.id);
                    table.ForeignKey(
                        name: "FK_doctors_application_users_user_id",
                        column: x => x.user_id,
                        principalTable: "application_users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_doctors_clinics_clinic_id",
                        column: x => x.clinic_id,
                        principalTable: "clinics",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "staff",
                columns: table => new
                {
                    id = table.Column<string>(type: "text", nullable: false),
                    user_id = table.Column<string>(type: "text", nullable: false),
                    position = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    department = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    schedule_json = table.Column<string>(type: "text", nullable: true),
                    permissions_json = table.Column<string>(type: "text", nullable: true),
                    status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    hire_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    salary = table.Column<decimal>(type: "numeric(18,2)", nullable: true),
                    BranchId = table.Column<string>(type: "text", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false),
                    created_by = table.Column<string>(type: "text", nullable: true),
                    updated_by = table.Column<string>(type: "text", nullable: true),
                    clinic_id = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_staff", x => x.id);
                    table.ForeignKey(
                        name: "FK_staff_application_users_user_id",
                        column: x => x.user_id,
                        principalTable: "application_users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_staff_branches_BranchId",
                        column: x => x.BranchId,
                        principalTable: "branches",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_staff_clinics_clinic_id",
                        column: x => x.clinic_id,
                        principalTable: "clinics",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "BranchDoctor",
                columns: table => new
                {
                    BranchesId = table.Column<string>(type: "text", nullable: false),
                    DoctorsId = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BranchDoctor", x => new { x.BranchesId, x.DoctorsId });
                    table.ForeignKey(
                        name: "FK_BranchDoctor_branches_BranchesId",
                        column: x => x.BranchesId,
                        principalTable: "branches",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_BranchDoctor_doctors_DoctorsId",
                        column: x => x.DoctorsId,
                        principalTable: "doctors",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "patients",
                columns: table => new
                {
                    id = table.Column<string>(type: "text", nullable: false),
                    user_id = table.Column<string>(type: "text", nullable: true),
                    first_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    last_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    email = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    phone = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    date_of_birth = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    gender = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    address = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    city = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    emergency_contact_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    emergency_contact_phone = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    insurance_provider = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    insurance_number = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    diagnosis = table.Column<string>(type: "text", nullable: true),
                    treatment_plan_description = table.Column<string>(type: "text", nullable: true),
                    treatment_type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    treatment_start_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    estimated_end_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    assigned_doctor_id = table.Column<string>(type: "text", nullable: true),
                    branch_id = table.Column<string>(type: "text", nullable: true),
                    tags = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    journey_score = table.Column<double>(type: "double precision", nullable: false),
                    compliance_score = table.Column<double>(type: "double precision", nullable: false),
                    total_treatment_cost = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    amount_paid = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    amount_remaining = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    notes = table.Column<string>(type: "text", nullable: true),
                    qr_code = table.Column<string>(type: "text", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false),
                    created_by = table.Column<string>(type: "text", nullable: true),
                    updated_by = table.Column<string>(type: "text", nullable: true),
                    clinic_id = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_patients", x => x.id);
                    table.ForeignKey(
                        name: "FK_patients_branches_branch_id",
                        column: x => x.branch_id,
                        principalTable: "branches",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_patients_clinics_clinic_id",
                        column: x => x.clinic_id,
                        principalTable: "clinics",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_patients_doctors_assigned_doctor_id",
                        column: x => x.assigned_doctor_id,
                        principalTable: "doctors",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "appointments",
                columns: table => new
                {
                    id = table.Column<string>(type: "text", nullable: false),
                    patient_id = table.Column<string>(type: "text", nullable: false),
                    doctor_id = table.Column<string>(type: "text", nullable: false),
                    branch_id = table.Column<string>(type: "text", nullable: true),
                    appointment_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    end_time = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    notes = table.Column<string>(type: "text", nullable: true),
                    duration = table.Column<int>(type: "integer", nullable: false),
                    color = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    is_recurring = table.Column<bool>(type: "boolean", nullable: false),
                    recurrence_pattern = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    recurrence_end_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false),
                    created_by = table.Column<string>(type: "text", nullable: true),
                    updated_by = table.Column<string>(type: "text", nullable: true),
                    clinic_id = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_appointments", x => x.id);
                    table.ForeignKey(
                        name: "FK_appointments_branches_branch_id",
                        column: x => x.branch_id,
                        principalTable: "branches",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_appointments_clinics_clinic_id",
                        column: x => x.clinic_id,
                        principalTable: "clinics",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_appointments_doctors_doctor_id",
                        column: x => x.doctor_id,
                        principalTable: "doctors",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_appointments_patients_patient_id",
                        column: x => x.patient_id,
                        principalTable: "patients",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "chat_messages",
                columns: table => new
                {
                    id = table.Column<string>(type: "text", nullable: false),
                    sender_id = table.Column<string>(type: "text", nullable: false),
                    receiver_id = table.Column<string>(type: "text", nullable: false),
                    patient_id = table.Column<string>(type: "text", nullable: true),
                    message = table.Column<string>(type: "text", nullable: false),
                    file_url = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    is_read = table.Column<bool>(type: "boolean", nullable: false),
                    read_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    typing_indicator = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false),
                    created_by = table.Column<string>(type: "text", nullable: true),
                    updated_by = table.Column<string>(type: "text", nullable: true),
                    clinic_id = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_chat_messages", x => x.id);
                    table.ForeignKey(
                        name: "FK_chat_messages_application_users_receiver_id",
                        column: x => x.receiver_id,
                        principalTable: "application_users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_chat_messages_application_users_sender_id",
                        column: x => x.sender_id,
                        principalTable: "application_users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_chat_messages_clinics_clinic_id",
                        column: x => x.clinic_id,
                        principalTable: "clinics",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_chat_messages_patients_patient_id",
                        column: x => x.patient_id,
                        principalTable: "patients",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "documents",
                columns: table => new
                {
                    id = table.Column<string>(type: "text", nullable: false),
                    patient_id = table.Column<string>(type: "text", nullable: false),
                    file_name = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    file_url = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    file_type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    file_size = table.Column<long>(type: "bigint", nullable: false),
                    type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    description = table.Column<string>(type: "text", nullable: true),
                    is_signed = table.Column<bool>(type: "boolean", nullable: false),
                    signed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false),
                    created_by = table.Column<string>(type: "text", nullable: true),
                    updated_by = table.Column<string>(type: "text", nullable: true),
                    clinic_id = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_documents", x => x.id);
                    table.ForeignKey(
                        name: "FK_documents_clinics_clinic_id",
                        column: x => x.clinic_id,
                        principalTable: "clinics",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_documents_patients_patient_id",
                        column: x => x.patient_id,
                        principalTable: "patients",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "emergency_requests",
                columns: table => new
                {
                    id = table.Column<string>(type: "text", nullable: false),
                    patient_id = table.Column<string>(type: "text", nullable: false),
                    doctor_id = table.Column<string>(type: "text", nullable: true),
                    description = table.Column<string>(type: "text", nullable: false),
                    severity = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    photo_urls_json = table.Column<string>(type: "text", nullable: true),
                    video_url = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    voice_note_url = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    resolved_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    doctor_notes = table.Column<string>(type: "text", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false),
                    created_by = table.Column<string>(type: "text", nullable: true),
                    updated_by = table.Column<string>(type: "text", nullable: true),
                    clinic_id = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_emergency_requests", x => x.id);
                    table.ForeignKey(
                        name: "FK_emergency_requests_clinics_clinic_id",
                        column: x => x.clinic_id,
                        principalTable: "clinics",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_emergency_requests_doctors_doctor_id",
                        column: x => x.doctor_id,
                        principalTable: "doctors",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_emergency_requests_patients_patient_id",
                        column: x => x.patient_id,
                        principalTable: "patients",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "feedbacks",
                columns: table => new
                {
                    id = table.Column<string>(type: "text", nullable: false),
                    patient_id = table.Column<string>(type: "text", nullable: false),
                    doctor_id = table.Column<string>(type: "text", nullable: true),
                    rating = table.Column<int>(type: "integer", nullable: false),
                    comment = table.Column<string>(type: "text", nullable: true),
                    type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    nps_score = table.Column<int>(type: "integer", nullable: true),
                    visit_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false),
                    created_by = table.Column<string>(type: "text", nullable: true),
                    updated_by = table.Column<string>(type: "text", nullable: true),
                    clinic_id = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_feedbacks", x => x.id);
                    table.ForeignKey(
                        name: "FK_feedbacks_clinics_clinic_id",
                        column: x => x.clinic_id,
                        principalTable: "clinics",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_feedbacks_doctors_doctor_id",
                        column: x => x.doctor_id,
                        principalTable: "doctors",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_feedbacks_patients_patient_id",
                        column: x => x.patient_id,
                        principalTable: "patients",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "invoices",
                columns: table => new
                {
                    id = table.Column<string>(type: "text", nullable: false),
                    invoice_number = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    patient_id = table.Column<string>(type: "text", nullable: false),
                    doctor_id = table.Column<string>(type: "text", nullable: true),
                    total_amount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    paid_amount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    remaining_amount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    discount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    tax = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    payment_status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    due_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    notes = table.Column<string>(type: "text", nullable: true),
                    items_json = table.Column<string>(type: "text", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false),
                    created_by = table.Column<string>(type: "text", nullable: true),
                    updated_by = table.Column<string>(type: "text", nullable: true),
                    clinic_id = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_invoices", x => x.id);
                    table.ForeignKey(
                        name: "FK_invoices_clinics_clinic_id",
                        column: x => x.clinic_id,
                        principalTable: "clinics",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_invoices_doctors_doctor_id",
                        column: x => x.doctor_id,
                        principalTable: "doctors",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_invoices_patients_patient_id",
                        column: x => x.patient_id,
                        principalTable: "patients",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "pain_reports",
                columns: table => new
                {
                    id = table.Column<string>(type: "text", nullable: false),
                    patient_id = table.Column<string>(type: "text", nullable: false),
                    report_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    pain_level = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    has_broken_wire = table.Column<bool>(type: "boolean", nullable: false),
                    has_loose_bracket = table.Column<bool>(type: "boolean", nullable: false),
                    has_bleeding = table.Column<bool>(type: "boolean", nullable: false),
                    has_swelling = table.Column<bool>(type: "boolean", nullable: false),
                    description = table.Column<string>(type: "text", nullable: true),
                    photo_url = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    is_emergency = table.Column<bool>(type: "boolean", nullable: false),
                    doctor_notified = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false),
                    created_by = table.Column<string>(type: "text", nullable: true),
                    updated_by = table.Column<string>(type: "text", nullable: true),
                    clinic_id = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_pain_reports", x => x.id);
                    table.ForeignKey(
                        name: "FK_pain_reports_clinics_clinic_id",
                        column: x => x.clinic_id,
                        principalTable: "clinics",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_pain_reports_patients_patient_id",
                        column: x => x.patient_id,
                        principalTable: "patients",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "photos",
                columns: table => new
                {
                    id = table.Column<string>(type: "text", nullable: false),
                    patient_id = table.Column<string>(type: "text", nullable: false),
                    doctor_id = table.Column<string>(type: "text", nullable: true),
                    url = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    file_name = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    file_type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    file_size = table.Column<long>(type: "bigint", nullable: false),
                    category = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    description = table.Column<string>(type: "text", nullable: true),
                    visit_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    before_photo_url = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    after_photo_url = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    comparison_score = table.Column<double>(type: "double precision", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false),
                    created_by = table.Column<string>(type: "text", nullable: true),
                    updated_by = table.Column<string>(type: "text", nullable: true),
                    clinic_id = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_photos", x => x.id);
                    table.ForeignKey(
                        name: "FK_photos_clinics_clinic_id",
                        column: x => x.clinic_id,
                        principalTable: "clinics",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_photos_doctors_doctor_id",
                        column: x => x.doctor_id,
                        principalTable: "doctors",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_photos_patients_patient_id",
                        column: x => x.patient_id,
                        principalTable: "patients",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "referrals",
                columns: table => new
                {
                    id = table.Column<string>(type: "text", nullable: false),
                    referrer_patient_id = table.Column<string>(type: "text", nullable: false),
                    referred_patient_id = table.Column<string>(type: "text", nullable: false),
                    referral_code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    discount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    reward = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    referred_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false),
                    created_by = table.Column<string>(type: "text", nullable: true),
                    updated_by = table.Column<string>(type: "text", nullable: true),
                    clinic_id = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_referrals", x => x.id);
                    table.ForeignKey(
                        name: "FK_referrals_clinics_clinic_id",
                        column: x => x.clinic_id,
                        principalTable: "clinics",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_referrals_patients_referred_patient_id",
                        column: x => x.referred_patient_id,
                        principalTable: "patients",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_referrals_patients_referrer_patient_id",
                        column: x => x.referrer_patient_id,
                        principalTable: "patients",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "treatment_visits",
                columns: table => new
                {
                    id = table.Column<string>(type: "text", nullable: false),
                    patient_id = table.Column<string>(type: "text", nullable: false),
                    doctor_id = table.Column<string>(type: "text", nullable: false),
                    appointment_id = table.Column<string>(type: "text", nullable: true),
                    visit_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    wire_changed = table.Column<bool>(type: "boolean", nullable: false),
                    bracket_replaced = table.Column<bool>(type: "boolean", nullable: false),
                    rubber_bands_used = table.Column<bool>(type: "boolean", nullable: false),
                    pain_level = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    doctor_notes = table.Column<string>(type: "text", nullable: true),
                    medication = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    next_appointment_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    photos_json = table.Column<string>(type: "text", nullable: true),
                    attachments_json = table.Column<string>(type: "text", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false),
                    created_by = table.Column<string>(type: "text", nullable: true),
                    updated_by = table.Column<string>(type: "text", nullable: true),
                    clinic_id = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_treatment_visits", x => x.id);
                    table.ForeignKey(
                        name: "FK_treatment_visits_appointments_appointment_id",
                        column: x => x.appointment_id,
                        principalTable: "appointments",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_treatment_visits_clinics_clinic_id",
                        column: x => x.clinic_id,
                        principalTable: "clinics",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_treatment_visits_doctors_doctor_id",
                        column: x => x.doctor_id,
                        principalTable: "doctors",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_treatment_visits_patients_patient_id",
                        column: x => x.patient_id,
                        principalTable: "patients",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "payments",
                columns: table => new
                {
                    id = table.Column<string>(type: "text", nullable: false),
                    invoice_id = table.Column<string>(type: "text", nullable: false),
                    patient_id = table.Column<string>(type: "text", nullable: false),
                    amount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    payment_method = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    transaction_id = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    notes = table.Column<string>(type: "text", nullable: true),
                    payment_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false),
                    created_by = table.Column<string>(type: "text", nullable: true),
                    updated_by = table.Column<string>(type: "text", nullable: true),
                    clinic_id = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_payments", x => x.id);
                    table.ForeignKey(
                        name: "FK_payments_clinics_clinic_id",
                        column: x => x.clinic_id,
                        principalTable: "clinics",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_payments_invoices_invoice_id",
                        column: x => x.invoice_id,
                        principalTable: "invoices",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_payments_patients_patient_id",
                        column: x => x.patient_id,
                        principalTable: "patients",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_application_users_branch_id",
                table: "application_users",
                column: "branch_id");

            migrationBuilder.CreateIndex(
                name: "IX_application_users_clinic_id",
                table: "application_users",
                column: "clinic_id");

            migrationBuilder.CreateIndex(
                name: "IX_application_users_DoctorProfileId",
                table: "application_users",
                column: "DoctorProfileId");

            migrationBuilder.CreateIndex(
                name: "IX_application_users_is_active",
                table: "application_users",
                column: "is_active");

            migrationBuilder.CreateIndex(
                name: "IX_application_users_is_deleted",
                table: "application_users",
                column: "is_deleted");

            migrationBuilder.CreateIndex(
                name: "IX_application_users_role",
                table: "application_users",
                column: "role");

            migrationBuilder.CreateIndex(
                name: "IX_appointments_appointment_date",
                table: "appointments",
                column: "appointment_date");

            migrationBuilder.CreateIndex(
                name: "IX_appointments_branch_id",
                table: "appointments",
                column: "branch_id");

            migrationBuilder.CreateIndex(
                name: "IX_appointments_clinic_id",
                table: "appointments",
                column: "clinic_id");

            migrationBuilder.CreateIndex(
                name: "IX_appointments_doctor_id",
                table: "appointments",
                column: "doctor_id");

            migrationBuilder.CreateIndex(
                name: "IX_appointments_is_deleted",
                table: "appointments",
                column: "is_deleted");

            migrationBuilder.CreateIndex(
                name: "IX_appointments_patient_id",
                table: "appointments",
                column: "patient_id");

            migrationBuilder.CreateIndex(
                name: "IX_appointments_status",
                table: "appointments",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetRoleClaims_RoleId",
                table: "AspNetRoleClaims",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "RoleNameIndex",
                table: "AspNetRoles",
                column: "NormalizedName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUserClaims_UserId",
                table: "AspNetUserClaims",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUserLogins_UserId",
                table: "AspNetUserLogins",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUserRoles_RoleId",
                table: "AspNetUserRoles",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "EmailIndex",
                table: "AspNetUsers",
                column: "NormalizedEmail");

            migrationBuilder.CreateIndex(
                name: "UserNameIndex",
                table: "AspNetUsers",
                column: "NormalizedUserName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_audit_logs_action",
                table: "audit_logs",
                column: "action");

            migrationBuilder.CreateIndex(
                name: "IX_audit_logs_clinic_id",
                table: "audit_logs",
                column: "clinic_id");

            migrationBuilder.CreateIndex(
                name: "IX_audit_logs_created_at",
                table: "audit_logs",
                column: "created_at");

            migrationBuilder.CreateIndex(
                name: "IX_audit_logs_entity_name",
                table: "audit_logs",
                column: "entity_name");

            migrationBuilder.CreateIndex(
                name: "IX_audit_logs_is_deleted",
                table: "audit_logs",
                column: "is_deleted");

            migrationBuilder.CreateIndex(
                name: "IX_audit_logs_user_id",
                table: "audit_logs",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "IX_BranchDoctor_DoctorsId",
                table: "BranchDoctor",
                column: "DoctorsId");

            migrationBuilder.CreateIndex(
                name: "IX_branches_clinic_id",
                table: "branches",
                column: "clinic_id");

            migrationBuilder.CreateIndex(
                name: "IX_branches_is_deleted",
                table: "branches",
                column: "is_deleted");

            migrationBuilder.CreateIndex(
                name: "IX_chat_messages_clinic_id",
                table: "chat_messages",
                column: "clinic_id");

            migrationBuilder.CreateIndex(
                name: "IX_chat_messages_is_deleted",
                table: "chat_messages",
                column: "is_deleted");

            migrationBuilder.CreateIndex(
                name: "IX_chat_messages_patient_id",
                table: "chat_messages",
                column: "patient_id");

            migrationBuilder.CreateIndex(
                name: "IX_chat_messages_receiver_id",
                table: "chat_messages",
                column: "receiver_id");

            migrationBuilder.CreateIndex(
                name: "IX_chat_messages_sender_id",
                table: "chat_messages",
                column: "sender_id");

            migrationBuilder.CreateIndex(
                name: "IX_clinics_email",
                table: "clinics",
                column: "email");

            migrationBuilder.CreateIndex(
                name: "IX_clinics_is_active",
                table: "clinics",
                column: "is_active");

            migrationBuilder.CreateIndex(
                name: "IX_clinics_is_deleted",
                table: "clinics",
                column: "is_deleted");

            migrationBuilder.CreateIndex(
                name: "IX_clinics_name",
                table: "clinics",
                column: "name");

            migrationBuilder.CreateIndex(
                name: "IX_doctors_clinic_id",
                table: "doctors",
                column: "clinic_id");

            migrationBuilder.CreateIndex(
                name: "IX_doctors_is_deleted",
                table: "doctors",
                column: "is_deleted");

            migrationBuilder.CreateIndex(
                name: "IX_doctors_license_number",
                table: "doctors",
                column: "license_number");

            migrationBuilder.CreateIndex(
                name: "IX_doctors_user_id",
                table: "doctors",
                column: "user_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_documents_clinic_id",
                table: "documents",
                column: "clinic_id");

            migrationBuilder.CreateIndex(
                name: "IX_documents_is_deleted",
                table: "documents",
                column: "is_deleted");

            migrationBuilder.CreateIndex(
                name: "IX_documents_patient_id",
                table: "documents",
                column: "patient_id");

            migrationBuilder.CreateIndex(
                name: "IX_documents_type",
                table: "documents",
                column: "type");

            migrationBuilder.CreateIndex(
                name: "IX_emergency_requests_clinic_id",
                table: "emergency_requests",
                column: "clinic_id");

            migrationBuilder.CreateIndex(
                name: "IX_emergency_requests_doctor_id",
                table: "emergency_requests",
                column: "doctor_id");

            migrationBuilder.CreateIndex(
                name: "IX_emergency_requests_is_deleted",
                table: "emergency_requests",
                column: "is_deleted");

            migrationBuilder.CreateIndex(
                name: "IX_emergency_requests_patient_id",
                table: "emergency_requests",
                column: "patient_id");

            migrationBuilder.CreateIndex(
                name: "IX_emergency_requests_severity",
                table: "emergency_requests",
                column: "severity");

            migrationBuilder.CreateIndex(
                name: "IX_feedbacks_clinic_id",
                table: "feedbacks",
                column: "clinic_id");

            migrationBuilder.CreateIndex(
                name: "IX_feedbacks_doctor_id",
                table: "feedbacks",
                column: "doctor_id");

            migrationBuilder.CreateIndex(
                name: "IX_feedbacks_is_deleted",
                table: "feedbacks",
                column: "is_deleted");

            migrationBuilder.CreateIndex(
                name: "IX_feedbacks_patient_id",
                table: "feedbacks",
                column: "patient_id");

            migrationBuilder.CreateIndex(
                name: "IX_inventory_items_branch_id",
                table: "inventory_items",
                column: "branch_id");

            migrationBuilder.CreateIndex(
                name: "IX_inventory_items_category",
                table: "inventory_items",
                column: "category");

            migrationBuilder.CreateIndex(
                name: "IX_inventory_items_clinic_id",
                table: "inventory_items",
                column: "clinic_id");

            migrationBuilder.CreateIndex(
                name: "IX_inventory_items_is_deleted",
                table: "inventory_items",
                column: "is_deleted");

            migrationBuilder.CreateIndex(
                name: "IX_invoices_clinic_id",
                table: "invoices",
                column: "clinic_id");

            migrationBuilder.CreateIndex(
                name: "IX_invoices_doctor_id",
                table: "invoices",
                column: "doctor_id");

            migrationBuilder.CreateIndex(
                name: "IX_invoices_invoice_number",
                table: "invoices",
                column: "invoice_number",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_invoices_is_deleted",
                table: "invoices",
                column: "is_deleted");

            migrationBuilder.CreateIndex(
                name: "IX_invoices_patient_id",
                table: "invoices",
                column: "patient_id");

            migrationBuilder.CreateIndex(
                name: "IX_invoices_status",
                table: "invoices",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "IX_notifications_clinic_id",
                table: "notifications",
                column: "clinic_id");

            migrationBuilder.CreateIndex(
                name: "IX_notifications_is_deleted",
                table: "notifications",
                column: "is_deleted");

            migrationBuilder.CreateIndex(
                name: "IX_notifications_is_read",
                table: "notifications",
                column: "is_read");

            migrationBuilder.CreateIndex(
                name: "IX_notifications_type",
                table: "notifications",
                column: "type");

            migrationBuilder.CreateIndex(
                name: "IX_notifications_user_id",
                table: "notifications",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "IX_pain_reports_clinic_id",
                table: "pain_reports",
                column: "clinic_id");

            migrationBuilder.CreateIndex(
                name: "IX_pain_reports_is_deleted",
                table: "pain_reports",
                column: "is_deleted");

            migrationBuilder.CreateIndex(
                name: "IX_pain_reports_is_emergency",
                table: "pain_reports",
                column: "is_emergency");

            migrationBuilder.CreateIndex(
                name: "IX_pain_reports_patient_id",
                table: "pain_reports",
                column: "patient_id");

            migrationBuilder.CreateIndex(
                name: "IX_patients_assigned_doctor_id",
                table: "patients",
                column: "assigned_doctor_id");

            migrationBuilder.CreateIndex(
                name: "IX_patients_branch_id",
                table: "patients",
                column: "branch_id");

            migrationBuilder.CreateIndex(
                name: "IX_patients_clinic_id",
                table: "patients",
                column: "clinic_id");

            migrationBuilder.CreateIndex(
                name: "IX_patients_email",
                table: "patients",
                column: "email");

            migrationBuilder.CreateIndex(
                name: "IX_patients_first_name_last_name",
                table: "patients",
                columns: new[] { "first_name", "last_name" });

            migrationBuilder.CreateIndex(
                name: "IX_patients_is_deleted",
                table: "patients",
                column: "is_deleted");

            migrationBuilder.CreateIndex(
                name: "IX_patients_status",
                table: "patients",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "IX_payments_clinic_id",
                table: "payments",
                column: "clinic_id");

            migrationBuilder.CreateIndex(
                name: "IX_payments_invoice_id",
                table: "payments",
                column: "invoice_id");

            migrationBuilder.CreateIndex(
                name: "IX_payments_is_deleted",
                table: "payments",
                column: "is_deleted");

            migrationBuilder.CreateIndex(
                name: "IX_payments_patient_id",
                table: "payments",
                column: "patient_id");

            migrationBuilder.CreateIndex(
                name: "IX_photos_category",
                table: "photos",
                column: "category");

            migrationBuilder.CreateIndex(
                name: "IX_photos_clinic_id",
                table: "photos",
                column: "clinic_id");

            migrationBuilder.CreateIndex(
                name: "IX_photos_doctor_id",
                table: "photos",
                column: "doctor_id");

            migrationBuilder.CreateIndex(
                name: "IX_photos_is_deleted",
                table: "photos",
                column: "is_deleted");

            migrationBuilder.CreateIndex(
                name: "IX_photos_patient_id",
                table: "photos",
                column: "patient_id");

            migrationBuilder.CreateIndex(
                name: "IX_referrals_clinic_id",
                table: "referrals",
                column: "clinic_id");

            migrationBuilder.CreateIndex(
                name: "IX_referrals_is_deleted",
                table: "referrals",
                column: "is_deleted");

            migrationBuilder.CreateIndex(
                name: "IX_referrals_referral_code",
                table: "referrals",
                column: "referral_code");

            migrationBuilder.CreateIndex(
                name: "IX_referrals_referred_patient_id",
                table: "referrals",
                column: "referred_patient_id");

            migrationBuilder.CreateIndex(
                name: "IX_referrals_referrer_patient_id",
                table: "referrals",
                column: "referrer_patient_id");

            migrationBuilder.CreateIndex(
                name: "IX_staff_BranchId",
                table: "staff",
                column: "BranchId");

            migrationBuilder.CreateIndex(
                name: "IX_staff_clinic_id",
                table: "staff",
                column: "clinic_id");

            migrationBuilder.CreateIndex(
                name: "IX_staff_is_deleted",
                table: "staff",
                column: "is_deleted");

            migrationBuilder.CreateIndex(
                name: "IX_staff_user_id",
                table: "staff",
                column: "user_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_subscriptions_clinic_id",
                table: "subscriptions",
                column: "clinic_id");

            migrationBuilder.CreateIndex(
                name: "IX_subscriptions_is_deleted",
                table: "subscriptions",
                column: "is_deleted");

            migrationBuilder.CreateIndex(
                name: "IX_treatment_visits_appointment_id",
                table: "treatment_visits",
                column: "appointment_id");

            migrationBuilder.CreateIndex(
                name: "IX_treatment_visits_clinic_id",
                table: "treatment_visits",
                column: "clinic_id");

            migrationBuilder.CreateIndex(
                name: "IX_treatment_visits_doctor_id",
                table: "treatment_visits",
                column: "doctor_id");

            migrationBuilder.CreateIndex(
                name: "IX_treatment_visits_is_deleted",
                table: "treatment_visits",
                column: "is_deleted");

            migrationBuilder.CreateIndex(
                name: "IX_treatment_visits_patient_id",
                table: "treatment_visits",
                column: "patient_id");

            migrationBuilder.AddForeignKey(
                name: "FK_application_users_doctors_DoctorProfileId",
                table: "application_users",
                column: "DoctorProfileId",
                principalTable: "doctors",
                principalColumn: "id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_application_users_branches_branch_id",
                table: "application_users");

            migrationBuilder.DropForeignKey(
                name: "FK_application_users_clinics_clinic_id",
                table: "application_users");

            migrationBuilder.DropForeignKey(
                name: "FK_doctors_clinics_clinic_id",
                table: "doctors");

            migrationBuilder.DropForeignKey(
                name: "FK_application_users_doctors_DoctorProfileId",
                table: "application_users");

            migrationBuilder.DropTable(
                name: "AspNetRoleClaims");

            migrationBuilder.DropTable(
                name: "AspNetUserClaims");

            migrationBuilder.DropTable(
                name: "AspNetUserLogins");

            migrationBuilder.DropTable(
                name: "AspNetUserRoles");

            migrationBuilder.DropTable(
                name: "AspNetUserTokens");

            migrationBuilder.DropTable(
                name: "audit_logs");

            migrationBuilder.DropTable(
                name: "BranchDoctor");

            migrationBuilder.DropTable(
                name: "chat_messages");

            migrationBuilder.DropTable(
                name: "documents");

            migrationBuilder.DropTable(
                name: "emergency_requests");

            migrationBuilder.DropTable(
                name: "feedbacks");

            migrationBuilder.DropTable(
                name: "inventory_items");

            migrationBuilder.DropTable(
                name: "notifications");

            migrationBuilder.DropTable(
                name: "pain_reports");

            migrationBuilder.DropTable(
                name: "payments");

            migrationBuilder.DropTable(
                name: "photos");

            migrationBuilder.DropTable(
                name: "referrals");

            migrationBuilder.DropTable(
                name: "staff");

            migrationBuilder.DropTable(
                name: "subscriptions");

            migrationBuilder.DropTable(
                name: "treatment_visits");

            migrationBuilder.DropTable(
                name: "AspNetRoles");

            migrationBuilder.DropTable(
                name: "AspNetUsers");

            migrationBuilder.DropTable(
                name: "invoices");

            migrationBuilder.DropTable(
                name: "appointments");

            migrationBuilder.DropTable(
                name: "patients");

            migrationBuilder.DropTable(
                name: "branches");

            migrationBuilder.DropTable(
                name: "clinics");

            migrationBuilder.DropTable(
                name: "doctors");

            migrationBuilder.DropTable(
                name: "application_users");
        }
    }
}

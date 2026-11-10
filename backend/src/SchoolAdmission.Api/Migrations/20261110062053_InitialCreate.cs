using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SchoolAdmission.Api.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "classes",
                columns: table => new
                {
                    id = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    total_seats = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_classes", x => x.id);
                    table.CheckConstraint("ck_classes_name_not_empty", "length(trim(\"name\")) > 0");
                    table.CheckConstraint("ck_classes_total_seats_positive", "\"total_seats\" > 0");
                });

            migrationBuilder.CreateTable(
                name: "seat_holds",
                columns: table => new
                {
                    id = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    class_id = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    student_id = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    expires_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_seat_holds", x => x.id);
                    table.CheckConstraint("ck_seat_holds_status", "\"status\" IN ('HELD', 'CONFIRMED', 'EXPIRED', 'CANCELLED')");
                    table.CheckConstraint("ck_seat_holds_student_id_format", "\"student_id\" ~ '^[0-9]{17}$'");
                    table.ForeignKey(
                        name: "FK_seat_holds_classes_class_id",
                        column: x => x.class_id,
                        principalTable: "classes",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_classes_created_at",
                table: "classes",
                column: "created_at");

            migrationBuilder.CreateIndex(
                name: "ix_seat_holds_class_id",
                table: "seat_holds",
                column: "class_id");

            migrationBuilder.CreateIndex(
                name: "ix_seat_holds_class_status_expires",
                table: "seat_holds",
                columns: new[] { "class_id", "status", "expires_at" });

            migrationBuilder.CreateIndex(
                name: "ix_seat_holds_student_class",
                table: "seat_holds",
                columns: new[] { "student_id", "class_id" });

            migrationBuilder.CreateIndex(
                name: "ix_seat_holds_sweeper",
                table: "seat_holds",
                column: "expires_at",
                filter: "\"status\" = 'HELD'");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "seat_holds");

            migrationBuilder.DropTable(
                name: "classes");
        }
    }
}

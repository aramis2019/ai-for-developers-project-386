using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Meetly.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "bookings",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    event_type_id = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    event_type_title = table.Column<string>(type: "text", nullable: false),
                    start_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    end_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    duration_minutes = table.Column<int>(type: "integer", nullable: false),
                    guest_name = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    guest_email = table.Column<string>(type: "character varying(254)", maxLength: 254, nullable: false),
                    guest_note = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_bookings", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "event_types",
                columns: table => new
                {
                    id = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    title = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    duration_minutes = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_event_types", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_bookings_event_type_id",
                table: "bookings",
                column: "event_type_id");

            migrationBuilder.CreateIndex(
                name: "IX_bookings_start_at",
                table: "bookings",
                column: "start_at");

            // ADR 0001: сквозная занятость обеспечивается на уровне БД.
            //
            // 1. Добавляем generated tstzrange-колонку "during" из полей start_at/end_at
            //    (полуинтервал '[)' — как в домене).
            // 2. Строим GiST-индекс по этой колонке.
            // 3. Exclusion constraint: EXCLUDE USING gist (during WITH &&) —
            //    Postgres физически не даст вставить пересекающийся интервал,
            //    независимо от event_type_id. При нарушении возвращает SqlState 23P01,
            //    который EfBookingRepository переводит в AddBookingResult.Conflict.
            //
            // btree_gist здесь не требуется: одиночная колонка tstzrange поддерживается
            // gist без расширения. Расширение понадобится, если добавим композитный
            // constraint (например, с owner_id).
            migrationBuilder.Sql("""
                ALTER TABLE bookings
                    ADD COLUMN during tstzrange
                    GENERATED ALWAYS AS (tstzrange(start_at, end_at, '[)')) STORED;
                """);

            migrationBuilder.Sql("""
                ALTER TABLE bookings
                    ADD CONSTRAINT bookings_no_overlap
                    EXCLUDE USING gist (during WITH &&);
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Constraint и generated column уходят вместе с таблицей, но явно снимаем
            // на случай, если DropTable будет менять поведение.
            migrationBuilder.Sql("ALTER TABLE bookings DROP CONSTRAINT IF EXISTS bookings_no_overlap;");
            migrationBuilder.Sql("ALTER TABLE bookings DROP COLUMN IF EXISTS during;");

            migrationBuilder.DropTable(
                name: "bookings");

            migrationBuilder.DropTable(
                name: "event_types");
        }
    }
}

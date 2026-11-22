using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace McpLab.Context.Persistence.Migrations;

/// <inheritdoc />
public partial class _20261122232222_CreateContextSchema : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.EnsureSchema(
            name: "context");

        // Soma chave a chave dois objetos JSON de números; usada no resumo semanal (weekly_rollups.metrics).
        migrationBuilder.Sql("""
            CREATE FUNCTION context.jsonb_sum(a jsonb, b jsonb) RETURNS jsonb
            LANGUAGE sql IMMUTABLE AS $$
                SELECT CASE WHEN a IS NULL AND b IS NULL THEN NULL ELSE COALESCE(
                    (SELECT jsonb_object_agg(k, COALESCE((a ->> k)::numeric, 0) + COALESCE((b ->> k)::numeric, 0))
                     FROM (SELECT jsonb_object_keys(COALESCE(a, '{}'::jsonb)) AS k
                           UNION SELECT jsonb_object_keys(COALESCE(b, '{}'::jsonb))) AS keys),
                    '{}'::jsonb) END
            $$;
            """);

        migrationBuilder.CreateTable(
            name: "clients",
            schema: "context",
            columns: table => new
            {
                client_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                name = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                fetched_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_clients", x => x.client_id);
            });

        migrationBuilder.CreateTable(
            name: "events",
            schema: "context",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                user_id = table.Column<Guid>(type: "uuid", nullable: false),
                occurred_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                module = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                tool = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                client_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                summary = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                entity_ref = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                metrics = table.Column<string>(type: "jsonb", nullable: true),
                idempotency_key = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_events", x => x.id);
            });

        migrationBuilder.CreateTable(
            name: "notes",
            schema: "context",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                user_id = table.Column<Guid>(type: "uuid", nullable: false),
                kind = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                text = table.Column<string>(type: "character varying(280)", maxLength: 280, nullable: false),
                module = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: true),
                status = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                pinned = table.Column<bool>(type: "boolean", nullable: false),
                client_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_notes", x => x.id);
            });

        migrationBuilder.CreateTable(
            name: "weekly_rollups",
            schema: "context",
            columns: table => new
            {
                user_id = table.Column<Guid>(type: "uuid", nullable: false),
                module = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                iso_week = table.Column<string>(type: "character varying(8)", maxLength: 8, nullable: false),
                event_count = table.Column<int>(type: "integer", nullable: false),
                metrics = table.Column<string>(type: "jsonb", nullable: true),
                updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_weekly_rollups", x => new { x.user_id, x.module, x.iso_week });
            });

        migrationBuilder.CreateIndex(
            name: "ix_events_user_id_occurred_at",
            schema: "context",
            table: "events",
            columns: new[] { "user_id", "occurred_at" },
            descending: new[] { false, true });

        migrationBuilder.CreateIndex(
            name: "ix_events_user_id_tool_idempotency_key",
            schema: "context",
            table: "events",
            columns: new[] { "user_id", "tool", "idempotency_key" },
            unique: true,
            filter: "idempotency_key IS NOT NULL");

        migrationBuilder.CreateIndex(
            name: "ix_notes_user_id_status_pinned_updated_at",
            schema: "context",
            table: "notes",
            columns: new[] { "user_id", "status", "pinned", "updated_at" });
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("DROP FUNCTION context.jsonb_sum(jsonb, jsonb);");

        migrationBuilder.DropTable(
            name: "clients",
            schema: "context");

        migrationBuilder.DropTable(
            name: "events",
            schema: "context");

        migrationBuilder.DropTable(
            name: "notes",
            schema: "context");

        migrationBuilder.DropTable(
            name: "weekly_rollups",
            schema: "context");
    }
}

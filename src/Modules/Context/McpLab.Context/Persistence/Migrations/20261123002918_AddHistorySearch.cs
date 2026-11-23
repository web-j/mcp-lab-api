using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace McpLab.Context.Persistence.Migrations;

/// <inheritdoc />
public partial class _20261123002918_AddHistorySearch : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // Busca em português sem diferenciar acentos (context_search_history). unaccent é extensão "trusted" no
        // Postgres 13+, então o dono do banco pode criar. O wrapper IMMUTABLE permite usar em coluna gerada e índice.
        migrationBuilder.Sql("""
            CREATE EXTENSION IF NOT EXISTS unaccent;

            CREATE FUNCTION context.f_unaccent(text) RETURNS text
            LANGUAGE sql IMMUTABLE PARALLEL SAFE STRICT AS $$ SELECT public.unaccent('public.unaccent', $1) $$;

            ALTER TABLE context.events ADD COLUMN search tsvector
                GENERATED ALWAYS AS (to_tsvector('portuguese', context.f_unaccent(summary))) STORED;
            CREATE INDEX ix_events_search ON context.events USING gin (search);

            ALTER TABLE context.notes ADD COLUMN search tsvector
                GENERATED ALWAYS AS (to_tsvector('portuguese', context.f_unaccent(text))) STORED;
            CREATE INDEX ix_notes_search ON context.notes USING gin (search);
            CREATE INDEX ix_notes_user_id_updated_at ON context.notes (user_id, updated_at DESC);
            """);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            DROP INDEX context.ix_notes_user_id_updated_at;
            DROP INDEX context.ix_notes_search;
            ALTER TABLE context.notes DROP COLUMN search;
            DROP INDEX context.ix_events_search;
            ALTER TABLE context.events DROP COLUMN search;
            DROP FUNCTION context.f_unaccent(text);
            """);
    }
}

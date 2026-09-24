using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace PedagoraPilot.Infrastructure.Persistence.Migrations.PedagoraPilot;

[DbContext(typeof(PedagoraPilotDbContext))]
[Migration("20260924133000_FixOutboxRowVersionPostgreSql")]
public sealed class FixOutboxRowVersionPostgreSql : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            CREATE OR REPLACE FUNCTION integration.set_outbox_row_version()
            RETURNS trigger
            LANGUAGE plpgsql
            AS $$
            BEGIN
                NEW."RowVersion" :=
                    decode(
                        md5(
                            clock_timestamp()::text
                            || random()::text
                            || NEW."Id"::text
                        ),
                        'hex'
                    );

                RETURN NEW;
            END;
            $$;
            """);

        migrationBuilder.Sql(
            """
            DROP TRIGGER IF EXISTS trg_outbox_row_version
            ON integration.outbox_messages;
            """);

        migrationBuilder.Sql(
            """
            CREATE TRIGGER trg_outbox_row_version
            BEFORE INSERT OR UPDATE
            ON integration.outbox_messages
            FOR EACH ROW
            EXECUTE FUNCTION integration.set_outbox_row_version();
            """);

        // Defensive backfill for databases where rows may have been manually inserted.
        migrationBuilder.Sql(
            """
            UPDATE integration.outbox_messages
            SET "RowVersion" =
                decode(
                    md5(
                        clock_timestamp()::text
                        || random()::text
                        || "Id"::text
                    ),
                    'hex'
                )
            WHERE "RowVersion" IS NULL
               OR octet_length("RowVersion") = 0;
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            DROP TRIGGER IF EXISTS trg_outbox_row_version
            ON integration.outbox_messages;
            """);

        migrationBuilder.Sql(
            """
            DROP FUNCTION IF EXISTS integration.set_outbox_row_version();
            """);
    }
}

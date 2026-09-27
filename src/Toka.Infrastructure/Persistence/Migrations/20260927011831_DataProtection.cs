using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Toka.Infrastructure.Persistence.Migrations
{
    /// <summary>
    /// Protects evidence data (orders, payment attempts, audit log) at the database level.
    /// Blocked by default; members of <c>toka_corrections</c> can apply emergency fixes by declaring a ticket
    /// and reason, and every fix is recorded in <c>data_corrections</c>. See docs/runbooks/correccion-de-datos.md.
    /// </summary>
    public partial class DataProtection : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Group roles. In Docker they already exist (created by the init script); elsewhere (tests) they are created here.
            migrationBuilder.Sql("""
                DO $$
                BEGIN
                    IF NOT EXISTS (SELECT FROM pg_roles WHERE rolname = 'toka_app_role') THEN
                        CREATE ROLE toka_app_role NOLOGIN;
                    END IF;
                    IF NOT EXISTS (SELECT FROM pg_roles WHERE rolname = 'toka_corrections') THEN
                        CREATE ROLE toka_corrections NOLOGIN;
                    END IF;
                END $$;

                GRANT USAGE ON SCHEMA public TO toka_app_role, toka_corrections;
                GRANT SELECT, INSERT, UPDATE ON customers, products, orders TO toka_app_role;
                GRANT SELECT, INSERT ON payment_attempts, audit_events TO toka_app_role;
                GRANT SELECT, UPDATE, DELETE ON orders, payment_attempts, audit_events TO toka_corrections;
                """);

            migrationBuilder.Sql("""
                CREATE TABLE data_corrections (
                    id          bigint GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
                    occurred_at timestamptz NOT NULL DEFAULT now(),
                    db_user     text        NOT NULL,
                    table_name  text        NOT NULL,
                    operation   text        NOT NULL,
                    row_id      uuid        NOT NULL,
                    ticket      text        NOT NULL,
                    reason      text        NOT NULL,
                    old_data    jsonb       NOT NULL,
                    new_data    jsonb
                );
                CREATE INDEX ix_data_corrections_row_id ON data_corrections (row_id);
                GRANT SELECT ON data_corrections TO toka_corrections;

                -- Used where there is no bypass at all: the correction log itself and TRUNCATE.
                CREATE FUNCTION reject_change() RETURNS trigger
                LANGUAGE plpgsql AS $$
                BEGIN
                    RAISE EXCEPTION USING
                        ERRCODE = 'insufficient_privilege',
                        MESSAGE = format('La tabla %s está protegida; no se permite %s.', TG_TABLE_NAME, TG_OP);
                END;
                $$;

                -- SECURITY DEFINER so it can write data_corrections, which nobody else can insert into.
                -- session_user is the real login even inside SECURITY DEFINER or after SET ROLE.
                CREATE FUNCTION guard_protected_change() RETURNS trigger
                LANGUAGE plpgsql SECURITY DEFINER SET search_path = public, pg_temp AS $$
                DECLARE
                    v_ticket text := nullif(trim(current_setting('toka.correction_ticket', true)), '');
                    v_reason text := nullif(trim(current_setting('toka.correction_reason', true)), '');
                BEGIN
                    -- The app updates status/payment fields of an order; only origin data and amounts are frozen.
                    IF TG_TABLE_NAME = 'orders' AND TG_OP = 'UPDATE' AND
                       (NEW.id, NEW.customer_id, NEW.product_id, NEW.quantity, NEW.unit_price, NEW.subtotal,
                        NEW.tax_rate, NEW.tax_amount, NEW.total, NEW.currency, NEW.created_at, NEW.idempotency_key)
                       IS NOT DISTINCT FROM
                       (OLD.id, OLD.customer_id, OLD.product_id, OLD.quantity, OLD.unit_price, OLD.subtotal,
                        OLD.tax_rate, OLD.tax_amount, OLD.total, OLD.currency, OLD.created_at, OLD.idempotency_key) THEN
                        RETURN NEW;
                    END IF;

                    IF NOT pg_has_role(session_user, 'toka_corrections', 'MEMBER') THEN
                        RAISE EXCEPTION USING
                            ERRCODE = 'insufficient_privilege',
                            MESSAGE = format('No se permite %s en %s: los datos están protegidos.', TG_OP, TG_TABLE_NAME),
                            HINT = 'Las correcciones de emergencia requieren el rol toka_corrections.';
                    END IF;

                    IF v_ticket IS NULL OR v_reason IS NULL OR length(v_reason) < 10 THEN
                        RAISE EXCEPTION USING
                            ERRCODE = 'insufficient_privilege',
                            MESSAGE = 'Corrección rechazada: falta el ticket o el motivo.',
                            HINT = 'Dentro de la transacción ejecuta SET LOCAL toka.correction_ticket y SET LOCAL toka.correction_reason (mínimo 10 caracteres).';
                    END IF;

                    INSERT INTO data_corrections (db_user, table_name, operation, row_id, ticket, reason, old_data, new_data)
                    VALUES (session_user, TG_TABLE_NAME, TG_OP, OLD.id, v_ticket, v_reason, to_jsonb(OLD),
                            CASE WHEN TG_OP = 'UPDATE' THEN to_jsonb(NEW) END);

                    IF TG_OP = 'DELETE' THEN
                        RETURN OLD;
                    END IF;
                    RETURN NEW;
                END;
                $$;
                REVOKE ALL ON FUNCTION guard_protected_change() FROM PUBLIC;

                CREATE TRIGGER trg_orders_protected
                    BEFORE UPDATE OR DELETE ON orders
                    FOR EACH ROW EXECUTE FUNCTION guard_protected_change();
                CREATE TRIGGER trg_payment_attempts_protected
                    BEFORE UPDATE OR DELETE ON payment_attempts
                    FOR EACH ROW EXECUTE FUNCTION guard_protected_change();
                CREATE TRIGGER trg_audit_events_protected
                    BEFORE UPDATE OR DELETE ON audit_events
                    FOR EACH ROW EXECUTE FUNCTION guard_protected_change();

                CREATE TRIGGER trg_data_corrections_append_only
                    BEFORE UPDATE OR DELETE ON data_corrections
                    FOR EACH ROW EXECUTE FUNCTION reject_change();

                CREATE TRIGGER trg_orders_no_truncate BEFORE TRUNCATE ON orders
                    FOR EACH STATEMENT EXECUTE FUNCTION reject_change();
                CREATE TRIGGER trg_payment_attempts_no_truncate BEFORE TRUNCATE ON payment_attempts
                    FOR EACH STATEMENT EXECUTE FUNCTION reject_change();
                CREATE TRIGGER trg_audit_events_no_truncate BEFORE TRUNCATE ON audit_events
                    FOR EACH STATEMENT EXECUTE FUNCTION reject_change();
                CREATE TRIGGER trg_data_corrections_no_truncate BEFORE TRUNCATE ON data_corrections
                    FOR EACH STATEMENT EXECUTE FUNCTION reject_change();
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Roles are cluster-wide and may be shared, so they are kept; only this database's grants are revoked.
            migrationBuilder.Sql("""
                DROP TRIGGER trg_data_corrections_no_truncate ON data_corrections;
                DROP TRIGGER trg_audit_events_no_truncate ON audit_events;
                DROP TRIGGER trg_payment_attempts_no_truncate ON payment_attempts;
                DROP TRIGGER trg_orders_no_truncate ON orders;
                DROP TRIGGER trg_data_corrections_append_only ON data_corrections;
                DROP TRIGGER trg_audit_events_protected ON audit_events;
                DROP TRIGGER trg_payment_attempts_protected ON payment_attempts;
                DROP TRIGGER trg_orders_protected ON orders;
                DROP FUNCTION guard_protected_change();
                DROP FUNCTION reject_change();
                DROP TABLE data_corrections;

                REVOKE ALL ON customers, products, orders, payment_attempts, audit_events FROM toka_app_role, toka_corrections;
                REVOKE USAGE ON SCHEMA public FROM toka_app_role, toka_corrections;
                """);
        }
    }
}

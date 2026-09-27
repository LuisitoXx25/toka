using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Toka.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Installments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "installments",
                table: "orders",
                type: "integer",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddCheckConstraint(
                name: "ck_orders_installments_positive",
                table: "orders",
                sql: "installments >= 1");

            // The installment plan is part of the charge terms, so it joins the columns frozen after creation.
            migrationBuilder.Sql("""
                CREATE OR REPLACE FUNCTION guard_protected_change() RETURNS trigger
                LANGUAGE plpgsql SECURITY DEFINER SET search_path = public, pg_temp AS $$
                DECLARE
                    v_ticket text := nullif(trim(current_setting('toka.correction_ticket', true)), '');
                    v_reason text := nullif(trim(current_setting('toka.correction_reason', true)), '');
                BEGIN
                    -- The app updates status/payment fields of an order; only origin data and amounts are frozen.
                    IF TG_TABLE_NAME = 'orders' AND TG_OP = 'UPDATE' AND
                       (NEW.id, NEW.customer_id, NEW.product_id, NEW.quantity, NEW.unit_price, NEW.subtotal,
                        NEW.tax_rate, NEW.tax_amount, NEW.total, NEW.currency, NEW.created_at, NEW.idempotency_key,
                        NEW.installments)
                       IS NOT DISTINCT FROM
                       (OLD.id, OLD.customer_id, OLD.product_id, OLD.quantity, OLD.unit_price, OLD.subtotal,
                        OLD.tax_rate, OLD.tax_amount, OLD.total, OLD.currency, OLD.created_at, OLD.idempotency_key,
                        OLD.installments) THEN
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
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                CREATE OR REPLACE FUNCTION guard_protected_change() RETURNS trigger
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
                """);

            migrationBuilder.DropCheckConstraint(
                name: "ck_orders_installments_positive",
                table: "orders");

            migrationBuilder.DropColumn(
                name: "installments",
                table: "orders");
        }
    }
}

#!/bin/sh
# Runs once, when the Postgres volume is created. Separates who owns the schema from who uses it:
#   toka_owner       -> owns the database; used only to run migrations.
#   toka_app         -> used by the API; read/insert/update only, no DDL, cannot run emergency corrections.
#   toka_corrections -> no login; granted temporarily to a named DBA during an incident.
set -eu

psql -v ON_ERROR_STOP=1 --username "$POSTGRES_USER" --dbname postgres <<-SQL
    CREATE ROLE toka_app_role NOLOGIN;
    CREATE ROLE toka_corrections NOLOGIN;
    CREATE ROLE toka_owner LOGIN PASSWORD '${TOKA_OWNER_PASSWORD}';
    CREATE ROLE toka_app LOGIN PASSWORD '${TOKA_APP_PASSWORD}' IN ROLE toka_app_role;
    CREATE DATABASE ${TOKA_DB} OWNER toka_owner;
    REVOKE ALL ON DATABASE ${TOKA_DB} FROM PUBLIC;
    GRANT CONNECT ON DATABASE ${TOKA_DB} TO toka_app_role, toka_corrections;
SQL

psql -v ON_ERROR_STOP=1 --username "$POSTGRES_USER" --dbname "$TOKA_DB" <<-SQL
    REVOKE CREATE ON SCHEMA public FROM PUBLIC;
    ALTER SCHEMA public OWNER TO toka_owner;
SQL

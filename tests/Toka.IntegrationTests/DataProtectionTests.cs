using Npgsql;

namespace Toka.IntegrationTests;

/// <summary>Database-level protections and the emergency correction path (docs/runbooks/correccion-de-datos.md).</summary>
[Collection(ApiCollection.Name)]
public class DataProtectionTests(ApiFactory factory)
{
    private const string InsufficientPrivilege = "42501";

    private async Task<Guid> PlacePaidOrderAsync()
    {
        var order = await (await factory.CreateAuthenticatedClient().PlaceAsync(Checkout.Order())).JsonAsync();
        return Guid.Parse((string)order["id"]!);
    }

    private static async Task<int> ExecuteAsync(string connectionString, string sql, Guid orderId)
    {
        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync();
        await using var command = new NpgsqlCommand(sql, connection) { Parameters = { new("id", orderId) } };
        return await command.ExecuteNonQueryAsync();
    }

    [Fact]
    public async Task App_role_cannot_change_order_amounts()
    {
        var orderId = await PlacePaidOrderAsync();

        var ex = await Assert.ThrowsAsync<PostgresException>(() => ExecuteAsync(factory.AppConnectionString,
            "UPDATE orders SET quantity = 5 WHERE id = @id", orderId));

        Assert.Equal(InsufficientPrivilege, ex.SqlState);
        Assert.Contains("los datos están protegidos", ex.MessageText);
    }

    [Fact]
    public async Task App_role_cannot_tamper_with_the_audit_trail()
    {
        var orderId = await PlacePaidOrderAsync();

        var ex = await Assert.ThrowsAsync<PostgresException>(() => ExecuteAsync(factory.AppConnectionString,
            "DELETE FROM audit_events WHERE entity_id = @id", orderId));

        Assert.Equal(InsufficientPrivilege, ex.SqlState);
    }

    [Fact]
    public async Task Correction_role_needs_ticket_and_reason_and_every_fix_is_logged()
    {
        var orderId = await PlacePaidOrderAsync();
        var dba = $"dba_{Guid.NewGuid():N}"[..20];
        await factory.ExecuteAsOwnerAsync($"CREATE ROLE {dba} LOGIN PASSWORD 'dba-pw' IN ROLE toka_corrections;");
        var dbaConnection = factory.ConnectionStringFor(dba, "dba-pw");

        var withoutTicket = await Assert.ThrowsAsync<PostgresException>(() => ExecuteAsync(dbaConnection,
            "UPDATE orders SET quantity = 2, unit_price = 1749.50 WHERE id = @id", orderId));
        Assert.Contains("falta el ticket o el motivo", withoutTicket.MessageText);

        var updated = await ExecuteAsync(dbaConnection, """
            BEGIN;
            SET LOCAL toka.correction_ticket = 'INC-TEST-1';
            SET LOCAL toka.correction_reason = 'Cantidad mal capturada en prueba';
            UPDATE orders SET quantity = 2, unit_price = 1749.50 WHERE id = @id;
            COMMIT;
            """, orderId);
        Assert.Equal(1, updated);

        await using var connection = new NpgsqlConnection(dbaConnection);
        await connection.OpenAsync();
        await using var query = new NpgsqlCommand(
            "SELECT db_user, ticket, old_data->>'quantity', new_data->>'quantity' FROM data_corrections WHERE row_id = @id", connection)
            { Parameters = { new("id", orderId) } };
        await using var reader = await query.ExecuteReaderAsync();
        Assert.True(await reader.ReadAsync());
        Assert.Equal((dba, "INC-TEST-1", "1", "2"), (reader.GetString(0), reader.GetString(1), reader.GetString(2), reader.GetString(3)));
    }
}

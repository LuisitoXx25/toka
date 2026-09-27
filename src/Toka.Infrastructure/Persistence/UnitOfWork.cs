using Microsoft.EntityFrameworkCore;
using Toka.Application.Abstractions;
using Toka.Application.Common;

namespace Toka.Infrastructure.Persistence;

internal sealed class UnitOfWork(AppDbContext db) : IUnitOfWork
{
    public async Task SaveChangesAsync(CancellationToken ct)
    {
        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateConcurrencyException ex)
        {
            // Drop the stale changes so the scoped context is not left in a broken state.
            db.ChangeTracker.Clear();
            throw new ConcurrencyConflictException("Concurrent update detected.", ex);
        }
    }
}

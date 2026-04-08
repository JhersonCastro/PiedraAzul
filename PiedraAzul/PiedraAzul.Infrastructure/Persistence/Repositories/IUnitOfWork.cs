using Microsoft.EntityFrameworkCore;
using PiedraAzul.Domain.Repositories;
using System;
using System.Collections.Generic;
using System.Text;

namespace PiedraAzul.Infrastructure.Persistence.Repositories
{
    public class UnitOfWork(AppDbContext context) : IUnitOfWork
    {
        public async Task<TResult> ExecuteAsync<TResult>(
            Func<CancellationToken, Task<TResult>> action,
            CancellationToken ct = default)
        {
            var strategy = context.Database.CreateExecutionStrategy();

            return await strategy.ExecuteAsync(async () =>
            {
                var hasTransaction = context.Database.CurrentTransaction != null;

                // 🔁 Ya hay transacción → solo ejecuta + guarda
                if (hasTransaction)
                {
                    var result = await action(ct);
                    await context.SaveChangesAsync(ct);
                    return result;
                }

                // 🧱 Nueva transacción
                await using var transaction = await context.Database.BeginTransactionAsync(ct);

                try
                {
                    var result = await action(ct);

                    await context.SaveChangesAsync(ct);
                    await transaction.CommitAsync(ct);

                    return result;
                }
                catch
                {
                    try
                    {
                        await transaction.RollbackAsync(ct);
                    }
                    catch
                    {
                        // opcional: logging
                    }

                    throw;
                }
            });
        }
    }
}

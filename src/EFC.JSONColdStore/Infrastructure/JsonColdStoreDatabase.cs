using System.Linq.Expressions;
using EFC.JSONColdStore.Storage;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Query;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.EntityFrameworkCore.Update;

namespace EFC.JSONColdStore.Infrastructure;

internal sealed class JsonColdStoreDatabase : IDatabase
{
    private readonly JsonColdStoreOptions _options;

    public JsonColdStoreDatabase(IDbContextOptions contextOptions)
    {
        ArgumentNullException.ThrowIfNull(contextOptions);
        _options = contextOptions.FindExtension<JsonColdStoreOptionsExtension>()?.Options
            ?? throw new InvalidOperationException("JSONColdStore options are not configured.");
    }

    public int SaveChanges(IList<IUpdateEntry> entries)
    {
        return SaveChangesAsync(entries).GetAwaiter().GetResult();
    }

    public Task<int> SaveChangesAsync(
        IList<IUpdateEntry> entries,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(entries);
        return SaveChangesInternalAsync(entries, cancellationToken);
    }

    public Func<QueryContext, TResult> CompileQuery<TResult>(Expression query, bool async)
    {
        ArgumentNullException.ThrowIfNull(query);
        return _ => throw Unsupported("LINQ query execution is not implemented yet.");
    }

    public Expression<Func<QueryContext, TResult>> CompileQueryExpression<TResult>(Expression query, bool async)
    {
        ArgumentNullException.ThrowIfNull(query);

        var exception = Expression.New(
            typeof(NotSupportedException).GetConstructor([typeof(string)])!,
            Expression.Constant("LINQ query expression compilation is not implemented yet."));
        var body = Expression.Throw(exception, typeof(TResult));
        var parameter = Expression.Parameter(typeof(QueryContext), "queryContext");

        return Expression.Lambda<Func<QueryContext, TResult>>(body, parameter);
    }

    private static NotSupportedException Unsupported(string message) =>
        new("JSONColdStore EF provider support is incomplete: " + message);

    private async Task<int> SaveChangesInternalAsync(
        IList<IUpdateEntry> entries,
        CancellationToken cancellationToken)
    {
        if (entries.Count == 0)
            return 0;

        await using var session = await JsonColdStoreDatabaseSession.OpenAsync(
            _options,
            acquireWriterLock: true,
            cancellationToken);
        var modelDescriptor = JsonColdStoreModelDescriptor.Create(entries[0].Context.Model);
        var entityStore = new JsonColdStoreEntityRecordStore(session, modelDescriptor);
        var saved = 0;

        foreach (var entry in entries)
        {
            cancellationToken.ThrowIfCancellationRequested();

            switch (entry.EntityState)
            {
                case EntityState.Added:
                case EntityState.Modified:
                    var entity = entry.ToEntityEntry().Entity;
                    await entityStore.WriteEntityAsync(
                        entity,
                        entry.EntityType.ClrType,
                        cancellationToken);
                    saved++;
                    break;

                case EntityState.Deleted:
                    throw Unsupported("Delete operations are not implemented yet.");

                default:
                    break;
            }
        }

        return saved;
    }
}

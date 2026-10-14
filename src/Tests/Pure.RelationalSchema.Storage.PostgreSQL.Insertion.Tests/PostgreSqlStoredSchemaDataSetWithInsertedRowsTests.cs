using Pure.RelationalSchema.Abstractions.Table;
using Pure.RelationalSchema.Storage.Abstractions;
using Pure.RelationalSchema.Storage.PostgreSQL.Insertion.Tests.Fakes;
using Pure.RelationalSchema.Storage.PostgreSQL.Querying;

namespace Pure.RelationalSchema.Storage.PostgreSQL.Insertion.Tests;

public sealed record PostgreSqlStoredSchemaDataSetWithInsertedRowsTests
    : IAsyncLifetime,
        IDisposable
{
    private DatabaseFixture? _fixture;

    [Fact]
    public void InsertMultipleRows()
    {
        IPostgreSqlStoredSchemaDataSet schema = new PostgreSqlStoredSchemaDataSet(
            _fixture!.Schema,
            _fixture.Connection
        );

        IStoredSchemaDataSet dataSetWithInsertedRows =
            new PostgreSqlStoredSchemaDataSetWithInsertedRows(
                schema,
                new TablesAndTheirRandomRows(schema.Keys)
            );

        Assert.Equal(200, dataSetWithInsertedRows.SelectMany(x => x.Value).Count());
    }

    [Fact]
    public void InsertOnlyDistinctRows()
    {
        IPostgreSqlStoredSchemaDataSet schema = new PostgreSqlStoredSchemaDataSet(
            _fixture!.Schema,
            _fixture.Connection
        );

        IStoredSchemaDataSet dataSetWithInsertedRows =
            new PostgreSqlStoredSchemaDataSetWithInsertedRows(
                schema,
                new TablesAndTheirDefaultRows(schema.Keys)
            );

        Assert.Equal(
            1,
            dataSetWithInsertedRows.Select(x => x.Value.Count()).Distinct().Single()
        );
    }

    [Fact]
    public void InsertOnlyDistinctRowsOnExisting()
    {
        IPostgreSqlStoredSchemaDataSet schema = new PostgreSqlStoredSchemaDataSet(
            _fixture!.Schema,
            _fixture.Connection
        );

        IEnumerable<IGrouping<ITable, IRow>> rows = new TablesAndTheirDefaultRows(
            schema.Keys
        );

        IPostgreSqlStoredSchemaDataSet dataSetWithInsertedRows =
            new PostgreSqlStoredSchemaDataSetWithInsertedRows(schema, rows);

        dataSetWithInsertedRows = new PostgreSqlStoredSchemaDataSetWithInsertedRows(
            dataSetWithInsertedRows,
            rows
        );

        Assert.Equal(
            1,
            dataSetWithInsertedRows.Select(x => x.Value.Count()).Distinct().Single()
        );
    }

    public void Dispose()
    {
        _fixture?.Dispose();
    }

    public Task DisposeAsync()
    {
        Dispose();
        return Task.CompletedTask;
    }

    public Task InitializeAsync()
    {
        _fixture = new DatabaseFixture();
        return Task.CompletedTask;
    }
}

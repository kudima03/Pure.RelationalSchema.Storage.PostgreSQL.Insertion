using System.Collections;
using System.Data;
using Pure.RelationalSchema.Abstractions.Table;
using Pure.RelationalSchema.Storage.Abstractions;
using Pure.RelationalSchema.Storage.PostgreSQL.Abstractions;
using Pure.RelationalSchema.Storage.PostgreSQL.Insertion.Tests.Fakes;

namespace Pure.RelationalSchema.Storage.PostgreSQL.Insertion.Tests;

public sealed record PostgreSqlStoredSchemaDataSetWithInsertedRowsMembersTests
    : IAsyncLifetime,
        IDisposable
{
    private const int ExistingRowsCount = 3;

    private DatabaseFixture? _fixture;

    [Fact]
    public void CountEqualsNumberOfSuppliedGroupings()
    {
        IPostgreSqlStoredSchemaDataSet schema = Schema();

        IPostgreSqlStoredSchemaDataSet dataSet =
            new PostgreSqlStoredSchemaDataSetWithInsertedRows(
                schema,
                new TablesAndTheirRandomRows(schema.Keys)
            );

        Assert.Equal(schema.Count, dataSet.Count);
    }

    [Fact]
    public void ExposesOnlyTablesFromSuppliedGroupings()
    {
        IPostgreSqlStoredSchemaDataSet schema = Schema();
        ITable included = schema.Keys.First();
        ITable excluded = schema.Keys.Last();

        IPostgreSqlStoredSchemaDataSet dataSet =
            new PostgreSqlStoredSchemaDataSetWithInsertedRows(
                schema,
                new TablesAndTheirRandomRows([included])
            );

        _ = Assert.Single(dataSet);
        Assert.Equal([included], dataSet.Keys);
        _ = Assert.Single(dataSet.Values);
        Assert.True(dataSet.ContainsKey(included));
        Assert.False(dataSet.ContainsKey(excluded));
        _ = Assert.ThrowsAny<Exception>(() => dataSet[excluded]);
    }

    [Fact]
    public void KeysAndValuesFollowSuppliedOrder()
    {
        IPostgreSqlStoredSchemaDataSet schema = Schema();

        IPostgreSqlStoredSchemaDataSet dataSet =
            new PostgreSqlStoredSchemaDataSetWithInsertedRows(
                schema,
                new TablesAndTheirRandomRows(schema.Keys)
            );

        Assert.Equal(schema.Keys, dataSet.Keys);
        Assert.Equal(dataSet.Count, dataSet.Values.Count());
        Assert.All(
            dataSet.Values.Zip(dataSet.Select(x => x.Value)),
            pair => Assert.Same(pair.First, pair.Second)
        );
    }

    [Fact]
    public void TryGetValueFindsSuppliedTable()
    {
        IPostgreSqlStoredSchemaDataSet schema = Schema();
        ITable table = schema.Keys.First();

        IPostgreSqlStoredSchemaDataSet dataSet =
            new PostgreSqlStoredSchemaDataSetWithInsertedRows(
                schema,
                new TablesAndTheirRandomRows([table])
            );

        Assert.True(dataSet.TryGetValue(table, out IStoredTableDataSet? value));
        Assert.Same(dataSet[table], value);
    }

    [Fact]
    public void TryGetValueDoesNotFindMissingTable()
    {
        IPostgreSqlStoredSchemaDataSet schema = Schema();

        IPostgreSqlStoredSchemaDataSet dataSet =
            new PostgreSqlStoredSchemaDataSetWithInsertedRows(
                schema,
                new TablesAndTheirRandomRows([schema.Keys.First()])
            );

        Assert.False(dataSet.TryGetValue(schema.Keys.Last(), out IStoredTableDataSet? _));
    }

    [Fact]
    public void ExposesConnectionAndSchemaOfWrappedDataSet()
    {
        IPostgreSqlStoredSchemaDataSet schema = Schema();

        IPostgreSqlStoredSchemaDataSet dataSet =
            new PostgreSqlStoredSchemaDataSetWithInsertedRows(
                schema,
                new TablesAndTheirRandomRows(schema.Keys)
            );

        Assert.Same(schema.Connection, dataSet.Connection);
        Assert.Same(schema.Schema, dataSet.Schema);
    }

    [Fact]
    public void NonGenericEnumerationYieldsAllPairs()
    {
        IPostgreSqlStoredSchemaDataSet schema = Schema();

        IEnumerable dataSet = new PostgreSqlStoredSchemaDataSetWithInsertedRows(
            schema,
            new TablesAndTheirRandomRows(schema.Keys)
        );

        Assert.Equal(schema.Count, dataSet.Cast<object>().Count());
    }

    [Fact]
    public void TableDataSetExposesWrappedTableSchema()
    {
        IPostgreSqlStoredSchemaDataSet schema = Schema();
        ITable table = schema.Keys.First();

        IPostgreSqlStoredSchemaDataSet dataSet =
            new PostgreSqlStoredSchemaDataSetWithInsertedRows(
                schema,
                new TablesAndTheirRandomRows([table])
            );

        Assert.Same(schema[table].TableSchema, dataSet[table].TableSchema);
    }

    [Fact]
    public void TableDataSetExposesWrappedElementType()
    {
        IPostgreSqlStoredSchemaDataSet schema = Schema();
        ITable table = schema.Keys.First();

        IPostgreSqlStoredSchemaDataSet dataSet =
            new PostgreSqlStoredSchemaDataSetWithInsertedRows(
                schema,
                new TablesAndTheirRandomRows([table])
            );

        Assert.Equal(schema[table].ElementType, dataSet[table].ElementType);
    }

    [Fact]
    public void TableDataSetExposesWrappedQueryableMembers()
    {
        IPostgreSqlStoredSchemaDataSet schema = Schema();
        ITable table = schema.Keys.First();

        IPostgreSqlStoredSchemaDataSet dataSet =
            new PostgreSqlStoredSchemaDataSetWithInsertedRows(
                schema,
                new TablesAndTheirRandomRows([table])
            );

        IStoredTableDataSet tableDataSet = dataSet[table];

        Assert.Same(schema[table].Expression, tableDataSet.Expression);
        Assert.Same(schema[table].Provider, tableDataSet.Provider);
        Assert.Equal(ExistingRowsCount, tableDataSet.AsQueryable().Count());
    }

    [Fact]
    public void TableDataSetEnumeratesWrappedRows()
    {
        IPostgreSqlStoredSchemaDataSet schema = Schema();
        ITable table = schema.Keys.First();

        IPostgreSqlStoredSchemaDataSet dataSet =
            new PostgreSqlStoredSchemaDataSetWithInsertedRows(
                schema,
                new TablesAndTheirRandomRows([table])
            );

        Assert.Equal(ExistingRowsCount, dataSet[table].AsEnumerable().Count());
    }

    [Fact]
    public void TableDataSetNonGenericEnumerationYieldsWrappedRows()
    {
        IPostgreSqlStoredSchemaDataSet schema = Schema();
        ITable table = schema.Keys.First();

        IPostgreSqlStoredSchemaDataSet dataSet =
            new PostgreSqlStoredSchemaDataSetWithInsertedRows(
                schema,
                new TablesAndTheirRandomRows([table])
            );

        IEnumerable tableDataSet = dataSet[table];

        Assert.Equal(ExistingRowsCount, tableDataSet.Cast<object>().Count());
    }

    [Fact]
    public async Task TableDataSetEnumeratesWrappedRowsAsynchronously()
    {
        IPostgreSqlStoredSchemaDataSet schema = Schema();
        ITable table = schema.Keys.First();

        IPostgreSqlStoredSchemaDataSet dataSet =
            new PostgreSqlStoredSchemaDataSetWithInsertedRows(
                schema,
                new TablesAndTheirRandomRows([table])
            );

        IRow[] rows = await dataSet[table].ToArrayAsync();

        Assert.Equal(ExistingRowsCount, rows.Length);
    }

    [Fact]
    public async Task AsynchronousEnumerationInsertsRows()
    {
        IPostgreSqlStoredSchemaDataSet schema = Schema();
        ITable table = schema.Keys.First();

        IPostgreSqlStoredSchemaDataSet dataSet =
            new PostgreSqlStoredSchemaDataSetWithInsertedRows(
                schema,
                new TablesAndTheirRandomRows([table])
            );

        _ = await dataSet[table].ToArrayAsync();

        Assert.Equal(100, _fixture!.RowCount(table));
    }

    [Fact]
    public void QueryableMembersInsertRows()
    {
        IPostgreSqlStoredSchemaDataSet schema = Schema();
        ITable table = schema.Keys.First();

        IPostgreSqlStoredSchemaDataSet dataSet =
            new PostgreSqlStoredSchemaDataSetWithInsertedRows(
                schema,
                new TablesAndTheirRandomRows([table])
            );

        _ = dataSet[table].Provider;

        Assert.Equal(100, _fixture!.RowCount(table));
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

    private FakeStoredSchemaDataSet Schema()
    {
        return new FakeStoredSchemaDataSet(
            _fixture!.Schema,
            _fixture.Connection,
            table => [.. new DefaultRowsForTable(table).Take(ExistingRowsCount)]
        );
    }
}

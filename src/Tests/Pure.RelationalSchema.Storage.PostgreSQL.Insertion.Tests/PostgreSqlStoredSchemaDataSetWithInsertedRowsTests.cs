using Pure.HashCodes.Abstractions;
using Pure.Primitives.String.Operations;
using Pure.RelationalSchema.Abstractions.Column;
using Pure.RelationalSchema.Abstractions.Table;
using Pure.RelationalSchema.ColumnType;
using Pure.RelationalSchema.HashCodes;
using Pure.RelationalSchema.Storage.Abstractions;
using Pure.RelationalSchema.Storage.PostgreSQL.Abstractions;
using Pure.RelationalSchema.Storage.PostgreSQL.Insertion.Tests.Fakes;
using String = Pure.Primitives.String.String;

namespace Pure.RelationalSchema.Storage.PostgreSQL.Insertion.Tests;

public sealed record PostgreSqlStoredSchemaDataSetWithInsertedRowsTests
    : IAsyncLifetime,
        IDisposable
{
    private DatabaseFixture? _fixture;

    [Fact]
    public void InsertMultipleRows()
    {
        IPostgreSqlStoredSchemaDataSet schema = EmptySchema();

        IPostgreSqlStoredSchemaDataSet dataSet =
            new PostgreSqlStoredSchemaDataSetWithInsertedRows(
                schema,
                new TablesAndTheirRandomRows(schema.Keys)
            );

        Touch(dataSet);

        Assert.Equal(200, schema.Keys.Sum(_fixture!.RowCount));
    }

    [Fact]
    public void InsertOnlyDistinctRows()
    {
        IPostgreSqlStoredSchemaDataSet schema = EmptySchema();

        IPostgreSqlStoredSchemaDataSet dataSet =
            new PostgreSqlStoredSchemaDataSetWithInsertedRows(
                schema,
                new TablesAndTheirDefaultRows(schema.Keys)
            );

        Touch(dataSet);

        Assert.All(schema.Keys, table => Assert.Equal(1, _fixture!.RowCount(table)));
    }

    [Fact]
    public void SkipRowsThatAlreadyExist()
    {
        IPostgreSqlStoredSchemaDataSet schema = new FakeStoredSchemaDataSet(
            _fixture!.Schema,
            _fixture.Connection,
            table => new DefaultRowsForTable(table).Take(1)
        );

        IPostgreSqlStoredSchemaDataSet dataSet =
            new PostgreSqlStoredSchemaDataSetWithInsertedRows(
                schema,
                new TablesAndTheirDefaultRows(schema.Keys)
            );

        Touch(dataSet);

        Assert.All(schema.Keys, table => Assert.Equal(0, _fixture.RowCount(table)));
    }

    [Fact]
    public void InsertOnlyRowsMissingFromExisting()
    {
        IPostgreSqlStoredSchemaDataSet schema = new FakeStoredSchemaDataSet(
            _fixture!.Schema,
            _fixture.Connection,
            table => new DefaultRowsForTable(table).Take(1)
        );

        IPostgreSqlStoredSchemaDataSet dataSet =
            new PostgreSqlStoredSchemaDataSetWithInsertedRows(
                schema,
                new TablesAndTheirRandomRows(schema.Keys)
            );

        Touch(dataSet);

        Assert.Equal(200, schema.Keys.Sum(_fixture.RowCount));
    }

    [Fact]
    public void InsertsOnlyOnce()
    {
        IPostgreSqlStoredSchemaDataSet schema = EmptySchema();
        ITable table = schema.Keys.First();

        IPostgreSqlStoredSchemaDataSet dataSet =
            new PostgreSqlStoredSchemaDataSetWithInsertedRows(
                schema,
                new TablesAndTheirDefaultRows([table])
            );

        _ = dataSet[table].TableSchema;
        _ = dataSet[table].ElementType;
        _ = dataSet[table].AsEnumerable().Count();

        Assert.Equal(1, _fixture!.RowCount(table));
    }

    [Fact]
    public void DoesNotInsertBeforeFirstAccess()
    {
        IPostgreSqlStoredSchemaDataSet schema = EmptySchema();

        _ = new PostgreSqlStoredSchemaDataSetWithInsertedRows(
            schema,
            new TablesAndTheirDefaultRows(schema.Keys)
        );

        Assert.All(schema.Keys, table => Assert.Equal(0, _fixture!.RowCount(table)));
    }

    [Fact]
    public void InsertsNothingIntoTablesOutsideOfGroupings()
    {
        IPostgreSqlStoredSchemaDataSet schema = EmptySchema();
        ITable included = schema.Keys.First();
        ITable untouched = schema.Keys.Last();

        IPostgreSqlStoredSchemaDataSet dataSet =
            new PostgreSqlStoredSchemaDataSetWithInsertedRows(
                schema,
                new TablesAndTheirRandomRows([included])
            );

        Touch(dataSet);

        Assert.Equal(100, _fixture!.RowCount(included));
        Assert.Equal(0, _fixture.RowCount(untouched));
    }

    [Theory]
    [InlineData("O'Brien")]
    [InlineData("''")]
    [InlineData("back\\slash")]
    [InlineData("\\'); SELECT 1; --")]
    [InlineData("x'); SELECT 1; --")]
    [InlineData("line\nbreak\ttab")]
    public void InsertValueVerbatim(string value)
    {
        IPostgreSqlStoredSchemaDataSet schema = EmptySchema();
        ITable table = TableWithStringColumn(schema);

        IPostgreSqlStoredSchemaDataSet dataSet =
            new PostgreSqlStoredSchemaDataSetWithInsertedRows(
                schema,
                [new Grouping(table, new DefaultRowWithStringValue(table, new String(value)))]
            );

        Touch(dataSet);

        Assert.Equal([value], _fixture!.Values(table, StringColumn(table)));
    }

    [Fact]
    public void DoesNotExecuteSqlInjectedThroughValue()
    {
        IPostgreSqlStoredSchemaDataSet schema = EmptySchema();
        ITable table = TableWithStringColumn(schema);
        string value =
            $"x'); DROP TABLE \"{TrimmedHex(new SchemaHash(schema.Schema))}\""
            + $".\"{TrimmedHex(new TableHash(table))}\"; --";

        IPostgreSqlStoredSchemaDataSet dataSet =
            new PostgreSqlStoredSchemaDataSetWithInsertedRows(
                schema,
                [new Grouping(table, new DefaultRowWithStringValue(table, new String(value)))]
            );

        Touch(dataSet);

        Assert.Equal([value], _fixture!.Values(table, StringColumn(table)));
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

    private static void Touch(IPostgreSqlStoredSchemaDataSet dataSet)
    {
        foreach (IStoredTableDataSet tableDataSet in dataSet.Values)
        {
            _ = tableDataSet.TableSchema;
        }
    }

    private static ITable TableWithStringColumn(IPostgreSqlStoredSchemaDataSet schema)
    {
        return schema.Keys.Single(x => x.Columns.Any(c => c.Type is StringColumnType));
    }

    private static IColumn StringColumn(ITable table)
    {
        return table.Columns.Single(x => x.Type is StringColumnType);
    }

    private static string TrimmedHex(IDeterminedHash hash)
    {
        return new HexString(hash).TextValue[..63];
    }

    private FakeStoredSchemaDataSet EmptySchema()
    {
        return new FakeStoredSchemaDataSet(
            _fixture!.Schema,
            _fixture.Connection,
            _ => []
        );
    }
}

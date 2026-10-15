using System.Collections;
using System.Data;
using System.Linq.Expressions;
using Pure.Primitives.Abstractions.Bool;
using Pure.Primitives.Abstractions.String;
using Pure.Primitives.Bool;
using Pure.Primitives.String.Operations;
using Pure.RelationalSchema.Abstractions.Table;
using Pure.RelationalSchema.HashCodes;
using Pure.RelationalSchema.Storage.Abstractions;
using Pure.RelationalSchema.Storage.HashCodes;

namespace Pure.RelationalSchema.Storage.PostgreSQL.Insertion;

internal sealed record PostgreSqlStoredTableDataSetWithInsertedRows : IStoredTableDataSet
{
    private readonly IStoredTableDataSet _dataSet;

    private readonly Lazy<IBool> _rowsInserted;

    public PostgreSqlStoredTableDataSetWithInsertedRows(
        IStoredTableDataSet dataSet,
        IDbConnection connection,
        IString schemaName,
        IEnumerable<IRow> rows
    )
    {
        _dataSet = dataSet;
        _rowsInserted = new Lazy<IBool>(() =>
        {
            IEnumerable<IRow> rowsToInsert = rows.DistinctBy(x =>
                    new HexString(new RowHash(x)).TextValue
                )
                .ExceptBy(
                    Queryable.Select(
                        dataSet,
                        c => new HexString(new RowHash(c)).TextValue
                    ),
                    x => new HexString(new RowHash(x)).TextValue
                );

            if (!rowsToInsert.Any())
            {
                return new True();
            }

            IDbCommand command = connection.CreateCommand();
            command.CommandText = new InsertStatement(
                new TrimmedHash(new HexString(new TableHash(dataSet.TableSchema))),
                schemaName,
                rowsToInsert
            ).TextValue;
            _ = command.ExecuteNonQuery();
            return new True();
        });
    }

    public ITable TableSchema =>
        _rowsInserted.Value.BoolValue
            ? _dataSet.TableSchema
            : throw new ArgumentException("Value was not inserted.");

    public Type ElementType =>
        _rowsInserted.Value.BoolValue
            ? _dataSet.ElementType
            : throw new ArgumentException("Value was not inserted.");

    public Expression Expression =>
        _rowsInserted.Value.BoolValue
            ? _dataSet.Expression
            : throw new ArgumentException("Value was not inserted.");

    public IQueryProvider Provider =>
        _rowsInserted.Value.BoolValue
            ? _dataSet.Provider
            : throw new ArgumentException("Value was not inserted.");

    public IEnumerator<IRow> GetEnumerator()
    {
        return _rowsInserted.Value.BoolValue
            ? _dataSet.GetEnumerator()
            : throw new ArgumentException("Value was not inserted.");
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

    public IAsyncEnumerator<IRow> GetAsyncEnumerator(
        CancellationToken cancellationToken = default
    )
    {
        return _rowsInserted.Value.BoolValue
            ? _dataSet.GetAsyncEnumerator(cancellationToken)
            : throw new ArgumentException("Value was not inserted.");
    }
}

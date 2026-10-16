using System.Collections;
using Pure.Collections.Generic;
using Pure.Primitives.Abstractions.String;
using Pure.RelationalSchema.Abstractions.Column;
using Pure.RelationalSchema.Abstractions.Table;
using Pure.RelationalSchema.ColumnType;
using Pure.RelationalSchema.HashCodes;
using Pure.RelationalSchema.Storage.Abstractions;

namespace Pure.RelationalSchema.Storage.PostgreSQL.Insertion.Tests.Fakes;

internal sealed record DefaultRowWithStringValue : IEnumerable<IRow>
{
    private readonly IEnumerable<IRow> _rows;

    public DefaultRowWithStringValue(ITable table, IString value)
    {
        _rows =
        [
            new Row(
                new Dictionary<IColumn, IColumn, ICell>(
                    table.Columns,
                    x => x,
                    x =>
                        x.Type is StringColumnType
                            ? new Cell(value)
                            : new Cell(new DefaultValueForColumnType(x.Type)),
                    x => new ColumnHash(x)
                )
            ),
        ];
    }

    public IEnumerator<IRow> GetEnumerator()
    {
        return _rows.GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }
}

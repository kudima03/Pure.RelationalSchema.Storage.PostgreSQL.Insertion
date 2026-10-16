using System.Collections;
using Pure.Primitives.Abstractions.Char;
using Pure.Primitives.Abstractions.String;
using Pure.Primitives.String;
using Pure.Primitives.String.Operations;
using Pure.RelationalSchema.Abstractions.Schema;
using Pure.RelationalSchema.Abstractions.Table;
using Pure.RelationalSchema.HashCodes;
using Pure.RelationalSchema.Storage.Abstractions;
using Char = Pure.Primitives.Char.Char;
using String = Pure.Primitives.String.String;

namespace Pure.RelationalSchema.Storage.PostgreSQL.Insertion;

internal sealed record InsertStatement : IString
{
    private readonly IString _tableName;

    private readonly IString _schemaName;

    private readonly IEnumerable<IRow> _rows;

    public InsertStatement(ITable table, ISchema schema, IEnumerable<IRow> rows)
        : this(
            new HexString(new TableHash(table)),
            new HexString(new SchemaHash(schema)),
            rows
        )
    { }

    public InsertStatement(IString tableName, IString schemaName, IEnumerable<IRow> rows)
    {
        _tableName = tableName;
        _schemaName = schemaName;
        _rows = rows;
    }

    public string TextValue =>
        new WhitespaceJoinedString(
            new String("INSERT INTO"),
            new JoinedString(
                new DotString(),
                [
                    new WrappedString(
                        new DoubleQuoteString(),
                        new TrimmedHash(_schemaName)
                    ),
                    new WrappedString(
                        new DoubleQuoteString(),
                        new TrimmedHash(_tableName)
                    ),
                ]
            ),
            new WrappedString(
                new LeftRoundBracketString(),
                new JoinedString(
                    new ConcatenatedString(new CommaString(), new WhitespaceString()),
                    _rows
                        .SelectMany(x => x.Cells.Keys)
                        .Select(x => new HexString(new ColumnHash(x)))
                        .DistinctBy(x => x.TextValue)
                        .Select(x => new WrappedString(
                            new DoubleQuoteString(),
                            new TrimmedHash(x)
                        ))
                ),
                new RightRoundBracketString()
            ),
            new String("VALUES"),
            new ConcatenatedString(
                new JoinedString(
                    new ConcatenatedString(new CommaString(), new WhitespaceString()),
                    _rows.Select(x => new WrappedString(
                        new LeftRoundBracketString(),
                        new JoinedString(
                            new ConcatenatedString(
                                new CommaString(),
                                new WhitespaceString()
                            ),
                            x.Cells.Values.Select(c => new ConcatenatedString(
                                new String("E"),
                                new WrappedString(
                                    new SingleQuoteString(),
                                    new String(
                                        c.Value.TextValue.Replace("\\", "\\\\")
                                            .Replace("'", "''")
                                    )
                                )
                            ))
                        ),
                        new RightRoundBracketString()
                    ))
                ),
                new SemicolonString()
            )
        ).TextValue;

    public IEnumerator<IChar> GetEnumerator()
    {
        return TextValue.Select(x => new Char(x)).GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }
}

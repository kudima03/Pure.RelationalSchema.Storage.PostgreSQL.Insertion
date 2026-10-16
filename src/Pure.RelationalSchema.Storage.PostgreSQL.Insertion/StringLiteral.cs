using System.Collections;
using Pure.Primitives.Abstractions.Char;
using Pure.Primitives.Abstractions.String;
using Pure.Primitives.String;
using Pure.Primitives.String.Operations;
using Char = Pure.Primitives.Char.Char;
using String = Pure.Primitives.String.String;

namespace Pure.RelationalSchema.Storage.PostgreSQL.Insertion;

internal sealed record StringLiteral : IString
{
    private readonly IString _value;

    public StringLiteral(IString value)
    {
        _value = value;
    }

    public string TextValue =>
        new ConcatenatedString(
            new String("E"),
            new WrappedString(
                new SingleQuoteString(),
                new String(
                    _value.TextValue.Replace("\\", "\\\\").Replace("'", "''")
                )
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

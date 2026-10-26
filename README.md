# Pure.RelationalSchema.Storage.PostgreSQL.Insertion

PostgreSQL row insertion for the **Pure** relational schema ecosystem — lazy `INSERT` decorators over `Pure.RelationalSchema.Storage.PostgreSQL.Querying` datasets.

[![.NET build & test](https://github.com/kudima03/Pure.RelationalSchema.Storage.PostgreSQL.Insertion/actions/workflows/build-and-test.yml/badge.svg?branch=main)](https://github.com/kudima03/Pure.RelationalSchema.Storage.PostgreSQL.Insertion/actions/workflows/build-and-test.yml)
[![publish NuGet](https://github.com/kudima03/Pure.RelationalSchema.Storage.PostgreSQL.Insertion/actions/workflows/publish-nuget.yml/badge.svg?branch=main)](https://github.com/kudima03/Pure.RelationalSchema.Storage.PostgreSQL.Insertion/actions/workflows/publish-nuget.yml)
[![NuGet](https://img.shields.io/nuget/v/Pure.RelationalSchema.Storage.PostgreSQL.Insertion)](https://www.nuget.org/packages/Pure.RelationalSchema.Storage.PostgreSQL.Insertion)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](LICENSE)

## Overview

`Pure.RelationalSchema.Storage.PostgreSQL.Insertion` writes rows into a live PostgreSQL database through the storage abstractions defined in `Pure.RelationalSchema.Storage`. `PostgreSqlStoredSchemaDataSetWithInsertedRows` wraps any `IPostgreSqlStoredSchemaDataSet` (the usual implementation is `PostgreSqlStoredSchemaDataSet` from `Pure.RelationalSchema.Storage.PostgreSQL.Querying`) and a set of rows grouped by table. The `INSERT INTO` statement is executed lazily, the first time a table dataset of the result is accessed, and only once. It then exposes the wrapped dataset, so the inserted rows can be read back through the same `IStoredTableDataSet`.

Rows are inserted exactly as supplied: duplicates within the batch are not removed, and the table is not read to skip rows that are already stored. Uniqueness is enforced by the database: declare a unique index on the table (`IIndex.IsUnique`), and inserting a row that conflicts with it makes the insert fail. Without one, inserting the same rows again stores them again.

The result exposes **only the tables present in the supplied groupings**, not every table of the wrapped dataset.

This package covers writes only. Creating the tables and reading rows are handled by the related packages listed below. The tables must already exist with the hash-based names produced by `Pure.RelationalSchema.Storage.PostgreSQL.Creation`. Schema, table and column identifiers are the first 63 hexadecimal characters of the hashes from `Pure.RelationalSchema.HashCodes`, not the model names. The caller owns the `IDbConnection`: it must be opened before use, and this package never disposes it.

## Public API

| Type | Kind | Description |
|------|------|-------------|
| `PostgreSqlStoredSchemaDataSetWithInsertedRows` | record | Wraps an `IPostgreSqlStoredSchemaDataSet` and inserts grouped `IRow` data into the corresponding table datasets; implements `IPostgreSqlStoredSchemaDataSet` |

The constructor takes any `IPostgreSqlStoredSchemaDataSet`. The table-level decorator, `InsertStatement` and the hashing helpers are internal. `IPostgreSqlStoredSchemaDataSet` comes from `Pure.RelationalSchema.Storage.PostgreSQL.Abstractions`; this package does not depend on Querying or Creation.

## Dependencies

- [`Pure.RelationalSchema.Storage.PostgreSQL.Abstractions`](https://github.com/kudima03/Pure.RelationalSchema.Storage.PostgreSQL.Abstractions/tree/0.1.0-preview.0.1.0) — provides `IPostgreSqlStoredSchemaDataSet`
- [`Pure.RelationalSchema.Storage`](https://github.com/kudima03/Pure.RelationalSchema.Storage/tree/0.1.0-preview.8.0.0) — storage abstractions (`IStoredSchemaDataSet`, `IStoredTableDataSet`, `IRow`) that this package implements
- [`Pure.RelationalSchema.HashCodes`](https://github.com/kudima03/Pure.RelationalSchema.HashCodes/tree/3.3.0) — hash codes used to derive PostgreSQL object names
- [`Pure.Primitives.Abstractions`](https://github.com/kudima03/Pure.Primitives.Abstractions/tree/4.3.0) — base interfaces for primitive types (`IString`, `IBool`, etc.)
- [`Pure.Primitives`](https://github.com/kudima03/Pure.Primitives/tree/3.6.5) — concrete primitive type implementations
- [`Pure.Primitives.String.Operations`](https://github.com/kudima03/Pure.Primitives.String.Operations/tree/1.5.1) — string composition utilities used to build SQL statements
- [`Pure.Collections.Generic`](https://github.com/kudima03/Pure.Collections.Generic/tree/0.1.0-preview.3.0.0) — generic collection implementations (e.g. `OrderedDictionary` and `Dictionary<TKey1, TKey2, TValue>` with dual-key lookup)

## Target Frameworks

- .NET 8
- .NET 9
- .NET 10

## Installation

```
dotnet add package Pure.RelationalSchema.Storage.PostgreSQL.Insertion
```

This package does not create tables or read rows itself. The example below also uses `Pure.RelationalSchema.Storage.PostgreSQL.Creation` to create the tables and `Pure.RelationalSchema.Storage.PostgreSQL.Querying` for the wrapped dataset; neither is a dependency of this package.

## Usage

```csharp
using System.Data;
using Npgsql;
using Pure.Collections.Generic;
using Pure.RelationalSchema.Abstractions.Column;
using Pure.RelationalSchema.Abstractions.Schema;
using Pure.RelationalSchema.Abstractions.Table;
using Pure.RelationalSchema.ColumnType;
using Pure.RelationalSchema.HashCodes;
using Pure.RelationalSchema.Storage;
using Pure.RelationalSchema.Storage.Abstractions;
using Pure.RelationalSchema.Storage.PostgreSQL.Abstractions;
using Pure.RelationalSchema.Storage.PostgreSQL.Creation;
using Pure.RelationalSchema.Storage.PostgreSQL.Insertion;
using Pure.RelationalSchema.Storage.PostgreSQL.Querying;
using Column = Pure.RelationalSchema.Column.Column;
using Schema = Pure.RelationalSchema.Schema.Schema;
using String = Pure.Primitives.String.String;
using Table = Pure.RelationalSchema.Table.Table;

IDbConnection connection = new NpgsqlConnection(connectionString);
connection.Open();

IColumn idColumn = new Column(new String("Id"), new IntColumnType());
IColumn nameColumn = new Column(new String("Name"), new StringColumnType());
ITable usersTable = new Table(new String("Users"), [idColumn, nameColumn], []);
ISchema schema = new Schema(new String("Shop"), [usersTable], []);

ISchema createdSchema = new PostgreSqlCreatedSchema(schema, connection);
_ = createdSchema.Tables.ToArray();

IPostgreSqlStoredSchemaDataSet dataSet =
    new PostgreSqlStoredSchemaDataSet(createdSchema, connection);

IRow alice = new Row(
    new Dictionary<IColumn, IColumn, ICell>(
        [idColumn, nameColumn],
        column => column,
        column =>
            column == idColumn
                ? new Cell(new String("1"))
                : new Cell(new String("Alice")),
        column => new ColumnHash(column)
    )
);

// Rows are supplied grouped by table. LINQ's GroupBy cannot be used here: it hashes
// the key, and ITable implementations do not support GetHashCode.
IEnumerable<IGrouping<ITable, IRow>> rows = [new TableRows(usersTable, [alice])];

IPostgreSqlStoredSchemaDataSet withInserted =
    new PostgreSqlStoredSchemaDataSetWithInsertedRows(dataSet, rows);

foreach (IRow row in withInserted[usersTable])
{
    Console.WriteLine(row.Cells[nameColumn].Value.TextValue);
}

internal sealed class TableRows : IGrouping<ITable, IRow>
{
    private readonly IEnumerable<IRow> _rows;

    public TableRows(ITable key, IEnumerable<IRow> rows)
    {
        Key = key;
        _rows = rows;
    }

    public ITable Key { get; }

    public IEnumerator<IRow> GetEnumerator()
    {
        return _rows.GetEnumerator();
    }

    System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }
}
```

`GroupBy` cannot be used to group the rows: it hashes the key, and `ITable` implementations do not support `GetHashCode`, so supply your own `IGrouping<ITable, IRow>` as above. Every cell value is an `IString`.

## How values are written

Every cell value is written into the `INSERT` statement as an escaped string literal (`E'...'`, with backslashes and single quotes doubled), so a value cannot end the literal or change the statement. PostgreSQL then converts the literal to the column's type, so a value that does not convert (for example `abc` for an `integer` column) makes the insert fail.

## Related packages

- `Pure.RelationalSchema.Storage.PostgreSQL.Creation` — creates the schema and tables this package writes to
- `Pure.RelationalSchema.Storage.PostgreSQL.Querying` — reads rows from those tables; `PostgreSqlStoredSchemaDataSet` is the usual dataset to wrap
- `Pure.RelationalSchema.Storage.PostgreSQL.Abstractions` — provides `IPostgreSqlStoredSchemaDataSet`

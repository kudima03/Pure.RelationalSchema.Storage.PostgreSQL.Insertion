# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Commands

All `dotnet` commands must be run from the `./src` directory.

```bash
dotnet restore
dotnet build --no-restore -warnaserror
dotnet format --verify-no-changes             # check code style (CI enforces this)
dotnet format                                 # auto-fix code style
dotnet test --no-build --verbosity normal     # run integration tests (requires Docker for Testcontainers)
dotnet pack --configuration Release -p:Version=<version> --output .
```

CI additionally passes `-p:AssemblyVersion` (pinned to the major) and `-p:FileVersion`; see `.github/workflows/publish-nuget.yml`.

## Architecture

This is the **insertion (write)** part of the PostgreSQL storage implementation for the Pure relational schema ecosystem. It has no public abstractions of its own — it provides one public decorator that implements `IPostgreSqlStoredSchemaDataSet` (from `Pure.RelationalSchema.Storage.PostgreSQL.Abstractions`). Creating tables and reading rows live in the sibling packages `Pure.RelationalSchema.Storage.PostgreSQL.Creation` and `Pure.RelationalSchema.Storage.PostgreSQL.Querying`, which this package does not depend on. There is no deletion code in this ecosystem yet.

**Dependencies:** `Pure.RelationalSchema.Storage.PostgreSQL.Abstractions` is the only package from this family that is referenced, and it provides `IPostgreSqlStoredSchemaDataSet`. The decorator re-reads the wrapped dataset to skip rows that already exist. There is no dependency on `Querying` or `Creation`, at runtime or in tests.

**Key relationships:**

- `PostgreSqlStoredSchemaDataSetWithInsertedRows` (public) wraps an `IPostgreSqlStoredSchemaDataSet` and rows grouped by `ITable`. It exposes **only the tables present in the supplied groupings**, and passes `Connection` and `Schema` through from the wrapped dataset.
- `PostgreSqlStoredTableDataSetWithInsertedRows` (internal) is the per-table decorator. On first access of any member it lazily runs `INSERT INTO` once, after dropping rows that are duplicates within the batch and rows that already exist (compared by `RowHash`), then delegates every member to the wrapped `IStoredTableDataSet`.
- `InsertStatement` and `TrimmedHash` are **internal**. The SQL is composed as an `IString` value using `Pure.Primitives.String.Operations` combinators.
- The caller owns the `IDbConnection`. It is shared with every other dataset using it, so no type here may dispose it.
- `InsertStatement` writes each value as `E'...'` with backslashes and single quotes doubled, so the result does not depend on `standard_conforming_strings`. `IDbCommand` parameters are not used on purpose: without a provider-specific type, Npgsql sends strings as `text`, which PostgreSQL will not assign to boolean/integer/timestamp/bytea columns, whereas an untyped literal is converted to the column type. The escaping is inlined rather than a separate `IString` type: tests go through the public API only (no `InternalsVisibleTo`), so another type's `GetEnumerator` boilerplate would be uncovered and line coverage on `main` is close to the 89% gate.

**Naming convention:** the tables must already exist with hash-based names. Schema, table and column identifiers are the first 63 characters (PostgreSQL's identifier limit) of `HexString` over `SchemaHash` / `TableHash` / `ColumnHash` from `Pure.RelationalSchema.HashCodes`. This convention must match the Creation and Querying packages, since it is the only contract between them and this one. `TrimmedHash` is deliberately duplicated in each repo rather than shared.

**Testing:** Integration tests use `Testcontainers.PostgreSql` to spin up a real PostgreSQL instance. `DatabaseFixture` creates the hash-named schema and tables with raw SQL (no indexes or foreign keys), using the same hash identifiers the library writes to. The wrapped dataset is the in-memory `Fakes/FakeStoredSchemaDataSet`, keyed by `TableHash` because `Table.GetHashCode()` throws. Inserts are observed with `SELECT count(*)` against the hashed table (`DatabaseFixture.RowCount`), not by enumerating the decorator, whose enumeration passes through to the fake. The decorator is lazy, so touch a member first to trigger the insert. On stored datasets use `.AsEnumerable()` with `Assert.Empty` / `Assert.Single`, because the bare overloads are ambiguous between `IEnumerable` and `IAsyncEnumerable`. CI runs mutation testing via `dotnet-stryker` at `Complete` level with a 58% break threshold and requires >=89% line coverage (warning at 99%).

**CI style check:** the `Code style check by csharpier` step in `.github/workflows/build-and-test.yml` is commented out on purpose. csharpier wants `) { }` on one line for chained constructors with an empty body, while `dotnet format` (IDE0055 with `csharp_new_line_before_open_brace = all`) wants `{ }` on its own line, so both cannot pass on `PostgreSqlStoredSchemaDataSetWithInsertedRows.cs` and `InsertStatement.cs`. `dotnet format` is the authoritative check.

**Multi-targeting:** net8.0, net9.0, net10.0. `IsAotCompatible` is `true` and the build must stay free of IL2026/IL3050 warnings. Do not use `AsQueryable()` (annotated as trim/AOT-unsafe); call `Queryable.Select(dataSet, ...)` explicitly, which also avoids the `IEnumerable`/`IAsyncEnumerable` overload ambiguity.

**Package validation:** `EnablePackageValidation = true` with `PackageValidationBaselineVersion` set to the last published version (`0.1.0-preview.0.1.1`). Breaking API changes fail the build; bump the baseline after each release.

**Publishing:** triggered by pushing a semver tag matching `*.*.*`. The tag name becomes the `PackageVersion`. Packages are published to both GitHub Packages and NuGet.org.

## Code Style

Enforced via `.editorconfig` and `dotnet format --verify-no-changes` in CI:

- No `var` — always use explicit types
- No expression-bodied methods or constructors — properties, indexers, accessors, and lambdas may be expression-bodied
- File-scoped namespace declarations
- Private fields prefixed with `_` (underscore camelCase)
- No non-private instance fields
- `using` directives outside the namespace, System directives sorted first
- Max line length: 90 characters
- Braces on new lines (`csharp_new_line_before_open_brace = all`)
- No implicit object creation (`new()` without type) when the type is apparent

## Commit Messages

Do not mention Claude or AI assistance in commit messages.

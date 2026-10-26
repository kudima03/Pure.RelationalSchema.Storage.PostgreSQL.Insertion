# Changelog

All notable changes to `Pure.RelationalSchema.Storage.PostgreSQL.Insertion` are documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [0.1.0-preview.1.0.0] - 2026-10-26

### Removed

- Rows are no longer de-duplicated before they are inserted. Duplicates within the supplied batch are inserted as given, and the table is no longer read to skip rows that are already stored, so inserting the same rows again stores them again unless a unique index rejects them. Skipping stored rows read the whole table on every insert and, with `Pure.RelationalSchema.Storage.PostgreSQL.Querying` 0.1.0-preview.0.6.0, failed with `NotSupportedException` because the row-hash projection cannot be translated to SQL. Declare a unique index (`IIndex.IsUnique`) where rows must be unique.
- Removed the dependency on `Pure.RelationalSchema.Storage.HashCodes`.

## [0.1.0-preview.0.4.0] - 2026-10-23

### Changed

- Updated the `Pure.Primitives.String.Operations` dependency from 1.6.0 to 1.7.0. No public API changes.

## [0.1.0-preview.0.3.0] - 2026-10-22

### Changed

- Updated the `Pure.Primitives.String.Operations` dependency from 1.5.1 to 1.6.0. No public API changes.

## [0.1.0-preview.0.2.0] - 2026-10-22

### Changed

- Updated the `Pure.Primitives` dependency from 3.6.5 to 3.7.0. No public API changes.

## [0.1.0-preview.0.1.1] - 2026-10-16

### Security

- Cell values are now escaped before they are written into the `INSERT` statement. Before, a value containing a single quote produced invalid SQL, and a crafted value could run arbitrary SQL, such as `x'); DROP TABLE ...; --`.

## [0.1.0-preview.0.1.0] - 2026-10-15

### Added

- Initial release: row insertion (`PostgreSqlStoredSchemaDataSetWithInsertedRows`) extracted from `Pure.RelationalSchema.Storage.PostgreSQL` 0.1.0-preview.8.1.2; `IPostgreSqlStoredSchemaDataSet` now comes from `Pure.RelationalSchema.Storage.PostgreSQL.Abstractions`.

### Changed

- The package is now marked AOT/trimming compatible (`IsAotCompatible`); the existing-row lookup no longer goes through `Queryable.AsQueryable`.

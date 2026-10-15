# Changelog

All notable changes to `Pure.RelationalSchema.Storage.PostgreSQL.Insertion` are documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

## [0.1.0-preview.0.1.0] - 2026-10-15

### Added

- Initial release: row insertion (`PostgreSqlStoredSchemaDataSetWithInsertedRows`) extracted from `Pure.RelationalSchema.Storage.PostgreSQL` 0.1.0-preview.8.1.2; `IPostgreSqlStoredSchemaDataSet` now comes from `Pure.RelationalSchema.Storage.PostgreSQL.Abstractions`.

### Changed

- The package is now marked AOT/trimming compatible (`IsAotCompatible`); the existing-row lookup no longer goes through `Queryable.AsQueryable`.

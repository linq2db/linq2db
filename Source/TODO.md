# TODO

This file tracks small cleanup ideas that are useful to keep, but are outside the scope of the current pull request.

## Cleanup Ideas

- Revisit explicit parameter type metadata for LINQ queries. Raw SQL can already carry user-provided `DataParameter` type details, but LINQ currently lacks a clear public way to mark `Sql.Parameter` metadata as explicitly user-provided rather than inferred from expression or column context. Consider extending `Sql.Parameter` or adding a related API so users can explicitly specify `DataType`, `DbType`, length, precision, and scale, and propagate that intent to provider parameter creation.

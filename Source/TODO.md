# TODO

This file tracks small cleanup ideas that are useful to keep, but are outside the scope of the current pull request.

## Cleanup Ideas

- Add a parameterless `ColumnReaderAttribute` constructor that defaults to ordinal parameter index `1`, then replace `[ColumnReader(1)]` usages where the custom reader shape is `(reader, ordinal)`.

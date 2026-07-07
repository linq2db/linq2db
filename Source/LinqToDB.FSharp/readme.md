# LINQ to DB F# Services<!-- omit in toc -->

[![License](https://img.shields.io/github/license/linq2db/linq2db)](MIT-LICENSE.txt)

LINQ to DB F# Services provides support for F#-specific language features in Linq To DB.

Supported features:

- F# record types support in mappings and projections
- Automatic mapping of F# `'T option` and `'T voption` columns: the "some" case is stored as the value
  and the "none" case as `NULL`, with no manual `MappingSchema` configuration. Value-typed options
  (e.g. `int option`) correctly store the "none" case as `NULL` rather than the default value. Only
  options over a scalar element type are auto-mapped. The column's DB type (including facets such as
  decimal precision/scale and string length) is resolved from the element type against the connection's
  provider mapping schema, so it matches the non-option column of the same element type.

More features planned for future releases.

### How to use

```cs
using var db = new DataConnection(
  new DataOptions()
    .UseSqlServer(@"Server=.\;Database=Northwind;Trusted_Connection=True;")
    // enables F# Services for connection
    .UseFSharp());
```

# LINQ to DB F# Services<!-- omit in toc -->

[![License](https://img.shields.io/github/license/linq2db/linq2db)](MIT-LICENSE.txt)

LINQ to DB F# Services provides support for F#-specific language features in Linq To DB.

Supported features:

- F# record types support in mappings and projections

More features planned for future releases.

### How to use

```cs
using var db = new DataConnection(
  new DataOptions()
    .UseSqlServer(@"Server=.\;Database=Northwind;Trusted_Connection=True;")
    // enables F# Services for connection
    .UseFSharp());
```


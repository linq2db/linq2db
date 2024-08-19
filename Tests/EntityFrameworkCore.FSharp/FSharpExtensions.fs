module LinqToDB.EntityFrameworkCore.FSharp.FSharpExtensions

open EntityFrameworkCore.FSharp.Extensions
open Microsoft.EntityFrameworkCore

let WithFSharp(builder: DbContextOptionsBuilder) =
    builder.UseFSharpTypes() |> ignore

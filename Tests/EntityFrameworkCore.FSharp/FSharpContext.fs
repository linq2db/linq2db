module LinqToDB.EntityFrameworkCore.FSharp.FSharpContext

open System
open System.ComponentModel.DataAnnotations
open System.ComponentModel.DataAnnotations.Schema
open Microsoft.EntityFrameworkCore
open EntityFrameworkCore.FSharp
open Microsoft.FSharp.Linq

[<CLIMutable>]
type WithIdentity = {
    [<Key>]
    [<DatabaseGenerated(DatabaseGeneratedOption.Identity)>]
    Id : int
    Name : string
}

[<CLIMutable>]
[<NoComparison>]
type Issue4646Table = {
    [<Key>]
    [<DatabaseGenerated(DatabaseGeneratedOption.Identity)>]
    [<LinqToDB.Mapping.Identity>]
    Id : int
    [<LinqToDB.Mapping.Column>]
    Value : int option
    [<LinqToDB.Mapping.Column>]
    ValueN : System.Nullable<int> option
}

type AppDbContext(options: DbContextOptions<AppDbContext>) =
    inherit DbContext(options)

    [<DefaultValue>] val mutable WithIdentity : DbSet<WithIdentity>
    member this.CompaniesInformation
        with get() = this.WithIdentity
        and set v = this.WithIdentity <- v

    override __.OnModelCreating mb =
        mb.Entity<Issue4646Table>().ToTable("Issue4646Table") |> ignore
        // option configuration shouldn't be required but looks like
        // https://github.com/efcore/EFCore.FSharp/issues/24
        // is broken
        mb.Entity<Issue4646Table>().Property(fun s -> s.Value).HasConversion(OptionConverter<int> ()) |> ignore
        mb.Entity<Issue4646Table>().Property(fun s -> s.ValueN).HasConversion(OptionConverter<Nullable<int>> ()) |> ignore

module LinqToDB.EntityFrameworkCore.FSharp.FSharpContext

open System.ComponentModel.DataAnnotations
open System.ComponentModel.DataAnnotations.Schema
open Microsoft.EntityFrameworkCore

[<CLIMutable>]
type WithIdentity = {
    [<Key>]
    [<DatabaseGenerated(DatabaseGeneratedOption.Identity)>]
    Id : int
    Name : string
}

type AppDbContext(options: DbContextOptions<AppDbContext>) =
    inherit DbContext(options)

    [<DefaultValue(false)>] val mutable WithIdentity : DbSet<WithIdentity>
    member this.CompaniesInformation
        with get() = this.WithIdentity
        and set v = this.WithIdentity <- v


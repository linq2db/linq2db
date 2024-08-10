module LinqToDB.EntityFrameworkCore.FSharpTests

open System.Linq
open LinqToDB
open System.ComponentModel.DataAnnotations
open System.ComponentModel.DataAnnotations.Schema
open Microsoft.EntityFrameworkCore
open EntityFrameworkCore.FSharp.Extensions
open NUnit.Framework

[<CLIMutable>]
type WithIdentity = {
    [<Key>]
    [<DatabaseGenerated(DatabaseGeneratedOption.Identity)>]
    Id : int
    Name : string
}

type AppDbContext(options: DbContextOptions<AppDbContext>) =
    inherit DbContext(options)

    [<DefaultValue>] val mutable WithIdentity : DbSet<WithIdentity>
    member this.CompaniesInformation
        with get() = this.WithIdentity
        and set v = this.WithIdentity <- v

type TestDbContextFactory() =
    member this.CreateDbContext() =
        let options = new DbContextOptionsBuilder<AppDbContext>()
        options.UseSqlite("DataSource=:memory:").UseFSharpTypes() |> ignore
        new AppDbContext(options.Options)

[<TestFixture>]
type Tests() =

    [<Test>]
    member this.TestLeftJoin() =
        let context = TestDbContextFactory().CreateDbContext()
        let q =
            context
                .WithIdentity
                .Join(
                    context.WithIdentity,
                    (fun p -> p.Id),
                    (fun c -> c.Id),
                    (fun p c ->
                    {|
                        Person = p
                        Company = c
                    |}) )
                .LeftJoin(
                    context.WithIdentity,
                    (fun partialPerson cInfo -> partialPerson.Company.Id = cInfo.Id),
                    (fun partialPerson cInfo ->
                    {|
                        Company = partialPerson.Company
                        CompanyInformation = cInfo
                        Person = partialPerson.Person
                    |}) )
        //q.ToArray() |> ignore
        q.ToLinqToDB().ToString() |> ignore

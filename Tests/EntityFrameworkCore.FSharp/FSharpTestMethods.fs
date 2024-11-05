module LinqToDB.EntityFrameworkCore.FSharp.FSharpTestMethods

open System.Linq
open LinqToDB
open LinqToDB.EntityFrameworkCore
open LinqToDB.EntityFrameworkCore.FSharp.FSharpContext

let TestLeftJoin(context: AppDbContext) =
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
    q.ToLinqToDB().ToString() |> ignore

let Issue4646TestEF(context: AppDbContext) =
    let record = { Id = 0; Value = Option.None; ValueN = Option.None } : Issue4646Table
    context.Set<Issue4646Table>().Add record |> ignore

let Issue4646TestLinqToDB(context: AppDbContext) =
    let record = { Id = 0; Value = Option.None; ValueN = Option.None } : Issue4646Table
    context.CreateLinqToDBConnection().Insert record |> ignore

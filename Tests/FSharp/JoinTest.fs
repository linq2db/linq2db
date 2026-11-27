module Tests.FSharp.JoinTest

open Tests.FSharp.Models

open System.Linq
open LinqToDB

let LeftJoinAfterJoinTupleResult(db : IDataContext, id: int) =
    let parents = db.GetTable<Parent>()
    let children = db.GetTable<Child>()
    let query =
            parents
                .Where(fun p -> p.ParentID = id)
                .Join(
                    children,
                    (fun p -> p.ParentID),
                    (fun c -> c.ParentID),
                    (fun p c ->
                        (p, c)) )
                .LeftJoin(
                    parents,
                    (fun (p, c) cInfo -> c.ParentID = cInfo.ParentID),
                    (fun (p, c) cInfo ->
                        (p, c, cInfo)
                    ))
    query.ToArray()
    |> ignore

let LeftJoinAfterJoinAnonymousResult(db : IDataContext, id: int) =
    let parents = db.GetTable<Parent>()
    let children = db.GetTable<Child>()
    let query =
            parents
                .Where(fun p -> p.ParentID = id)
                .Join(
                    children,
                    (fun p -> p.ParentID),
                    (fun c -> c.ParentID),
                    (fun p c ->
                        {|
                            p = p
                            c = c
                        |}) )
                .LeftJoin(
                    parents,
                    (fun x cInfo -> x.c.ParentID = cInfo.ParentID),
                    (fun x cInfo ->
                        (x.p, x.c, cInfo)
                    ))
    query.ToArray()
    |> ignore

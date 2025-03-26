module Tests.FSharp.Issue4851

open LinqToDB
open LinqToDB.FSharp

let Issue4851Test1() =
    let _ =
        (new DataOptions())
            .UseFSharp()
    ()

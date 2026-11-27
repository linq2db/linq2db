module Tests.FSharp.UseFSharpExtension

open LinqToDB
open LinqToDB.FSharp

let UseFSharpAsExtensionMethodCompiles() =
    let _ =
        (new DataOptions())
            .UseFSharp()
    ()

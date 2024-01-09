namespace LinqToDB.FSharp

open LinqToDB
open System.Runtime.CompilerServices

[<Extension>]
module Methods =
    [<Extension>]
    let UseFSharpRecords(options : DataOptions) =
        options.UseInterceptor FSharpExpressionInterceptor.Instance

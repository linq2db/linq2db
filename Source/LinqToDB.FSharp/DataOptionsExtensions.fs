namespace LinqToDB.FSharp

open LinqToDB
open System.Runtime.CompilerServices

[<Extension>]
module Methods =
    /// <summary>Enables support for F#-specific features.</summary>
    /// <remarks>Currently it is limited to record types support, but we plan to add more support in future releases.</remarks>
    [<Extension>]
    let UseFSharp(options : DataOptions) =
        options.UseInterceptor FSharpEntityBindingInterceptor.Instance

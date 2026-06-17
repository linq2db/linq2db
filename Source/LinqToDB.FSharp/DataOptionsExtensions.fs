namespace LinqToDB.FSharp

open LinqToDB
open LinqToDB.Mapping
open System.Runtime.CompilerServices

[<Extension>]
type Methods() =
    // Shared schema carrying the F# option metadata reader; combined into the user's options below.
    static let optionMappingSchema =
        let ms = MappingSchema()
        ms.AddMetadataReader(FSharpOptionMetadataReader())
        ms

    /// <summary>Enables support for F#-specific features.</summary>
    /// <remarks>Adds support for F# record types and automatic mapping of F# <c>'T option</c> columns.</remarks>
    [<Extension>]
    static member UseFSharp(options : DataOptions) =
        options
            .UseInterceptor(FSharpEntityBindingInterceptor.Instance)
            .UseAdditionalMappingSchema(optionMappingSchema)

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
        ms.AddMetadataReader(FSharpSingleCaseUnionMetadataReader())
        ms

    /// <summary>Enables support for F#-specific features.</summary>
    /// <remarks>
    /// Adds support for:
    /// <list type="bullet">
    /// <item>F# record types;</item>
    /// <item>automatic mapping of F# <c>'T option</c> and <c>'T voption</c> columns.</item>
    /// </list>
    /// </remarks>
    [<Extension>]
    static member UseFSharp(options : DataOptions) =
        let options =
            options
                .UseInterceptor(FSharpEntityBindingInterceptor.Instance)
                .UseInterceptor(FSharpQueryExpressionInterceptor.Instance)
                .UseMemberTranslator(FSharpMemberTranslator.Instance)
        // Combine the option schema as a *lower-priority* fallback so it never shadows the user's
        // explicit mappings: auto 'T option support only fills in members the user hasn't mapped.
        // (UseAdditionalMappingSchema would add it at higher priority, letting its embedded default
        // attribute reader override fluent column metadata - e.g. dropping an explicit DataType.)
        let combined =
            match options.ConnectionOptions.MappingSchema with
            | null     -> optionMappingSchema
            | existing -> MappingSchema.CombineSchemas(existing, optionMappingSchema)
        options.UseMappingSchema(combined)

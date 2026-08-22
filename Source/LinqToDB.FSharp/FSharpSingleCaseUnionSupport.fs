namespace LinqToDB.FSharp

open System
open System.Collections.Concurrent
open System.Linq.Expressions
open System.Reflection

open Microsoft.FSharp.Reflection

open LinqToDB.Mapping
open LinqToDB.Metadata

/// Automatic mapping support for an F# single-case discriminated union over a scalar - e.g.
/// <c>type UserId = UserId of int</c>. A metadata reader supplies a <see cref="ValueConverterAttribute"/>
/// that stores the union as its single wrapped field's value (and reconstructs it on read), so such columns
/// round-trip with no manual configuration - mirroring <see cref="FSharpOptionSupport"/> for options.
type internal FSharpSingleCaseUnionSupport =

    static let cache = ConcurrentDictionary<Type, IValueConverter>()

    // Builds the bidirectional converter for a single-case union `Case of 'T`: to-DB reads the wrapped field,
    // from-DB calls the case constructor.
    static let build (unionType: Type) : IValueConverter =
        let case         = (FSharpType.GetUnionCases(unionType, true)).[0]
        let field        = (case.GetFields()).[0]                                    // the single wrapped field
        let providerType = field.PropertyType
        let ctor         = FSharpValue.PreComputeUnionConstructorInfo(case, true)     // static Case factory

        let uParam       = Expression.Parameter(unionType, "u")
        let toProvider   = Expression.Lambda(Expression.Property(uParam, field), uParam)

        let pParam       = Expression.Parameter(providerType, "p")
        let fromProvider = Expression.Lambda(Expression.Call(ctor, pParam), pParam)

        let converterType = typedefof<ValueConverter<_, _>>.MakeGenericType(unionType, providerType)
        match Activator.CreateInstance(converterType, [| box toProvider; box fromProvider; box true |]) with
        | :? IValueConverter as c -> c
        | _ -> raise (InvalidOperationException $"Failed to create F# single-case union value converter for '{unionType}'")

    /// <c>true</c> when <paramref name="t"/> is a single-case union with exactly one field (excludes
    /// option/voption, list, and multi-case unions, which have more than one case).
    static member IsSingleCaseUnion(t: Type) =
        FSharpType.IsUnion(t, true) &&
        (let cases = FSharpType.GetUnionCases(t, true) in cases.Length = 1 && (cases.[0].GetFields()).Length = 1)

    /// <c>true</c> when <paramref name="t"/> is a single-case union whose wrapped field is a scalar (column) type.
    static member IsScalarSingleCaseUnion(t: Type) =
        FSharpSingleCaseUnionSupport.IsSingleCaseUnion t &&
        MappingSchema.Default.IsScalarType(((FSharpType.GetUnionCases(t, true)).[0].GetFields()).[0].PropertyType)

    static member GetConverter(unionType: Type) : IValueConverter =
        cache.GetOrAdd(unionType, build)

/// Supplies a <see cref="ValueConverterAttribute"/> for every scalar single-case-union member, so those
/// columns are recognised and converted during entity-descriptor construction.
type internal FSharpSingleCaseUnionMetadataReader() =

    static let memberType (mi: MemberInfo) : Type =
        match mi with
        | :? PropertyInfo as p -> p.PropertyType
        | :? FieldInfo    as f -> f.FieldType
        | _                    -> typeof<obj>

    interface IMetadataReader with
        member _.GetAttributes(_type: Type) =
            if FSharpSingleCaseUnionSupport.IsScalarSingleCaseUnion _type then
                [| ScalarTypeAttribute() :> MappingAttribute |]
            else
                Array.empty<MappingAttribute>

        member _.GetAttributes(_type: Type, memberInfo: MemberInfo) =
            let mt = memberType memberInfo
            if FSharpSingleCaseUnionSupport.IsScalarSingleCaseUnion mt then
                // DB type left unset: ColumnDescriptor resolves it from the converter's provider type against
                // the active provider-inclusive schema (preserving facets), as for F# option columns.
                [| ValueConverterAttribute(ValueConverter = FSharpSingleCaseUnionSupport.GetConverter mt) :> MappingAttribute |]
            else
                Array.empty<MappingAttribute>

        member _.GetDynamicColumns(_type: Type) = Array.empty<MemberInfo>
        member _.GetObjectID() = ".FSharpSingleCaseUnionMetadataReader."

namespace LinqToDB.FSharp

open System
open System.Collections.Concurrent
open System.Linq.Expressions
open System.Reflection

open LinqToDB.Mapping
open LinqToDB.Metadata

/// Provides automatic mapping support for F# <c>'T option</c> columns. A metadata reader supplies a
/// <see cref="ValueConverterAttribute"/> for every <c>'T option</c> member, mapping <c>Some v</c> to the
/// stored value and <c>None</c> to <c>NULL</c> - so option columns round-trip with no manual
/// <see cref="MappingSchema"/> configuration. Value-typed elements are routed through
/// <see cref="System.Nullable`1"/> so that, e.g., <c>int option</c> <c>None</c> stores as <c>NULL</c>
/// rather than <c>0</c> (issue #4646).
type internal FSharpOptionSupport =

    static let cache = ConcurrentDictionary<Type, IValueConverter>()

    // Builds the bidirectional value converter for a closed 'T option type using explicit expression
    // trees. F# None is represented as a null reference, which makes IsSome / None construction trivial.
    // For a value element 'a the DB-facing type is Nullable<'a> so that None stores as NULL rather than
    // default('a) - the root cause of issue #4646 (int option None stored as 0).
    static let build (optionType: Type) : IValueConverter =
        let elementType  = optionType.GetGenericArguments().[0]
        let providerType = if elementType.IsValueType then typedefof<Nullable<_>>.MakeGenericType(elementType) else elementType
        let valueProp    = optionType.GetProperty("Value") |> nonNull
        let someMethod   = optionType.GetMethod("Some", BindingFlags.Public ||| BindingFlags.Static) |> nonNull

        // ToProvider: fun (o: optionType) -> if o <> null then (provider)o.Value else default(provider)
        let oParam     = Expression.Parameter(optionType, "o")
        let someValue  = Expression.Property(oParam, valueProp) :> Expression
        let someStored = if elementType.IsValueType then Expression.Convert(someValue, providerType) :> Expression else someValue
        let toProvider =
            Expression.Lambda(
                Expression.Condition(
                    Expression.ReferenceNotEqual(oParam, Expression.Constant(null, optionType)),
                    someStored,
                    Expression.Default(providerType)),
                oParam)

        // FromProvider: fun (p: provider) -> if hasValue/p<>null then Some p else None(null)
        let pParam   = Expression.Parameter(providerType, "p")
        let none     = Expression.Constant(null, optionType) :> Expression
        let fromBody =
            if elementType.IsValueType then
                Expression.Condition(
                    Expression.Property(pParam, "HasValue"),
                    Expression.Call(someMethod, Expression.Property(pParam, "Value")),
                    none)
            else
                Expression.Condition(
                    Expression.ReferenceEqual(pParam, Expression.Constant(null, providerType)),
                    none,
                    Expression.Call(someMethod, pParam))
        let fromProvider = Expression.Lambda(fromBody, pParam)

        let converterType = typedefof<ValueConverter<_, _>>.MakeGenericType(optionType, providerType)
        match Activator.CreateInstance(converterType, [| box toProvider; box fromProvider; box true |]) with
        | :? IValueConverter as c -> c
        | _ -> raise (InvalidOperationException $"Failed to create F# option value converter for '{optionType}'")

    /// Returns <c>true</c> when <paramref name="t"/> is <c>Microsoft.FSharp.Core.FSharpOption&lt;_&gt;</c>.
    static member IsOption(t: Type) =
        t.IsGenericType && t.GetGenericTypeDefinition() = typedefof<_ option>

    /// Returns the cached <see cref="IValueConverter"/> for a closed <c>'T option</c> type.
    static member GetConverter(optionType: Type) : IValueConverter =
        cache.GetOrAdd(optionType, build)

/// Supplies a <see cref="ValueConverterAttribute"/> for every <c>'T option</c> member encountered, so
/// option columns are recognised and converted during entity-descriptor construction.
type internal FSharpOptionMetadataReader() =

    static let memberType (mi: MemberInfo) : Type =
        match mi with
        | :? PropertyInfo as p -> p.PropertyType
        | :? FieldInfo    as f -> f.FieldType
        | _                    -> typeof<obj>

    interface IMetadataReader with
        // Mark FSharpOption<_> as a scalar type so option members are treated as columns rather than
        // nested entities; the per-member ValueConverter (below) supplies the actual conversion.
        member _.GetAttributes(_type: Type) =
            if FSharpOptionSupport.IsOption _type then
                [| ScalarTypeAttribute() :> MappingAttribute |]
            else
                Array.empty<MappingAttribute>

        member _.GetAttributes(_type: Type, memberInfo: MemberInfo) =
            let mt = memberType memberInfo
            if FSharpOptionSupport.IsOption mt then
                let elementType = mt.GetGenericArguments().[0]
                // The column's DB type is the element's DB type; derive it from the default schema so the
                // option type (whose own DB type is undefined) maps to a concrete column type.
                // Limitation: a metadata reader has no access to the connection/provider mapping schema, so
                // this resolves against MappingSchema.Default only - provider-specific or user-custom DB-type
                // overrides for the element type are not honored (e.g. 'string option' maps to NVarChar, not
                // a provider's preferred VarChar). Set the column's DataType explicitly (attribute or fluent
                // mapping) when a provider-faithful type is required.
                let dbDataType  = MappingSchema.Default.GetDbDataType(elementType)
                [|
                    DataTypeAttribute(dbDataType.DataType, DbType = dbDataType.DbType) :> MappingAttribute
                    ValueConverterAttribute(ValueConverter = FSharpOptionSupport.GetConverter mt) :> MappingAttribute
                |]
            else
                Array.empty<MappingAttribute>

        member _.GetDynamicColumns(_type: Type) = Array.empty<MemberInfo>
        member _.GetObjectID() = ".FSharpOptionMetadataReader."

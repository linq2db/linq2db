namespace LinqToDB.FSharp

open System
open System.Collections.Concurrent
open System.Linq.Expressions
open System.Reflection

open LinqToDB.Mapping
open LinqToDB.Metadata

/// Provides automatic mapping support for F# <c>'T option</c> and <c>'T voption</c> columns. A metadata
/// reader supplies a <see cref="ValueConverterAttribute"/> for every option-typed member, mapping the
/// "some" case to the stored value and the "none" case to <c>NULL</c> - so option columns round-trip with
/// no manual <see cref="MappingSchema"/> configuration. Value-typed elements are routed through
/// <see cref="System.Nullable`1"/> so that, e.g., <c>int option</c> <c>None</c> stores as <c>NULL</c>
/// rather than <c>0</c> (issue #4646). Only options over a scalar element type are auto-mapped; an option
/// over a complex/entity element is left untouched.
type internal FSharpOptionSupport =

    static let cache = ConcurrentDictionary<Type, IValueConverter>()

    // Builds the bidirectional value converter for a closed option type ('T option or 'T voption) using
    // explicit expression trees. The DB-facing ("provider") type is the element type, except a
    // non-nullable value element 'a is wrapped in Nullable<'a> so None stores as NULL rather than
    // default('a) - the root cause of issue #4646 (int option None stored as 0).
    static let build (optionType: Type) : IValueConverter =
        let elementType   = optionType.GetGenericArguments().[0]
        let isValueOption = optionType.GetGenericTypeDefinition() = typedefof<_ voption>
        // Wrap only a non-nullable value element; a reference or already-Nullable<_> element already
        // carries null itself. The Nullable check also guards Nullable<Nullable<_>>, which MakeGenericType
        // rejects (e.g. a 'Nullable<int> option' column).
        let wrapInNullable = elementType.IsValueType && isNull (Nullable.GetUnderlyingType elementType)
        let providerType   = if wrapInNullable then typedefof<Nullable<_>>.MakeGenericType(elementType) else elementType

        let valueProp = optionType.GetProperty("Value") |> nonNull
        // "some" factory and "none" value differ between the reference option (None is a null reference,
        // Some is a static factory) and the struct value-option (ValueNone is a static value, ValueSome is
        // the NewValueSome factory).
        let someFactory =
            optionType.GetMethod((if isValueOption then "NewValueSome" else "Some"), BindingFlags.Public ||| BindingFlags.Static) |> nonNull
        let noneExpr : Expression =
            if isValueOption then
                Expression.Property(Unchecked.defaultof<Expression>, optionType.GetProperty("ValueNone", BindingFlags.Public ||| BindingFlags.Static) |> nonNull) :> Expression
            else
                Expression.Constant(null, optionType) :> Expression
        let isSome (o: Expression) : Expression =
            if isValueOption then Expression.Property(o, "IsValueSome") :> Expression
            else Expression.ReferenceNotEqual(o, Expression.Constant(null, optionType)) :> Expression

        // ToProvider: fun (o: option) -> if isSome o then (provider) o.Value else default(provider)
        let oParam     = Expression.Parameter(optionType, "o")
        let someValue  = Expression.Property(oParam, valueProp) :> Expression
        let someStored = if wrapInNullable then Expression.Convert(someValue, providerType) :> Expression else someValue
        let toProvider =
            Expression.Lambda(
                Expression.Condition(isSome oParam, someStored, Expression.Default(providerType)),
                oParam)

        // FromProvider: fun (p: provider) -> if p has value then someFactory(elementOf p) else none
        let pParam   = Expression.Parameter(providerType, "p")
        let hasValue : Expression =
            if elementType.IsValueType then Expression.Property(pParam, "HasValue") :> Expression
            else Expression.ReferenceNotEqual(pParam, Expression.Constant(null, providerType)) :> Expression
        let element  : Expression =
            if wrapInNullable then Expression.Property(pParam, "Value") :> Expression else pParam :> Expression
        let fromProvider =
            Expression.Lambda(Expression.Condition(hasValue, Expression.Call(someFactory, element), noneExpr), pParam)

        let converterType = typedefof<ValueConverter<_, _>>.MakeGenericType(optionType, providerType)
        match Activator.CreateInstance(converterType, [| box toProvider; box fromProvider; box true |]) with
        | :? IValueConverter as c -> c
        | _ -> raise (InvalidOperationException $"Failed to create F# option value converter for '{optionType}'")

    /// Returns <c>true</c> when <paramref name="t"/> is <c>FSharpOption&lt;_&gt;</c> or <c>FSharpValueOption&lt;_&gt;</c>.
    static member IsOption(t: Type) =
        t.IsGenericType &&
        (let d = t.GetGenericTypeDefinition() in d = typedefof<_ option> || d = typedefof<_ voption>)

    /// Returns <c>true</c> when <paramref name="t"/> is an option type whose element is a scalar (column)
    /// type. An option over a complex/entity element is not treated as a column.
    static member IsScalarOption(t: Type) =
        FSharpOptionSupport.IsOption t && MappingSchema.Default.IsScalarType(t.GetGenericArguments().[0])

    /// Returns the cached <see cref="IValueConverter"/> for a closed option type.
    static member GetConverter(optionType: Type) : IValueConverter =
        cache.GetOrAdd(optionType, build)

/// Supplies a <see cref="ValueConverterAttribute"/> for every scalar <c>'T option</c> / <c>'T voption</c>
/// member encountered, so option columns are recognised and converted during entity-descriptor construction.
type internal FSharpOptionMetadataReader() =

    static let memberType (mi: MemberInfo) : Type =
        match mi with
        | :? PropertyInfo as p -> p.PropertyType
        | :? FieldInfo    as f -> f.FieldType
        | _                    -> typeof<obj>

    interface IMetadataReader with
        // Mark an option type as scalar (only when its element is itself scalar) so option members are
        // treated as columns rather than nested entities; the per-member ValueConverter (below) supplies
        // the actual conversion.
        member _.GetAttributes(_type: Type) =
            if FSharpOptionSupport.IsScalarOption _type then
                [| ScalarTypeAttribute() :> MappingAttribute |]
            else
                Array.empty<MappingAttribute>

        member _.GetAttributes(_type: Type, memberInfo: MemberInfo) =
            let mt = memberType memberInfo
            if FSharpOptionSupport.IsScalarOption mt then
                // An option column is always nullable (the "none" case maps to NULL). A reference option is
                // nullable by virtue of its type, but a struct value-option ('T voption) is a non-nullable
                // value type, so the column must be marked CanBeNull explicitly - otherwise the DDL emits
                // NOT NULL and rejects the "none" case.
                // The DB type is intentionally left unset: with no explicit DataType, ColumnDescriptor resolves
                // it from the value converter's provider type (the element, or Nullable<element>) against the
                // active provider-inclusive schema, so provider-faithful facets (decimal precision/scale, string
                // length, etc.) are preserved - unlike deriving here from MappingSchema.Default, which has no
                // provider context and would truncate them (#5645; e.g. 'decimal option' -> decimal(18,0)).
                [|
                    ColumnAttribute(CanBeNull = true) :> MappingAttribute
                    ValueConverterAttribute(ValueConverter = FSharpOptionSupport.GetConverter mt) :> MappingAttribute
                |]
            else
                Array.empty<MappingAttribute>

        member _.GetDynamicColumns(_type: Type) = Array.empty<MemberInfo>
        member _.GetObjectID() = ".FSharpOptionMetadataReader."

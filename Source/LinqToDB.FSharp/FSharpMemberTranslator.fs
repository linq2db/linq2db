namespace LinqToDB.FSharp

open System
open System.Linq.Expressions

open LinqToDB.Linq.Translation
open LinqToDB.Internal.Expressions
open LinqToDB.Internal.DataProvider.Translation

/// Translates F# <c>option</c> / <c>voption</c> member access in a query to SQL - <c>opt.IsSome</c> ->
/// the column <c>IS NOT NULL</c>, <c>opt.IsNone</c> -> <c>IS NULL</c>, <c>opt.Value</c> -> the underlying
/// column value. Option columns already map to a nullable column (<see cref="FSharpOptionSupport"/>), but
/// their FSharpOption/FSharpValueOption members carry no SQL mapping; without this the translator throws
/// "'FSharpOption&lt;...&gt;.get_IsSome(x)' could not be converted to SQL". Registered by <c>UseFSharp()</c>.
/// Matching is done generically in <see cref="TranslateOverrideHandler"/> (the member is generic over the
/// element type, which the pattern registry can't match for a property).
type FSharpMemberTranslator private () =
    inherit MemberTranslatorBase()

    static let _instance = FSharpMemberTranslator()

    let isOption (t: Type) = FSharpOptionSupport.IsOption t

    // The FSharp.Core module functions are static one-argument calls on these two types:
    // Option.isSome/isNone/get -> OptionModule.IsSome/IsNone/GetValue, and the ValueOption equivalents.
    // Checking the declaring type keeps a user-defined static of the same name from being claimed.
    let isOptionModuleCall (mc: MethodCallExpression) =
        mc.Arguments.Count = 1 && isOption mc.Arguments.[0].Type &&
        (match mc.Method.DeclaringType with
         | null -> false
         | t    -> t.Namespace = "Microsoft.FSharp.Core" && (t.Name = "OptionModule" || t.Name = "ValueOption"))

    // IS [NOT] NULL on the operand's column placeholder; declines (null) when the operand isn't a column.
    let translateIsNull (isNot: bool) (ctx: ITranslationContext) (operand: Expression) (basedOn: Expression) : Expression | null =
        match ctx.Translate(operand, TranslationFlags.Sql) with
        | :? SqlPlaceholderExpression as ph ->
            let f  = ctx.ExpressionFactory
            let sc = f.SearchCondition()
            sc.Add(f.IsNull(ph.Sql, isNot)) |> ignore
            ctx.CreatePlaceholder(ctx.CurrentSelectQuery, sc, basedOn)
        | _ -> null

    // opt.Value -> the operand's column placeholder re-typed to the element (matching how the core unwraps
    // Nullable<T>.Value); declines when the operand isn't a column.
    let translateValue (ctx: ITranslationContext) (operand: Expression) (valueType: Type) : Expression | null =
        match ctx.Translate(operand, TranslationFlags.Sql) with
        | :? SqlPlaceholderExpression as ph -> ph.WithType(valueType)
        | _ -> null

    /// Shared stateless instance, reused across every <c>UseFSharp()</c> call. Member translators are keyed
    /// by instance identity in the DataContextOptions ConfigurationID (the query-cache key), so a fresh
    /// instance per call would give every context a distinct id and defeat the query cache.
    static member Instance = _instance

    override _.TranslateOverrideHandler(ctx: ITranslationContext, expr: Expression, _flags: TranslationFlags) : Expression | null =
        match expr with
        // property form: x.Opt.IsSome / .IsNone / .Value
        | :? MemberExpression as m when not (isNull m.Expression) && isOption (nonNull m.Expression).Type ->
            match m.Member.Name with
            // voption also exposes the generated case testers IsValueSome / IsValueNone
            | "IsSome" | "IsValueSome" -> translateIsNull true  ctx (nonNull m.Expression) expr
            | "IsNone" | "IsValueNone" -> translateIsNull false ctx (nonNull m.Expression) expr
            | "Value"                  -> translateValue ctx (nonNull m.Expression) m.Type
            | _                        -> null
        // getter-method form: FSharpOption.get_IsSome(x.Opt) (static) / x.Opt.get_Value() (instance)
        | :? MethodCallExpression as mc ->
            match mc.Method.Name with
            | "get_IsSome" when mc.Arguments.Count = 1 && isOption mc.Arguments.[0].Type -> translateIsNull true  ctx mc.Arguments.[0] expr
            | "get_IsNone" when mc.Arguments.Count = 1 && isOption mc.Arguments.[0].Type -> translateIsNull false ctx mc.Arguments.[0] expr
            | "get_Value"  when not (isNull mc.Object) && isOption (nonNull mc.Object).Type -> translateValue ctx (nonNull mc.Object) mc.Type
            // module-function form: Option.isSome x / ValueOption.get x / ...
            | "IsSome"   when isOptionModuleCall mc -> translateIsNull true  ctx mc.Arguments.[0] expr
            | "IsNone"   when isOptionModuleCall mc -> translateIsNull false ctx mc.Arguments.[0] expr
            | "GetValue" when isOptionModuleCall mc -> translateValue ctx mc.Arguments.[0] mc.Type
            | _ -> null
        | _ -> null

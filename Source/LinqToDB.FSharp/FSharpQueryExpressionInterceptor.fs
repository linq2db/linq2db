namespace LinqToDB.FSharp

open System
open System.Linq.Expressions

open Microsoft.FSharp.Quotations

open LinqToDB.Expressions
open LinqToDB.Interceptors
open LinqToDB.Mapping
open LinqToDB.Reflection
open LinqToDB.Internal.Reflection

/// Rewrites F#-specific expression-tree shapes the core translator does not understand:
///   * reduces the F# quotation machinery a captured-variable lambda compiles to -
///       LeafExpressionConverter.QuotationToLambdaExpression(SubstHelper(quotation, freeVars, capturedValues))
///     (emitted for e.g. `.LeftJoin(fun y -> ... outerVar ...)` / `.Where(fun a -> ... outerVar ...)`) - into
///     the plain `Quote(lambda)` a C# `y => ...` would emit, with the free vars replaced by the captured outer
///     value-expressions. Without this the translator's UnwrapLambda casts the raw MethodCallExpression to
///     LambdaExpression and throws (issue #1813).
///   * inlines the unnecessary block F# emits for record construction
///       { var x = expr1; new type(x, expr2) }  ->  new type(expr1, expr2)
///   * turns an F# record-copy update (`q.Update(p, fun r -> { r with Field = v })`) into the explicit
///     partial-update form `q.Where(p).Set(x => x.Field, x => v).Update()`, so only the changed columns
///     are written. A ctor argument that is a self-copy `r.SameField` is dropped; when *every* argument
///     is a self-copy (a literal no-op such as `{ r with Field = r.Field }`), the change set is empty, so
///     every non-PK column is assigned to its own value instead - this keeps the PK out of SET (which YDB
///     rejects) rather than re-emitting the full all-column UPDATE.
/// (Kept out of core so F# quirks live in the F# library.)
type private FSharpRewriteVisitor(mappingSchema: MappingSchema) =
    inherit ExpressionVisitor()

    // Rewrites an F# Update(... , setter) call into a Where(...)?.Set(...).Update() chain that assigns
    // only the members whose ctor argument is not a self-copy `row.SameMember`.
    member private _.RewriteUpdate(mc: MethodCallExpression, hasPredicate: bool) : Expression =
        let entityType = mc.Method.GetGenericArguments().[0]
        let source     = mc.Arguments.[0]
        let setterArg  = mc.Arguments.[mc.Arguments.Count - 1]
        match setterArg with
        | :? UnaryExpression as q when q.NodeType = ExpressionType.Quote ->
            match q.Operand with
            | :? LambdaExpression as setter when
                    setter.Parameters.Count = 1
                    && (setter.Body :? NewExpression)
                    && FSharpEntityBindingInterceptor.isRecord setter.Body.Type ->
                let ne = setter.Body :?> NewExpression
                match FSharpEntityBindingInterceptor.TryMapMembersToConstructor(TypeAccessor.GetAccessor ne.Type) with
                | Some map ->
                    let rowParam = setter.Parameters.[0]
                    let changed  = ResizeArray<MemberAccessor * Expression>()
                    for i in 0 .. ne.Arguments.Count - 1 do
                        match map.TryGetValue i with
                        | true, ma ->
                            let arg = ne.Arguments.[i]
                            let isSelfCopy =
                                match arg with
                                | :? MemberExpression as me -> me.Member = ma.MemberInfo && obj.ReferenceEquals(me.Expression, rowParam)
                                | _ -> false
                            if not isSelfCopy then changed.Add((ma, arg))
                        | _ -> ()

                    // when at least one column actually changes, assign just those; for a literal no-op
                    // `{ r with Field = r.Field }` (empty change set) assign every non-PK column to its own
                    // value, so the PK is kept out of SET (YDB rejects it) instead of re-emitting the full
                    // all-column UPDATE.
                    let assignments =
                        if changed.Count > 0 then
                            changed
                        else
                            let entityDescriptor = mappingSchema.GetEntityDescriptor entityType
                            let isPrimaryKey (ma: MemberAccessor) =
                                entityDescriptor.Columns |> Seq.exists (fun c -> c.IsPrimaryKey && c.MemberInfo = ma.MemberInfo)
                            let nonPk = ResizeArray<MemberAccessor * Expression>()
                            for i in 0 .. ne.Arguments.Count - 1 do
                                match map.TryGetValue i with
                                | true, ma when not (isPrimaryKey ma) -> nonPk.Add((ma, ne.Arguments.[i]))
                                | _ -> ()
                            nonPk

                    if assignments.Count = 0 then
                        mc :> Expression
                    else
                        let mutable chain : Expression = source
                        if hasPredicate then
                            chain <- Expression.Call(Methods.Queryable.Where.MakeGenericMethod(entityType), source, mc.Arguments.[1])

                        let mutable first = true
                        for (ma, valueExpr) in assignments do
                            let x       = Expression.Parameter(entityType, "x")
                            let extract = Expression.Lambda(Expression.MakeMemberAccess(x, ma.MemberInfo), [| x |])
                            let update  = Expression.Lambda(valueExpr.Replace(rowParam, x), [| x |])
                            let setM    = if first then Methods.LinqToDB.Update.SetQueryablePrev else Methods.LinqToDB.Update.SetUpdatablePrev
                            chain <- Expression.Call(setM.MakeGenericMethod(entityType, ma.Type), chain, Expression.Quote extract, Expression.Quote update)
                            first <- false

                        Expression.Call(Methods.LinqToDB.Update.UpdateUpdatable.MakeGenericMethod(entityType), chain) :> Expression
                | None -> mc :> Expression
            | _ -> mc :> Expression
        | _ -> mc :> Expression

    // Do not descend into non-reducible linq2db custom nodes: the BCL ExpressionVisitor would call
    // VisitChildren on them and throw "must be reducible node". F# constructs we rewrite live in the raw
    // standard-node tree, so leaving extension nodes untouched is safe.
    override _.VisitExtension(node: Expression) : Expression = node

    override this.VisitBlock(node: BlockExpression) =
        // block items must be: N assignments to block variables + a result expression
        if node.Variables.Count > 0
           && node.Variables.Count + 1 = node.Expressions.Count
           && obj.ReferenceEquals(node.Result, node.Expressions.[node.Expressions.Count - 1]) then

            let mutable result : Expression = node.Result
            let mutable simplified = true
            let mutable i = node.Expressions.Count - 2

            while simplified && i >= 0 do
                match node.Expressions.[i] with
                | :? BinaryExpression as assign when
                        assign.NodeType = ExpressionType.Assign
                        && isNull assign.Method
                        && (assign.Left :? ParameterExpression) ->
                    let variable = assign.Left :?> ParameterExpression
                    let value    = assign.Right
                    // embed only one of the block's own variables, whose value does not reference itself,
                    // used at most once in the result (count = 0 accepted: F# can emit unused variables).
                    if not (node.Variables.Contains variable)
                       || value.GetCount(variable, (fun v n -> obj.ReferenceEquals(n, v))) <> 0
                       || result.GetCount(variable, (fun v n -> obj.ReferenceEquals(n, v))) > 1 then
                        simplified <- false
                    else
                        result <- result.Replace(variable, value)
                | _ -> simplified <- false
                i <- i - 1

            if simplified then this.Visit result |> nonNull
            else base.VisitBlock node
        else
            base.VisitBlock node

    // A call on Microsoft.FSharp.Linq.RuntimeHelpers.LeafExpressionConverter, matched by name + declaring-type
    // full name (never a cached MethodInfo) so it tolerates the FSharp.Core version difference between the
    // net10.0 (10.1.x) and net462 (v9) builds.
    member private _.IsLeafCall (m: System.Reflection.MethodInfo) (name: string) =
        match m.DeclaringType with
        | null -> false
        | dt   -> m.Name = name
                  && String.Equals(dt.FullName, "Microsoft.FSharp.Linq.RuntimeHelpers.LeafExpressionConverter", StringComparison.Ordinal)

    // Evaluate a self-contained builder sub-expression (the quotation / the Var[]) to its runtime value.
    // Never used on the captured-values array - those reference outer query parameters with no value here.
    member private _.Eval (e: Expression) : obj =
        Expression.Lambda(e).Compile().DynamicInvoke() |> nonNull

    // Distinct placeholder instance for a free var's type, matched later by reference identity. GetUninitializedObject
    // lives on RuntimeHelpers (net5+) or FormatterServices (netfx / netstandard fallback); resolve it by reflection
    // so this assembly compiles for every TFM (net462 / netstandard2.0 / net8-10) without a direct reference.
    member private _.CreateSentinel (t: Type) : obj =
        let getMethod (typeName: string) =
            match Type.GetType typeName with
            | null -> null
            | ty   -> ty.GetMethod("GetUninitializedObject", [| typeof<Type> |])
        let m =
            match getMethod "System.Runtime.CompilerServices.RuntimeHelpers" with
            | null -> getMethod "System.Runtime.Serialization.FormatterServices"
            | m    -> m
        (nonNull m).Invoke(null, [| box t |]) |> nonNull

    // Strip the Convert(_, obj) box F# wraps captured values in, then re-Convert to the reduced parameter's
    // type when they differ, so the substituted value-expression is assignment-compatible with the parameter.
    member private _.StripBox (e: Expression) (targetType: Type) : Expression =
        let operand =
            match e with
            | :? UnaryExpression as u when
                    (u.NodeType = ExpressionType.Convert || u.NodeType = ExpressionType.ConvertChecked)
                    && u.Type = typeof<obj> -> u.Operand
            | _ -> e
        if operand.Type = targetType then operand
        else Expression.Convert(operand, targetType) :> Expression

    // Reduce QuotationToLambdaExpression(SubstHelper(q, freeVars, capturedValues)) - or a bare
    // QuotationToLambdaExpression(q) with no captures - to Quote(lambda). We re-run F#'s own SubstHelper and
    // QuotationToLambdaExpression (the exact MethodInfos from the tree, already closed over the predicate
    // delegate type) but substitute each free var with a distinct uninitialized sentinel instance; that yields
    // a real Expression<Func<..>> whose free-var sites are sentinel constants, which we then replace with the
    // captured outer value-expressions - reproducing the correlated predicate a C# lambda would have emitted.
    member private this.TryReduceFSharpQuotation (node: MethodCallExpression) : Expression option =
        if not (this.IsLeafCall node.Method "QuotationToLambdaExpression") || node.Arguments.Count <> 1 then
            None
        else
            try
                match node.Arguments.[0] with
                | :? MethodCallExpression as sh when this.IsLeafCall sh.Method "SubstHelper" && sh.Arguments.Count = 3 ->
                    let valueExprs =
                        match sh.Arguments.[2] with
                        | :? NewArrayExpression as na -> Array.ofSeq na.Expressions
                        | _                           -> [||]
                    // Evaluate the quotation and the Var[] in a single execution so a shared free-var node keeps
                    // its identity (SubstHelper matches free vars by reference).
                    let boxed =
                        Expression.NewArrayInit(
                            typeof<obj>,
                            [| Expression.Convert(sh.Arguments.[0], typeof<obj>) :> Expression
                               Expression.Convert(sh.Arguments.[1], typeof<obj>) :> Expression |])
                    let arr  = this.Eval boxed :?> obj[]
                    let vars = arr.[1] :?> Var[]
                    if vars.Length = 0 || vars.Length <> valueExprs.Length then None
                    else
                        let sentinels = vars |> Array.map (fun v -> this.CreateSentinel v.Type)
                        let subst  = sh.Method.Invoke(null, [| arr.[0]; box vars; box sentinels |])
                        let lam    = (node.Method.Invoke(null, [| subst |]) |> nonNull) :?> LambdaExpression
                        let map    = System.Collections.Generic.Dictionary<obj, Expression>(HashIdentity.Reference)
                        for i in 0 .. sentinels.Length - 1 do
                            map.[sentinels.[i]] <- this.StripBox valueExprs.[i] vars.[i].Type
                        let replaced =
                            lam.Transform(fun e ->
                                match e with
                                | :? ConstantExpression as c ->
                                    match c.Value with
                                    | null -> e
                                    | v when map.ContainsKey v -> map.[v]
                                    | _ -> e
                                | _ -> e)
                            |> nonNull
                        // Re-normalize any residual F# shapes, then wrap the way the translator's UnwrapLambda
                        // expects (it peels Quote/Convert, then casts to LambdaExpression).
                        Some (Expression.Quote(this.Visit (replaced :?> LambdaExpression) |> nonNull))
                | bare ->
                    // No captured variables: convert the quotation directly with F#'s own method.
                    let q   = this.Eval bare
                    let lam = (node.Method.Invoke(null, [| q |]) |> nonNull) :?> LambdaExpression
                    Some (Expression.Quote(this.Visit lam |> nonNull))
            // Fail safe: any unexpected shape/evaluation surprise leaves the node untouched (no worse than
            // before this rewrite existed), so only genuinely-matching F# quotation predicates are transformed.
            with _ -> None

    override this.VisitMethodCall(node: MethodCallExpression) =
        match this.TryReduceFSharpQuotation node with
        | Some reduced -> reduced
        | None ->
            let visited = base.VisitMethodCall node
            match visited with
            | :? MethodCallExpression as mc when mc.Method.IsGenericMethod ->
                let def = mc.Method.GetGenericMethodDefinition()
                if   def = Methods.LinqToDB.Update.UpdateSetter          then this.RewriteUpdate(mc, false)
                elif def = Methods.LinqToDB.Update.UpdatePredicateSetter then this.RewriteUpdate(mc, true)
                else visited
            | _ -> visited

/// Handles F#-specific expression-tree shapes that the core translator does not understand.
type FSharpQueryExpressionInterceptor private () =

    static let _instance = FSharpQueryExpressionInterceptor() :> IQueryExpressionInterceptor

    /// Interceptor instance.
    static member Instance = _instance

    interface IQueryExpressionInterceptor with
        member _.ProcessExpression(expression: Expression, args: QueryExpressionArgs) : Expression =
            // Only the raw, pre-expose query tree (standard nodes) - the post-expose tree carries
            // linq2db custom nodes the F# constructs we handle never appear in.
            if args.Kind = QueryExpressionArgs.ExpressionKind.Query then
                FSharpRewriteVisitor(args.DataContext.MappingSchema).Visit expression |> nonNull
            else
                expression

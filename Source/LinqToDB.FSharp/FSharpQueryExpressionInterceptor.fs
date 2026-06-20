namespace LinqToDB.FSharp

open System.Linq.Expressions

open LinqToDB.Expressions
open LinqToDB.Interceptors
open LinqToDB.Reflection
open LinqToDB.Internal.Reflection

/// Rewrites F#-specific expression-tree shapes the core translator does not understand:
///   * inlines the unnecessary block F# emits for record construction
///       { var x = expr1; new type(x, expr2) }  ->  new type(expr1, expr2)
///   * turns an F# record-copy update (`q.Update(p, fun r -> { r with Field = v })`) into the explicit
///     partial-update form `q.Where(p).Set(x => x.Field, x => v).Update()`, so only the changed columns
///     are written. A ctor argument that is a self-copy `r.SameField` is dropped; when *every* argument
///     is a self-copy (a literal no-op such as `{ r with Field = r.Field }`), the change set is empty and
///     the original full-record construction is left as-is, so that no-op input still emits an all-column
///     UPDATE (PK included) - acceptable, since such input changes no column anyway.
/// (Kept out of core so F# quirks live in the F# library.)
type private FSharpRewriteVisitor() =
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

                    if changed.Count = 0 then
                        mc :> Expression
                    else
                        let mutable chain : Expression = source
                        if hasPredicate then
                            chain <- Expression.Call(Methods.Queryable.Where.MakeGenericMethod(entityType), source, mc.Arguments.[1])

                        let mutable first = true
                        for (ma, valueExpr) in changed do
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

    override this.VisitMethodCall(node: MethodCallExpression) =
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
                FSharpRewriteVisitor().Visit expression |> nonNull
            else
                expression

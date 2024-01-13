namespace LinqToDB.FSharp

open System
open System.Collections
open System.Collections.Concurrent
open System.Collections.Generic
open System.Linq
open System.Linq.Expressions
open System.Reflection

open LinqToDB.Expressions
open LinqToDB.Extensions
open LinqToDB.Interceptors.Internal
open LinqToDB.Reflection

/// Provides Linq To DB interceptor with F# support implementation.
[<AllowNullLiteral>]
type FSharpEntityBindingInterceptor private () =
    inherit EntityBindingInterceptor()

    static let _cache = ConcurrentDictionary<Type, Dictionary<int, MemberAccessor> option>()

    static let _instance = FSharpEntityBindingInterceptor() :> IEntityBindingInterceptor

    /// Interceptor instance.
    static member Instance
        with get() = _instance

    static member isRecord(objectType: Type) =
        if AttributesExtensions.HasAttribute<CLIMutableAttribute> objectType = true
            then false
        else
            let mapping = objectType.GetAttribute<CompilationMappingAttribute>()
            if mapping :> obj = null
            then false
            else mapping.SourceConstructFlags = SourceConstructFlags.RecordType

    override x.TryMapMembersToConstructor(typeAccessor: TypeAccessor) : IReadOnlyDictionary<int, MemberAccessor> =
        let found, map = _cache.TryGetValue typeAccessor.Type
        if found
        then
            match map with
            | Some m -> m
            | None -> null
        else
            if FSharpEntityBindingInterceptor.isRecord typeAccessor.Type
            then
                let mappings = Dictionary<int, MemberAccessor>()
                for m in typeAccessor.Members do
                    let memberAttr = m.MemberInfo.GetAttribute<CompilationMappingAttribute> true
                    if memberAttr :> obj = null
                    then ()
                    else
                        if memberAttr.SourceConstructFlags = SourceConstructFlags.Field
                        then mappings.Add(memberAttr.SequenceNumber, m)
                        else ()
                match _cache.GetOrAdd(typeAccessor.Type, Some mappings) with
                | Some m -> m
                | None -> null
            else null

    override x.ConvertConstructorExpression(expression: SqlGenericConstructorExpression) : SqlGenericConstructorExpression =
        if expression.ConstructType = SqlGenericConstructorExpression.CreateType.Full
        then
            let map = x.TryMapMembersToConstructor(TypeAccessor.GetAccessor expression.ObjectType)
            match map with
            | null -> expression
            | _ ->
                let constructors = expression.ObjectType.GetConstructors(BindingFlags.Instance ||| BindingFlags.NonPublic ||| BindingFlags.Public)
                let ctor = query {
                    for c in constructors do
                    where (c.GetParameters().Length >= map.Count)
                    exactlyOne
                }
                let parameters = ctor.GetParameters()
                let arguments: Expression array = Array.zeroCreate parameters.Length
                let assignments = expression.Assignments.ToDictionary((fun a -> a.MemberInfo), fun a -> a.Expression)
                for i in 0 .. parameters.Length - 1 do
                    match map.TryGetValue i with
                    | (true, ma) ->
                        match assignments.TryGetValue ma.MemberInfo with
                        | (true, expr) ->
                            arguments.[i] <- expr
                            assignments.Remove ma.MemberInfo
                        | _ ->
                            let memberType = ma.MemberInfo.GetMemberType()
                            arguments.[i] <- Expression.Constant(expression.MappingSchema.GetDefaultValue(memberType), memberType)
                            false
                    | _ ->
                        let memberType = parameters[i].ParameterType
                        arguments.[i] <- Expression.Constant(expression.MappingSchema.GetDefaultValue(memberType), memberType)
                        false
                    |> ignore

                let newExpr = Expression.New(ctor, arguments)

                if assignments.Count > 0
                then
                    let bindings = assignments.Select(fun kvp -> Expression.Bind(kvp.Key, kvp.Value) :> MemberBinding)
                    let initExpression = Expression.MemberInit(newExpr, bindings)
                    SqlGenericConstructorExpression(initExpression)
                else
                    let initExpression = Expression.MemberInit newExpr
                    SqlGenericConstructorExpression(initExpression)
        else expression

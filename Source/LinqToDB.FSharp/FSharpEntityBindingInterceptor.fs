namespace LinqToDB.FSharp

open System
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
type FSharpEntityBindingInterceptor private () =
    inherit EntityBindingInterceptor()

    static let _cache = ConcurrentDictionary<Type, IReadOnlyDictionary<int, MemberAccessor> option>()

    static let _instance = FSharpEntityBindingInterceptor() :> IEntityBindingInterceptor

    /// Interceptor instance.
    static member Instance
        with get() = _instance

    static member isRecord(objectType: Type) =
        if AttributesExtensions.HasAttribute<CLIMutableAttribute> objectType = true
            then false
        else
            let mapping = objectType.GetAttribute<CompilationMappingAttribute>()
            match mapping with
            | null -> false
            | attr -> attr.SourceConstructFlags = SourceConstructFlags.RecordType

    static member TryMapMembersToConstructor(typeAccessor: TypeAccessor) : IReadOnlyDictionary<int, MemberAccessor> option =
        let found, map = _cache.TryGetValue typeAccessor.Type
        if found
        then
            map
        else
            if FSharpEntityBindingInterceptor.isRecord typeAccessor.Type
            then
                let mappings = Dictionary<int, MemberAccessor>()
                for m in typeAccessor.Members do
                    let memberAttr = m.MemberInfo.GetAttribute<CompilationMappingAttribute> true
                    match memberAttr with
                    | null -> ()
                    | attr ->
                        if attr.SourceConstructFlags = SourceConstructFlags.Field
                        then mappings.Add(attr.SequenceNumber, m)
                        else ()
                _cache.GetOrAdd(typeAccessor.Type, Some(mappings :> IReadOnlyDictionary<int, MemberAccessor>))
            else None

    override x.ConvertConstructorExpression(expression: SqlGenericConstructorExpression) : SqlGenericConstructorExpression =
        if expression.ConstructType = SqlGenericConstructorExpression.CreateType.Full
        then
            match FSharpEntityBindingInterceptor.TryMapMembersToConstructor(TypeAccessor.GetAccessor expression.ObjectType) with
            | None -> expression
            | Some map ->
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
                            let defaultValue = match expression.MappingSchema with
                                                | null -> null
                                                | ms -> ms.GetDefaultValue(memberType)
                            arguments.[i] <- Expression.Constant(defaultValue, memberType)
                            false
                    | _ ->
                        let memberType = parameters[i].ParameterType
                        let defaultValue = match expression.MappingSchema with
                                            | null -> null
                                            | ms -> ms.GetDefaultValue(memberType)
                        arguments.[i] <- Expression.Constant(defaultValue, memberType)
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

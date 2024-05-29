namespace LinqToDB.FSharp

open LinqToDB.Extensions
open LinqToDB.Interceptors.Internal
open System
open System.Reflection
open System.Collections
open System.Collections.Generic
open LinqToDB.Reflection

[<AllowNullLiteral>]
type FSharpExpressionInterceptor private () =
    inherit ExpressionInterceptor()

    static let _instance = FSharpExpressionInterceptor()

    static member Instance
        with get() = _instance

    override x.TryMapMembersToConstructor(typeAccessor: TypeAccessor) : IReadOnlyDictionary<int, MemberAccessor> =
        if typeAccessor.Type.HasAttribute<CLIMutableAttribute> true
        then null
        else
            let mapping = typeAccessor.Type.GetAttribute<CompilationMappingAttribute>()
            if mapping :> obj = null
            then null
            else
                if mapping.SourceConstructFlags <> SourceConstructFlags.RecordType
                then null
                else
                    let mappings = Dictionary<int, MemberAccessor>()
                    for m in typeAccessor.Members do
                        let memberAttr = m.MemberInfo.GetAttribute<CompilationMappingAttribute> true
                        if memberAttr :> obj = null
                        then ()
                        else
                            if memberAttr.SourceConstructFlags = SourceConstructFlags.Field
                            then mappings.Add(memberAttr.SequenceNumber, m)
                            else ()
                    mappings

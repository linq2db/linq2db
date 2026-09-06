namespace LinqToDB.FSharp

open System
open System.Reflection

/// Reflection helpers shared by the F# metadata readers.
module internal FSharpMemberInfo =

    /// The declared type of a mapped member (property or field); <c>obj</c> for anything else.
    let memberType (mi: MemberInfo) : Type =
        match mi with
        | :? PropertyInfo as p -> p.PropertyType
        | :? FieldInfo    as f -> f.FieldType
        | _                    -> typeof<obj>

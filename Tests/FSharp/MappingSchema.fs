module Tests.FSharp.MappingSchema

open LinqToDB
open LinqToDB.Mapping



let Initialize () = 
    // Add a AddScalarType for all option types you plan on using.
    LinqToDB.Mapping.MappingSchema.Default.AddScalarType(typeof<string option>,          None, LinqToDB.DataType.NVarChar)
    LinqToDB.Mapping.MappingSchema.Default.SetConvertExpression<Option<_>,_>( fun x -> if x.IsSome then x.Value else None )
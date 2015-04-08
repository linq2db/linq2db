module Tests.FSharp.MappingTest

open Tests.FSharp.Models

open LinqToDB
open LinqToDB.Mapping
open NUnit.Framework

let MapSomeType () = 
    
    let optionDate = Some(DateTime.Now)

    let ms = MappingSchema()
    
    Assert.AreEqual(DataType.DateTime2, ms.GetDataType(optionDate.GetType()).DataType)

    ms.AddScalarType(typedefof<System.DateTime>, DataType.Char)

    Assert.AreEqual(DataType.Char, ms.GetDataType(optionDate.GetType()).DataType)
//    let dataType = 
//    //if db.
//    Assert.AreEqual(DataType.DateTime2, dataType.DataType)
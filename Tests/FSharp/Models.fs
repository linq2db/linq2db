namespace Tests.FSharp.Models

open LinqToDB
open LinqToDB.Mapping

type Gender = 
    | [<MapValue("M")>] Male = 0
    | [<MapValue("F")>] Female = 1
    | [<MapValue("U")>] Unknown = 2 
    | [<MapValue("O")>] Other = 3
type PersonID = int

type Person = 
    { [<SequenceName(ProviderName.Firebird, "PersonID")>]
      [<Column("PersonID"); Identity; PrimaryKey>]
      ID : int64
      [<NotNull>] 
      FirstName : string
      [<NotNull>]
      LastName : string
      [<Column>]
      MiddleName : string option
      Gender : Gender }

type Child = 
    { [<PrimaryKey>] ParentID : int
      [<PrimaryKey>] ChildID : int }


type FullName = { FirstName : string; MiddleName: string; LastName: string}
type LastName = { Value: string }
type NestedFullName = { FirstName : string; MiddleName: string; LastName: LastName}

[<Table("Person", IsColumnAttributeRequired=false)>]
[<Column("FirstName",  "Name.FirstName")>]
[<Column("MiddleName", "Name.MiddleName")>]
[<Column("LastName",   "Name.LastName")>]
type ComplexPerson = 
    { [<Identity>]
      [<SequenceName(ProviderName.Firebird, "PersonID")>]
      [<Column("PersonID", IsPrimaryKey=true)>]
      ID : int64
      Name : FullName
      Gender : string }

[<Table("Person", IsColumnAttributeRequired=false)>]
[<Column("FirstName",  "Name.FirstName")>]
[<Column("MiddleName", "Name.MiddleName")>]
[<Column("LastName",   "Name.LastName.Value")>]
type DeeplyComplexPerson = 
    { [<Identity>]
      [<SequenceName(ProviderName.Firebird, "PersonID")>]
      [<Column("PersonID", IsPrimaryKey=true)>]
      ID : int64
      Name : NestedFullName
      Gender : string }

//and Patient =
//    { [<PrimaryKey>]
//      PersonID : PersonID
//      Diagnosis : string
//      [<Association(ThisKey = "PersonID", OtherKey = "ID", CanBeNull = false)>]
//      Person : Person }

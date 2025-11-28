module Tests.FSharp.Issue1813

open Tests.FSharp.Models

open LinqToDB
open LinqToDB.Mapping
open System.Linq
open NUnit.Framework
open Tests
open Tests.Tools

[<Table>]
type Names =
    { [<PrimaryKey>]
      Id: int
      [<Column>]
      Name: string }

[<Table>]
type Addresses =
    { [<PrimaryKey>]
      Id: int
      [<Column>]
      Text: string }

[<Table>]
type TradeValid =
    { [<PrimaryKey>]
      Id: int
      [<Column>]
      DealNumber: int
      [<Column>]
      ParcelGroupID: int
      [<Column>]
      ParcelID: int }

[<Table>]
type NominationValid =
    { [<PrimaryKey>]
      Id: int
      [<Column>]
      DeliveryDealNumber: int
      [<Column>]
      DeliveryParcelGroup: int
      [<Column>]
      DeliveryParcelID: int
      [<Column>]
      ReceiptDealNumber: int
      [<Column>]
      ReceiptParcelGroup: int
      [<Column>]
      ReceiptParcelID: int }

let Issue1813Test1(db : IDataContext) =
    use table1 = db.CreateLocalTable<Names>()
    use table2 = db.CreateLocalTable<Addresses>()

    db.Insert({Names.Id=1; Name="name1"}) |> ignore
    db.Insert({Names.Id=2; Name="name2"}) |> ignore
    db.Insert({Addresses.Id=1; Text="address"}) |> ignore

    let query = query {
        for n in db.GetTable<Names>() do
        for a in db.GetTable<Addresses>().Where(fun a1 -> n.Id = a1.Id).DefaultIfEmpty() do
        sortBy n.Id
        select (n.Id, n.Name, a)
    }

    let result = query |> Seq.toArray

    Assert.That(result, Has.Length.EqualTo(2))
    Assert.That(result[0], Is.EqualTo( (1, "name1", {Addresses.Id=1; Text="address"}) ) )
    Assert.That(result[1], Is.EqualTo( (2, "name2", null) ) )

let Issue1813Test2(db : IDataContext) =
    use table1 = db.CreateLocalTable<Names>()
    use table2 = db.CreateLocalTable<Addresses>()

    db.Insert({Names.Id=1; Name="name1"}) |> ignore
    db.Insert({Names.Id=2; Name="name2"}) |> ignore
    db.Insert({Addresses.Id=1; Text="address"}) |> ignore

    let query = query {
        for n in db.GetTable<Names>() do
        leftOuterJoin a in db.GetTable<Addresses>() on (n.Id = a.Id) into g_a
        sortBy n.Id
        select (n.Id, n.Name, g_a)
    }

    let result = query |> Seq.toArray

    Assert.That(result, Has.Length.EqualTo(2))
    Assert.That(result[0], Is.EqualTo( (1, "name1", [{Addresses.Id=1; Text="address"}]) ) )
    Assert.That(result[1], Is.EqualTo( (2, "name2", [null]) ) )

let Issue1813Test3(db : IDataContext) =
    use table1 = db.CreateLocalTable<Names>()
    use table2 = db.CreateLocalTable<Addresses>()

    db.Insert({Names.Id=1; Name="name1"}) |> ignore
    db.Insert({Names.Id=2; Name="name2"}) |> ignore
    db.Insert({Addresses.Id=1; Text="address"}) |> ignore

    let query = query {
        for n in db.GetTable<Names>() do
        leftOuterJoin a in db.GetTable<Addresses>() on (n.Id = a.Id) into g_a
        for a in g_a do
        sortBy n.Id
        select (n.Id, n.Name, a)
    }

    let result = query |> Seq.toArray

    Assert.That(result, Has.Length.EqualTo(2))
    Assert.That(result[0], Is.EqualTo( (1, "name1", {Addresses.Id=1; Text="address"}) ) )
    Assert.That(result[1], Is.EqualTo( (2, "name2", null) ) )

let Issue1813Test4(db : IDataContext) =
    use table1 = db.CreateLocalTable<TradeValid>()
    use table2 = db.CreateLocalTable<NominationValid>()

    db.Insert({TradeValid.Id=1; DealNumber=2;ParcelGroupID=3;ParcelID=4}) |> ignore
    db.Insert({TradeValid.Id=2; DealNumber=3;ParcelGroupID=4;ParcelID=5}) |> ignore
    db.Insert({TradeValid.Id=3; DealNumber=5;ParcelGroupID=6;ParcelID=7}) |> ignore
    db.Insert({TradeValid.Id=4; DealNumber=8;ParcelGroupID=6;ParcelID=9}) |> ignore
    db.Insert({NominationValid.Id=1; DeliveryDealNumber=2;DeliveryParcelGroup=3;DeliveryParcelID=4; ReceiptDealNumber=9;ReceiptParcelGroup=9;ReceiptParcelID=9}) |> ignore
    db.Insert({NominationValid.Id=2; DeliveryDealNumber=9;DeliveryParcelGroup=9;DeliveryParcelID=9; ReceiptDealNumber=3;ReceiptParcelGroup=4;ReceiptParcelID=5}) |> ignore
    db.Insert({NominationValid.Id=3; DeliveryDealNumber=8;DeliveryParcelGroup=6;DeliveryParcelID=9; ReceiptDealNumber=3;ReceiptParcelGroup=4;ReceiptParcelID=5}) |> ignore
    db.Insert({NominationValid.Id=4; DeliveryDealNumber=2;DeliveryParcelGroup=3;DeliveryParcelID=4; ReceiptDealNumber=8;ReceiptParcelGroup=6;ReceiptParcelID=9}) |> ignore

    let query = query {
        for tr in db.GetTable<TradeValid>() do
        groupJoin n_del in db.GetTable<NominationValid>()
            on ((tr.DealNumber,tr.ParcelGroupID, tr.ParcelID) = (n_del.DeliveryDealNumber, n_del.DeliveryParcelGroup, n_del.DeliveryParcelID)) into n_del_g
        for x in n_del_g.DefaultIfEmpty() do
        groupJoin n_rec in db.GetTable<NominationValid>()
            on ((tr.DealNumber,tr.ParcelGroupID, tr.ParcelID) = (n_rec.ReceiptDealNumber, n_rec.ReceiptParcelGroup, n_rec.ReceiptParcelID)) into n_rec_g
        for y in n_rec_g.DefaultIfEmpty() do
        sortBy tr.Id
        yield (tr, x, y)
    }

    let result = query.Take(90) |> Seq.toArray

    Assert.That(result, Has.Length.EqualTo(4))
    Assert.That(result[0], Is.EqualTo( ({TradeValid.Id=1; DealNumber=2;ParcelGroupID=3;ParcelID=4}, {NominationValid.Id=1; DeliveryDealNumber=2;DeliveryParcelGroup=3;DeliveryParcelID=4; ReceiptDealNumber=9;ReceiptParcelGroup=9;ReceiptParcelID=9}, null) ) )
    Assert.That(result[0], Is.EqualTo( ({TradeValid.Id=2; DealNumber=3;ParcelGroupID=4;ParcelID=5}, null, {NominationValid.Id=2; DeliveryDealNumber=9;DeliveryParcelGroup=9;DeliveryParcelID=9; ReceiptDealNumber=3;ReceiptParcelGroup=4;ReceiptParcelID=5}) ) )
    Assert.That(result[0], Is.EqualTo( ({TradeValid.Id=3; DealNumber=5;ParcelGroupID=6;ParcelID=7}, {NominationValid.Id=3; DeliveryDealNumber=8;DeliveryParcelGroup=6;DeliveryParcelID=9; ReceiptDealNumber=3;ReceiptParcelGroup=4;ReceiptParcelID=5}, {NominationValid.Id=4; DeliveryDealNumber=2;DeliveryParcelGroup=3;DeliveryParcelID=4; ReceiptDealNumber=8;ReceiptParcelGroup=6;ReceiptParcelID=9}) ) )
    Assert.That(result[0], Is.EqualTo( ({TradeValid.Id=4; DealNumber=8;ParcelGroupID=6;ParcelID=9}, null, null) ) )

let Issue1813Test5(db : IDataContext) =
    use table1 = db.CreateLocalTable<TradeValid>()
    use table2 = db.CreateLocalTable<NominationValid>()

    db.Insert({TradeValid.Id=1; DealNumber=2;ParcelGroupID=3;ParcelID=4}) |> ignore
    db.Insert({TradeValid.Id=2; DealNumber=3;ParcelGroupID=4;ParcelID=5}) |> ignore
    db.Insert({TradeValid.Id=3; DealNumber=5;ParcelGroupID=6;ParcelID=7}) |> ignore
    db.Insert({TradeValid.Id=4; DealNumber=8;ParcelGroupID=6;ParcelID=9}) |> ignore
    db.Insert({NominationValid.Id=1; DeliveryDealNumber=2;DeliveryParcelGroup=3;DeliveryParcelID=4; ReceiptDealNumber=9;ReceiptParcelGroup=9;ReceiptParcelID=9}) |> ignore
    db.Insert({NominationValid.Id=2; DeliveryDealNumber=9;DeliveryParcelGroup=9;DeliveryParcelID=9; ReceiptDealNumber=3;ReceiptParcelGroup=4;ReceiptParcelID=5}) |> ignore
    db.Insert({NominationValid.Id=3; DeliveryDealNumber=8;DeliveryParcelGroup=6;DeliveryParcelID=9; ReceiptDealNumber=3;ReceiptParcelGroup=4;ReceiptParcelID=5}) |> ignore
    db.Insert({NominationValid.Id=4; DeliveryDealNumber=2;DeliveryParcelGroup=3;DeliveryParcelID=4; ReceiptDealNumber=8;ReceiptParcelGroup=6;ReceiptParcelID=9}) |> ignore

    let tradesQueryL1 = 
            query {
                for tr in db.GetTable<TradeValid>() do 
                groupJoin n_rec in db.GetTable<NominationValid>()
                    on ((tr.DealNumber,tr.ParcelGroupID, tr.ParcelID) = (n_rec.ReceiptDealNumber, n_rec.ReceiptParcelGroup, n_rec.ReceiptParcelID))  into n_rec_g
                for y in n_rec_g.DefaultIfEmpty() do 
                yield (tr, y)
                }

    let query =
        query {
            for (tr,y) in tradesQueryL1 do
                groupJoin n_del in db.GetTable<NominationValid>()
                    on ((tr.DealNumber,tr.ParcelGroupID, tr.ParcelID) = (n_del.DeliveryDealNumber, n_del.DeliveryParcelGroup, n_del.DeliveryParcelID))  into n_del_g
                for x in n_del_g.DefaultIfEmpty() do
                sortBy tr.Id
                yield (tr, x, y)
        }

    let result = query.Take(90) |> Seq.toArray

    Assert.That(result, Has.Length.EqualTo(4))
    Assert.That(result[0], Is.EqualTo( ({TradeValid.Id=1; DealNumber=2;ParcelGroupID=3;ParcelID=4}, {NominationValid.Id=1; DeliveryDealNumber=2;DeliveryParcelGroup=3;DeliveryParcelID=4; ReceiptDealNumber=9;ReceiptParcelGroup=9;ReceiptParcelID=9}, null) ) )
    Assert.That(result[0], Is.EqualTo( ({TradeValid.Id=2; DealNumber=3;ParcelGroupID=4;ParcelID=5}, null, {NominationValid.Id=2; DeliveryDealNumber=9;DeliveryParcelGroup=9;DeliveryParcelID=9; ReceiptDealNumber=3;ReceiptParcelGroup=4;ReceiptParcelID=5}) ) )
    Assert.That(result[0], Is.EqualTo( ({TradeValid.Id=3; DealNumber=5;ParcelGroupID=6;ParcelID=7}, {NominationValid.Id=3; DeliveryDealNumber=8;DeliveryParcelGroup=6;DeliveryParcelID=9; ReceiptDealNumber=3;ReceiptParcelGroup=4;ReceiptParcelID=5}, {NominationValid.Id=4; DeliveryDealNumber=2;DeliveryParcelGroup=3;DeliveryParcelID=4; ReceiptDealNumber=8;ReceiptParcelGroup=6;ReceiptParcelID=9}) ) )
    Assert.That(result[0], Is.EqualTo( ({TradeValid.Id=4; DealNumber=8;ParcelGroupID=6;ParcelID=9}, null, null) ) )

let Issue1813Test6(db : IDataContext) =
    use table1 = db.CreateLocalTable<TradeValid>()
    use table2 = db.CreateLocalTable<NominationValid>()

    db.Insert({TradeValid.Id=1; DealNumber=2;ParcelGroupID=3;ParcelID=4}) |> ignore
    db.Insert({TradeValid.Id=2; DealNumber=3;ParcelGroupID=4;ParcelID=5}) |> ignore
    db.Insert({TradeValid.Id=3; DealNumber=5;ParcelGroupID=6;ParcelID=7}) |> ignore
    db.Insert({TradeValid.Id=4; DealNumber=8;ParcelGroupID=6;ParcelID=9}) |> ignore
    db.Insert({NominationValid.Id=1; DeliveryDealNumber=2;DeliveryParcelGroup=3;DeliveryParcelID=4; ReceiptDealNumber=9;ReceiptParcelGroup=9;ReceiptParcelID=9}) |> ignore
    db.Insert({NominationValid.Id=2; DeliveryDealNumber=9;DeliveryParcelGroup=9;DeliveryParcelID=9; ReceiptDealNumber=3;ReceiptParcelGroup=4;ReceiptParcelID=5}) |> ignore
    db.Insert({NominationValid.Id=3; DeliveryDealNumber=8;DeliveryParcelGroup=6;DeliveryParcelID=9; ReceiptDealNumber=3;ReceiptParcelGroup=4;ReceiptParcelID=5}) |> ignore
    db.Insert({NominationValid.Id=4; DeliveryDealNumber=2;DeliveryParcelGroup=3;DeliveryParcelID=4; ReceiptDealNumber=8;ReceiptParcelGroup=6;ReceiptParcelID=9}) |> ignore

    let tradesQueryL1 = 
       query {
           for tr in db.GetTable<TradeValid>() do 
           for y in db.GetTable<NominationValid>()
            .LeftJoin(fun y -> 
                       y.ReceiptDealNumber = tr.DealNumber && 
                       y.ReceiptParcelGroup = tr.ParcelGroupID && 
                       y.ReceiptParcelID = tr.ParcelID) do 
           yield (tr, y)
           }

    let query =
        query {
            for (tr,y) in tradesQueryL1 do
                groupJoin n_del in db.GetTable<NominationValid>()
                    on ((tr.DealNumber,tr.ParcelGroupID, tr.ParcelID) = (n_del.DeliveryDealNumber, n_del.DeliveryParcelGroup, n_del.DeliveryParcelID))  into n_del_g
                for x in n_del_g.DefaultIfEmpty() do
                sortBy tr.Id
                yield (tr, x, y)
        }

    let result = query.Take(90) |> Seq.toArray

    Assert.That(result, Has.Length.EqualTo(4))
    Assert.That(result[0], Is.EqualTo( ({TradeValid.Id=1; DealNumber=2;ParcelGroupID=3;ParcelID=4}, {NominationValid.Id=1; DeliveryDealNumber=2;DeliveryParcelGroup=3;DeliveryParcelID=4; ReceiptDealNumber=9;ReceiptParcelGroup=9;ReceiptParcelID=9}, null) ) )
    Assert.That(result[0], Is.EqualTo( ({TradeValid.Id=2; DealNumber=3;ParcelGroupID=4;ParcelID=5}, null, {NominationValid.Id=2; DeliveryDealNumber=9;DeliveryParcelGroup=9;DeliveryParcelID=9; ReceiptDealNumber=3;ReceiptParcelGroup=4;ReceiptParcelID=5}) ) )
    Assert.That(result[0], Is.EqualTo( ({TradeValid.Id=3; DealNumber=5;ParcelGroupID=6;ParcelID=7}, {NominationValid.Id=3; DeliveryDealNumber=8;DeliveryParcelGroup=6;DeliveryParcelID=9; ReceiptDealNumber=3;ReceiptParcelGroup=4;ReceiptParcelID=5}, {NominationValid.Id=4; DeliveryDealNumber=2;DeliveryParcelGroup=3;DeliveryParcelID=4; ReceiptDealNumber=8;ReceiptParcelGroup=6;ReceiptParcelID=9}) ) )
    Assert.That(result[0], Is.EqualTo( ({TradeValid.Id=4; DealNumber=8;ParcelGroupID=6;ParcelID=9}, null, null) ) )

let Issue1813Test7(db : IDataContext) =
    use table1 = db.CreateLocalTable<TradeValid>()
    use table2 = db.CreateLocalTable<NominationValid>()

    db.Insert({TradeValid.Id=1; DealNumber=2;ParcelGroupID=3;ParcelID=4}) |> ignore
    db.Insert({TradeValid.Id=2; DealNumber=3;ParcelGroupID=4;ParcelID=5}) |> ignore
    db.Insert({TradeValid.Id=3; DealNumber=5;ParcelGroupID=6;ParcelID=7}) |> ignore
    db.Insert({TradeValid.Id=4; DealNumber=8;ParcelGroupID=6;ParcelID=9}) |> ignore
    db.Insert({NominationValid.Id=1; DeliveryDealNumber=2;DeliveryParcelGroup=3;DeliveryParcelID=4; ReceiptDealNumber=9;ReceiptParcelGroup=9;ReceiptParcelID=9}) |> ignore
    db.Insert({NominationValid.Id=2; DeliveryDealNumber=9;DeliveryParcelGroup=9;DeliveryParcelID=9; ReceiptDealNumber=3;ReceiptParcelGroup=4;ReceiptParcelID=5}) |> ignore
    db.Insert({NominationValid.Id=3; DeliveryDealNumber=8;DeliveryParcelGroup=6;DeliveryParcelID=9; ReceiptDealNumber=3;ReceiptParcelGroup=4;ReceiptParcelID=5}) |> ignore
    db.Insert({NominationValid.Id=4; DeliveryDealNumber=2;DeliveryParcelGroup=3;DeliveryParcelID=4; ReceiptDealNumber=8;ReceiptParcelGroup=6;ReceiptParcelID=9}) |> ignore

    let tradesQueryL1 = 
       query {
           for tr in db.GetTable<TradeValid>() do 
           for y in db.GetTable<NominationValid>()
            .LeftJoin(fun y -> 
                       y.ReceiptDealNumber = tr.DealNumber && 
                       y.ReceiptParcelGroup = tr.ParcelGroupID && 
                       y.ReceiptParcelID = tr.ParcelID) do 
           yield (tr, y)
           }

    let query =
        query {
            for (tr,y) in tradesQueryL1 do
            for x in db.GetTable<NominationValid>()
                .LeftJoin(fun x ->
                    x.ReceiptDealNumber = tr.DealNumber && 
                    x.ReceiptParcelGroup = tr.ParcelGroupID &&
                    x.ReceiptParcelID = tr.ParcelID) do
            sortBy tr.Id
            yield (tr, x, y)
        }

    let result = query.Take(90) |> Seq.toArray

    Assert.That(result, Has.Length.EqualTo(4))
    Assert.That(result[0], Is.EqualTo( ({TradeValid.Id=1; DealNumber=2;ParcelGroupID=3;ParcelID=4}, {NominationValid.Id=1; DeliveryDealNumber=2;DeliveryParcelGroup=3;DeliveryParcelID=4; ReceiptDealNumber=9;ReceiptParcelGroup=9;ReceiptParcelID=9}, null) ) )
    Assert.That(result[0], Is.EqualTo( ({TradeValid.Id=2; DealNumber=3;ParcelGroupID=4;ParcelID=5}, null, {NominationValid.Id=2; DeliveryDealNumber=9;DeliveryParcelGroup=9;DeliveryParcelID=9; ReceiptDealNumber=3;ReceiptParcelGroup=4;ReceiptParcelID=5}) ) )
    Assert.That(result[0], Is.EqualTo( ({TradeValid.Id=3; DealNumber=5;ParcelGroupID=6;ParcelID=7}, {NominationValid.Id=3; DeliveryDealNumber=8;DeliveryParcelGroup=6;DeliveryParcelID=9; ReceiptDealNumber=3;ReceiptParcelGroup=4;ReceiptParcelID=5}, {NominationValid.Id=4; DeliveryDealNumber=2;DeliveryParcelGroup=3;DeliveryParcelID=4; ReceiptDealNumber=8;ReceiptParcelGroup=6;ReceiptParcelID=9}) ) )
    Assert.That(result[0], Is.EqualTo( ({TradeValid.Id=4; DealNumber=8;ParcelGroupID=6;ParcelID=9}, null, null) ) )


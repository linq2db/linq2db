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

let private key (n: NominationValid | null) = match n with | null -> 0 | nn -> nn.Id
let private keyT (t: TradeValid | null) = match t with | null -> 0 | tt -> tt.Id

// Encodes a trade/delivery/receipt row set as a sorted "tradeId-deliveryId-receiptId" string (0 = no match),
// so the chained-join tests can compare their results order-tolerantly.
let private encodeJoins (result: (TradeValid * (NominationValid | null) * (NominationValid | null))[]) =
    result
    |> Array.map (fun (tr, x, y) -> sprintf "%d-%d-%d" tr.Id (key x) (key y))
    |> Array.sort
    |> String.concat ","

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

    // trade LEFT JOIN nom (Delivery) then LEFT JOIN nom (Receipt); x = Delivery match, y = Receipt match.
    // Same result set as Test5 (join order does not change it): 6 rows. Compared order-tolerantly within a trade.
    Assert.That(encodeJoins result, Is.EqualTo "1-1-0,1-4-0,2-0-2,2-0-3,3-0-0,4-3-4")

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

    // x = Delivery match, y = Receipt match (0 = no match). Trade 1 has two Delivery matches (N1,N4) and
    // trade 2 two Receipt matches (N2,N3), so the correct LEFT-join result is 6 rows. Encoded and sorted so the
    // comparison is order-tolerant within a trade (only tr.Id is ordered).
    Assert.That(encodeJoins result, Is.EqualTo "1-1-0,1-4-0,2-0-2,2-0-3,3-0-0,4-3-4")

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

    // Same shape as Test5 (Receipt LEFT JOIN via .LeftJoin, then Delivery groupJoin): x = Delivery match,
    // y = Receipt match. Correct result is 6 rows; encoded and sorted for order-tolerant comparison.
    Assert.That(encodeJoins result, Is.EqualTo "1-1-0,1-4-0,2-0-2,2-0-3,3-0-0,4-3-4")

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

    // Both joins are on Receipt (as written): x and y are each a Receipt match. Trade 2 has two Receipt
    // matches (N2,N3), so it yields the 2x2 cross (4 rows); total 7. Encoded and sorted for order-tolerance.
    Assert.That(encodeJoins result, Is.EqualTo "1-0-0,2-2-2,2-2-3,2-3-2,2-3-3,3-0-0,4-4-4")

// Regression pin: a captured interface-typed *local* used inside an F# join predicate translates
// correctly. Such a local is an ordinary closure capture rather than a SubstHelper free variable, so it
// takes the closure path and not the quotation reduction.
let Issue1813Test8(db : IDataContext) =
    use table1 = db.CreateLocalTable<Names>()
    use table2 = db.CreateLocalTable<Addresses>()

    db.Insert({Names.Id=1; Name="name1"}) |> ignore
    db.Insert({Names.Id=2; Name="name2"}) |> ignore
    db.Insert({Addresses.Id=1; Text="address"}) |> ignore

    let ids : System.Collections.Generic.IList<int> = System.Collections.Generic.List<int>([1])

    let query = query {
        for n in db.GetTable<Names>() do
        for a in db.GetTable<Addresses>().Where(fun a1 -> a1.Id = ids.[0] && n.Id = a1.Id).DefaultIfEmpty() do
        sortBy n.Id
        select (n.Id, n.Name, a)
    }

    let result = query |> Seq.toArray

    Assert.That(result, Has.Length.EqualTo(2))
    Assert.That(result[0], Is.EqualTo( (1, "name1", {Addresses.Id=1; Text="address"}) ) )
    Assert.That(result[1], Is.EqualTo( (2, "name2", null) ) )

// Regression pin: the outer query range variable is a Nullable<int> projected from an IQueryable, so the
// quotation reduction gets a SubstHelper free variable that is a value type rather than an entity.
let Issue1813Test9(db : IDataContext) =
    use table1 = db.CreateLocalTable<Names>()
    use table2 = db.CreateLocalTable<Addresses>()

    db.Insert({Names.Id=1; Name="name1"}) |> ignore
    db.Insert({Names.Id=2; Name="name2"}) |> ignore
    db.Insert({Addresses.Id=1; Text="address"}) |> ignore

    let ids = query {
        for n in db.GetTable<Names>() do
        select (System.Nullable<int> n.Id)
    }

    let query = query {
        for id in ids do
        for a in db.GetTable<Addresses>().Where(fun a1 -> System.Nullable<int> a1.Id = id).DefaultIfEmpty() do
        select (id, a)
    }

    let result = query |> Seq.toArray |> Array.sortBy (fun (id: System.Nullable<int>, _) -> id.Value)

    Assert.That(result, Has.Length.EqualTo(2))
    Assert.That(result[0], Is.EqualTo( (System.Nullable<int> 1, {Addresses.Id=1; Text="address"}) ) )
    Assert.That(result[1], Is.EqualTo( (System.Nullable<int> 2, null) ) )

// Regression pin: the outer query range variable is a string projected from an IQueryable - a type no
// placeholder instance can be built for, which is why the reduction substitutes marker calls.
let Issue1813Test10(db : IDataContext) =
    use table1 = db.CreateLocalTable<Names>()
    use table2 = db.CreateLocalTable<Addresses>()

    db.Insert({Names.Id=1; Name="address"}) |> ignore
    db.Insert({Names.Id=2; Name="other"}) |> ignore
    db.Insert({Addresses.Id=1; Text="address"}) |> ignore

    let names = query {
        for n in db.GetTable<Names>() do
        select n.Name
    }

    let query = query {
        for nm in names do
        for a in db.GetTable<Addresses>().Where(fun a1 -> a1.Text = nm).DefaultIfEmpty() do
        select (nm, a)
    }

    let result = query |> Seq.toArray |> Array.sortBy fst

    Assert.That(result, Has.Length.EqualTo(2))
    Assert.That(result[0], Is.EqualTo( ("address", {Addresses.Id=1; Text="address"}) ) )
    Assert.That(result[1], Is.EqualTo( ("other", null) ) )

// Regression pin: THREE chained groupJoin/DefaultIfEmpty blocks. From the third join onwards F# widens the
// accumulated element from AnonymousObject`2 to `4, so a flatten that re-closes the outer element's own
// generic definition over two arguments throws and silently leaves the un-flattened shape - which drops
// unmatched rows on providers with LATERAL and does not translate at all on those without.
let Issue1813Test11(db : IDataContext) =
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
        groupJoin tr2 in db.GetTable<TradeValid>()
            on (tr.DealNumber = tr2.Id) into tr2_g
        for z in tr2_g.DefaultIfEmpty() do
        sortBy tr.Id
        yield (tr, x, y, z)
    }

    let result = query.Take(90) |> Seq.toArray

    // Third join deliberately does NOT match every trade: DealNumbers are 2,3,5,8 and trade ids are 1..4, so
    // trades 1 and 2 match (z = 2, 3) while trades 3 and 4 do not (z = 0). Under a correct LEFT JOIN that is
    // Test4's 6 rows with a fourth field; if the third join degrades to INNER JOIN LATERAL, the two unmatched
    // rows (3-0-0-0 and 4-3-4-0) disappear and only 4 rows come back.
    let actual =
        result
        |> Array.map (fun (tr, x, y, z) -> sprintf "%d-%d-%d-%d" tr.Id (key x) (key y) (keyT z))
        |> Array.sort
        |> String.concat ","

    Assert.That(actual, Is.EqualTo "1-1-0-2,1-4-0-2,2-0-2-3,2-0-3-3,3-0-0-0,4-3-4-0")

// Test11's chain with the third groupJoin replaced by a plain inner `join`. The trailing join puts a further
// range variable in scope, so F# re-projects the accumulated element instead of passing it through, the
// flatten declines the shape, and the second DefaultIfEmpty is emitted as an INNER JOIN - silently dropping
// the rows it should have kept. Gated on #5794.
let Issue5794Test(db : IDataContext) =
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
        join tr2 in db.GetTable<TradeValid>() on (tr.DealNumber = tr2.Id)
        sortBy tr.Id
        yield (tr, x, y, tr2)
    }

    let result = query.Take(90) |> Seq.toArray

    // The trailing join is INNER, so only trades whose DealNumber matches a TradeValid.Id survive:
    // DealNumbers 2 and 3 match ids 2 and 3; 5 and 8 match nothing. That filters Test11's six
    // LEFT-join rows down to the four belonging to trades 1 and 2.
    let actual =
        result
        |> Array.map (fun (tr, x, y, tr2) -> sprintf "%d-%d-%d-%d" tr.Id (key x) (key y) tr2.Id)
        |> Array.sort
        |> String.concat ","

    Assert.That(actual, Is.EqualTo "1-1-0-2,1-4-0-2,2-0-2-3,2-0-3-3")


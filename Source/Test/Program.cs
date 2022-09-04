#nullable enable

using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using LinqToDB;
using LinqToDB.Data;
using LinqToDB.DataProvider.Oracle;
using LinqToDB.DataProvider.SqlServer;
using LinqToDB.Linq;
using LinqToDB.Mapping;
using LinqToDB.Tools;
using Moq;
using Moq.Protected;
using Oracle.ManagedDataAccess.Types;

#region Mocks 

DbParameter MockParameter()
{
	return new Mock<DbParameter>()
		.SetupProperty(x => x.ParameterName)
		.SetupProperty(x => x.Value)
		.Object;
}

var parameters = new List<object>();
var ps = new Mock<DbParameterCollection>();
ps.Setup(x => x.Count).Returns(() => parameters.Count);
ps.Setup(x => x.GetEnumerator()).Returns(() => parameters.GetEnumerator());
ps.Setup(x => x.Add(It.IsAny<object>())).Callback((object p) => parameters.Add(p));

var reader = new Mock<DbDataReader>();
reader.Setup(x => x.Read()).Returns(false);

var cmd = new Mock<DbCommand>();
cmd.Protected().Setup<DbParameterCollection>("DbParameterCollection").Returns(ps.Object);
cmd.Protected().Setup<DbParameter>("CreateDbParameter").Returns(MockParameter);
cmd.SetupProperty(x => x.CommandText);
cmd.Protected().Setup<DbDataReader>("ExecuteDbDataReader", ItExpr.IsAny<CommandBehavior>()).Returns(() => reader.Object);

var con = new Mock<DbConnection>();
con.Protected().Setup<DbCommand>("CreateDbCommand").Returns(cmd.Object);

DataConnection.TurnTraceSwitchOn();

//var providerType = typeof(OracleDataProvider).Assembly.GetType("LinqToDB.DataProvider.Oracle.OracleDataProviderManaged12");
//var provider = (OracleDataProvider)Activator.CreateInstance(providerType);
var providerType = typeof(SqlServerDataProvider).Assembly.GetType("LinqToDB.DataProvider.SqlServer.SqlServerDataProvider2019MicrosoftDataSqlClient");
var provider = (SqlServerDataProvider)Activator.CreateInstance(providerType);
//  (OracleDataProvider)providerType
//     .GetConstructor(
//       BindingFlags.NonPublic | BindingFlags.CreateInstance | BindingFlags.Instance, 
//       null, 
//       new[] { typeof(string) }, 
//       null
//     )
//     .Invoke();

var db = new DataConnection(
	provider,
	con.Object);

#endregion

Console.WriteLine("Starting tests...");

// (from a in db.GetTable<Basic>()
//  from b in db.GetTable<Basic>().LeftJoin(b => a.Id == b.Id)
//  select Sql.AsSql(b == null ? a.Id : b.Id))
//  .ToList();
// Console.WriteLine("OUTER JOIN");
// Console.WriteLine("=================");
// Console.WriteLine(cmd.Object.CommandText);

// (from a in db.GetTable<Table3>()
// join b in db.GetTable<Table3>() on a.ParentID equals b.ParentID + 2
// select 1)
// .ToList();
// Console.WriteLine("INNER JOIN");
// Console.WriteLine("=================");
// Console.WriteLine(cmd.Object.CommandText);

var values = new Table3[] 
{
	new() { ParentID = 1, ChildID = null },
	new() { ParentID = 2, ChildID = null },
};

//db.Update(new Table3 { ParentID = 10000, ChildID = null, GrandChildID = 1000 });

int a = 3;

db.GetTable<Table3>()
	.Where(p => (a == 1 ? 3 : a == 2 ? 4 : 5) == 4)
	.ToList();
Console.WriteLine("update null PK");
Console.WriteLine("=================");
Console.WriteLine(cmd.Object.CommandText);

//TestAll(true);
//TestIsNull(true);
//TestIsNull(false);
//TestAllEnums(true);
//TestAllCEnums(false);

void TestAll(bool nulls)
{
	Console.WriteLine("\n---------------------------\nCompareNullsAsValues = " + nulls + "\n---------------------------\n");
	LinqToDB.Common.Configuration.Linq.CompareNullsAsValues = nulls;
	Query.ClearCaches();

	TestOne(x => x.A == x.B, "A == B");
	TestOne(x => x.A != x.B, "A != B");
	TestOne(x => x.A >= x.B, "A >= B");
	TestOne(x => x.A > x.B, "A > B");
	TestOne(x => x.A <= x.B, "A <= B");
	TestOne(x => x.A < x.B, "A < B");

	TestOne(x => !(x.A == x.B), "NOT A == B");
	TestOne(x => !(x.A != x.B), "NOT A != B");
	TestOne(x => !(x.A >= x.B), "NOT A >= B");
	TestOne(x => !(x.A > x.B), "NOT A > B");
	TestOne(x => !(x.A <= x.B), "NOT A <= B");
	TestOne(x => !(x.A < x.B), "NOT A < B");	
}

void TestIsNull(bool nulls)
{
	Console.WriteLine("\n---------------------------\nCompareNullsAsValues = " + nulls + "\n---------------------------\n");
	LinqToDB.Common.Configuration.Linq.CompareNullsAsValues = nulls;
	Query.ClearCaches();
	int? nilvar = null;
	TestOne(x => x.A == null, "A == null");
	TestOne(x => x.A == nilvar, "A == nilvar");
	TestOne(x => Sql.Row(x.A, x.B) == null, "(A, B) == null");
}

void TestAllEnums(bool nulls)
{
	Console.WriteLine("\n---------------------------\nCompareNullsAsValues = " + nulls + "\n---------------------------\n");
	LinqToDB.Common.Configuration.Linq.CompareNullsAsValues = nulls;
	Query.ClearCaches();

	TestOne(x => x.Enum == x.Enum, "Enum == Enum");
	TestOne(x => x.Enum != x.Enum, "Enum != Enum");
	TestOne(x => x.Enum >= x.Enum, "Enum >= Enum");
	TestOne(x => x.Enum > x.Enum, "Enum > Enum");
	TestOne(x => x.Enum <= x.Enum, "Enum <= Enum");
	TestOne(x => x.Enum < x.Enum, "Enum < Enum");

	TestOne(x => !(x.Enum == x.Enum), "NOT Enum == Enum");
	TestOne(x => !(x.Enum != x.Enum), "NOT Enum != Enum");
	TestOne(x => !(x.Enum >= x.Enum), "NOT Enum >= Enum");
	TestOne(x => !(x.Enum > x.Enum), "NOT Enum > Enum");
	TestOne(x => !(x.Enum <= x.Enum), "NOT Enum <= Enum");
	TestOne(x => !(x.Enum < x.Enum), "NOT Enum < Enum");	
}

void TestAllCEnums(bool nulls)
{
	Console.WriteLine("\n---------------------------\nCompareNullsAsValues = " + nulls + "\n---------------------------\n");
	LinqToDB.Common.Configuration.Linq.CompareNullsAsValues = nulls;
	Query.ClearCaches();

	TestOne(x => x.CEnum == x.CEnum, "CEnum == CEnum");
	TestOne(x => x.CEnum != x.CEnum, "CEnum != CEnum");
	TestOne(x => x.CEnum >= x.CEnum, "CEnum >= CEnum");
	TestOne(x => x.CEnum > x.CEnum, "CEnum > CEnum");
	TestOne(x => x.CEnum <= x.CEnum, "CEnum <= CEnum");
	TestOne(x => x.CEnum < x.CEnum, "CEnum < CEnum");

	TestOne(x => !(x.CEnum == x.CEnum), "NOT CEnum == CEnum");
	TestOne(x => !(x.CEnum != x.CEnum), "NOT CEnum != CEnum");
	TestOne(x => !(x.CEnum >= x.CEnum), "NOT CEnum >= CEnum");
	TestOne(x => !(x.CEnum > x.CEnum), "NOT CEnum > CEnum");
	TestOne(x => !(x.CEnum <= x.CEnum), "NOT CEnum <= CEnum");
	TestOne(x => !(x.CEnum < x.CEnum), "NOT CEnum < CEnum");	
}

void TestOne(Expression<Func<Basic, bool>> where, string name)
{
	db.GetTable<Basic>().Where(where).ToList();
	Console.WriteLine("--- " + name);
	Console.WriteLine(cmd.Object.CommandText);
}

void SetupSrcTable(IDataContext db)
{
	db.GetFluentMappingBuilder()
		.Entity<Basic>()
			.Property(e => e.CEnum)
				.HasDataType(DataType.VarChar)
				.HasConversion(v => $"___{v}___", v => (ConvertedEnum)Enum.Parse(typeof(ConvertedEnum), v.Substring(3, v.Length - 6)));

	// var data = new[]
	// {
	// 	new Src { Id = 1 },
	// 	new Src { Id = 2, Int = 2, Enum = ContainsEnum.Value2, CEnum = ConvertedEnum.Value2 },
	// };

	// var src  = db.CreateLocalTable(data);
	// return src;
}

class Basic
{
	[PrimaryKey]
	public int Id { get; set; }
	public int? A { get; set; }
	public int? B { get; set; }
	public ContainsEnum?  Enum  { get; set; }
	public ConvertedEnum? CEnum { get; set; }
}

enum ContainsEnum
{
	[MapValue("ONE")  ] Value1,
	[MapValue("TWO")  ] Value2,
	[MapValue("THREE")] Value3,
	[MapValue("FOUR") ] Value4,
}

enum ConvertedEnum
{
	Value1,
	Value2,
	Value3,
	Value4,
}

class Parent
{
	[PrimaryKey]
	public int? ID { get; set; }
	public string? Name {get;set;}
}

class Child
{
	[PrimaryKey]
	public int ID { get; set; }
	public int ParentID {get;set;}	
	[Association(ThisKey = "ParentID", OtherKey = "ID" )]
	public Parent? Parent {get;set;}
}

[Table("GrandChild")]
class Table3
{	
	[PrimaryKey(1)] public int? ParentID;
	[PrimaryKey(2)] public int? ChildID;
	[Column]        public int? GrandChildID;
	[Column] public int NotNull;
}

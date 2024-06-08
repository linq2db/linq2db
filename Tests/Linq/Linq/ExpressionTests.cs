using System;
using System.Linq;
using FluentAssertions;
using LinqToDB;
using LinqToDB.Data;
using LinqToDB.Linq;
using LinqToDB.Mapping;
using NUnit.Framework;

namespace Tests.Linq
{

	[TestFixture]
	public class ExpressionTests : TestBase
	{
		public static class Functions
		{
			[Sql.Expression("DATE()", ServerSideOnly = true)]
			public static DateTime DateExpr(DataConnection db, ExpressionTestsFakeType fake)
			{
				throw new NotImplementedException();
			}

			[Sql.Expression("DATE({1})", ServerSideOnly = true)]
			public static DateTime DateExprKind(DataConnection db, string kind, ExpressionTestsFakeType fake)
			{
				throw new NotImplementedException();
			}

			[Sql.Function("DATE", ArgIndices = new[] { 1 }, ServerSideOnly = true)]
			public static DateTime DateFuncKind(DataConnection db, string kind, ExpressionTestsFakeType fake)
			{
				throw new NotImplementedException();
			}

			[Sql.Function("DATE", ServerSideOnly = true)]
			public static DateTime DateFuncFail(DataConnection db, ExpressionTestsFakeType fake)
			{
				throw new NotImplementedException();
			}

			[Sql.Expression("DATE({2})", ServerSideOnly = true)]
			public static DateTime DateExprKindFail(DataConnection db, string kind, ExpressionTestsFakeType fake)
			{
				throw new NotImplementedException();
			}
		}

		public class ExpressionTestsFakeType
		{

		}

		[Table]
		private sealed class ExpressionTestClass
		{
			[Column]
			public int Id { get; set; }

			[Column]
			public int Value { get; set; }
		}

		[Test]
		public void PostiveTest([IncludeDataSources(TestProvName.AllSQLite)]
			string context)
		{
			using (var db = (DataConnection)GetDataContext(context))
			using (db.CreateLocalTable<ExpressionTestClass>())
			{
				_ = db.Select(() => new
				{
					Date1 = Functions.DateExpr(db, new ExpressionTestsFakeType()),
					Date2 = Functions.DateExprKind(db, "now", new ExpressionTestsFakeType()),
					Date3 = Functions.DateFuncKind(db, "now", new ExpressionTestsFakeType()),
				});
			}
		}

		[Test]
		public void FailTest([IncludeDataSources(ProviderName.SQLiteMS)]
			string context)
		{
			using (var db = (DataConnection)GetDataContext(context))
			using (db.CreateLocalTable<ExpressionTestClass>())
			{
				Assert.Throws<LinqException>(() => _ = db.Select(() => Functions.DateFuncFail(db, new ExpressionTestsFakeType())));
				Assert.Throws<LinqException>(() => _ = db.Select(() => Functions.DateExprKindFail(db, "now", new ExpressionTestsFakeType())));
			}
		}

		sealed class MyContext : DataConnection
		{
			public MyContext(string configurationString) : base(configurationString)
			{
			}

			[Sql.Expression("10", ServerSideOnly = true)]
			public int SomeValue 
				=> this.SelectQuery(() => SomeValue).AsEnumerable().First();
		}

		[Test]
		public void TestAsProperty([DataSources(false)] string context)
		{
			using (var db = new MyContext(context))
			{
				db.SomeValue.Should().Be(10);
			}
		}

	}
}

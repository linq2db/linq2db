using System;
using System.Collections.Generic;
using System.Linq;

using LinqToDB;
using LinqToDB.Data;

using NUnit.Framework;

namespace Tests.UserTests
{
	using Model;

	[TestFixture]
	public class Issue358Tests : TestBase
	{
#if !MONO
		private DataConnection _connection;

		enum TestIssue358Enum
		{
			Value1,
			Value2
		}

		class TestIssue358Class
		{
			public TestIssue358Enum? MyEnum;
			public TestIssue358Enum  MyEnum2;
		}

		[Test]
		public void HasIsNull()
		{
			using (var db = new TestDataConnection())
			{
				var qry =
					from p in db.GetTable<TestIssue358Class>()
					where p.MyEnum != TestIssue358Enum.Value1
					select p;

				var sql = qry.ToString();

				Assert.That(sql.IndexOf("NULL"), Is.GreaterThan(0), sql);
			} 
		}

		[Test]
		public void ContainsHasIsNull()
		{
			using (var db = new TestDataConnection())
			{
				var filter = new[] {TestIssue358Enum.Value2};

				var qry =
					from p in db.GetTable<TestIssue358Class>()
					where !!filter.Contains(p.MyEnum.Value)
					select p;

				var sql = qry.ToString();

				Assert.That(sql.IndexOf("NULL"), Is.GreaterThan(0), sql);
			} 
		}

		[Test]
		public void NoIsNull()
		{
			using (var db = new TestDataConnection())
			{
				var qry =
					from p in db.GetTable<TestIssue358Class>()
					where p.MyEnum2 != TestIssue358Enum.Value1
					select p;

				var sql = qry.ToString();

				Assert.That(sql.IndexOf("NULL"), Is.LessThan(0), sql);
			} 
		}

		[Test]
		public void ContainsNoIsNull()
		{
			using (var db = new TestDataConnection())
			{
				var filter = new[] {TestIssue358Enum.Value2};

				var qry =
					from p in db.GetTable<TestIssue358Class>()
					where !filter.Contains(p.MyEnum2)
					select p;

				var sql = qry.ToString();

				Assert.That(sql.IndexOf("NULL"), Is.LessThan(0), sql);
			} 
		}

		[OneTimeSetUp]
		public void SetUp()
		{
			_connection = new DataConnection(ProviderName.SQLite, "Data Source=:memory:;");

			_connection.CreateTable<LinqDataTypes2>();

			var dt = DateTime.Today;
			var guid = Guid.NewGuid();

			_types = new[]
			{
				new LinqDataTypes2() {ID = 1, DateTimeValue = null, BigIntValue = null, BoolValue = null,  DateTimeValue2 = null, GuidValue = null, IntValue = null, MoneyValue = 0, SmallIntValue = null},
				new LinqDataTypes2() {ID = 2, DateTimeValue = dt,   BigIntValue = 1,    BoolValue = false, DateTimeValue2 = dt,   GuidValue = guid, IntValue = 1,    MoneyValue = 0, SmallIntValue = 1   }
			};

			_connection.BulkCopy(_types);


		}

		private IEnumerable<LinqDataTypes2> _types;

		[OneTimeTearDown]
		public void TearDown()
		{
			_connection.Dispose();
		}


		[Test]
		public void Test1()
		{
			var types = _connection.GetTable<LinqDataTypes2>();
			var bigintFilter = new Int64?[] {2};
			var boolFilter   = new bool? [] {true};

			AreEqual(_types.Where(_ => _.BigIntValue != 2),             types.Where(_ => _.BigIntValue != 2));
			AreEqual(_types,                                            types.Where(_ => !_.BoolValue.Value));
			AreEqual(_types.Where(_ => (_.BoolValue ?? false) != true), types.Where(_ => _.BoolValue != true));

			AreEqual(_types.Where(_ => !bigintFilter.Contains(_.BigIntValue)), types.Where(_ => !bigintFilter.Contains(_.BigIntValue)));
			AreEqual(_types.Where(_ => !boolFilter.  Contains(_.BoolValue)),   types.Where(_ => !boolFilter.  Contains(_.BoolValue)));

			AreEqual(_types.Where(_ => !bigintFilter.Contains(_.BigIntValue)), types.Where(_ => bigintFilter.Contains(_.BigIntValue) == false));
			AreEqual(_types.Where(_ => !boolFilter.  Contains(_.BoolValue)),   types.Where(_ => boolFilter.  Contains(_.BoolValue)   == false));

			 AreEqual(_types.Where(_ => !bigintFilter.Contains(_.BigIntValue)), types.Where(_ => bigintFilter.Contains(_.BigIntValue) != true));
			AreEqual(_types.Where(_ => !boolFilter.   Contains(_.BoolValue)),   types.Where(_ => boolFilter.  Contains(_.BoolValue)   != true));
		}
#endif
	}
}

using System;
using System.Linq;

using LinqToDB.Mapping;

using NUnit.Framework;

namespace Tests.Linq
{
	using Model;

	[TestFixture]
	public class ExplicitInterfaceTests : TestBase
	{
		interface IDate
		{
			DateTime? Date { get; }
		}

		interface IDate2
		{
			DateTime? Date { get; set; }
		}

		[Table("LinqDataTypes")]
		class TestTable : IDate
		{
			[Column("GuidValue")] Guid? GuidValue { get; set; }
			[Column("BoolValue")] public bool? Bit { get; set; }

			private DateTime? _date;

			[Column("DateTimeValue", Storage = "_date")]
			DateTime? IDate.Date
			{
				get { return _date; }
			}
		}

		[Table("LinqDataTypes")]
		public class TestTable2 : IDate
		{
			[Column("GuidValue")] Guid?  GuidValue { get; set; }
			[Column("BoolValue")] public bool? Bit { get; set; }

			private DateTime? _date;

			[Column("DateTimeValue", Storage = "_date")]
			DateTime? IDate.Date => _date;
		}

		[Table("LinqDataTypes")]
		public class TestTable3 : IDate2
		{
			[Column("GuidValue")]     Guid?     GuidValue   { get; set; }
			[Column("BoolValue")]     public    bool? Bit   { get; set; }
			[Column("DateTimeValue")] DateTime? IDate2.Date { get; set; }
		}

		[Table("LinqDataTypes")]
		public class TestTable4 : IDate2
		{
			[Column("GuidValue")]            Guid?     GuidValue     { get; set; }
			[Column("BoolValue")]     public bool?     Bit           { get; set; }
			[Column("DateTimeValue")] public DateTime? DateTimeValue { get; set; }

			[ColumnAlias("DateTimeValue")]
			public DateTime? Date { get; set; }
		}

		static IQueryable<T> SelectNoDate<T>(IQueryable<T> items) where T : IDate
		{
			return items.Where(i => i.Date == null);
		}

		static IQueryable<T> SelectNoDate2<T>(IQueryable<T> items) where T : IDate2
		{
			return items.Where(i => i.Date == null);
		}

		[Test]
		public void ExplicitInterface1()
		{
			using (var db = new TestDataConnection())
			{
				var result = SelectNoDate(db.GetTable<TestTable>()).ToList();
			}
		}

		[Test]
		public void ExplicitInterface2()
		{
			using (var db = new TestDataConnection())
			{
				var result = SelectNoDate(db.GetTable<TestTable2>()).ToList();
			}
		}

		[Test]
		public void ExplicitInterface3()
		{
			using (var db = new TestDataConnection())
			{
				var result = SelectNoDate2(db.GetTable<TestTable3>()).ToList();
			}
		}

		[Test]
		public void ExplicitInterface4()
		{
			using (var db = new TestDataConnection())
			{
				var result = SelectNoDate2(db.GetTable<TestTable4>()).ToList();
			}
		}
	}
}

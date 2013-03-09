using System;
using System.Linq;

using LinqToDB.Mapping;

using NUnit.Framework;

namespace Tests.Linq
{
	using Model;

	[TestFixture]
	public class ImplicitInterfaceTests : TestBase
	{
		interface IDate
		{
			DateTime? Date { get; }
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

		static IQueryable<T> SelectNoDate<T>(IQueryable<T> items) where T : IDate
		{
			return items.Where(i => i.Date == null);
		}

		[Test]
		public void TestInterface()
		{
			using (var db = new TestDataConnection())
			{
				var result = SelectNoDate(db.GetTable<TestTable>()).ToList();
			}
		}
	}
}

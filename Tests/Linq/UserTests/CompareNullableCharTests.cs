using System;
using System.Linq;

using LinqToDB;
using LinqToDB.Data;
using LinqToDB.Mapping;

using NUnit.Framework;

namespace Tests.UserTests
{
	[TestFixture]
	public class CompareNullableCharTests : TestBase
	{
		sealed class Table1
		{
			[PrimaryKey(1)]
			[Identity] public int  Field1 { get; set; }
			[Nullable] public char? Foeld2 { get; set; }
		}

		[Test]
		public void Test([DataSources] string context)
		{
			using var db = GetDataContext(context);
			using var tb = db.CreateLocalTable<Table1>();

			var q =
					from current  in db.GetTable<Table1>()
					from previous in db.GetTable<Table1>()
					where current.Foeld2 == previous.Foeld2
					select new { current.Field1, Field2 = previous.Field1 };

			_ = q.ToArray();
		}
	}
}

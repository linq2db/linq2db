using System;
using System.Linq;

using LinqToDB;
using LinqToDB.Mapping;

using NUnit.Framework;

namespace Tests.Playground
{
	[TestFixture]
	public class TestTemplate : TestBase
	{
		[Table]
		class SampleClass
		{
			[Column] public int Id    { get; set; }
			[Column] public int Value { get; set; }
		}

		[Test]
		public void SampleSelectTest([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			using (var table = db.CreateLocalTable<SampleClass>())
			{
				var result = table.ToArray();
			}
		}

		[Test]
		public void Test([IncludeDataSources(
			ProviderName.SqlCe, ProviderName.SqlServer2012, ProviderName.SqlServer2014)] string context)
		{
			using (var db = GetDataContext(context))
			{
				var query =
					from p in db.Parent
					from c in db.Child.Take(1).DefaultIfEmpty()
					select new
					{
						a = p.ParentID == 1 ? c != null ? "1" : "2" : "3"
					};

				_ = query.ToList();
			}
		}
	}
}

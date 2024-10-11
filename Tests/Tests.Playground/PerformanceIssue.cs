using System.Linq;

using FluentAssertions;

using LinqToDB;
using LinqToDB.Mapping;

using NUnit.Framework;

namespace Tests.Playground
{
	[TestFixture]
	public class PerformanceIssue : TestBase
	{
		[LinqToDB.Mapping.Table]
		public class Narrow
		{
			[LinqToDB.Mapping.PrimaryKey]
			public int ID { get; set; }

			[LinqToDB.Mapping.Column, LinqToDB.Mapping.NotNull]
			public int Field1 { get; set; }
		}

		[Test]
		public void SampleSelectTest([IncludeDataSources(TestProvName.AllSQLite)] string context)
		{
			using var db = GetDataContext(context);
			using var table = db.CreateLocalTable<Narrow>();

			var sw = new System.Diagnostics.Stopwatch();
			sw.Start();

			var repeatCount = 10;

			for (var i = 0; i < repeatCount; i++)
			{
				var takeCount = i;

				var q =
					(
						from n1 in db.GetTable<Narrow>()
						join n2 in db.GetTable<Narrow>() on new { n1.ID, n1.Field1 } equals new { n2.ID, n2.Field1 }
						where n1.ID < 100 && n2.Field1 <= 50
						group n1 by n1.ID into gr
						select new
						{
							gr.Key,
							Count = gr.Count()
						}
					)
					.OrderBy(n1 => n1.Key)
					.Skip(1)
					.Take(takeCount);

				var result = q.ToArray();
			}

			sw.Stop();

			TestContext.Out.WriteLine($"------------------- Total: {sw.ElapsedMilliseconds} -----------------");
		}
	}
}

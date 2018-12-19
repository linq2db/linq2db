using System;
using System.Linq;
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
	}
}

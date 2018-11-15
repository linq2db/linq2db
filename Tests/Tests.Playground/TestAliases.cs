using System;
using System.Linq;
using LinqToDB;
using LinqToDB.Mapping;
using NUnit.Framework;

namespace Tests.Playground
{
	[TestFixture]
	public class TestAliases : TestBase
	{
		[Table]
		class SampleClass
		{
			[Column] public int Id    { get; set; }
			[Column] public int Value { get; set; }
		}

		[Test]
		public void NewInitTest([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			{
				var query = from child in db.Child
					select new
					{
						TrackId = child.ChildID,
					};

				var str = query.ToString();
			 
				var table = db.GetTable<SampleClass>();

				var query2 = from t in table
					select new
					{
						NewId = t.Id,
						NewValue = t.Value
					};

				Console.WriteLine(query2.ToString());

				query2.GetSelectQuery();
			}
		}

	}
}

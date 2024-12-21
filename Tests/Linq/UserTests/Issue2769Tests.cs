using System.Linq;

using LinqToDB;
using LinqToDB.Mapping;

using NUnit.Framework;

namespace Tests.UserTests
{
	[TestFixture]
	public class Issue2769Tests : TestBase
	{
		[Table]
		sealed class SampleClass
		{
			[Column] public int Id    { get; set; }
			[Column] public int? NullValue { get; set; }
		}

		[Test]
		public void ObjectInListTest([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			using (var table = db.CreateLocalTable<SampleClass>())
			{
				var criteria = Enumerable.Range(0, 3).Select(i => new {RECORDNAME = i, KEYNUMB = i})
					.ToArray();

				Assert.DoesNotThrow(() =>
				{
					_ = table.Select(a => new
						{
							Alert = a, Key = new {RECORDNAME = a.Id, KEYNUMB = a.NullValue.GetValueOrDefault()}
						})
						.Where(a => criteria.Contains(a.Key))
						.Select(a => a.Alert)
						.ToList();
				});
			}
		}
	}
}

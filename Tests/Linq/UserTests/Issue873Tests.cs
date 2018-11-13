using NUnit.Framework;
using System.Linq;

namespace Tests.UserTests
{

	[TestFixture]
	public class Issue873Tests : TestBase
	{
		[Test]
		public void Test([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			{
				var q = db.Child;

				var query = db.Parent
					.Select(e => new
					{
						Fields = new
						{
							Label = " " + (e.Value1 ?? 0).ToString(),
							Sum = new { SubSum = q.Where(c => c.Parent == e).Sum(c => c.ChildID) },
							Any = q.Where(c => c.Parent == e).Any(),
							Count = q.Where(p => p.Parent == e).Count()
						},
					})
					.Where(f => f.Fields.Label.Contains("1") && f.Fields.Sum.SubSum > 0);

				var qc = Child;

				var expected = Parent
					.Select(e => new
					{
						Fields = new
						{
							Label = " " + (e.Value1 ?? 0).ToString(),
							Sum = new { SubSum = qc.Where(c => c.Parent == e).Sum(c => c.ChildID) },
							Any = qc.Where(c => c.Parent == e).Any(),
							Count = qc.Where(p => p.Parent == e).Count()
						},
					})
					.Where(f => f.Fields.Label.Contains("1") && f.Fields.Sum.SubSum > 0);

				AreEqual(expected, query);
			}
		}
	}
}

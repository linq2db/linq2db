using System.Linq;

using LinqToDB;

using NUnit.Framework;

using Tests.Model;

namespace Tests.UserTests
{
	[TestFixture]
	public class Issue817Tests : TestBase
	{
		[Test]
		public void TestUnorderedTake([DataSources] string context)
		{
			using var db = GetDataContext(context);
			var result = db.GetTable<Person>().Take(1).Select(_ => new { }).ToList();

			Assert.That(result, Has.Count.EqualTo(1));
		}

		[Test]
		public void TestUnorderedSkip([DataSources] string context)
		{
			using var db = GetDataContext(context);
			var cnt = db.GetTable<Person>().Count();

			var result = db.GetTable<Person>().Skip(1).Select(_ => new { }).ToList();

			Assert.That(result, Has.Count.EqualTo(cnt - 1));
		}

		[Test]
		public void TestUnorderedTakeSkip([DataSources] string context)
		{
			using var db = GetDataContext(context);
			var result = db.GetTable<Person>().Skip(1).Take(1).Select(_ => new { }).ToList();

			Assert.That(result, Has.Count.EqualTo(1));
		}

		[Test]
		public void TestUnorderedTakeSkipZero([DataSources] string context)
		{
			using var db = GetDataContext(context);
			var result = db.GetTable<Person>().Skip(0).Take(1).Select(_ => new { }).ToList();

			Assert.That(result, Has.Count.EqualTo(1));
		}
	}
}

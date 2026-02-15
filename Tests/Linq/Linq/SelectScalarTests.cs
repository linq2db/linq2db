using System;
using System.Linq;
using System.Threading.Tasks;

using LinqToDB;
using LinqToDB.Internal.Common;

using NUnit.Framework;

using Tests.Model;

namespace Tests.Linq
{
	[TestFixture]
	public class SelectScalarTests : TestBase
	{
		[Test]
		public void Parameter1([DataSources] string context)
		{
			var p = 1;
			using var db = GetDataContext(context);
			Assert.That(db.Select(() => p), Is.EqualTo(p));
		}

		[Test]
		public async Task Parameter1Async([DataSources] string context)
		{
			var p = 1;
			using var db = GetDataContext(context);
			Assert.That(await db.SelectAsync(() => p), Is.EqualTo(p));
		}

		[Test]
		public void Parameter2([DataSources] string context)
		{
			using var db = GetDataContext(context);
			var p = 1;
			Assert.That(db.Select(() => new { p }).p, Is.EqualTo(p));
		}

		[Test]
		public void Constant1([DataSources] string context)
		{
			using var db = GetDataContext(context);
			Assert.That(db.Select(() => 1), Is.EqualTo(1));
		}

		[Test]
		public void Constant2([DataSources] string context)
		{
			using var db = GetDataContext(context);
			Assert.That(db.Select(() => new { p = 1 }).p, Is.EqualTo(1));
		}

		[Test]
		public void Constant3([DataSources] string context)
		{
			using var db = GetDataContext(context);
			Assert.That(db.Select(() => new Person { ID = 1, FirstName = "John" }).ID, Is.EqualTo(1));
		}

		[Test]
		public void StrLen([DataSources] string context)
		{
			using var db = GetDataContext(context);
			Assert.That(db.Select(() => "1".Length), Is.EqualTo("1".Length));
		}

		[Test]
		public void IntMaxValue([DataSources] string context)
		{
			using var db = GetDataContext(context);
			Assert.That(db.Select(() => int.MaxValue), Is.EqualTo(int.MaxValue));
		}

		[Test]
		public void Substring([DataSources] string context)
		{
			const string s = "123";
			using var db = GetDataContext(context);
			Assert.That(db.Select(() => s.Substring(1)), Is.EqualTo(s.Substring(1)));
		}

		[Test]
		public void Add([DataSources] string context)
		{
			const string s = "123";
			using var db = GetDataContext(context);
			Assert.That(db.Select(() => s.Substring(1).Length + 3), Is.EqualTo(s.Substring(1).Length + 3));
		}

		[Test]
		public void Scalar1([DataSources] string context)
		{
			using var db = GetDataContext(context);
			var q = (from p in db.Person select new { p } into p1 select p1.p).ToList().Where(p => p.ID == 1).First();
			Assert.That(q.ID, Is.EqualTo(1));
		}

		[Test]
		public void Scalar11([DataSources] string context)
		{
			using var db = GetDataContext(context);
			var n = (from p in db.Person select p.ID).ToList().Where(id => id == 1).First();
			Assert.That(n, Is.EqualTo(1));
		}

		[Test]
		public void Scalar2([DataSources] string context)
		{
			using var db = GetDataContext(context);
			var q = (from p in db.Person select new { p }).ToList().Where(p => p.p.ID == 1).First();
			Assert.That(q.p.ID, Is.EqualTo(1));
		}

		[Test]
		public void Scalar21([DataSources] string context)
		{
			using var db = GetDataContext(context);
			var n = (from p in db.Person select p.FirstName.Length).ToList().Where(len => len == 4).First();
			Assert.That(n, Is.EqualTo(4));
		}

		[Test]
		public void Scalar22([DataSources] string context)
		{
			using var db = GetDataContext(context);
			var expected =
					from p in Person
					select new { p1 = p, p2 = p }
					into p1
					where p1.p1.ID == 1 && p1.p2.ID == 1
					select p1;

			var result =
					from p in db.Person
					select new { p1 = p, p2 = p }
					into p1
					where p1.p1.ID == 1 && p1.p2.ID == 1
					select p1;

			Assert.That(result.ToList().SequenceEqual(expected), Is.True);
		}

		[Test]
		public void Scalar23([DataSources] string context)
		{
			using var db = GetDataContext(context);
			var expected =
					from p in Person
					select p.ID
					into p1
					where p1 == 1
					select new { p1 };

			var result =
					from p in db.Person
					select p.ID
					into p1
					where p1 == 1
					select new { p1 };

			Assert.That(result.ToList().SequenceEqual(expected), Is.True);
		}

		[Test]
		public void Scalar3([DataSources] string context)
		{
			using var db = GetDataContext(context);
			var expected = from p in    Person where p.ID == 1 select 1;
			var result   = from p in db.Person where p.ID == 1 select 1;
			Assert.That(result.ToList().SequenceEqual(expected), Is.True);
		}

		[Test]
		public void Scalar31([DataSources] string context)
		{
			using var db = GetDataContext(context);
			var n = 1;
			var expected = from p in    Person where p.ID == 1 select n;
			var result   = from p in db.Person where p.ID == 1 select n;
			Assert.That(result.ToList().SequenceEqual(expected), Is.True);
		}

		[Test]
		public void Scalar4([DataSources] string context)
		{
			using var db = GetDataContext(context);
			var expected =
					from p in Parent
					join c in Child on p.ParentID equals c.ParentID
					where c.ChildID > 20
					select p;

			var result =
					from p in db.Parent
					join c in db.Child on p.ParentID equals c.ParentID
					where c.ChildID > 20
					select p;

			Assert.That(result.Where(p => p.ParentID == 3).First(), Is.EqualTo(expected.Where(p => p.ParentID == 3).First()));
		}

		[Test]
		public void Function([DataSources] string context)
		{
			var text = "123";

			using var db = GetDataContext(context);
			Assert.That(
				db.Child.Select(c => string.Format("{0},{1}", c.ChildID, text)).FirstOrDefault(), Is.EqualTo(Child.Select(c => string.Format("{0},{1}", c.ChildID, text)).FirstOrDefault()));
		}

		[Test]
		[ThrowsForProvider(typeof(LinqToDBException), providers: [ProviderName.SqlCe], ErrorMessage = ErrorHelper.Error_Subquery_in_Column)]
		[ThrowsForProvider(typeof(LinqToDBException), providers: [TestProvName.AllSybase, TestProvName.AllInformix], ErrorMessage = ErrorHelper.Error_Take_in_Subquery)]
		public void SubQueryTest([DataSources(TestProvName.AllAccess)]
			string context)
		{
			using var db = GetDataContext(context);
			db.Select(() => new
			{
				f1 = db.Parent.Select(p => p.Value1).FirstOrDefault()
			});
		}

		[Test]
		public void SubQueryWithCastAndHasValue([DataSources(TestProvName.AllSybase, ProviderName.SqlCe)] string context)
		{
			using var db = GetDataContext(context);
			db
				.Parent
				.Where(_ =>
					db
						.Parent
						.Select(r => (int?)r.Value1)
						.FirstOrDefault()
						.HasValue)
				.ToList();
		}

		[Test]
		public void SubQueryWithCast([DataSources(TestProvName.AllSybase, ProviderName.SqlCe)] string context)
		{
			using var db = GetDataContext(context);
			db
				.Parent
				.Where(_ =>
					db
						.Parent
						.Select(r => (int?)r.Value1)
						.FirstOrDefault() != null)
				.ToList();
		}

		[Test]
		public void SubQueryWithHasValue([DataSources(TestProvName.AllSybase, ProviderName.SqlCe)] string context)
		{
			using var db = GetDataContext(context);
			db
				.Parent
				.Where(_ =>
					db
						.Parent
						.Select(r => r.Value1)
						.FirstOrDefault()
						.HasValue)
				.ToList();
		}

		[Test]
		public void SubQueryWithoutCastAndHasValue([DataSources(TestProvName.AllSybase, ProviderName.SqlCe)] string context)
		{
			using var db = GetDataContext(context);
			db
				.Parent
				.Where(_ =>
					db
						.Parent
						.Select(r => r.Value1)
						.FirstOrDefault() != null)
				.ToList();
		}

		[Test]
		public void SubQueryWithCastAndHasValueByGuid([DataSources(TestProvName.AllSybase, ProviderName.SqlCe)] string context)
		{
			using var db = GetDataContext(context);
			db
				.Parent
				.Where(_ =>
					db
						.Types2
						.Select(r => (Guid?)r.GuidValue)
						.FirstOrDefault()
						.HasValue)
				.ToList();
		}

		[Test]
		public void SubQueryWithCastByGuid([DataSources(TestProvName.AllSybase, ProviderName.SqlCe)] string context)
		{
			using var db = GetDataContext(context);
			db
				.Parent
				.Where(_ =>
					db
						.Types2
						.Select(r => (Guid?)r.GuidValue)
						.FirstOrDefault() != null)
				.ToList();
		}

		[Test]
		public void SubQueryWithHasValueByGuid([DataSources(TestProvName.AllSybase, ProviderName.SqlCe)] string context)
		{
			using var db = GetDataContext(context);
			db
				.Parent
				.Where(_ =>
					db
						.Types2
						.Select(r => r.GuidValue)
						.FirstOrDefault()
						.HasValue)
				.ToList();
		}

		[Test]
		public void SubQueryWithoutCastAndHasValueByGuid([DataSources(TestProvName.AllSybase, ProviderName.SqlCe)] string context)
		{
			using var db = GetDataContext(context);
			db
				.Parent
				.Where(_ =>
					db
						.Types2
						.Select(r => r.GuidValue)
						.FirstOrDefault() != null)
				.ToList();
		}
	}
}

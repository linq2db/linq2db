using System;
using System.Linq;

using LinqToDB;

using NUnit.Framework;

namespace Tests.Linq
{
	[TestFixture]
	public class ParameterTest : TestBase
	{
		[Test, DataContextSource]
		public void InlineParameter(string context)
		{
			using (var  db = GetDataContext(context))
			{
				db.InlineParameters = true;

				var id = 1;

				var parent1 = db.Parent.FirstOrDefault(p => p.ParentID == id);
				id++;
				var parent2 = db.Parent.FirstOrDefault(p => p.ParentID == id);

				Assert.That(parent1.ParentID, Is.Not.EqualTo(parent2.ParentID));
			}
		}

		[Test, DataContextSource]
		public void TestQueryCacheWithNullParameters(string context)
		{
			using (var db = GetDataContext(context))
			{
				int? id = null;
				Assert.AreEqual(0, db.Person.Where(_ => _.ID == id).Count());

				id = 1;
				Assert.AreEqual(1, db.Person.Where(_ => _.ID == id).Count());
			}
		}

		[Test, DataContextSource]
		public void AsSqlParameter(string context)
		{
			using (var  db = GetDataContext(context))
			{
				var id = 1;

				var q1 = db.Parent.Where(p => p.ParentID == id);

				Assert.That(q1.ToString(), Contains.Substring("id").Or.ContainsSubstring("?"));

				var q2 = db.Parent.Where(p => p.ParentID == Sql.ToSql(id));

				Assert.That(q2.ToString(), Is.Not.ContainsSubstring("id").And.Not.ContainsSubstring("?"));
			}
		}
	}
}

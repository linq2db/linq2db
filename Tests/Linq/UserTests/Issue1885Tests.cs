using System.Linq;

using LinqToDB;
using LinqToDB.Mapping;

using NUnit.Framework;

namespace Tests.UserTests
{
	[TestFixture]
	public class Issue1885Tests : TestBase
	{

		public class User
		{
			public int Id { get; set; }
			public Testing? Data { get; set; }
		}

		public class Testing
		{
			[NotColumn]
			public int[]? Ids { get; set; }

			[Column]
			public string? Value { get; set; }
		}

		public class Model
		{
			public string? Data { get; set; }
		}

		[Test]
		public void TestGenericAssociationRuntime([IncludeDataSources(ProviderName.SqlCe, TestProvName.AllSqlServer2008Plus)] string context)
		{
			var ms = new MappingSchema();
			var mb = new FluentMappingBuilder(ms);

			var values = new[] { 1, 5 };

			mb.Entity<User>()
				.Association(t => t.Data, (t, db) => db.FromSql<Model>("SELECT 'Testing' AS Data")
					.Where(x => t.Id == t.Id) // Necessary so that AssociationParentJoin is not null
					.Select(x => new Testing
					{
						Ids = values,
						Value = x.Data
					})
				)
				.Build();

			using (var db = GetDataContext(context, ms))
			using (var u = db.CreateLocalTable<User>())
			{
				u.Insert(() => new User { Id = 1 });
				u.Insert(() => new User { Id = 2 });

				var q =
					from t in db.GetTable<User>()
					select t.Data;

				var list = q.ToList();

				Assert.That(list, Has.Count.EqualTo(2));

				Assert.Multiple(() =>
				{
					Assert.That(list[0].Value, Is.EqualTo("Testing"));
					Assert.That(list[0].Ids, Is.EqualTo(values));

					Assert.That(list[1].Value, Is.EqualTo("Testing"));
					Assert.That(list[1].Ids, Is.EqualTo(values));
				});
			}
		}

	}
}

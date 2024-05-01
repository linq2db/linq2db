using System.Linq;
using FluentAssertions;
using LinqToDB;
using LinqToDB.Mapping;
using NUnit.Framework;

namespace Tests.Linq
{
	using Model;

	[TestFixture]
	public class InterfaceTests : TestBase
	{
		[Test]
		public void Test([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			{
				var q =
					from p in db.Parent2
					group p by p.ParentID into gr
					select new
					{
						Count = gr.Count()
					};

				var _ = q.ToList();
			}
		}

		#region Issue 4031
		[Table("Person")]
		public class Issue4031BaseInternal
		{
			[Column("PersonID")] public int Id { get; set; }
		}

		[Table("Person")]
		public class Issue4031BaseImplicit : IIssue4031
		{
			[Column("PersonID")] public int Id { get; set; }
		}

		[Table("Person")]
		public class Issue4031BaseExplicit : IIssue4031
		{
			[Column("PersonID")] int IIssue4031.Id { get; set; }
		}

		[Table("Person")]
		public class Issue4031BaseImplicitBad : IIssue4031
		{
			[Column("UNKNOWN")] public int Id { get; set; }
		}

		[Table("Person")]
		public class Issue4031BaseExplicitBad : IIssue4031
		{
			[Column("UNKNOWN")] int IIssue4031.Id { get; set; }
		}

		public class Issue4031Case01 : Issue4031BaseExternal, IIssue4031
		{
		}

		public class Issue4031Case02 : Issue4031BaseInternal, IIssue4031
		{
		}

		public class Issue4031Case03 : Issue4031BaseImplicit
		{
		}

		public class Issue4031Case04 : Issue4031BaseImplicit
		{
			[Column("UNKNOWN")] public new int Id { get; set; }
		}

		public class Issue4031Case05 : Issue4031BaseExplicit
		{
		}

		public class Issue4031Case06 : Issue4031BaseExplicit
		{
			[Column("UNKNOWN")] public int Id { get; set; }
		}

		public class Issue4031Case07 : Issue4031BaseImplicit, IIssue4031
		{
		}

		public class Issue4031Case08 : Issue4031BaseImplicit, IIssue4031
		{
			[Column("PersonID")] public new int Id { get; set; }
		}

		public class Issue4031Case09 : Issue4031BaseImplicitBad, IIssue4031
		{
			[Column("PersonID")] public new int Id { get; set; }
		}

		public class Issue4031Case10 : Issue4031BaseExplicit, IIssue4031
		{
		}

		public class Issue4031Case11 : Issue4031BaseExplicit, IIssue4031
		{
			[Column("PersonID")] public int Id { get; set; }
		}

		public class Issue4031Case12 : Issue4031BaseExplicit, IIssue4031
		{
			[Column("PersonID")] int IIssue4031.Id { get; set; }
		}

		public class Issue4031Case13 : Issue4031BaseImplicit, IIssue4031
		{
		}

		public class Issue4031Case14 : Issue4031BaseImplicitBad, IIssue4031
		{
			[Column("PersonID")] public new int Id { get; set; }
		}

		public class Issue4031Case15 : Issue4031BaseImplicitBad, IIssue4031
		{
			[Column("PersonID")] int IIssue4031.Id { get; set; }
		}

		public class Issue4031Case16 : Issue4031BaseExternal, IIssue4031<int>
		{
		}

		[Test]
		public void Issue4031_Case01([IncludeDataSources(ProviderName.SQLiteClassic)] string context)
		{
			Issue4031_Execute<Issue4031Case01>(context);
		}

		[Test]
		public void Issue4031_Case02([IncludeDataSources(ProviderName.SQLiteClassic)] string context)
		{
			Issue4031_Execute<Issue4031Case02>(context);
		}

		[Test]
		public void Issue4031_Case03([IncludeDataSources(ProviderName.SQLiteClassic)] string context)
		{
			Issue4031_Execute<Issue4031Case03>(context);
		}

		// unsuported case:
		// we prefer member declared with "new" over interface implementation member for backward compatibility
		// (see https://github.com/linq2db/linq2db/issues/4113)
		[Test, ActiveIssue]
		public void Issue4031_Case04([IncludeDataSources(ProviderName.SQLiteClassic)] string context)
		{
			Issue4031_Execute_TwoFields<Issue4031Case04>(context);

			using var db = GetDataContext(context);
			var sql = db.GetTable<Issue4031Case15>().Where(c => c.Id == -1).Select(c => new { c.Id }).ToString();
			Assert.That(sql, Is.Not.Contains("PersonID"));
			sql.Should().Contain("UNKNOWN", Exactly.Twice());
		}

		[Test]
		public void Issue4031_Case05([IncludeDataSources(ProviderName.SQLiteClassic)] string context)
		{
			Issue4031_Execute<Issue4031Case05>(context);
		}

		[Test]
		public void Issue4031_Case06([IncludeDataSources(ProviderName.SQLiteClassic)] string context)
		{
			Issue4031_Execute_TwoFields<Issue4031Case06>(context);

			using var db = GetDataContext(context);
			var sql = db.GetTable<Issue4031Case15>().Where(c => c.Id == -1).Select(c => new { c.Id }).ToString();
			Assert.That(sql, Is.Not.Contains("PersonID"));
			sql.Should().Contain("UNKNOWN", Exactly.Twice());
		}

		[Test]
		public void Issue4031_Case07([IncludeDataSources(ProviderName.SQLiteClassic)] string context)
		{
			Issue4031_Execute<Issue4031Case07>(context);
		}

		[Test]
		public void Issue4031_Case08([IncludeDataSources(ProviderName.SQLiteClassic)] string context)
		{
			Issue4031_Execute<Issue4031Case08>(context);
		}

		[Test]
		public void Issue4031_Case09([IncludeDataSources(ProviderName.SQLiteClassic)] string context)
		{
			Issue4031_Execute<Issue4031Case09>(context);
		}

		[Test]
		public void Issue4031_Case10([IncludeDataSources(ProviderName.SQLiteClassic)] string context)
		{
			Issue4031_Execute<Issue4031Case10>(context);
		}

		[Test]
		public void Issue4031_Case11([IncludeDataSources(ProviderName.SQLiteClassic)] string context)
		{
			Issue4031_Execute<Issue4031Case11>(context);
		}

		[Test]
		public void Issue4031_Case12([IncludeDataSources(ProviderName.SQLiteClassic)] string context)
		{
			Issue4031_Execute<Issue4031Case12>(context);
		}

		[Test]
		public void Issue4031_Case13([IncludeDataSources(ProviderName.SQLiteClassic)] string context)
		{
			Issue4031_Execute<Issue4031Case13>(context);
		}

		[Test]
		public void Issue4031_Case14([IncludeDataSources(ProviderName.SQLiteClassic)] string context)
		{
			Issue4031_Execute<Issue4031Case14>(context);
		}

		[Test]
		public void Issue4031_Case15([IncludeDataSources(ProviderName.SQLiteClassic)] string context)
		{
			Issue4031_Execute_TwoFields<Issue4031Case15>(context);

			using var db = GetDataContext(context);
			var sql = db.GetTable<Issue4031Case15>().Where(c => c.Id == -1).Select(c => new { c.Id }).ToString();
			Assert.That(sql, Is.Not.Contains("PersonID"));
			sql.Should().Contain("UNKNOWN", Exactly.Twice());
		}

		[Test]
		public void Issue4031_Case16([IncludeDataSources(ProviderName.SQLiteClassic)] string context)
		{
			Issue4031_ExecuteT<Issue4031Case16>(context);
		}

		void Issue4031_ExecuteT<T>(string context) where T : class, IIssue4031<int>
		{
			using var db = GetDataContext(context);
			db.GetTable<T>().Where(c => c.Id == -1).ToList();

			var ed = db.MappingSchema.GetEntityDescriptor(typeof(T));
			Assert.That(ed.Columns, Has.Count.EqualTo(1));
			Assert.That(ed.Columns[0].ColumnName, Is.EqualTo("PersonID"));
		}

		void Issue4031_Execute<T>(string context) where T : class, IIssue4031
		{
			using var db = GetDataContext(context);
			db.GetTable<T>().Where(c => c.Id == -1).ToList();

			var ed = db.MappingSchema.GetEntityDescriptor(typeof(T));
			Assert.That(ed.Columns, Has.Count.EqualTo(1));
			Assert.That(ed.Columns[0].ColumnName, Is.EqualTo("PersonID"));
		}

		void Issue4031_Execute_TwoFields<T>(string context) where T : class, IIssue4031
		{
			using var db = GetDataContext(context);
			db.GetTable<T>().Where(c => c.Id == -1).Select(c => new { c.Id }).ToList();

			var ed = db.MappingSchema.GetEntityDescriptor(typeof(T));
			Assert.That(ed.Columns, Has.Count.EqualTo(2));
			var columnNames = ed.Columns.Select(c => c.ColumnName).ToArray();
			Assert.That(columnNames, Does.Contain("PersonID"));
			Assert.That(columnNames, Does.Contain("UNKNOWN"));
		}
		#endregion

		#region Issue 3034
		interface IA
		{
			int Id { get; set; }
		}

		interface IB : IA
		{
			string Name { get; set; }
		}

		sealed class MyTable
		{
			public int     Id   { get; set; }
			public string? Name { get; set; }
		}

		[Test]
		public void Issue3034([IncludeDataSources(ProviderName.SQLiteClassic)] string context)
		{
			using var db = GetDataContext(context);
			using var t = db.CreateLocalTable<MyTable>(new[]{ new MyTable() { Id = 1, Name = "old_name" }, new MyTable() { Id = 2, Name = "old_name" } });

			db.GetTable<IB>().TableName("MyTable")
				.Where(x => x.Id == 1)
				.Set(x => x.Name, x => "new_name")
				.Update();

			var results = t.OrderBy(r => r.Id).ToArray();

			Assert.That(results, Has.Length.EqualTo(2));
			Assert.Multiple(() =>
			{
				Assert.That(results[0].Id, Is.EqualTo(1));
				Assert.That(results[0].Name, Is.EqualTo("new_name"));
				Assert.That(results[1].Id, Is.EqualTo(2));
				Assert.That(results[1].Name, Is.EqualTo("old_name"));
			});
		}
		#endregion

		#region Issue 4082
		public interface IIdentifiable
		{
			int Id { get; }
		}

		[Table]
		public class UserAccount : IIdentifiable
		{
			[PrimaryKey] public int     Id   { get; set; }
			[Column    ] public string? Name { get; set; }
		}

		[Test]
		public void Issue4082([IncludeDataSources(ProviderName.SQLiteClassic)] string context)
		{
			using var db = GetDataContext(context);
			using var t = db.CreateLocalTable<UserAccount>(new[]{ new UserAccount() { Id = 1, Name = "old_name" }, new UserAccount() { Id = 2, Name = "old_name" } });

			var results = ((IQueryable<IIdentifiable>)db.GetTable<UserAccount>())
				.Where(x => x.Id == 1)
				.ToArray();

			Assert.That(results, Has.Length.EqualTo(1));
			Assert.That(results[0].Id, Is.EqualTo(1));
		}
		#endregion
	}
}

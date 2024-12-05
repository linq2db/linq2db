using System;
using System.Linq;

using LinqToDB;
using LinqToDB.Data;
using LinqToDB.Mapping;

using NUnit.Framework;

namespace Tests.Linq
{
	using FluentAssertions;

	using Model;

	using xUpdate;

	using static Tests.xUpdate.MergeTests;
	using static xUpdate.MultiInsertTests;

	[TestFixture]
	public class QueryGenerationTests : TestBase
	{
		[Table("TableWithIdentitySrc")]
		sealed class TableWithIdentitySource
		{
			[PrimaryKey, Identity] public int Id { get; set; }
			[Column] public int Value { get; set; }
		}

		[Table("TableWithIdentity")]
		sealed class TableWithIdentity
		{
			[PrimaryKey, Identity] public int Id { get; set; }
			[Column] public int Value { get; set; }
		}

		[Test]
		public void ToSqlQuery_NotLinqQuery()
		{
			Assert.That(() => Array.Empty<string>().AsQueryable().ToSqlQuery(), Throws.InstanceOf<LinqToDBException>().With.Message.EqualTo("LinqToDB method 'ToSqlQuery' called on non-LinqToDB IQueryable."));
		}

		[Test]
		public void ToString_Default([IncludeDataSources(ProviderName.SQLiteClassic)] string context)
		{
			using var db = GetDataContext(context);

			var query = db.Person;

			var toString = query.Where(r => r.ID == 1).ToString();

			Assert.That(toString, Is.EqualTo("LinqToDB.Linq.ExpressionQueryImpl`1[Tests.Model.Person]"));
		}

		[Test]
		public void ToSqlQuery_Table([DataSources] string context)
		{
			using var db = GetDataContext(context);

			var query = db.Person;

			var command = query.ToSqlQuery();

			using var dc = GetDataConnection(context.StripRemote());
			var bySql = dc.Query<Person>(command.Sql).ToArray();
			var expected = query.ToArray();

			Assert.Multiple(() =>
			{
				Assert.That(command.Sql, Does.Contain("SELECT"));
				Assert.That(command.Parameters, Has.Count.EqualTo(0));
				Assert.That(bySql, Has.Length.EqualTo(expected.Length));
			});
		}

		[ActiveIssue("Final aliases break by-name mapping for raw SQL (not an issue?)", Configuration = ProviderName.SqlCe)]
		[Test]
		public void ToSqlQuery_SimpleQuery([DataSources] string context)
		{
			using var db = GetDataContext(context);

			var query = db.Person.Where(p => p.ID == 1);

			var command = query.ToSqlQuery();

			using var dc = GetDataConnection(context.StripRemote());
			var person = dc.Query<Person>(command.Sql).ToArray();
			var expected = query.Single();

			Assert.That(person, Has.Length.EqualTo(1));
			Assert.Multiple(() =>
			{
				Assert.That(command.Sql, Does.Contain("SELECT"));
				Assert.That(command.Parameters, Has.Count.EqualTo(0));
				Assert.That(person[0].ID, Is.EqualTo(expected.ID));
				Assert.That(person[0].FirstName, Is.EqualTo(expected.FirstName));
				Assert.That(person[0].MiddleName, Is.EqualTo(expected.MiddleName));
				Assert.That(person[0].LastName, Is.EqualTo(expected.LastName));
				Assert.That(person[0].Gender, Is.EqualTo(expected.Gender));
			});
		}

		[ActiveIssue("Final aliases break by-name mapping for raw SQL (not an issue?)", Configuration = ProviderName.SqlCe)]
		[Test]
		public void ToSqlQuery_WithParameters([DataSources] string context)
		{
			using var db = GetDataContext(context);

			var id = 1;

			var query = db.Person.Where(p => p.ID == id);

			var command = query.ToSqlQuery();

			using var dc = GetDataConnection(context.StripRemote());

			var person = dc.Query<Person>(command.Sql, command.Parameters).ToArray();
			var expected = query.Single();

			var expectedParams = context.IsUseParameters()
					? 1
					: 0;

			Assert.That(person, Has.Length.EqualTo(1));
			Assert.Multiple(() =>
			{
				Assert.That(command.Sql, Does.Contain("SELECT"));
				Assert.That(command.Parameters, Has.Count.EqualTo(expectedParams));
				Assert.That(person[0].ID, Is.EqualTo(expected.ID));
				Assert.That(person[0].FirstName, Is.EqualTo(expected.FirstName));
				Assert.That(person[0].MiddleName, Is.EqualTo(expected.MiddleName));
				Assert.That(person[0].LastName, Is.EqualTo(expected.LastName));
				Assert.That(person[0].Gender, Is.EqualTo(expected.Gender));
			});
		}

		[ActiveIssue("Final aliases break by-name mapping for raw SQL (not an issue?)", Configuration = ProviderName.SqlCe)]
		[Test]
		public void ToSqlQuery_WithParametersDeduplication([DataSources] string context)
		{
			using var db = GetDataContext(context);

			var firstName = "John";

			var query = db.Person.Where(p => p.FirstName == firstName || p.LastName == firstName);

			var command = query.ToSqlQuery();

			using var dc = GetDataConnection(context.StripRemote());

			var person = dc.Query<Person>(command.Sql, command.Parameters).ToArray();
			var expected = query.Single();

			var expectedParams = context.IsUseParameters()
					? context.IsUsePositionalParameters()
						? 2 : 1
					: 0;

			Assert.That(person, Has.Length.EqualTo(1));
			Assert.Multiple(() =>
			{
				Assert.That(command.Sql, Does.Contain("SELECT"));
				Assert.That(command.Parameters, Has.Count.EqualTo(expectedParams));
				Assert.That(person[0].ID, Is.EqualTo(expected.ID));
				Assert.That(person[0].FirstName, Is.EqualTo(expected.FirstName));
				Assert.That(person[0].MiddleName, Is.EqualTo(expected.MiddleName));
				Assert.That(person[0].LastName, Is.EqualTo(expected.LastName));
				Assert.That(person[0].Gender, Is.EqualTo(expected.Gender));
			});
		}

		[ActiveIssue("Final aliases break by-name mapping for raw SQL (not an issue?)", Configuration = ProviderName.SqlCe)]
		[Test]
		public void ToSqlQuery_WithNullableParameters([DataSources] string context)
		{
			using var db = GetDataContext(context);

			string? middleName = null;

			var query = db.Person.Where(p => p.MiddleName != middleName);

			var command = query.ToSqlQuery();

			using var dc = GetDataConnection(context.StripRemote());

			var person = dc.Query<Person>(command.Sql).ToArray();
			var expected = query.Single();

			Assert.That(person, Has.Length.EqualTo(1));
			Assert.Multiple(() =>
			{
				Assert.That(command.Sql, Does.Contain("SELECT"));
				Assert.That(command.Parameters, Has.Count.EqualTo(0));
				Assert.That(person[0].ID, Is.EqualTo(expected.ID));
				Assert.That(person[0].FirstName, Is.EqualTo(expected.FirstName));
				Assert.That(person[0].MiddleName, Is.EqualTo(expected.MiddleName));
				Assert.That(person[0].LastName, Is.EqualTo(expected.LastName));
				Assert.That(person[0].Gender, Is.EqualTo(expected.Gender));
			});
		}

		[Test]
		public void ToSqlQuery_EagerLoad([DataSources] string context)
		{
			// currently preambule queries not exposed by API
			using var db = GetDataContext(context);

			var id = 2;

			var query = db.Parent.Where(p => p.ParentID == id).Select(p => new { Parent = p, Children = p.Children.ToArray() });

			var command = query.ToSqlQuery();

			using var dc = GetDataConnection(context.StripRemote());

			var parent = dc.Query<Parent>(command.Sql, command.Parameters).ToArray();
			var expected = db.Person.Where(p => p.ID == id).Single();

			Assert.That(parent, Has.Length.EqualTo(1));
			Assert.Multiple(() =>
			{
				Assert.That(command.Sql, Does.Contain("SELECT"));
				Assert.That(command.Parameters, Has.Count.EqualTo(context.IsUseParameters() ? 1 : 0));
				Assert.That(parent[0].ParentID, Is.EqualTo(expected.ID));
			});
		}

		[Test]
		public void ToSqlQuery_IUpdatable([DataSources] string context)
		{
			using var db = GetDataContext(context);
			using var tb = db.CreateLocalTable<TableWithIdentity>();

			var newValue = 123;
			var query = tb.Set(r => r.Value, r => newValue);

			var command = query.ToSqlQuery();

			using var dc = GetDataConnection(context.StripRemote());
			dc.Insert(new TableWithIdentity() { Value = 1 });
			dc.Execute(command.Sql, command.Parameters);

			var record = dc.GetTable<TableWithIdentity>().Single();

			Assert.Multiple(() =>
			{
				Assert.That(command.Sql, Does.Contain("UPDATE"));
				Assert.That(command.Parameters, Has.Count.EqualTo(context.IsUseParameters() ? 1 : 0));
				Assert.That(record.Value, Is.EqualTo(newValue));
			});
		}

		[Test]
		public void ToSqlQuery_IUpdatable_ClientIdentiy([DataSources(ProviderName.SqlCe, TestProvName.AllAccess, TestProvName.AllInformix, TestProvName.AllClickHouse, TestProvName.AllSqlServer, TestProvName.AllDB2, TestProvName.AllSybase)] string context)
		{
			using var db = GetDataContext(context);
			using var tb = db.CreateLocalTable<TableWithIdentity>();

			var newValue = 123;
			var query = tb
				.Set(r => r.Id, r => 492)
				.Set(r => r.Value, r => newValue);

			var command = query.ToSqlQuery();

			using var dc = GetDataConnection(context.StripRemote());
			dc.Insert(new TableWithIdentity() { Value = 1 });
			dc.Execute(command.Sql, command.Parameters);

			var record = dc.GetTable<TableWithIdentity>().Single();

			Assert.Multiple(() =>
			{
				Assert.That(command.Sql, Does.Contain("UPDATE"));
				Assert.That(command.Parameters, Has.Count.EqualTo(context.IsUseParameters() ? 1 : 0));
				Assert.That(record.Value, Is.EqualTo(newValue));
			});
		}

		[Test]
		public void ToSqlQuery_IValueInsertable([DataSources] string context)
		{
			using var db = GetDataContext(context);
			using var tb = db.CreateLocalTable<TableWithIdentity>();

			var value = 123;
			var query = tb.Value(r => r.Value, () => value);

			var command = query.ToSqlQuery();

			using var dc = GetDataConnection(context.StripRemote());

			dc.Execute(command.Sql, command.Parameters);

			var record = dc.GetTable<TableWithIdentity>().Single();

			Assert.Multiple(() =>
			{
				Assert.That(command.Sql, Does.Contain("INSERT"));
				Assert.That(command.Parameters, Has.Count.EqualTo(context.IsUseParameters() ? 1 : 0));
				Assert.That(record.Value, Is.EqualTo(value));
			});
		}

		[Test]
		public void ToSqlQuery_IValueInsertable_ClientIdentity([DataSources(ProviderName.SqlCe, TestProvName.AllSqlServer, TestProvName.AllDB2, TestProvName.AllSybase)] string context)
		{
			using var db = GetDataContext(context);
			using var tb = db.CreateLocalTable<TableWithIdentity>();

			var value = 123;
			var query = tb
				.Value(r => r.Id, () => 543)
				.Value(r => r.Value, () => value);

			var command = query.ToSqlQuery();

			using var dc = GetDataConnection(context.StripRemote());

			dc.Execute(command.Sql, command.Parameters);

			var record = dc.GetTable<TableWithIdentity>().Single();

			Assert.Multiple(() =>
			{
				Assert.That(command.Sql, Does.Contain("INSERT"));
				Assert.That(command.Parameters, Has.Count.EqualTo(context.IsUseParameters() ? 1 : 0));
				Assert.That(record.Value, Is.EqualTo(value));
			});
		}

		[Test]
		public void ToSqlQuery_ISelectInsertable([DataSources] string context)
		{
			using var db = GetDataContext(context);
			using var ts = db.CreateLocalTable<TableWithIdentitySource>();
			using var td = db.CreateLocalTable<TableWithIdentity>();

			var addition = 123;
			var query = ts.Into(td).Value(r => r.Value, r => r.Value + addition);

			var command = query.ToSqlQuery();

			using var dc = GetDataConnection(context.StripRemote());
			dc.Insert(new TableWithIdentitySource() { Value = 1 });

			dc.Execute(command.Sql, command.Parameters);

			var records = dc.GetTable<TableWithIdentity>().ToArray();

			Assert.That(records, Has.Length.EqualTo(1));
			Assert.Multiple(() =>
			{
				Assert.That(command.Sql, Does.Contain("INSERT"));
				Assert.That(command.Sql, Does.Contain("SELECT"));
				Assert.That(command.Parameters, Has.Count.EqualTo(context.IsUseParameters() ? 1 : 0));
				Assert.That(records.Count(r => r.Value == 1 + addition), Is.EqualTo(1));
			});
		}

		[Test]
		public void ToSqlQuery_ISelectInsertable_ClientIdentity([DataSources(ProviderName.SqlCe, TestProvName.AllDB2, TestProvName.AllSqlServer, TestProvName.AllSybase)] string context)
		{
			using var db = GetDataContext(context);
			using var ts = db.CreateLocalTable<TableWithIdentitySource>();
			using var td = db.CreateLocalTable<TableWithIdentity>();

			var addition = 123;
			var query = ts.Into(td)
				.Value(r => r.Id, r => 345)
				.Value(r => r.Value, r => r.Value + addition);

			var command = query.ToSqlQuery();

			using var dc = GetDataConnection(context.StripRemote());
			dc.Insert(new TableWithIdentitySource() { Value = 1 });

			dc.Execute(command.Sql, command.Parameters);

			var records = dc.GetTable<TableWithIdentity>().ToArray();

			Assert.That(records, Has.Length.EqualTo(1));
			Assert.Multiple(() =>
			{
				Assert.That(command.Sql, Does.Contain("INSERT"));
				Assert.That(command.Sql, Does.Contain("SELECT"));
				Assert.That(command.Parameters, Has.Count.EqualTo(context.IsUseParameters() ? 1 : 0));
				Assert.That(records.Count(r => r.Value == 1 + addition), Is.EqualTo(1));
			});
		}

		[Test]
		public void ToSqlQuery_ILoadWithQueryable([DataSources] string context)
		{
			using var db = GetDataContext(context);

			var query = db.Parent.Where(p => p.ParentID == 1).LoadWith(p => p.Children);

			var command = query.ToSqlQuery();

			using var dc = GetDataConnection(context.StripRemote());
			var parent = dc.Query<Parent>(command.Sql).ToArray();
			var expected = query.Single();

			Assert.That(parent, Has.Length.EqualTo(1));
			Assert.Multiple(() =>
			{
				Assert.That(command.Sql, Does.Contain("SELECT"));
				Assert.That(command.Parameters, Has.Count.EqualTo(0));
				Assert.That(parent[0].ParentID, Is.EqualTo(expected.ParentID));
			});
		}

		[Test]
		public void ToSqlQuery_IMultiInsertInto([IncludeDataSources(true, TestProvName.AllOracle)] string context)
		{
			using var db = GetDataContext(context);
			using var dest1 = db.CreateLocalTable<MultiInsertTests.Dest1>();
			using var dest2 = db.CreateLocalTable<MultiInsertTests.Dest2>();

			var query = db
				.SelectQuery(() => new { ID = 1000, N = (short)42 })
				.MultiInsert()
				.Into(
					dest1,
					x => new MultiInsertTests.Dest1 { ID = x.ID + 1, Value = x.N }
				)
				.Into(
					dest1,
					x => new MultiInsertTests.Dest1 { ID = x.ID + 2, Value = x.N }
				)
				.Into(
					dest2,
					x => new MultiInsertTests.Dest2 { ID = x.ID + 3, Int = x.ID + 1 }
				);

			var command = query.ToSqlQuery();

			using var dc = GetDataConnection(context.StripRemote());
			var count = dc.Execute(command.Sql);

			Assert.Multiple(() =>
			{
				Assert.That(command.Sql, Does.Contain("INSERT ALL"));
				Assert.That(command.Parameters, Has.Count.EqualTo(0));
				Assert.That(count, Is.EqualTo(3));
				Assert.That(dest1.Count(), Is.EqualTo(2));
				Assert.That(dest2.Count(x => x.ID == 1003), Is.EqualTo(1));
			});
		}

		[Test]
		public void ToSqlQuery_IMultiInsertElse([IncludeDataSources(true, TestProvName.AllOracle)] string context)
		{
			using var db = GetDataContext(context);
			using var dest1 = db.CreateLocalTable<Dest1>();
			using var dest2 = db.CreateLocalTable<Dest2>();

			var query = db
				.SelectQuery(() => new { ID = 1000, N = (short)42 })
				.MultiInsert()
				.When(
					x => x.N > 40,
					dest1,
					x => new Dest1 { ID = x.ID + 1, Value = x.N }
				)
				.When(
					x => x.N < 40,
					dest1,
					x => new Dest1 { ID = x.ID + 2, Value = x.N }
				)
				.When(
					x => true,
					dest2,
					x => new Dest2 { ID = x.ID + 3, Int = x.ID + 1 }
				);

			var command = query.ToSqlQuery();

			using var dc = GetDataConnection(context.StripRemote());
			var count = dc.Execute(command.Sql);

			Assert.Multiple(() =>
			{
				Assert.That(command.Sql, Does.Contain("INSERT ALL"));
				Assert.That(command.Sql, Does.Contain("WHEN"));
				Assert.That(command.Parameters, Has.Count.EqualTo(0));

				Assert.That(count, Is.EqualTo(2));
				Assert.That(dest1.Count(), Is.EqualTo(1));
				Assert.That(dest1.Count(x => x.ID == 1001), Is.EqualTo(1));
				Assert.That(dest2.Count(x => x.ID == 1003), Is.EqualTo(1));
			});
		}

		[Test]
		public void ToSqlQuery_IMergeable([MergeDataContextSource] string context)
		{
			using var db = GetDataContext(context);

			PrepareData(db);

			var table = GetTarget(db);

			var query = table
					.Merge()
					.Using(GetSource1(db))
					.OnTargetKey()
					.UpdateWhenMatched()
					.InsertWhenNotMatched();

			var command = query.ToSqlQuery();

			using var dc = GetDataConnection(context.StripRemote());
			var count = dc.Execute(command.Sql);

			Assert.Multiple(() =>
			{
				Assert.That(command.Sql, Does.Contain("MERGE"));
				Assert.That(command.Parameters, Has.Count.EqualTo(0));
			});
		}

		[Test]
		public void ToSqlQuery_IMergeable_WithIdentity([IdentityInsertMergeDataContextSource] string context)
		{
			using var db = GetDataContext(context);
			using var tb = db.CreateLocalTable<TableWithIdentity>();

			var query = tb
					.Merge()
					.Using(new [] { new TableWithIdentity() { Id = 1, Value = 2 } })
					.OnTargetKey()
					.UpdateWhenMatched()
					.InsertWhenNotMatched();

			var command = query.ToSqlQuery();

			using var dc = GetDataConnection(context.StripRemote());
			var count = dc.Execute(command.Sql);

			Assert.Multiple(() =>
			{
				Assert.That(command.Sql, Does.Contain("MERGE"));
				Assert.That(command.Parameters, Has.Count.EqualTo(0));
			});
		}

		[Test]
		public void ToSqlQuery_IMergeable_WithIdentity_ClientValue([IdentityInsertMergeDataContextSource] string context)
		{
			using var db = GetDataContext(context);
			using var tb = db.CreateLocalTable<TableWithIdentity>();

			var query = tb
					.Merge()
					.Using(new [] { new TableWithIdentity() { Id = 1, Value = 2 } })
					.OnTargetKey()
					.UpdateWhenMatched()
					.InsertWhenNotMatched(s => new TableWithIdentity() { Id = 123, Value = 321 });

			var command = query.ToSqlQuery();

			using var dc = GetDataConnection(context.StripRemote());
			var count = dc.Execute(command.Sql);

			Assert.Multiple(() =>
			{
				Assert.That(command.Sql, Does.Contain("MERGE"));
				Assert.That(command.Parameters, Has.Count.EqualTo(0));
			});
		}
	}
}

#if NET6_0_OR_GREATER

using System;
using System.Collections.Generic;
using System.Linq;

using LinqToDB;
using LinqToDB.Mapping;

using NUnit.Framework;

using Shouldly;

namespace Tests.Linq
{
	[TestFixture]
	public class MinByMaxByMethodTests : TestBase
	{
		[Table]
		public class TestTable
		{
			[Column] public int Id { get; set; }
			[Column] public int TestId { get; set; }
		}

		[Table]
		public class MainTable
		{
			[Column] public int Id { get; set; }
			[Column] public string? Name { get; set; }

			[Association(ThisKey = nameof(Id), OtherKey = nameof(ChildTable.ParentId))]
			public IEnumerable<ChildTable> Children { get; set; } = null!;
		}

		[Table]
		public class ChildTable
		{
			[Column] public int Id { get; set; }
			[Column] public int ParentId { get; set; }
			[Column] public int Value { get; set; }
			[Column] public string? Name { get; set; }
		}

		private TestTable[] CreateTestTableData()
		{
			return [
				new TestTable() { Id = 1, TestId = 20},
				new TestTable() { Id = 2, TestId = 20 },
				new TestTable() { Id = 3, TestId = 30 },
				new TestTable() { Id = 4, TestId = 30 },
				new TestTable() { Id = 5, TestId = 40 }
				];
		}

		private MainTable[] CreateParentData()
		{
			return [
				new MainTable { Id = 1, Name = "Parent1" },
				new MainTable { Id = 2, Name = "Parent2" },
				new MainTable { Id = 3, Name = "Parent3" }
			];
		}

		private ChildTable[] CreateChildData()
		{
			return [
				new ChildTable { Id = 1, ParentId = 1, Value = 10, Name = "Child1" },
				new ChildTable { Id = 2, ParentId = 1, Value = 20, Name = "Child2" },
				new ChildTable { Id = 3, ParentId = 1, Value = 15, Name = "Child3" },
				new ChildTable { Id = 4, ParentId = 2, Value = 30, Name = "Child4" },
				new ChildTable { Id = 5, ParentId = 2, Value = 25, Name = "Child5" },
				new ChildTable { Id = 6, ParentId = 3, Value = 40, Name = "Child6" }
			];
		}

		[Test]
		public void MinBy([DataSources] string context)
		{
			var testData = CreateTestTableData();

			using var db    = GetDataContext(context);
			using var table = db.CreateLocalTable(testData);

			var result      = table.MinBy(x => x.Id);
			var compareData = testData.MinBy(x => x.Id);

			result!.Id.ShouldBe(compareData!.Id);
		}

		[Test]
		public void MaxBy([DataSources] string context)
		{
			var testData = CreateTestTableData();

			using var db    = GetDataContext(context);
			using var table = db.CreateLocalTable(testData);

			var result = table.OrderBy(x => x.TestId).MaxBy(x => x.Id);
			var compareData = testData.OrderBy(x => x.TestId).MaxBy(x => x.Id);

			result!.Id.ShouldBe(compareData!.Id);
		}

		[Test]
		public void MinByAssociation([IncludeDataSources(TestProvName.AllSQLite, TestProvName.AllPostgreSQL)] string context)
		{
			var parents = CreateParentData();
			var children = CreateChildData();

			using var db = GetDataContext(context);
			using var parentTable = db.CreateLocalTable(parents);
			using var childTable = db.CreateLocalTable(children);

			var result = parentTable
				.Where(p => p.Id == 1)
				.Select(p => p.Children.MinBy(c => c.Value))
				.FirstOrDefault();

			var compareData = parents
				.Where(p => p.Id == 1)
				.Select(p => children.Where(c => c.ParentId == p.Id).MinBy(c => c.Value))
				.FirstOrDefault();

			result.ShouldNotBeNull();
			compareData.ShouldNotBeNull();
			result.Id.ShouldBe(compareData.Id);
			result.Value.ShouldBe(compareData.Value);
		}

		[Test]
		public void MaxByAssociation([IncludeDataSources(TestProvName.AllSQLite, TestProvName.AllPostgreSQL)] string context)
		{
			var parents = CreateParentData();
			var children = CreateChildData();

			using var db = GetDataContext(context);
			using var parentTable = db.CreateLocalTable(parents);
			using var childTable = db.CreateLocalTable(children);

			var result = parentTable
				.Where(p => p.Id == 1)
				.Select(p => p.Children.MaxBy(c => c.Value))
				.FirstOrDefault();

			var compareData = parents
				.Where(p => p.Id == 1)
				.Select(p => children.Where(c => c.ParentId == p.Id).MaxBy(c => c.Value))
				.FirstOrDefault();

			result.ShouldNotBeNull();
			compareData.ShouldNotBeNull();
			result.Id.ShouldBe(compareData.Id);
			result.Value.ShouldBe(compareData.Value);
		}

		[Test]
		public void MinByAssociationWithMultipleParents([IncludeDataSources(TestProvName.AllSQLite, TestProvName.AllPostgreSQL)] string context)
		{
			var parents = CreateParentData();
			var children = CreateChildData();

			using var db = GetDataContext(context);
			using var parentTable = db.CreateLocalTable(parents);
			using var childTable = db.CreateLocalTable(children);

			var results = parentTable
				.Select(p => new { Parent = p, MinChild = p.Children.MinBy(c => c.Value) })
				.ToArray();

			var compareData = parents
				.Select(p => new { Parent = p, MinChild = children.Where(c => c.ParentId == p.Id).MinBy(c => c.Value) })
				.ToArray();

			results.Length.ShouldBe(compareData.Length);
			
			for (int i = 0; i < results.Length; i++)
			{
				results[i].Parent.Id.ShouldBe(compareData[i].Parent.Id);
				if (compareData[i].MinChild != null)
				{
					results[i].MinChild.ShouldNotBeNull();
					results[i].MinChild!.Id.ShouldBe(compareData[i].MinChild!.Id);
					results[i].MinChild!.Value.ShouldBe(compareData[i].MinChild!.Value);
				}
			}
		}

		[Test]
		public void MaxByAssociationWithMultipleParents([IncludeDataSources(TestProvName.AllSQLite, TestProvName.AllPostgreSQL)] string context)
		{
			var parents = CreateParentData();
			var children = CreateChildData();

			using var db = GetDataContext(context);
			using var parentTable = db.CreateLocalTable(parents);
			using var childTable = db.CreateLocalTable(children);

			var results = parentTable
				.Select(p => new { Parent = p, MaxChild = p.Children.MaxBy(c => c.Value) })
				.ToArray();

			var compareData = parents
				.Select(p => new { Parent = p, MaxChild = children.Where(c => c.ParentId == p.Id).MaxBy(c => c.Value) })
				.ToArray();

			results.Length.ShouldBe(compareData.Length);
			
			for (int i = 0; i < results.Length; i++)
			{
				results[i].Parent.Id.ShouldBe(compareData[i].Parent.Id);
				if (compareData[i].MaxChild != null)
				{
					results[i].MaxChild.ShouldNotBeNull();
					results[i].MaxChild!.Id.ShouldBe(compareData[i].MaxChild!.Id);
					results[i].MaxChild!.Value.ShouldBe(compareData[i].MaxChild!.Value);
				}
			}
		}

		[Test]
		public void MinByAssociationByName([IncludeDataSources(TestProvName.AllSQLite, TestProvName.AllPostgreSQL)] string context)
		{
			var parents = CreateParentData();
			var children = CreateChildData();

			using var db = GetDataContext(context);
			using var parentTable = db.CreateLocalTable(parents);
			using var childTable = db.CreateLocalTable(children);

			var result = parentTable
				.Where(p => p.Id == 2)
				.Select(p => p.Children.MinBy(c => c.Name))
				.FirstOrDefault();

			var compareData = parents
				.Where(p => p.Id == 2)
				.Select(p => children.Where(c => c.ParentId == p.Id).MinBy(c => c.Name))
				.FirstOrDefault();

			result.ShouldNotBeNull();
			compareData.ShouldNotBeNull();
			result.Id.ShouldBe(compareData.Id);
			result.Name.ShouldBe(compareData.Name);
		}

		[Test]
		public void MaxByAssociationById([IncludeDataSources(TestProvName.AllSQLite, TestProvName.AllPostgreSQL)] string context)
		{
			var parents = CreateParentData();
			var children = CreateChildData();

			using var db = GetDataContext(context);
			using var parentTable = db.CreateLocalTable(parents);
			using var childTable = db.CreateLocalTable(children);

			var result = parentTable
				.Where(p => p.Id == 2)
				.Select(p => p.Children.MaxBy(c => c.Id))
				.FirstOrDefault();

			var compareData = parents
				.Where(p => p.Id == 2)
				.Select(p => children.Where(c => c.ParentId == p.Id).MaxBy(c => c.Id))
				.FirstOrDefault();

			result.ShouldNotBeNull();
			compareData.ShouldNotBeNull();
			result.Id.ShouldBe(compareData.Id);
		}

		[ThrowsCannotBeConverted]
		[Test]
		public void MinByAssociationWithComparerShouldFail([IncludeDataSources(TestProvName.AllSQLite)] string context)
		{
			var parents = CreateParentData();
			var children = CreateChildData();

			using var db = GetDataContext(context);
			using var parentTable = db.CreateLocalTable(parents);
			using var childTable = db.CreateLocalTable(children);

			var comparer = Comparer<int>.Default;

			_ = parentTable
				.Where(p => p.Id == 1)
				.Select(p => Sql.AsSql(p.Children.MinBy(c => c.Value, comparer)))
				.FirstOrDefault();
		}

		[ThrowsCannotBeConverted]
		[Test]
		public void MaxByAssociationWithComparerShouldFail([IncludeDataSources(TestProvName.AllSQLite)] string context)
		{
			var parents = CreateParentData();
			var children = CreateChildData();

			using var db = GetDataContext(context);
			using var parentTable = db.CreateLocalTable(parents);
			using var childTable = db.CreateLocalTable(children);

			var comparer = Comparer<int>.Default;

			_ = parentTable
				.Where(p => p.Id == 1)
				.Select(p => Sql.AsSql(p.Children.MaxBy(c => c.Value, comparer)))
				.FirstOrDefault();
		}

		[Test]
		public void MaxByEmptySequenceShouldThrow([IncludeDataSources(TestProvName.AllSQLite)] string context)
		{
			var testData = CreateTestTableData();

			using var db    = GetDataContext(context);
			using var table = db.CreateLocalTable(testData);

			// Query returns empty sequence, MaxBy on value type should throw
			Assert.Throws<InvalidOperationException>(() =>
			{
				_ = table.Where(x => x.Id < 0).Select(x => x.Id).MaxBy(x => x);
			});
		}

		[Test]
		public void MinByEmptySequenceShouldThrow([IncludeDataSources(TestProvName.AllSQLite)] string context)
		{
			var testData = CreateTestTableData();

			using var db    = GetDataContext(context);
			using var table = db.CreateLocalTable(testData);

			// Query returns empty sequence, MinBy on value type should throw
			Assert.Throws<InvalidOperationException>(() =>
			{
				_ = table.Where(x => x.Id < 0).Select(x => x.Id).MinBy(x => x);
			});
		}
	}
}

#endif
